using System;
using System.Collections.Generic;
using OpenCvSharp;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography.X509Certificates;
using static ImageSegmentation.LabeledContourGenerator;
using System.Reflection.Emit;
using OpenCvSharp.Face;
using Newtonsoft.Json;
using System.Reflection.Metadata.Ecma335;
using System.Net.Http.Headers;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization.Metadata;
using System.Reflection;
using System.Drawing;
using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;

namespace ImageSegmentation
{
    
    public class UnitToBoardLayoutEngine
    {
        public class RegionProcessingResult
        {
            public FlagsBinaryMask BinaryMasks { get; set; }
            public FlagsBinaryMask ColorMasks { get; set; }
            public FlagsBinaryMask NoInspMasks { get; set; }
            public LabelImages SolderImage { get; set; }
            public BoardContour Contours { get; set; }
            public List<LabelBndBox> BndBoxRegion { get; set; }
            public BoardContour OriginalSizeContours { get; set; }
        }
        public Mat Layout(string boardJsonDirectory, string unitImgPath)
        {
            string jsonFile = Path.Combine(boardJsonDirectory, "Layout_Data.json");
            var savePath = Path.Combine(boardJsonDirectory, "Output");
            var dataPath = Path.Combine(boardJsonDirectory, "Datas");
            var data = DataInfo.JsonConvertTo(jsonFile);

            var result = new LabelProcessingResult
            {
                UnitBinaryMasks = new Dictionary<string, Mat>(),
                FlagsNoInspRegion = new List<FlagsBinaryMask>(),
                OriginalSizeContours = new List<BoardContour>(),
                FullBoardContoursBasedEdgeFid = new List<BoardContour>(),
                LayoutCoordinate = new List<Coordinate>(),
                PixelCoordinateBaseEdgeFid = new List<Coordinate>(),
                FlagsBinaryMasks = new List<FlagsBinaryMask>(),
                FlagsColorMasks = new List<FlagsBinaryMask>(),
                FlagsGrayMasks = new List<FlagsBinaryMask>(),
                FlagsLabelBndBox = new List<List<LabelBndBox>>(),
                FlagsSolderImage = new List<LabelImages>(),
            };

            result.Threshold = UnitThreshold(unitImgPath);

            // Step 1: Process each region and get their individual results.
            var regionResults = ProcessAllRegions(dataPath, result.Threshold, data);

            // Step 2: Aggregate results from all regions.
            AggregateRegionResults(regionResults, result, data);

            RestoreToFullBoardCoordinates(result, data);

            // Step 3: Generate the full board layout from aggregated contours.
            var layoutResult = EdgeFidAsOriginalToCalcAllContours(result.FullBoardContours, data);

            result.FullBoardContoursBasedEdgeFid = layoutResult.FullBoardContoursBasedEdgeFid;
            result.LayoutCoordinate = layoutResult.LayoutCoordinate;
            result.PixelCoordinateBaseEdgeFid = layoutResult.PixelCoordinateBaseEdgeFid;
            //result.FullBoardContours = layoutResult.FullBoardContours;

            // Step 4: Save all generated artifacts.
            SaveContourData(result, data, savePath);
            SaveLayoutArtifacts(result, data);

            return new Mat(); 
        }

