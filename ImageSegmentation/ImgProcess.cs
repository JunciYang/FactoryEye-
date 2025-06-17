using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using static System.Net.Mime.MediaTypeNames;
using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    internal class ImgProcess
    {
        public static (OpenCvSharp.Point[][], Mat) ThresholdMaskAndFindContours(Mat image, int threshold, ThresholdTypes thresholdTypes)
        {
            var binaryImage = new Mat();
            var grayImage = new Mat();
            var gray = new Mat();
            if (image.Channels() == 3)
            {
                Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                grayImage = image;
            }
            Cv2.Threshold(grayImage, binaryImage, threshold, 255, thresholdTypes);
            Cv2.ImWrite(Path.Combine(@$"E:\\ImgSegment\\Test", "binaryImage.jpg"), binaryImage);

            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.MorphologyEx(binaryImage, binaryImage, MorphTypes.Dilate, kernel);

            OpenCvSharp.Point[][] contours = null;
            HierarchyIndex[] hierarchyIndices = null;
            Cv2.FindContours(binaryImage, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            Point[][] filteredContours = contours
                    .Where(contour => Cv2.ContourArea(contour) >= 20)
                    .ToArray();

            return (filteredContours, binaryImage);
        }

        public static int Threshold(Mat image, Mat binaryImage, int minValue, int maxValue, ThresholdTypes thresholdTypes)
        {
            var bilateral = EnhanceContrastAndEnhancement(image);
            var threshold = Cv2.Threshold(bilateral, binaryImage, minValue, maxValue, thresholdTypes);
            
            // 使用增强后的阈值对灰度图进行二值化
            //Cv2.Threshold(grayImage, binaryImage, threshold1, maxValue, ThresholdTypes.Binary);
            //Cv2.ImWrite(Path.Combine(@$"E:\\ImgSegment\\Test", "binaryImage.jpg"), binaryImage);

            return (int)threshold;
        }

        public static Mat EnhanceContrastAndEnhancement(Mat image)
        {
            var grayImage = new Mat();
            var gray = new Mat();
            if (image.Channels() >= 3)
            {
                Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                grayImage = image;
            }

            //1.应用CLAHE增强对比度
            var claheEnhanced = EnhanceContrast(grayImage);

            // 2. 局部对比度增强
            var localEnhanced = LocalContrastEnhancement(claheEnhanced); // 阈值太高，若遇到前景背景不好分离的时候可以考虑

            // 3. 使用Bilateral滤波保持边缘的同时减少噪声
            var bilateral = new Mat();
            Cv2.BilateralFilter(localEnhanced, bilateral, 15, 65, 65);
            return bilateral;
        }



        public static Mat ColorImage(Mat image)
        {

            // 彩图处理
            using var lab = new Mat();
            Cv2.CvtColor(image, lab, ColorConversionCodes.BGR2Lab);

            // 分离通道（注意不要释放通道！）
            Mat[] labChannels = Cv2.Split(lab);

            // 仅处理L通道（亮度）
            using (var clahe = Cv2.CreateCLAHE(2.0, new Size(8, 8)))
            {
                clahe.Apply(labChannels[0], labChannels[0]);
            }

            // 合并通道
            using var mergedLab = new Mat();
            Cv2.Merge(labChannels, mergedLab);  // 使用新矩阵存储合并结果

            var colorImg = new Mat();
            // 转回BGR
            Cv2.CvtColor(mergedLab, colorImg, ColorConversionCodes.Lab2BGR);

            // 手动释放通道（因为Split()创建的通道不会自动释放）
            foreach (var channel in labChannels)
            {
                channel.Dispose();
            }
            Cv2.ImWrite(Path.Combine(@$"E:\\ImgSegment\\Test", $"FCCSP_colorSrc.jpg"), colorImg);

            var grayImage = new Mat();
            Cv2.CvtColor(colorImg, grayImage, ColorConversionCodes.BGR2GRAY);
            Cv2.ImWrite(Path.Combine(@$"E:\\ImgSegment\\Test", $"FCCSP_colorSrc_grayImage.jpg"), grayImage);

            int minValue = 0, maxValue = 255;
            var binaryImage = new Mat();
            var threshold2 = Cv2.Threshold(grayImage, binaryImage, minValue, maxValue, ThresholdTypes.Otsu | ThresholdTypes.Binary);
            Cv2.ImWrite(Path.Combine(@$"E:\\ImgSegment\\Test", $"FCCSP_colorSrc_binaryImage.jpg"), binaryImage);

            Cv2.Threshold(grayImage, binaryImage, threshold2, maxValue, ThresholdTypes.Binary);
            Cv2.ImWrite(Path.Combine(@$"E:\\ImgSegment\\Test", $"FCCSP_colorSrc_binaryImage_Binary.jpg"), binaryImage);


            return colorImg;
        }


        /// <summary>
        /// 计算图像的局部统计特征
        /// </summary>
        private static (Mat mean, Mat stdDev) CalculateLocalStatistics(Mat input, Size windowSize)
        {
            var mean = new Mat();
            var stdDev = new Mat();

            // 直接使用OpenCV的内置函数计算局部均值和标准差
            using (var kernel = Mat.Ones(windowSize, MatType.CV_32F).ToMat())
            {
                // 归一化卷积核
                var normalizedKernel = kernel.Clone();

                // 归一化卷积核（现在可以安全修改）
                normalizedKernel /= (windowSize.Width * windowSize.Height);

                // 计算局部均值
                Cv2.Filter2D(input, mean, MatType.CV_32F, kernel);

                // 计算局部方差
                using (var squaredInput = new Mat())
                using (var localSquaredMean = new Mat())
                {
                    Cv2.Multiply(input, input, squaredInput);
                    Cv2.Filter2D(squaredInput, localSquaredMean, MatType.CV_32F, kernel);
                    Cv2.Multiply(mean, mean, stdDev);
                    Cv2.Subtract(localSquaredMean, stdDev, stdDev);

                    // 确保所有值都非负
                    Cv2.Max(stdDev, 0, stdDev);

                    // 计算标准差
                    Cv2.Sqrt(stdDev, stdDev);
                }

                // 转换回8位无符号整型
                mean.ConvertTo(mean, MatType.CV_8U);
                stdDev.ConvertTo(stdDev, MatType.CV_8U);
            }

            return (mean, stdDev);
        }

        public static Mat EnhanceContrast(Mat input)
        {
            // 1. CLAHE（限制对比度自适应直方图均衡）
            var clahe = Cv2.CreateCLAHE(clipLimit: 1.0, tileGridSize: new Size(8, 8));
            var enhanced = new Mat();
            clahe.Apply(input, enhanced);
            return enhanced;
        }

        /// <summary>
        /// 使用局部对比度增强
        /// </summary>
        public static Mat LocalContrastEnhancement(Mat input)
        {
            var result = new Mat();
            using (var mean = new Mat())
            {
                // 计算局部均值
                //Cv2.Blur(input, mean, new Size(20, 20));
                Cv2.GaussianBlur(input, mean, new Size(0, 0), sigmaX: 25);
                // 计算局部对比度
                //result = input - mean + new Scalar(127);
                result = 2.0 * (input - mean) + new Scalar(127); // 增强因子可调
            }
            return result;
        }

      
    
    }

   
}
