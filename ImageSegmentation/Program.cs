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

            // Manually enable High DPI support for the application run
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LayerViewerForm());
        }


    }
}