        public void RestoreToFullBoardCoordinates(LabelProcessingResult result, DataInfo.Root data)
        {
            var flags = result.OriginalSizeContours.Select(c => c.JsonName).ToList();

            var validPoint = new Point();
            var cutPosition = data.CutPosition;
            foreach (var flag in flags)
            {
                if (cutPosition != null && !string.IsNullOrEmpty(flag))
                {
                    PropertyInfo propertyInfo = typeof(DataInfo.CutPosition).GetProperty(flag, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    var coords = propertyInfo.GetValue(cutPosition) as List<int>;

                    if (coords != null && coords.Count == 2)
                    {
                        validPoint = new Point(coords[0], coords[1]);// 对应还原到整版坐标
                    }
                }
                foreach (var cnts in result.OriginalSizeContours)
                {
                    var contours = new List<ContourLabel>();
                    if (flag == cnts.JsonName)
                    {
                        foreach (var cnt in cnts.ContourGroups)
                        {
                            var p = AdjustContoursToFullBoard(cnt.Contours, validPoint); // 根据截图的位置，还原到整板对应的位置
                            contours.Add(new ContourLabel()
                            {
                                LabelName = cnt.LabelName,
                                Contours = p
                            });
                        }

                        result.FullBoardContours.Add(new BoardContour
                        {
                            JsonName = cnts.JsonName,
                            ContourGroups = contours,
                        });

                    }
                }
            }

        }


        private void SaveContourData(LabelProcessingResult result, DataInfo.Root data, string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);

            // Save global metadata with both layout sizes
            var globalMetadata = new
            {
                FullBoardLayout = new { data.FullBoardLayout.Blocks, data.FullBoardLayout.Rows, data.FullBoardLayout.Cols, data.FullBoardLayout.Width, data.FullBoardLayout.Height },
                UnitLayout = new { data.UnitLayout.Rows, data.UnitLayout.Cols, data.UnitLayout.Width, data.UnitLayout.Height },
                OriginOffsetX = SystemMgr.EdgeFidCutPosition.X,
                OriginOffsetY = SystemMgr.EdgeFidCutPosition.Y
            };
            File.WriteAllText(Path.Combine(path, "metadata.json"), JsonConvert.SerializeObject(globalMetadata, Formatting.Indented));

            // Get the final tiled contours for BOARDSIDE and UNIT
            var boardSideTiledContour = result.FullBoardContoursBasedEdgeFid
                .FirstOrDefault(c => c.JsonName.Equals(BoardInfo.BOARDSIDE.ToString(), StringComparison.OrdinalIgnoreCase));
            var unitTiledContour = result.FullBoardContoursBasedEdgeFid
                .FirstOrDefault(c => c.JsonName.Equals(BoardInfo.UNIT.ToString(), StringComparison.OrdinalIgnoreCase));

            // Get the original, untiled UNIT contour from before coordinate restoration and tiling
            var unitOriginalContour = result.OriginalSizeContours
                .FirstOrDefault(c => c.JsonName.Equals(BoardInfo.UNIT.ToString(), StringComparison.OrdinalIgnoreCase));

            // Save original UNIT as a standalone, top-level flag
            if (unitOriginalContour != null)
            {
                var unitDir = Path.Combine(path, unitOriginalContour.JsonName);
                SaveFlagData(unitOriginalContour, unitDir, new { data.UnitLayout.Width, data.UnitLayout.Height });
            }

            // Prepare and save BOARDSIDE, including the TILED UNIT's data
            if (boardSideTiledContour != null)
            {
                var boardSidePlusTiledUnitGroups = new List<ContourLabel>(boardSideTiledContour.ContourGroups);
                if (unitTiledContour != null)
                {
                    boardSidePlusTiledUnitGroups.AddRange(unitTiledContour.ContourGroups);
                }

                var finalBoardSideContour = new BoardContour
                {
                    JsonName = boardSideTiledContour.JsonName,
                    ContourGroups = boardSidePlusTiledUnitGroups
                };

                var boardSideDir = Path.Combine(path, finalBoardSideContour.JsonName);
                SaveFlagData(finalBoardSideContour, boardSideDir, new { data.FullBoardLayout.Width, data.FullBoardLayout.Height });
            }
            else if (unitTiledContour != null)
            {
                var boardSidePlusTiledUnitGroups = new List<ContourLabel>(boardSideTiledContour.ContourGroups);
                boardSidePlusTiledUnitGroups.AddRange(unitTiledContour.ContourGroups);

                var finalBoardSideContour = new BoardContour
                {
                    JsonName = boardSideTiledContour.JsonName,
                    ContourGroups = boardSidePlusTiledUnitGroups
                };

                var boardSideDir = Path.Combine(path, finalBoardSideContour.JsonName);
                SaveFlagData(finalBoardSideContour, boardSideDir, new { data.FullBoardLayout.Width, data.FullBoardLayout.Height });
            }
        }

