using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    internal class TileImage
    {
    }

    public class ImageTiler
    {
        /// <summary>
        /// 将小图像以自身尺寸为步长平铺到大图像中
        /// </summary>
        /// <param name="sourceImage">小图像（将被平铺的图像）</param>
        /// <param name="targetImage">大图像（目标图像）</param>
        /// <param name="overwriteTarget">是否直接修改目标图像（false时会创建副本）</param>
        /// <returns>平铺后的图像</returns>
        public Mat TileImage(Mat source, Mat target, int tilesX, int tilesY, Point point, bool overwriteTarget = true)
        {
            Mat result = overwriteTarget ? target : target.Clone();
            int startX = point.X;
            int startY = point.Y;
            // 计算每个平铺的位置
            for (int y = 0; y < tilesY; y++)
            {
                for (int x = 0; x < tilesX; x++)
                {
                    int posX = startX + x * source.Width;
                    int posY = startY + y * source.Height;

                    // 检查是否超出目标图像边界
                    if (posX + source.Width > result.Width || posY + source.Height > result.Height)
                    {
                        continue; // 跳过超出边界的平铺
                    }

                    Rect roi = new Rect(posX, posY, source.Width, source.Height);
                    Mat destROI = new Mat(result, roi);
                    source.CopyTo(destROI);
                }
            }

            return result;
        }


        public Mat TileImageWithExclusion(Mat source, Mat target, Rect exclusionZone, bool overwriteTarget = true)
        {
            Mat result = overwriteTarget ? target : target.Clone();
            int tileWidth = source.Width;
            int tileHeight = source.Height;

            for (int y = 0; y < result.Height; y += tileHeight)
            {
                for (int x = 0; x < result.Width; x += tileWidth)
                {
                    // 当前 tile 的位置和大小
                    Rect currentTile = new Rect(x, y, tileWidth, tileHeight);

                    // 检查是否与排除区域重叠
                    if (currentTile.IntersectsWith(exclusionZone))
                        continue; // 跳过重叠区域

                    // 计算实际可复制的区域（处理边缘）
                    int width = Math.Min(tileWidth, result.Width - x);
                    int height = Math.Min(tileHeight, result.Height - y);

                    // 复制图像
                    Mat sourceROI = new Mat(source, new Rect(0, 0, width, height));
                    Mat destROI = new Mat(result, currentTile);
                    sourceROI.CopyTo(destROI);
                }
            }

            return result;
        }

    }

}
