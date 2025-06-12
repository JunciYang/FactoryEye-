using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using static ImageSegmentation.LabeledContourGenerator;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    public class SameLabelsProcessor
    {
        private readonly IShapeDrawerFactory _shapeDrawerFactory;

        // 绘制形状
        public SameLabelsProcessor(IShapeDrawerFactory shapeDrawerFactory)
        {
            _shapeDrawerFactory = shapeDrawerFactory;
        }

        public void DrawShapeMask(AnnotationShape label, Mat shapeMask, SingleJsonFileResult singleJson)
        {
            var drawer = _shapeDrawerFactory.GetDrawer(label.ShapeType.ToUpper());
            var points = label.Points.Select(p => new Point((int)p[0], (int)p[1])).ToList();
            drawer.Draw(shapeMask, points);
        }


        public void CreateSolderRegion(Mat image, LabelProcessingResult result)
        {

            var groupedByLabels = result.UnitBinaryMasks
                   .Where(s => !string.IsNullOrEmpty(s.Key?.ToString()))
                   .GroupBy(s => s.Key);

            Mat mergedAllMasks = new Mat(image.Size(), MatType.CV_8UC1, Scalar.Black);
            foreach (var group in groupedByLabels)
            {
                foreach (var labelMask in group)
                {
                    Cv2.BitwiseOr(mergedAllMasks, labelMask.Value, mergedAllMasks);
                }
            }
            Cv2.BitwiseNot(mergedAllMasks, mergedAllMasks);

            var roi = new Mat();
            image.CopyTo(roi, mergedAllMasks);
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\image_Solder.bmp", roi);

        }


    }


}
