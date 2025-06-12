using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenCvSharp;

namespace ImageSegmentation
{
    internal class ImgStitcher
    {
    }

class ImageStitcher
    {
       public static void stitcher(string imageDirectory, string outputFileName)
        {
            // 获取所有符合命名规则的图像文件
            var imageFiles = Directory.GetFiles(imageDirectory, "FullViewImage-*.png");

            // 解析文件名并确定行数和列数
            var imageInfoList = new List<(int col, int row, string path)>();
            int maxRow = 0;
            int maxCol = 0;

            foreach (var file in imageFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("FullViewImage-"))
                {
                    string[] parts = fileName.Substring(14).Split('-');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int col) &&
                        int.TryParse(parts[1], out int row))
                    {
                        imageInfoList.Add((col, row, file));
                        maxCol = Math.Max(maxCol, col);
                        maxRow = Math.Max(maxRow, row);
                    }
                }
            }

            // 如果没有找到图像，退出
            if (imageInfoList.Count == 0)
            {
                Console.WriteLine("没有找到符合命名规则的图像文件。");
                return;
            }

            // 按列和行排序图像（从左到右，从上到下）
            imageInfoList.Sort((a, b) =>
            {
                int colCompare = a.col.CompareTo(b.col);
                if (colCompare != 0) return colCompare;
                return a.row.CompareTo(b.row);
            });

            // 计算每列的最大宽度和每行的最大高度
            var colGroups = imageInfoList.GroupBy(x => x.col).OrderBy(g => g.Key);
            var rowGroups = imageInfoList.GroupBy(x => x.row).OrderBy(g => g.Key);

            // 每列的最大宽度
            var colWidths = new Dictionary<int, int>();
            foreach (var group in colGroups)
            {
                int maxWidth = group.Max(x =>
                {
                    using var img = Cv2.ImRead(x.path);
                    return img.Width;
                });
                colWidths[group.Key] = maxWidth;
            }

            // 每行的最大高度
            var rowHeights = new Dictionary<int, int>();
            foreach (var group in rowGroups)
            {
                int maxHeight = group.Max(x =>
                {
                    using var img = Cv2.ImRead(x.path);
                    return img.Height;
                });
                rowHeights[group.Key] = maxHeight;
            }

            // 计算拼接后的总宽度和总高度
            int totalWidth = colWidths.Values.Sum();
            int totalHeight = rowHeights.Values.Sum();

            // 创建最终拼接的图像（黑色背景）
            using var stitchedImage = new Mat(
                totalHeight,
                totalWidth,
                MatType.CV_8UC3,
                new Scalar(0, 0, 0));

            // 计算每列和每行的起始位置
            var colStartPositions = new Dictionary<int, int>();
            int currentX = 0;
            foreach (var col in colWidths.OrderBy(x => x.Key))
            {
                colStartPositions[col.Key] = currentX;
                currentX += col.Value;
            }

            var rowStartPositions = new Dictionary<int, int>();
            int currentY = 0;
            foreach (var row in rowHeights.OrderBy(x => x.Key))
            {
                rowStartPositions[row.Key] = currentY;
                currentY += row.Value;
            }

            // 将每张图像放置到正确的位置
            foreach (var (col, row, path) in imageInfoList)
            {
                using var currentImage = Cv2.ImRead(path);

                // 计算放置位置
                int x = colStartPositions[col];
                int y = rowStartPositions[row];
                int width = currentImage.Width;
                int height = currentImage.Height;

                // 将图像复制到拼接图像中的正确位置
                Rect roi = new Rect(x, y, width, height);
                currentImage.CopyTo(new Mat(stitchedImage, roi));
            }

            // 保存拼接后的图像
            Cv2.ImWrite(Path.Combine(imageDirectory, outputFileName), stitchedImage);
            Console.WriteLine($"图像拼接完成，已保存为 {outputFileName}");
        }
    }
}