        private void SaveFlagData(BoardContour boardContour, string directory, object? layoutData = null)
        {
            Directory.CreateDirectory(directory);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            bool hasPoints = false;
            if (boardContour.ContourGroups != null)
            {
                foreach (var label in boardContour.ContourGroups)
                {
                    var ss = new ContourLabel
                    {
                        LabelName = label.LabelName,
                        Contours = label.Contours,
                        ContourPositions = label.ContourPositions
                    };

                    var fileName = string.Join("_", label.LabelName.Split(Path.GetInvalidFileNameChars()));
                    var filePath = Path.Combine(directory, $"{fileName}.json");
                    var jsonContent = JsonConvert.SerializeObject(ss, Formatting.Indented);
                    File.WriteAllText(filePath, jsonContent);
                }
            }

            
        }

        private void SaveLayoutArtifacts(LabelProcessingResult result, DataInfo.Root data)
        {
            var path = "E:\\ImgSegment\\Test\\BOC_mask";

            //foreach (var bMask in result.FlagsBinaryMasks)
            //{
            //    foreach (var m in bMask.LabelImages)
            //    {
            //        Cv2.ImWrite(@$"{path}\\{bMask.FlagsName}_{m.LabelName}_binary.png", m.Images);
            //    }
            //}

            //foreach (var cMask in result.FlagsColorMasks)
            //{
            //    foreach (var m in cMask.LabelImages)
            //    {
            //        Cv2.ImWrite(@$"{path}\\{cMask.FlagsName}_{m.LabelName}_color.png", m.Images);
            //    }
            //}

            //foreach (var nMask in result.FlagsNoInspRegion)
            //{
            //    foreach (var m in nMask.LabelImages)
            //    {
            //        Cv2.ImWrite(@$"{path} \\{nMask.FlagsName}_{m.LabelName}_NoInsp.png", m.Images);
            //    }
            //}

            foreach (var sMask in result.FlagsSolderImage)
            {
                Cv2.ImWrite(@$"{path}\\{sMask.LabelName}_{sMask.LabelName}_Solder.png", sMask.Images);
            }
           
            //var jsonLayoutCoordinate = JsonConvert.SerializeObject(result.LayoutCoordinate);
            //var jsonPixelCoordinate = JsonConvert.SerializeObject(result.PixelCoordinate);
            //File.WriteAllText(@$"{path}\\LayoutCoordinate.json", jsonLayoutCoordinate);
            //File.WriteAllText(@$"{path}\\PixelCoordinate.json", jsonPixelCoordinate);

            //var size = new Size(data.FullBoardLayout.Width, data.FullBoardLayout.Height);
            //var mask = new Mat(size, MatType.CV_8UC1, Scalar.All(0));
            //var maskNew = new Mat();
            //foreach (var fullBoardContour in result.FullBoardContoursBasedEdgeFid)
            //{
            //    var allTiledUnitsCombined = fullBoardContour.ContourGroups;
            //    maskNew = DrawContours(allTiledUnitsCombined, mask);
            //}
            //Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\mask_all_processed_units.png", maskNew);
        }

