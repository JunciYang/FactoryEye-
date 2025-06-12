using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    public interface IBoardInfos
    {
        void Draw(Mat image, List<Point> points);
    }

    public class BoardSideInfo : IBoardInfos
    {
        public void Draw(Mat image, List<Point> points)
        {
            if (points.Count == 2)
                Cv2.Rectangle(image, points[0], points[1], Scalar.White, -1);
        }
    }

    public class UnitInfo : IBoardInfos
    {
        public void Draw(Mat image, List<Point> points)
        {
            if (points.Count == 2)
                Cv2.Rectangle(image, points[0], points[1], Scalar.White, -1);
        }
    }


    public interface IBoardInfoFactory
    {
        IBoardInfos GetBoardInfo(string boardInfo);
    }

    public class BoardInfoFactory : IBoardInfoFactory
    {
        private readonly Dictionary<string, IBoardInfos> _drawers;

        public BoardInfoFactory()
        {
            _drawers = new Dictionary<string, IBoardInfos>
        {
            { BoardInfo.BOARDSIDE.ToString(), new BoardSideInfo()},
            { BoardInfo.UNIT.ToString(), new UnitInfo()}
        };
        }

        public IBoardInfos GetBoardInfo(string boardInfo)
        {
            if (_drawers.TryGetValue(boardInfo, out var drawer))
                return drawer;

            throw new NotSupportedException($"Board info {boardInfo} is not supported");
        }
    }

}
