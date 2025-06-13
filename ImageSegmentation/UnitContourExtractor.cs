using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenCvSharp;
using static ImageSegmentation.LabeledContourGenerator;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    public class LabeledUnitContourExtractor : BaseLabelProcessor
    {
        private readonly SameLabelsProcessor _sameLabelsProcessor;
        public LabeledUnitContourExtractor(IShapeDrawerFactory shapeDrawerFactory)
        {
            _sameLabelsProcessor = new SameLabelsProcessor(shapeDrawerFactory);
        }


        // 解析图片
        protected override Mat DecodeImage(LabelAnnotation annotation)
        {
            return LabeledContourGenerator.DecodeImageFromJson(annotation.ImageData);
        }

        // 处理Unit ROI
        protected override void ProcessingNetingRegion(LabelAnnotation annotation, Mat image, SingleJsonFileResult singleJsonResult, Point point, int threshold)
        {
            var labelShapes = annotation.Shapes;
            if (labelShapes == null)
            {
                return;
            }
            GetAllMasksOfPriorInfo(labelShapes, image, singleJsonResult);

            var groupedList = singleJsonResult.SingleJsonInfo
                            .GroupBy(item => item.LabelName)
                            .Select(group => new LabelListImages
                            {
                                LabelName = group.Key,
                                Images = group.Select(item => item.Images).ToList()
                            })
                            .ToList();

            Mat noInspImg = CreateNoInspRegion(image, singleJsonResult, groupedList);

            var results = MergeAndThresholdMasksByLabel(noInspImg, image, singleJsonResult, groupedList, point, threshold);

            CreateSolderRegion(noInspImg, results); // 防焊区域
        }

        private void GetAllMasksOfPriorInfo(
            List<AnnotationShape> labelShapes, Mat image, SingleJsonFileResult singleJson)
        {
            var groupedShapes = new Dictionary<string, List<(AnnotationShape shape, Mat mask)>>();

            foreach (var shape in labelShapes)
            {
                Mat shapeMask = new Mat(image.Size(), MatType.CV_8UC1, Scalar.Black);

                _sameLabelsProcessor.DrawShapeMask(shape, shapeMask, singleJson);

                // 保存绘制的每个label对应的形状mask
                singleJson.SingleJsonInfo.Add(new LabelImages
                {
                    LabelName = shape.Label,
                    Images = shapeMask
                });
                SplitNestedMaskBasedGroupID(groupedShapes, shape, shapeMask);
            }
            SubtractNestedMasksByArea(groupedShapes);
        }

        private void SplitNestedMaskBasedGroupID(
                Dictionary<string, List<(AnnotationShape shape, Mat mask)>> groupedShapes,
                AnnotationShape label,
                Mat shapeMask)
        {
            if (label.GroupId != null)
            {
                string groupKey = label.GroupId.ToString();
                if (!groupedShapes.ContainsKey(groupKey))
                    groupedShapes[groupKey] = new List<(AnnotationShape, Mat)>();
                groupedShapes[groupKey].Add((label, shapeMask));
            }
        }
        private void SubtractNestedMasksByArea(
            Dictionary<string, List<(AnnotationShape shape, Mat mask)>> groupedShapes)
        {
            foreach (var group in groupedShapes.Values)
            {
                if (group.Count == 1) continue;
                var sortedShapes = group.OrderByDescending(item =>
                {
                    var rect = Cv2.BoundingRect(item.shape.Points.Select(p => new Point((int)p[0], (int)p[1])).ToList());
                    return rect.Width * rect.Height;
                }).ToList();

                var minShape = sortedShapes.Last();

                for (int i = 0; i < sortedShapes.Count - 1; i++)
                {
                    Mat currentMask = sortedShapes[i].mask.Clone();
                    for (int j = i + 1; j < sortedShapes.Count; j++)
                    {
                        Cv2.Subtract(currentMask, sortedShapes[j].mask, currentMask);// 最外层的mask依次减去内层的每一个mask
                    }
                }
            }
        }

        private SingleJsonFileResult MergeAndThresholdMasksByLabel(Mat noInspImg, Mat image, SingleJsonFileResult singleJson,
            List<LabelListImages> groupedList, Point point, int threshold)
        {
            Point[][] contours = null;
            var binaryMask = new Mat();
            var binaryImage = new Mat();

            //var threshold = ImgProcess.Threshold(image, binaryImage, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary); // 自动阈值
            //result.Threshold = threshold;
            //Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\binaryImage.bmp", binaryImage);

            //Mat mergedAllMasks = new Mat(noInspImg.Size(), MatType.CV_8UC1, Scalar.Black);
            var allMaskROI = new Mat();
            var singlelabelContours = new ContourLabel();
            var allLabelMeMask = new Mat(noInspImg.Size(), MatType.CV_8UC1, Scalar.Black);

            foreach (var group in groupedList)
            {
                string label = group.LabelName;
                Mat labelMeShapesMask = new Mat(noInspImg.Size(), MatType.CV_8UC1, Scalar.Black);
                foreach (var maskImage in group.Images)
                {
                    Cv2.BitwiseOr(labelMeShapesMask, maskImage, labelMeShapesMask);
                    Cv2.BitwiseOr(allLabelMeMask, maskImage, allLabelMeMask);

                }

                Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\{label}mergedsameLabelMasks.bmp", labelMeShapesMask);

                var grayROI = new Mat();

                // 如果是不检区 或 卡槽
                if (label.ToUpper() == LabelTypes.NOINSP.ToString() || label.ToUpper() == LabelTypes.SLOTCUTOUT.ToString())
                {
                    grayROI = labelMeShapesMask;                    
                }
                else
                {
                    noInspImg.CopyTo(grayROI, labelMeShapesMask);
                }
                Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\{label}_grayROI.bmp", grayROI);

                (contours, binaryMask) = ImgProcess.ThresholdMaskAndFindContours(grayROI, threshold, ThresholdTypes.Binary);
                Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\{label}_binaryMask.bmp", binaryMask);

                //AdjustContourPositions(contours, point); // 根据截图的位置，还原到整板对应的位置

                // 存二值图
                singleJson.LabelBinaryMasks.Add(new LabelImages
                {
                    LabelName = label,
                    Images = binaryMask
                });

                // 存彩图
                var colorROI = new Mat();
                noInspImg.CopyTo(colorROI, binaryMask);
                singleJson.LabelColorMasks.Add(new LabelImages
                {
                    LabelName = label,
                    Images = colorROI
                });
                Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\{label}_color.bmp", colorROI);

                // 存轮廓
                singleJson.SinglelabelContours.Add(new ContourLabel
                {
                    LabelName = label,
                    Contours = contours,
                });
            }
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\allLabelMeMask00000.bmp", allLabelMeMask);

            var img = new Mat(noInspImg.Size(), MatType.CV_8UC1, Scalar.Black);
            foreach (var cnt in singleJson.SinglelabelContours)
            {
                for (int j = 0; j < cnt.Contours.Length; j++)
                {
                    Cv2.DrawContours(img, cnt.Contours, j, Scalar.All(255), -1);
                }
            }
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\img00000.bmp", img);



            return singleJson;
        }

        private void AdjustContourPositions(Point[][] contours, Point point)
        {
            var xOffset = point.X;
            var yOffset = point.Y;
            foreach (var contour in contours)
            {
                for (int i = 0; i < contour.Length; i++)
                {
                    contour[i].X += xOffset;
                    contour[i].Y += yOffset;
                }
            }
        }

        private Mat CreateNoInspRegion(Mat image, SingleJsonFileResult singleJson, List<LabelListImages> groupedList)
        {
            var noInspImage = new Mat();
            if (image.Empty())
                throw new ArgumentException("Input image is empty.");

            var noInspGroup = groupedList.FirstOrDefault(g => g.LabelName.ToUpper() == LabelTypes.NOINSP.ToString());
            if (noInspGroup == null)
                return image;

            using (Mat mergedSameLabelMasks = new Mat(image.Size(), MatType.CV_8UC1, Scalar.Black))
            {
                foreach (var maskImage in noInspGroup.Images)
                {
                    if (!maskImage.Empty() && maskImage.Size() == image.Size())
                        Cv2.BitwiseOr(mergedSameLabelMasks, maskImage, mergedSameLabelMasks);
                }
                var noInspROI = new Mat();
                image.CopyTo(noInspROI, mergedSameLabelMasks);
                Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\{noInspGroup.LabelName}_noInspROI.bmp", noInspROI);

                singleJson.LabelNoInspMasks.Add(new LabelImages
                {
                    LabelName = noInspGroup.LabelName,
                    Images = noInspROI
                });

                Cv2.BitwiseNot(mergedSameLabelMasks, mergedSameLabelMasks);

                using (Mat image1chExpanded = new Mat())
                {
                    Cv2.CvtColor(mergedSameLabelMasks, image1chExpanded, ColorConversionCodes.GRAY2BGR);
                    Cv2.BitwiseAnd(image, image1chExpanded, noInspImage);
                }
            }
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\{noInspGroup.LabelName}_noInspImage.bmp", noInspImage);

            return noInspImage;
        }

        private void CreateSolderRegion(Mat image, SingleJsonFileResult result)
        {
            Mat mergedAllMasks = new Mat(image.Size(), MatType.CV_8UC1, Scalar.Black);
            foreach (var mask in result.LabelBinaryMasks)
            {
                Cv2.BitwiseOr(mergedAllMasks, mask.Images, mergedAllMasks);
            }
            Cv2.BitwiseNot(mergedAllMasks, mergedAllMasks);
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\mergedAllMasks.bmp", mergedAllMasks);

            var roi = new Mat();
            image.CopyTo(roi, mergedAllMasks);
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\image_Solder.bmp", roi);
            result.SolderImage = roi;
        }

    }


}