        private List<RegionProcessingResult> ProcessAllRegions(string boardJsonDirectory, int threshold, DataInfo.Root data)
        {
            var jsonFiles = Directory.GetFiles(boardJsonDirectory, "*.json");
            var regionResults = new List<RegionProcessingResult>();
            foreach (var jsonFile in jsonFiles)
            {
                var shapeDrawerFactory = new ShapeDrawerFactory();
                var labelAnnotation = LabeledContourGenerator.LoadAnnotation(jsonFile);

                var unitProcessor = new LabeledUnitContourExtractor(shapeDrawerFactory);
                var currentJsonProcessingResult = unitProcessor.Process(labelAnnotation, new Point(10,0), threshold);

                regionResults.Add(MergeDataBasedFlags(labelAnnotation, currentJsonProcessingResult, data));
            }

            return regionResults;
        }
        private void AggregateRegionResults(List<RegionProcessingResult> regionResults, LabelProcessingResult result, DataInfo.Root data)
        {
            foreach (var regionResult in regionResults)
            {
                result.FlagsBinaryMasks.Add(regionResult.BinaryMasks);
                result.FlagsColorMasks.Add(regionResult.ColorMasks);
                result.FlagsNoInspRegion.Add(regionResult.NoInspMasks);
                if (regionResult.SolderImage.Images != null) 
                {
                    result.FlagsSolderImage.Add(regionResult.SolderImage);
                }
                result.OriginalSizeContours.Add(regionResult.Contours);
            }
        }

