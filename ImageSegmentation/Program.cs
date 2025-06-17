using System.Diagnostics;
using ImageSegmentation;
using OpenCvSharp;
using System;
using System.IO;
using System.Text.Json.Serialization;
using static ImageSegmentation.LabeledContourGenerator;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Reflection.Emit;
using ImageSegmentation.WinFormsUI;
using System.Windows.Forms;
using Size = OpenCvSharp.Size;

namespace ImageSegmentation
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // First, run the layout engine to generate the latest contour data.
            //Console.WriteLine("Generating layout data...");
            //var layout = new UnitToBoardLayoutEngine();
            //string jsonDirectory = "E:\\ImgSegment\\BoardSide\\BOC\\";
            //var UnitImgPath = "E:\\ImgSegment\\BoardSide\\BOC\\Unit.tif";
            //layout.Layout(jsonDirectory, UnitImgPath);
            //Console.WriteLine("Layout data generation complete.");

            //BotMaterial2();


            // Manually enable High DPI support for the application run
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LayerViewerForm());
        }

        public static void BotMaterial()
        {
            var path = "E:\\ImgSegment\\Test_BotMaterial\\2";
            var solder = Cv2.ImRead(@$"{path}\\img2.bmp", ImreadModes.Unchanged);
            //var img0 = Cv2.ImRead(@$"{path}\\1.bmp", ImreadModes.Unchanged);
            var grayImage = new Mat();
            var gray = new Mat();
            if (solder.Channels() >= 3)
            {
                Cv2.CvtColor(solder, grayImage, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                grayImage = solder;
            }

            var test = new Test();


            Mat invertedColor = test.InvertColorImage(grayImage);
            Cv2.ImWrite(@$"{path}\\InvertColorImage.bmp", invertedColor);

            //var labelSolder = new Mat();
            //solder.CopyTo(labelSolder, labelMask);
            //Cv2.ImWrite(@$"{path}\\labelSolder.bmp", labelSolder);

            #region
            //var bilateral = ImgProcess.EnhanceContrastAndEnhancement(invertedColor);
            var claheEnhanced = ImgProcess.EnhanceContrast(invertedColor);

            // 2. 局部对比度增强
            var localEnhanced = ImgProcess.LocalContrastEnhancement(claheEnhanced); // 阈值太高，若遇到前景背景不好分离的时候可以考虑

            // 3. 使用Bilateral滤波保持边缘的同时减少噪声
            var bilateral = new Mat();
            Cv2.BilateralFilter(localEnhanced, bilateral, 15, 65, 65);
            #endregion
            Cv2.ImWrite(@$"{path}\\bilateral.bmp", bilateral);

            var binary = new Mat();
            Mat labelMask = Cv2.ImRead(@$"{path}\\claheEnhanced.bmp", ImreadModes.Unchanged);
            var t = Cv2.Threshold(labelMask, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            Cv2.ImWrite(@$"{path}\\binary.bmp", binary);
            Mat invertedColor1 = test.InvertColorImage(binary);
            Cv2.ImWrite(@$"{path}\\invertedColor1111.bmp", invertedColor1);

            Mat labelMasks = Cv2.ImRead(@$"{path}\\0.bmp", ImreadModes.Unchanged);
            var img2 = new Mat();
            Cv2.BitwiseAnd(invertedColor1, labelMasks, img2);
            Cv2.ImWrite(@$"{path}\\img2_binary.bmp", img2);

            Mat allBlob = Cv2.ImRead(@$"{path}\\allBlobs.bmp", ImreadModes.Unchanged);
            var imgs = new Mat();
            Cv2.Subtract(img2, allBlob,  imgs);
            Cv2.ImWrite(@$"{path}\\imgs00000.bmp", imgs);


            //var labemMask = Cv2.ImRead(@$"{path}\\3.bmp", ImreadModes.Unchanged);

            //Cv2.BitwiseAnd(img2, labemMask, img2);
            //Cv2.ImWrite(@$"{path}\\img2_labelMask.bmp", img2);


            //var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            //Cv2.MorphologyEx(img2, img2, MorphTypes.Open, kernel);
            //Cv2.ImWrite(@$"{path}\\MorphologyEx.bmp", img2);

            //Cv2.ImShow("Inverted Color", invertedColor);
            //Cv2.WaitKey(0);
        }

        public static void BotMaterial2()
        {
            var path = "E:\\ImgSegment\\Test_BotMaterial\\3";
            var solder = Cv2.ImRead(@$"{path}\\Solder.png", ImreadModes.Unchanged);
            //var img0 = Cv2.ImRead(@$"{path}\\1.bmp", ImreadModes.Unchanged);


            var test = new Test();
            var binary = new Mat();
            Mat labelMask = Cv2.ImRead(@$"{path}\\allLabelMeMask.bmp", ImreadModes.Unchanged);

            var labelSolder = new Mat();
            solder.CopyTo(labelSolder, labelMask);
            Cv2.ImWrite(@$"{path}\\labelSolder.bmp", labelSolder);

            Cv2.CvtColor(labelSolder, binary, ColorConversionCodes.BGR2GRAY);

            #region
            //var claheEnhanced = EnhanceContrast(binary);
            //Cv2.ImWrite(@$"{path}\\claheEnhanced.bmp", claheEnhanced);

            //2.局部对比度增强
            //var localEnhanced = LocalContrastEnhancement(claheEnhanced); // 阈值太高，若遇到前景背景不好分离的时候可以考虑
            //Cv2.ImWrite(@$"{path}\\localEnhanced.bmp", localEnhanced);

            //var bilateral = new Mat();
            //Cv2.BilateralFilter(localEnhanced, bilateral, 9, 75, 75);

            var bilateral = ImgProcess.EnhanceContrastAndEnhancement(binary);

            #endregion

            Mat invertedColor = test.InvertColorImage(bilateral);
            Cv2.ImWrite(@$"{path}\\InvertColorImage.bmp", invertedColor);

            var t = Cv2.Threshold(invertedColor, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            Cv2.ImWrite(@$"{path}\\binary.bmp", binary);

            var img2 = new Mat();
            Cv2.BitwiseAnd(binary, labelMask, img2);
            Cv2.ImWrite(@$"{path}\\img2_binary.bmp", img2);

            var allBlobs = Cv2.ImRead(@$"{path}\\allBlobs.bmp", ImreadModes.Unchanged);
            var imgs = new Mat();
            Cv2.Subtract(img2, allBlobs, imgs);
            Cv2.ImWrite(@$"{path}\\imgs00000.bmp", imgs);


            //var labemMask = Cv2.ImRead(@$"{path}\\3.bmp", ImreadModes.Unchanged);

            //Cv2.BitwiseAnd(img2, labemMask, img2);
            //Cv2.ImWrite(@$"{path}\\img2_labelMask.bmp", img2);


            //var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            //Cv2.MorphologyEx(img2, img2, MorphTypes.Open, kernel);
            //Cv2.ImWrite(@$"{path}\\MorphologyEx.bmp", img2);

            //Cv2.ImShow("Inverted Color", invertedColor);
            //Cv2.WaitKey(0);
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
