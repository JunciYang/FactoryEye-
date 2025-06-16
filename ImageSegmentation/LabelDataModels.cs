using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{

    public enum LabelTypes
    {
        EDGEFID, // 板边定位点
        COMPFID, // 颗内定位点
        PAD,
        FINGER,
        SLOTMARK, // 卡槽标记
        SLOTCUTOUT,// 镂空卡槽
        NOINSP
    }

    public enum ShapeTypes
    {
        RECTANGLE,
        POLYGON,
        CIRCLE
    }

    public enum BoardInfo
    {
        BOARDSIDE,
        UNIT
    }

    public enum RegionTypes
    {
        UP,
        BOTTOM,
        LEFT,
        RIGHT,
        UNIT
    }

    internal class LabelDataModels
    {
    }
    /// <summary>
    /// LabelShape: 标注的形状
    /// Rectangle
    /// Polygon
    /// </summary>
    
    public class LabelFlags
    {
        [JsonProperty("boardSide")]
        public string BoardSide { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }
    }

    public class AnnotationShape
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("points")]
        public List<List<float>> Points { get; set; }

        [JsonProperty("shape_type")]
        public string ShapeType { get; set; }

        [JsonProperty("group_id")]
        public string GroupId { get; set; }
    }

    /// <summary>
    /// LabelAnnotation: 标注文件
    /// </summary>
    public class LabelAnnotation
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("flags")]
        //public LabelFlags Flags { get; set; }
        public Dictionary<string, bool> Flags { get; set; }

        [JsonProperty("shapes")]
        public List<AnnotationShape> Shapes { get; set; }

        [JsonProperty("imagePath")]
        public string ImagePath { get; set; }

        [JsonProperty("imageData")]
        public string ImageData { get; set; }

        [JsonProperty("imageHeight")]
        public int imageHeight { get; set; }

        [JsonProperty("imageWidth")]
        public int imageWidth { get; set; }
    }

    public class LabeledContourGenerator
    {
        /// <summary>
        /// 处理结果：包含轮廓和对应的标签
        /// BinaryInfos：每个标签对应的先验信息的形状区域
        /// BinaryMasks：相同标签的最终二值化Mask
        /// LabelContours：每个标签的所有轮廓
        /// UniqueLabels：所有唯一标签
        /// </summary>
        public class LabelProcessingResult
        {
            public Dictionary<string, Mat> UnitBinaryMasks { get; set; }
            public List<FlagsBinaryMask> FlagsNoInspRegion { get; set; }
            public List<BoardContour> OriginalSizeContours { get; set; }
            public List<BoardContour> FullBoardContours { get; set; }
            public List<BoardContour> FullBoardContoursBasedEdgeFid { get; set; }
            public int Threshold { get; set; }
            public List<Coordinate> LayoutCoordinate { get; set; }
            public List<Coordinate> PixelCoordinateBaseEdgeFid { get; set; }
            public List<Coordinate> PixelCoordinateBaseFullBoard { get; set; }

            public List<FlagsBinaryMask> FlagsBinaryMasks { get; set; }
            public List<FlagsBinaryMask> FlagsColorMasks { get; set; }
            public List<FlagsBinaryMask> FlagsGrayMasks { get; set; }
            public List<List<LabelBndBox>> FlagsLabelBndBox { get; set; }

            public List<LabelImages> FlagsSolderImage { get; set; } 

            public LabelProcessingResult()
            {
                UnitBinaryMasks = new Dictionary<string, Mat>();
                FlagsNoInspRegion = new List<FlagsBinaryMask>();
                OriginalSizeContours = new List<BoardContour>();
                FullBoardContours = new List<BoardContour>();
                FullBoardContoursBasedEdgeFid = new List<BoardContour>();
                LayoutCoordinate = new List<Coordinate>();
                PixelCoordinateBaseEdgeFid = new List<Coordinate>();
                PixelCoordinateBaseFullBoard = new List<Coordinate>();
                FlagsBinaryMasks = new List<FlagsBinaryMask>();
                FlagsColorMasks = new List<FlagsBinaryMask>();
                FlagsGrayMasks = new List<FlagsBinaryMask>();
                FlagsLabelBndBox = new List<List<LabelBndBox>>();
            }
        }

        public class SingleJsonFileResult
        {
            public List<LabelImages> SingleJsonInfo { get; set; }
            public List<LabelImages> LabelBinaryMasks { get; set; }
            public List<LabelImages> LabelColorMasks { get; set; }
            public List<LabelImages> LabelGrayMasks { get; set; }
            public List<LabelImages> LabelNoInspMasks { get; set; }
            public List<ContourLabel> SinglelabelContours {  get; set; }
            public List<ContourLabel> OriginalSizeContours {  get; set; }
            public Mat SolderImage { get; set; }
        }

        public class LabelBndBox 
        { 
            public string Label { get; set; }
            public BndBox BndBox { get; set; }
        }

        public class BndBox
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class LabelImages
        {
            public string LabelName { get; set; }
            public Mat Images { get; set; }
        }

        public class LabelListImages
        {
            public string LabelName { get; set; }
            public List<Mat> Images { get; set; }
        }


        public class FlagsBinaryMask
        {
            public string FlagsName { get; set; }
            public List<LabelImages> LabelImages { get; set; }
        }

        public class BoardContour
        {
            public string JsonName { get; set; }
            public List<ContourLabel> ContourGroups { get; set; }
        }

        public class ContourLabel
        {
            public string LabelName { get; set; }
            public Point[][] Contours { get; set; }
            public List<ContourWithPosition> ContourPositions { get; set; }
        }

        public class ContourPosition
        {
            public Point[] Contour { get; set; }
            public int Block { get; set; }
            public int Row { get; set; }
            public int Col { get; set; }
        }

        public class ContourWithPosition
        {
            public Point[] Contour { get; set; }
            public int Block { get; set; }
            public int Row { get; set; }
            public int Col { get; set; }
        }

        public class LabeledContours
        {
            public string LabelName { get; set; }
            public List<ContourWithPosition> Contours { get; set; } = new List<ContourWithPosition>();
        }

        public class Coordinate
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        /// <summary>
        /// 从 JSON 文件加载标注数据
        /// </summary>
        /// <param name="jsonFilePath">JSON 文件路径</param>
        /// <returns>反序列化的标注数据</returns>
        public static LabelAnnotation LoadAnnotation(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("LabelMe JSON file not found", jsonFilePath);
            }

            try
            {
                string json = File.ReadAllText(jsonFilePath);
                var result = JsonConvert.DeserializeObject<LabelAnnotation>(json);

                if (result == null)
                {
                    throw new InvalidDataException("JSON file is valid but deserialized to null");
                }

                return result;
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidDataException($"Invalid JSON format in file: {jsonFilePath}", ex);
            }
        }

        /// <summary>
        /// 从json文件中加载ImageData，解析为图像
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public static Mat DecodeImageFromJson(string imageData)
        {
            if (imageData == null)
            {
                return null;
            }
            byte[] imageBytes = Convert.FromBase64String(imageData);
            Environment.SetEnvironmentVariable("OPENCV_IO_MAX_IMAGE_PIXELS", "2000000000"); // 例如 2GB 像素限制
            Cv2.SetNumThreads(1);
            Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);

            if (image.Empty())
            {
                Console.WriteLine("Failed to decode image!");
                return null;
            }
            return image;
        }

    }


    public class DataInfo
    {
        public class CutPosition
        {
            /// <summary>
            /// 上板边
            /// </summary>
            public List<int> Up { get; set; }
            /// <summary>
            /// 下板边
            /// </summary>
            public List<int> Bottom { get; set; }
            /// <summary>
            /// 左板边
            /// </summary>
            public List<int> Left { get; set; }
            /// <summary>
            /// 右板边
            /// </summary>
            public List<int> Right { get; set; }
            /// <summary>
            /// 单颗
            /// </summary>
            public List<int> Unit { get; set; }
            public List<int> Block1 { get; set; }
            public List<int> Block2 { get; set; }
        }

        public class UnitLayout
        {
            public int Rows { get; set; }
            public int Cols { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
        public class FullBoardLayout
        {
            public int Blocks { get; set; }
            public int Rows { get; set; }
            public int Cols { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
        

        public class Root
        {
            public CutPosition CutPosition { get; set; }
            public FullBoardLayout FullBoardLayout { get; set; }
            public UnitLayout UnitLayout { get; set; }
        }


        public static Root JsonConvertTo(string jsonFilePath)
        {
            string json = File.ReadAllText(jsonFilePath);
            var result = JsonConvert.DeserializeObject<Root>(json);
            return result;
        }

    }
}