        public int UnitThreshold(string imgPath)
        {
            var binaryImage = new Mat();
            var unitImage = Cv2.ImRead(imgPath, ImreadModes.Unchanged);
            if (unitImage.Empty())
            {
                Console.WriteLine("Error: Unit image could not be loaded.");
            }
            var threshold = ImgProcess.Threshold(unitImage, binaryImage, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
            return threshold;
        }

        private RegionProcessingResult MergeDataBasedFlags(LabelAnnotation labelAnnotation, SingleJsonFileResult result, DataInfo.Root data)
        {
            var flags = string.Join(", ", labelAnnotation?.Flags?
                        .Where(kvp => kvp.Value)
                        .Select(kvp => kvp.Key)
                        .ToList() ?? new List<string>());

            var validPoint = new Point();
            var cutPosition = data.CutPosition;

            if (cutPosition != null && !string.IsNullOrEmpty(flags))
            {
                PropertyInfo propertyInfo = typeof(DataInfo.CutPosition).GetProperty(flags, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo != null)
                {
                    var coords = propertyInfo.GetValue(cutPosition) as List<int>;

                    if (coords != null && coords.Count == 2)
                    {
                        validPoint = new Point(coords[0], coords[1]);// 对应还原到整版坐标
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Coordinates for flag '{flags}' are invalid or not a List<int> with 2 elements.");
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: No matching property found for flag '{flags}' in CutPosition.");
                }
            }
            else
            {
                Console.WriteLine("Warning: data.CutPosition is null or flags are empty.");
            }

            return new RegionProcessingResult
            {
                BinaryMasks = new FlagsBinaryMask
                {
                    FlagsName = flags,
                    LabelImages = result.LabelBinaryMasks
                },
                ColorMasks = new FlagsBinaryMask
                {
                    FlagsName = flags,
                    LabelImages = result.LabelColorMasks
                },
                NoInspMasks = new FlagsBinaryMask
                {
                    FlagsName = flags,
                    LabelImages = result.LabelNoInspMasks
                },
                SolderImage = new LabelImages
                {
                    LabelName = flags,
                    Images = result.SolderImage
                },
                Contours = new BoardContour
                {
                    JsonName = flags,
                    ContourGroups = result.SinglelabelContours
                },

            };
        }
        private Point[][] AdjustContoursToFullBoard(Point[][] contours, Point point)
        {
            var xOffset = point.X;
            var yOffset = point.Y;

            Point[][] adjustedContours = new Point[contours.Length][];

            for (int i = 0; i < contours.Length; i++)
            {
                adjustedContours[i] = new Point[contours[i].Length];

                for (int j = 0; j < contours[i].Length; j++)
                {
                    adjustedContours[i][j] = new Point(
                        contours[i][j].X + xOffset,
                        contours[i][j].Y + yOffset);
                }
            }

            return adjustedContours;
        }


        public LabelProcessingResult EdgeFidAsOriginalToCalcAllContours(List<BoardContour> allRegionContours, DataInfo.Root data)
        {
            var result = new LabelProcessingResult();

            var mom = CalcMomentOfEdgeFid(allRegionContours, data);
            var OriginalOffsetContours = new List<BoardContour>();
            if (mom == new Point(0, 0))
            {
                OriginalOffsetContours = allRegionContours;
            }
            else
            {
                OriginalOffsetContours = CalcOfMomentAsOffset(allRegionContours, mom, data);//EdgeFidAsOriginalToCalcAllContours
            }
            var finalMergedContourLabels = MergedContoursByFlagsName(OriginalOffsetContours);

            result = UnitMappingToFullBoard(finalMergedContourLabels, data, result);

            return result;
        }

        private Point CalcMomentOfEdgeFid(List<BoardContour> regionContours, DataInfo.Root data)
        {
            var edgeFidRegion = regionContours.FirstOrDefault(rc =>
                        rc.ContourGroups.Any(cl => string.Equals(cl.LabelName, LabelTypes.EDGEFID.ToString(), StringComparison.OrdinalIgnoreCase)));
            if (edgeFidRegion == null)
            {
                return new Point(0, 0);
            }
            var edgeFidInTiled = edgeFidRegion.ContourGroups.First(cl =>
                    string.Equals(cl.LabelName, LabelTypes.EDGEFID.ToString(), StringComparison.OrdinalIgnoreCase));

            var mom = new Point(0, 0);
            if (edgeFidInTiled != null && edgeFidInTiled.Contours != null && edgeFidInTiled.Contours.Any())
            {
                mom = GetMoments(edgeFidInTiled.Contours); // 原质心点
                SystemMgr.EdgeFidCutPosition = new System.Drawing.Point(mom.X, mom.Y);
            }
            else
            {
                Console.WriteLine("Warning: No EDGEFID found in Tiled layout to calculate global origin. Using (0,0).");
            }
            //var edgeAndUnitOffsetContours = CalcOfMomentAsOffset(regionContours, mom, data);

            return mom;
        }

        private List<BoardContour> CalcOfMomentAsOffset(List<BoardContour> regionContours, Point offsetPoint, DataInfo.Root data)
        {
            var mask = new Mat(new Size(data.FullBoardLayout.Width, data.FullBoardLayout.Height), MatType.CV_8UC1, Scalar.All(0));
            var maskNew = new Mat();

            //var finalMergedContourLabels = MergedContoursByFlagsName(regionContours);
            var relativeEdgeFidContours = new List<BoardContour>();
            foreach (var mergedContourLabels in regionContours)
            {
                var contourLabel = mergedContourLabels.ContourGroups;

                var newListContoursLabel = TranslateAllContours(contourLabel, new Point(-offsetPoint.X, -offsetPoint.Y));

                maskNew = DrawContours(newListContoursLabel, mask);

                relativeEdgeFidContours.Add(new BoardContour
                {
                    JsonName = mergedContourLabels.JsonName,
                    ContourGroups = newListContoursLabel,
                });
            }
            Cv2.ImWrite(@$"E:\\ImgSegment\\Test\\mask_globally_merged11111.png", maskNew);


            return relativeEdgeFidContours;
        }

        private List<BoardContour> MergedContoursByFlagsName(List<BoardContour> result)
        {
            // 合并不同区域的相同标签对应的轮廓数据
            var mergedContoursByLabel = new Dictionary<string, List<Point[]>>();

            var mergedContoursByFlags = new List<BoardContour>();
            var boardContours = new BoardContour();

            if (result != null)
            {
                foreach (var boardContour in result)
                {
                    var isUnit = string.Equals(boardContour.JsonName, BoardInfo.UNIT.ToString(), StringComparison.OrdinalIgnoreCase);
                    if (!isUnit)
                    {
                        foreach (var contourLabel in boardContour.ContourGroups)
                        {
                            if (contourLabel?.LabelName != null && contourLabel.Contours != null)
                            {
                                if (!mergedContoursByLabel.ContainsKey(contourLabel.LabelName))
                                {
                                    mergedContoursByLabel[contourLabel.LabelName] = new List<Point[]>();
                                }
                                mergedContoursByLabel[contourLabel.LabelName].AddRange(contourLabel.Contours.Where(c => c != null));
                            }
                        }
                    }
                    else
                    {
                        boardContours = new BoardContour
                        {
                            JsonName = BoardInfo.UNIT.ToString(),
                            ContourGroups = boardContour.ContourGroups,
                        };
                        mergedContoursByFlags.Add(boardContours);
                    }
                }
            }

            var finalMergedContourLabels = new List<ContourLabel>();
            foreach (var kvp in mergedContoursByLabel)
            {
                finalMergedContourLabels.Add(new ContourLabel
                {
                    LabelName = kvp.Key,
                    Contours = kvp.Value.ToArray()
                });
               
            }
            boardContours = new BoardContour
            {
                JsonName = BoardInfo.BOARDSIDE.ToString(),
                ContourGroups = finalMergedContourLabels,
            };
            mergedContoursByFlags.Add(boardContours);

            return mergedContoursByFlags;
        }

        private Point TranslateContoursToNonNegative(List<ContourLabel> contoursToTranslate)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;

            foreach (var contourLabel in contoursToTranslate)
            {
                if (contourLabel?.Contours == null) continue;
                foreach (var contour in contourLabel.Contours)
                {
                    if (contour == null) continue;
                    foreach (var point in contour)
                    {
                        if (point.X < minX) minX = point.X;
                        if (point.Y < minY) minY = point.Y;
                    }
                }
            }

            int translateX = 0;
            int translateY = 0;

            if (minX < 0)
            {
                translateX = (int)Math.Ceiling(-minX);
            }
            if (minY < 0)
            {
                translateY = (int)Math.Ceiling(-minY);
            }

            if (translateX == 0 && translateY == 0)
            {
                return new Point(0, 0); 
            }
            
            Console.WriteLine($"Applying global translation for drawing: dX={translateX}, dY={translateY}");
            return new Point(translateX, translateY);

        }

        private List<ContourLabel> TranslateAllContours(List<ContourLabel> sourceContours, Point offset)
        {
            if (sourceContours == null) return null;

            var translatedResult = new List<ContourLabel>();

            foreach (var sourceContourLabel in sourceContours)
            {
                if (sourceContourLabel == null)
                {
                    translatedResult.Add(null);
                    continue;
                }

                var newContourLabel = new ContourLabel
                {
                    LabelName = sourceContourLabel.LabelName 
                };

                if (sourceContourLabel.Contours == null)
                {
                    newContourLabel.Contours = null;
                }
                else
                {
                    newContourLabel.Contours = new Point[sourceContourLabel.Contours.Length][];
                    for (int i = 0; i < sourceContourLabel.Contours.Length; i++)
                    {
                        Point[] currentContour = sourceContourLabel.Contours[i];
                        if (currentContour == null)
                        {
                            newContourLabel.Contours[i] = null;
                            continue;
                        }

                        Point[] newPoints = new Point[currentContour.Length];
                        for (int j = 0; j < currentContour.Length; j++)
                        {
                            newPoints[j] = new Point(
                                currentContour[j].X + offset.X,
                                currentContour[j].Y + offset.Y
                            );
                        }
                        newContourLabel.Contours[i] = newPoints;
                    }
                }
                translatedResult.Add(newContourLabel);
            }
            return translatedResult;
        }

        public List<ContourLabel> AlignOriginPoint(List<ContourLabel> originalResult, Point origin)
        {
            if (originalResult == null)
            {
                Console.WriteLine("Warning: AlignOriginPoint received a null list of ContourLabel. Returning null.");
                return null;
            }

            return TranslateAllContours(originalResult, new Point(-origin.X, -origin.Y));
        }

        public Point GetMoments(Point[][] contours)
        {
            Point origin = new Point(0, 0); // Default to (0,0)

            foreach (var cnt in contours)
            {
                var moments = Cv2.Moments(cnt);

                if (Math.Abs(moments.M00) < 1e-6) // 1e-6 is a small epsilon
                {
                    Console.WriteLine($"Warning: Cannot calculate centroid for an EDGEFID contour (M00 is {moments.M00}). Skipping this contour.");
                    continue;
                }
                origin.X = (int)(moments.M10 / moments.M00);
                origin.Y = (int)(moments.M01 / moments.M00);
            }

            return origin;
        }

        public Mat BoardMasks(List<ContourLabel> result, Size size)
        {
            var maskNew = new Mat();
            var mask = new Mat(size, MatType.CV_8UC1, Scalar.All(0));
            maskNew = DrawContours(result, mask);

            return maskNew;
        }

        public static Mat DrawContours(List<ContourLabel> result, Mat image)
        {
            for (int i = 0; i < result.Count(); i++)
            {
                var cnt = result[i].Contours;
                if (cnt == null || cnt.Length == 0)
                {
                    continue;
                }
                for (int j = 0; j < cnt.Length; j++)
                {
                    Cv2.DrawContours(image, cnt, j, Scalar.All(255), -1);
                }
            }
            return image;
        }

        private LabelProcessingResult UnitMappingToFullBoard(List<BoardContour> edgeAndUnitOffsetContours, DataInfo.Root data, LabelProcessingResult resultToUpdate)
        {
            resultToUpdate.FullBoardContoursBasedEdgeFid.Clear(); 

            foreach (var offsetContour in edgeAndUnitOffsetContours)
            {
                List<ContourLabel> currentGroupToProcess = offsetContour.ContourGroups;
                //if (offsetContour.JsonName.ToUpper().Contains(BoardInfo.UNIT.ToString()))
                if (offsetContour.JsonName.ToUpper() == BoardInfo.UNIT.ToString())
                {
                    Console.WriteLine($"Tiling unit: {offsetContour.JsonName}");
                    var tiledOutputForThisUnit = TileNormalizedUnitContours(currentGroupToProcess, data, resultToUpdate);

                    resultToUpdate.FullBoardContoursBasedEdgeFid.Add(
                    new BoardContour
                    {
                        JsonName = offsetContour.JsonName,
                        ContourGroups = tiledOutputForThisUnit,
                    });
                }
                else
                {
                    resultToUpdate.FullBoardContoursBasedEdgeFid.Add(
                    new BoardContour
                    {
                        JsonName = offsetContour.JsonName,
                        ContourGroups = currentGroupToProcess,
                    });
                }
            }

            return resultToUpdate;
        }

        public List<ContourLabel> TileNormalizedUnitContours(List<ContourLabel> normalizedTemplateUnitContours,
            DataInfo.Root data, LabelProcessingResult resultToUpdate)
        {
            var unitWidth = data.UnitLayout.Width;
            var unitHeight = data.UnitLayout.Height;
            var currRows = data.UnitLayout.Rows;
            var currCols = data.UnitLayout.Cols;

            var blocks = data.FullBoardLayout.Blocks;
            var totalRows = data.FullBoardLayout.Rows;
            var totalCols = data.FullBoardLayout.Cols;

            var cutUnit = data.CutPosition.Unit;

            var imageWidth = data.FullBoardLayout.Width;
            var imageHeight = data.FullBoardLayout.Height;

            var blocksRows = (int)(totalRows / blocks);
            var blocksHeight = (int)(imageHeight / blocks);

            var unitOffsetX = cutUnit[0] - SystemMgr.EdgeFidCutPosition.X;
            var unitOffsetY = cutUnit[1] - SystemMgr.EdgeFidCutPosition.Y;

            // Dictionary to store contours by label name with their position info
            var mergedContours = new Dictionary<string, LabeledContours>();
            if (normalizedTemplateUnitContours == null) return new List<ContourLabel>();

            for (int b = 0; b < blocks; b++)
            {
                var blockH = blocksHeight * b;

                for (int r = 1; r <= blocksRows; r++)
                {
                    for (int c = 1; c <= totalCols; c++)
                    {
                        resultToUpdate.LayoutCoordinate.Add(new LabeledContourGenerator.Coordinate { X = r, Y = c });

                        int currentUnitOffsetX = (c - currCols) * unitWidth;
                        int currentUnitOffsetY = (r - currRows) * unitHeight + blockH;

                        // TODO 判断鼠标在界面显示的坐标落在哪个颗内
                        //为何 - cutUnit[0]？
                        resultToUpdate.PixelCoordinateBaseEdgeFid.Add(new LabeledContourGenerator.Coordinate { X = currentUnitOffsetX - unitOffsetX, Y = currentUnitOffsetY - unitOffsetY });
                        resultToUpdate.PixelCoordinateBaseFullBoard.Add(new LabeledContourGenerator.Coordinate { X = currentUnitOffsetX, Y = currentUnitOffsetY });

                        Point offset = new Point(currentUnitOffsetX, currentUnitOffsetY);

                        var translatedContoursForTile = TranslateAllContours(normalizedTemplateUnitContours, offset);

                        foreach (var translatedLabel in translatedContoursForTile)
                        {
                            if (translatedLabel == null || translatedLabel.Contours == null) continue;

                            var baseLabelName = translatedLabel.LabelName;
                            if (!mergedContours.ContainsKey(baseLabelName))
                            {
                                mergedContours[baseLabelName] = new LabeledContours 
                                { 
                                    LabelName = baseLabelName,
                                    Contours = new List<ContourWithPosition>()
                                };
                            }
                            foreach (var contour in translatedLabel.Contours)
                            {
                                mergedContours[baseLabelName].Contours.Add(new ContourWithPosition
                                {
                                    Contour = contour,
                                    Block = b + 1,
                                    Row = r,
                                    Col = c
                                });
                            }
                        }
                    }
                }
            }

            File.WriteAllText(Path.Combine("E:\\ImgSegment\\BoardSide\\BOC", "PixelCoordinateBaseFullBoard.json"), JsonConvert.SerializeObject(resultToUpdate.PixelCoordinateBaseFullBoard, Formatting.Indented));

            // Create final ContourLabel list with numbered contours under each label
            var result = new List<ContourLabel>();
            foreach (var kvp in mergedContours)
            {
                var labeledContours = kvp.Value;
                
                // Sort contours by block, row, and column
                labeledContours.Contours.Sort((a, b) =>
                {
                    if (a.Block != b.Block) return a.Block.CompareTo(b.Block);
                    if (a.Row != b.Row) return a.Row.CompareTo(b.Row);
                    return a.Col.CompareTo(b.Col);
                });

                // Create a single ContourLabel for this label name with all numbered contours
                var numberedContours = new List<Point[]>();
                var contourPositions = new List<ContourWithPosition>();
                
                foreach (var contourWithPos in labeledContours.Contours)
                {
                    numberedContours.Add(contourWithPos.Contour);
                    contourPositions.Add(new ContourWithPosition
                    { 
                        Contour = contourWithPos.Contour,
                        Block = contourWithPos.Block,
                        Row = contourWithPos.Row,
                        Col = contourWithPos.Col
                    });
                }

                var labelWithNumberedContours = new ContourLabel
                {
                    LabelName = labeledContours.LabelName,
                    //Contours = numberedContours.ToArray(),
                    ContourPositions = contourPositions
                };
                result.Add(labelWithNumberedContours);
            }

            return result;
        }
    }

    
}
