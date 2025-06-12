using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using static ImageSegmentation.LabeledContourGenerator;
using Point = OpenCvSharp.Point;

namespace ImageSegmentation
{
    public abstract class BaseLabelProcessor
    {

        public SingleJsonFileResult Process(LabelAnnotation annotation, Point point, int threshold)
        {
            if (annotation == null) throw new ArgumentNullException(nameof(annotation));

            var singleJsonRes = IniSingleJsonFileResult();

            Mat image = DecodeImage(annotation);
            ProcessLabelGroups(annotation, image, singleJsonRes, point, threshold);

            return singleJsonRes;
        }

        protected virtual SingleJsonFileResult IniSingleJsonFileResult()
        {
            return new SingleJsonFileResult
            {
                //SingleFileInfo = new List<KeyValuePair<AnnotationShape, Mat>>(),
                SingleJsonInfo = new List<LabelImages>(),
                SinglelabelContours = new List<ContourLabel>(),
                OriginalSizeContours = new List<ContourLabel>(),
                LabelBinaryMasks = new List<LabelImages>(),
                LabelColorMasks = new List<LabelImages>(),
                LabelGrayMasks = new List<LabelImages>(),
                LabelNoInspMasks = new List<LabelImages>()
            };
        }

        protected abstract Mat DecodeImage(LabelAnnotation annotation);

        public virtual void ProcessLabelGroups(LabelAnnotation annotation, Mat image, SingleJsonFileResult singleJsonRes, Point point, int threshold)
        {
            ProcessingNetingRegion(annotation, image, singleJsonRes, point, threshold);
        }

        protected abstract void ProcessingNetingRegion(LabelAnnotation annotation, Mat image, SingleJsonFileResult singleJsonRes, Point point, int threshold);


    }

}
