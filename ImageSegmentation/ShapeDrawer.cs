using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    public interface ShapeDrawer
    {
        void Draw(Mat image, List<Point> points);
    }
    public class RectangleDrawer : ShapeDrawer
    {
        public void Draw(Mat image, List<Point> points)
        {
            if (points.Count == 2)
                Cv2.Rectangle(image, points[0], points[1], Scalar.White, -1);
        }
    }

    public class PolygonDrawer : ShapeDrawer
    {
        public void Draw(Mat image, List<Point> points)
        {
            if (points.Count >= 3)
                Cv2.FillPoly(image, new[] { points.ToArray() }, Scalar.White);
        }
    }

    public class CircleDrawer : ShapeDrawer
    {
        public void Draw(Mat image, List<Point> points)
        {
            double radius = Math.Sqrt(Math.Pow(points[1].X - points[0].X, 2) + Math.Pow(points[1].Y - points[0].Y, 2));
            Cv2.Circle(image, points[0], (int)radius, Scalar.White, -1);
        }
    }

    public interface IShapeDrawerFactory
    {
        ShapeDrawer GetDrawer(string shapeType);
    }

    public class ShapeDrawerFactory : IShapeDrawerFactory
    {
        private readonly Dictionary<string, ShapeDrawer> _drawers;

        public ShapeDrawerFactory()
        {
            _drawers = new Dictionary<string, ShapeDrawer>
        {
            { ShapeTypes.RECTANGLE.ToString(), new RectangleDrawer() },
            { ShapeTypes.POLYGON.ToString(), new PolygonDrawer() },
            { ShapeTypes.CIRCLE.ToString(), new CircleDrawer() }
        };
        }

        public ShapeDrawer GetDrawer(string shapeType)
        {
            if (_drawers.TryGetValue(shapeType, out var drawer))
                return drawer;

            throw new NotSupportedException($"Shape type {shapeType} is not supported");
        }
    }
}
