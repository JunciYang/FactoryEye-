using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace ImageSegmentation.WinFormsUI
{
    public partial class LayerViewerForm : Form
    {
        //private readonly string _layerDirectory = "E:\\ImgSegment\\BoardSide\\HDSR01 783HDS\\Test";
        private readonly string boardJsonDirectory = "E:\\ImgSegment\\BoardSide\\FCBOC";
        private List<FlagInfo> _flagInfos = new List<FlagInfo>();
        private Size _fullBoardSize = Size.Empty;
        private Size _unitLayoutSize = Size.Empty;
        private Size _boardSize = Size.Empty;
        private Point _originOffset = Point.Empty;
        private string _currentViewMode = BoardInfo.BOARDSIDE.ToString(); // "BOARDSIDE" or "UNIT"

        // Fields for Zoom and Pan
        private float _zoomFactor = 1.0f;
        private bool _isPanning = false;
        private Point _panStartPoint = Point.Empty;

        // A class to hold contour data along with its pre-calculated bounding box for culling
        private class ContourData
        {
            public Point[] Points { get; }
            public Rectangle BoundingBox { get; }

            public ContourData(Point[] points)
            {
                Points = points;
                BoundingBox = CalculateBoundingBox(points);
            }

            private static Rectangle CalculateBoundingBox(Point[] points)
            {
                if (points == null || points.Length == 0)
                    return Rectangle.Empty;

                int minX = points[0].X;
                int minY = points[0].Y;
                int maxX = points[0].X;
                int maxY = points[0].Y;

                for (int i = 1; i < points.Length; i++)
                {
                    minX = Math.Min(minX, points[i].X);
                    minY = Math.Min(minY, points[i].Y);
                    maxX = Math.Max(maxX, points[i].X);
                    maxY = Math.Max(maxY, points[i].Y);
                }
                return new Rectangle(minX, minY, maxX - minX, maxY - minY);
            }
        }

        private class LayerInfo
        {
            public string Name { get; set; }
            public Color DisplayColor { get; set; }
            public List<ContourData> Contours { get; set; }
        }

        // Custom converter to handle Point deserialization from an object { "X": x, "Y": y }
        public class PointConverter : JsonConverter<Point>
        {
            public override Point ReadJson(JsonReader reader, Type objectType, Point existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var token = JToken.Load(reader);
                if (token.Type == JTokenType.Object)
                {
                    int x = token["X"]?.Value<int>() ?? 0;
                    int y = token["Y"]?.Value<int>() ?? 0;
                    return new Point(x, y);
                }
                // Fallback for other potential formats if necessary, though not expected here.
                return Point.Empty;
            }

            public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer)
            {
                // This converter is only for reading
                throw new NotImplementedException();
            }
        }

        private static readonly Random _random = new Random();

        public LayerViewerForm()
        {
            InitializeComponent();
            ApplyCustomStyles();
        }

        #region
        private void ApplyCustomStyles()
        {
            // Form setup
            this.Text = "Layer Viewer";
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // Panel Colors
            leftPanel.BackColor = Color.FromArgb(63, 63, 70);
            rightPanel.BackColor = Color.FromArgb(30, 30, 30);
            
            // Header Label
            leftPanelHeader.Text = "Layers";
            leftPanelHeader.ForeColor = Color.White;
            leftPanelHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            leftPanelHeader.TextAlign = ContentAlignment.MiddleCenter;

            // TreeView
            _layerTreeView.BackColor = Color.FromArgb(45, 45, 48);
            _layerTreeView.ForeColor = Color.White;
            _layerTreeView.BorderStyle = BorderStyle.None;
            _layerTreeView.CheckBoxes = true;
            _layerTreeView.Padding = new Padding(10);
            _layerTreeView.Margin = new Padding(30);
            _layerTreeView.AfterCheck += OnLayerSelectionChanged;
            _layerTreeView.AfterSelect += OnNodeSelected;
            _layerTreeView.NodeMouseClick += (sender, e) => {
                // This ensures that clicking on the node text also toggles the checkbox
                if (e.Node != null) {
                    e.Node.Checked = !e.Node.Checked;
                }
            };

            // Load Button
            _loadButton.Text = "Load Layers";
            _loadButton.FlatStyle = FlatStyle.Flat;
            _loadButton.BackColor = Color.FromArgb(0, 122, 204);
            _loadButton.ForeColor = Color.White;
            _loadButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _loadButton.Margin = new Padding(0);
            _loadButton.FlatAppearance.BorderSize = 0;
            _loadButton.Click += LoadLayers_Click;

            // Color Button
            _colorButton.Text = "Change Color";
            _colorButton.FlatStyle = FlatStyle.Flat;
            _colorButton.BackColor = Color.FromArgb(80, 80, 85);
            _colorButton.ForeColor = Color.White;
            _colorButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _colorButton.Margin = new Padding(0);
            _colorButton.Enabled = false;
            _colorButton.Click += ColorButton_Click;

            // Process Button
            _processButton.Text = "Process Selected";
            _processButton.FlatStyle = FlatStyle.Flat;
            _processButton.BackColor = Color.FromArgb(80, 80, 85);
            _processButton.ForeColor = Color.White;
            _processButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _processButton.Margin = new Padding(0);
            _processButton.FlatAppearance.BorderSize = 0;
            _processButton.Click += ProcessSelected_Click;

            // Start Button
            _startButton.Text = "Start Layout Engine";
            _startButton.FlatStyle = FlatStyle.Flat;
            _startButton.BackColor = Color.FromArgb(28, 151, 234);
            _startButton.ForeColor = Color.White;
            _startButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _startButton.Margin = new Padding(0);
            _startButton.FlatAppearance.BorderSize = 0;
            _startButton.Click += StartButton_Click;

            // PictureBox
            _pictureBox.SizeMode = PictureBoxSizeMode.Normal; // Use Normal mode for custom drawing
            _pictureBox.BackColor = Color.Black; // Set a background color

            // Add Event Handlers for Zoom and Pan
            _pictureBox.Paint += PictureBox_Paint;
            _pictureBox.MouseWheel += PictureBox_MouseWheel;
            _pictureBox.MouseDown += PictureBox_MouseDown;
            _pictureBox.MouseMove += PictureBox_MouseMove;
            _pictureBox.MouseUp += PictureBox_MouseUp;
        }
        #endregion
        
        private void UpdatePictureBoxSizeAndPosition()
        {
            if (_boardSize.IsEmpty) return;

            // Set the virtual size of the PictureBox.
            // This determines the range of the scrollbars.
            _pictureBox.Size = new Size(
                (int)(_boardSize.Width * _zoomFactor),
                (int)(_boardSize.Height * _zoomFactor)
            );
            _pictureBox.Invalidate();
        }
        
        private void FitImageToPanel()
        {
            if (_boardSize.IsEmpty) return;

            float panelAspect = (float)rightPanel.ClientSize.Width / rightPanel.ClientSize.Height;
            float imageAspect = (float)_boardSize.Width / _boardSize.Height;

            if (panelAspect > imageAspect)
            {
                _zoomFactor = (float)rightPanel.ClientSize.Height / _boardSize.Height;
            }
            else
            {
                _zoomFactor = (float)rightPanel.ClientSize.Width / _boardSize.Width;
            }
            UpdatePictureBoxSizeAndPosition();
            _pictureBox.Invalidate(); // Trigger a repaint
        }

        private void OnNodeSelected(object? sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null) return;
            
            // Enable color button only for actual layers (child nodes)
            _colorButton.Enabled = e.Node.Parent != null;

            // Determine if a top-level flag was selected (BOARDSIDE or UNIT)
            if (e.Node.Tag is BoardInfo flag)
            {
                string selectedView = flag.ToString();
                if (selectedView != _currentViewMode)
                {
                    _currentViewMode = selectedView;

                    // Find and update the other top-level node
                    var otherNode = _layerTreeView.Nodes.OfType<TreeNode>()
                        .FirstOrDefault(n => n.Tag is BoardInfo && n != e.Node);

                    if (otherNode != null)
                    {
                        otherNode.Collapse();
                        // Uncheck all children of the other node by simulating a click
                        if (otherNode.Checked)
                        {
                           otherNode.Checked = false;
                        }
                    }

                    UpdateCompositeImage(); // This will re-calculate size and fit the image
                }
            }
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            if (_layerTreeView.SelectedNode?.Tag is LayerInfo layerInfo)
            {
                using (var colorDialog = new ColorDialog())
                {
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        layerInfo.DisplayColor = colorDialog.Color;
                        UpdateCompositeImage();
                    }
                }
            }
        }

        private void LoadLayers_Click(object sender, EventArgs e)
        {
            string selectedPath = "";
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择包含图层数据的文件夹";
                // 你可以设置一个默认的起始路径
                // dialog.SelectedPath = "E:\\ImgSegment\\Test\\"; 
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return; // 用户取消了选择
                }
                selectedPath = dialog.SelectedPath;
            }

            _layerTreeView.Nodes.Clear();
            _flagInfos.Clear();
            _fullBoardSize = Size.Empty;
            _unitLayoutSize = Size.Empty;
            _boardSize = Size.Empty;
            _originOffset = Point.Empty;

            if (!Directory.Exists(selectedPath))
            {
                MessageBox.Show($"目录不存在: {selectedPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load global metadata first
            var metaFile = Path.Combine(selectedPath, "metadata.json");
            if (File.Exists(metaFile))
            {
                var metaJson = File.ReadAllText(metaFile);
                var metadata = JsonConvert.DeserializeObject<BoardMetadata>(metaJson);
                _fullBoardSize = new Size(metadata.FullBoardLayout.Width, metadata.FullBoardLayout.Height);
                _unitLayoutSize = new Size(metadata.UnitLayout.Width, metadata.UnitLayout.Height);
                _originOffset = new Point(metadata.OriginOffsetX, metadata.OriginOffsetY);
            }
            else
            {
                MessageBox.Show("全局 metadata.json 未找到，无法确定电路板尺寸。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _layerTreeView.BeginUpdate();
            var flagSubDirs = Directory.GetDirectories(selectedPath);
            foreach (var flagDir in flagSubDirs)
            {
                var flagName = new DirectoryInfo(flagDir).Name;
                if (Enum.TryParse<BoardInfo>(flagName, true, out var flagEnum))
                {
                    var flagNode = new TreeNode(flagName) { Tag = flagEnum };
                    _layerTreeView.Nodes.Add(flagNode);

                    var layerFiles = Directory.GetFiles(flagDir, "*.json");
                    foreach (var layerFile in layerFiles)
                    {
                        var layerName = Path.GetFileNameWithoutExtension(layerFile);
                        // Read contour data
                        var json = File.ReadAllText(layerFile);
                        var contours = JsonConvert.DeserializeObject<List<Point[]>>(json, new PointConverter());
                        var layerInfo = new LayerInfo
                        {
                            Name = layerName,
                            DisplayColor = GetRandomColor(),
                            Contours = contours.Select(p => new ContourData(p)).ToList()
                        };
                        var layerNode = new TreeNode(layerName) { Tag = layerInfo };
                        flagNode.Nodes.Add(layerNode);
                    }
                }
            }
            _layerTreeView.ExpandAll();
            _layerTreeView.EndUpdate();

            _currentViewMode = BoardInfo.BOARDSIDE.ToString(); // Set initial view
            UpdateCompositeImage();
        }

        private void OnLayerSelectionChanged(object? sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown) return;

            // Temporarily unsubscribe to prevent recursive calls
            _layerTreeView.AfterCheck -= OnLayerSelectionChanged;

            // Sync parent/child checkboxes
            // 1. If a parent is checked, check all children
            if (e.Node.Nodes.Count > 0)
            {
                foreach (TreeNode child in e.Node.Nodes)
                {
                    child.Checked = e.Node.Checked;
                }
            }
            // 2. If a child is checked, ensure parent is checked. If all children are unchecked, uncheck parent.
            else if (e.Node.Parent != null)
            {
                e.Node.Parent.Checked = e.Node.Parent.Nodes.OfType<TreeNode>().Any(n => n.Checked);
            }

            // Re-subscribe to the event
            _layerTreeView.AfterCheck += OnLayerSelectionChanged;

            _pictureBox.Invalidate(); // Trigger repaint
        }

        private void UpdateCompositeImage()
        {
            // Determine board size and offset based on the current view mode
            Point currentOffset;
            if (_currentViewMode == BoardInfo.UNIT.ToString())
            {
                _boardSize = _unitLayoutSize;
            }
            else // Default to BOARDSIDE
            {
                _boardSize = _fullBoardSize;
            }

            FitImageToPanel();
        }

        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            if (_boardSize.IsEmpty) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Apply transformations: First scale, then translate.
            // The translation is based on the panel's scroll position.
            g.ScaleTransform(_zoomFactor, _zoomFactor);
            g.TranslateTransform(rightPanel.AutoScrollPosition.X / _zoomFactor, rightPanel.AutoScrollPosition.Y / _zoomFactor);

            // Culling: Calculate the visible rectangle in world coordinates
            RectangleF visibleWorldRect = GetVisibleWorldRect();
            
            // Determine the correct offset for the current view's coordinate system
            Point currentOffset = (_currentViewMode == BoardInfo.BOARDSIDE.ToString()) ? _originOffset : Point.Empty;

            // Find the active flag node and draw its checked layers
            TreeNode? activeFlagNode = _layerTreeView.Nodes
                .OfType<TreeNode>()
                .FirstOrDefault(n => n.Text.Equals(_currentViewMode, StringComparison.OrdinalIgnoreCase));

            if (activeFlagNode != null)
            {
                foreach (TreeNode layerNode in activeFlagNode.Nodes)
                {
                    if (layerNode.Checked && layerNode.Tag is LayerInfo layer)
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(150, layer.DisplayColor)))
                        {
                            foreach (var contourData in layer.Contours)
                            {
                                // Apply the view offset to the contour's bounding box for an accurate check
                                var checkBox = new Rectangle(
                                    contourData.BoundingBox.X + currentOffset.X,
                                    contourData.BoundingBox.Y + currentOffset.Y,
                                    contourData.BoundingBox.Width,
                                    contourData.BoundingBox.Height
                                );

                                // Culling check: only draw if the contour is in the visible area
                                if (visibleWorldRect.IntersectsWith(checkBox))
                                {
                                    var drawingPoints = contourData.Points.Select(p => new Point(p.X + currentOffset.X, p.Y + currentOffset.Y)).ToArray();
                                    if (drawingPoints.Length > 2)
                                    {
                                        g.FillPolygon(brush, drawingPoints);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private RectangleF GetVisibleWorldRect()
        {
            float worldX = -rightPanel.AutoScrollPosition.X / _zoomFactor;
            float worldY = -rightPanel.AutoScrollPosition.Y / _zoomFactor;
            float worldWidth = rightPanel.ClientSize.Width / _zoomFactor;
            float worldHeight = rightPanel.ClientSize.Height / _zoomFactor;
            return new RectangleF(worldX, worldY, worldWidth, worldHeight);
        }

        private void ProcessSelected_Click(object sender, EventArgs e)
        {
            var selectedLayers = new List<string>();
            foreach (TreeNode flagNode in _layerTreeView.Nodes)
            {
                foreach (TreeNode layerNode in flagNode.Nodes)
                {
                    if (layerNode.Checked)
                    {
                        selectedLayers.Add($"{flagNode.Text}/{layerNode.Text}");
                    }
                }
            }

            if (!selectedLayers.Any())
            {
                MessageBox.Show("No layers selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MessageBox.Show($"Processing layers: {string.Join(", ", selectedLayers)}", "Processing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                _startButton.Enabled = false;
                _startButton.Text = "Processing...";

                // 使用在UnitToBoardLayoutEngine中定义的路径作为临时占位符
                //string boardJsonDirectory = "E:\\ImgSegment\\BoardSide\\HDSR01 783HDS"; 
                //string  = "E:\\ImgSegment\\Unit\\Unit.png";

                var unitImgPath = Path.Combine(boardJsonDirectory, "Unit.png");

                var layoutEngine = new UnitToBoardLayoutEngine();
                layoutEngine.Layout(boardJsonDirectory, unitImgPath);

                MessageBox.Show("Layout processing completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // 自动重新加载图层
                LoadLayers_Click(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _startButton.Enabled = true;
                _startButton.Text = "Start Layout Engine";
                this.Cursor = Cursors.Default;
            }
        }

        private void PictureBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (_boardSize.IsEmpty) return;
            // Mark the event as handled to prevent the container from scrolling
            if (e is HandledMouseEventArgs h) h.Handled = true;

            // Calculate the new zoom factor
            float scale = e.Delta > 0 ? 1.25f : 0.8f;
            var newZoomFactor = _zoomFactor * scale;
            newZoomFactor = Math.Max(0.1f, Math.Min(newZoomFactor, 20.0f)); // Clamp zoom level

            if (Math.Abs(newZoomFactor - _zoomFactor) < 0.001) return;

            // Point-based zooming
            PointF mousePos = e.Location;

            // Calculate scroll positions before zoom
            float scrollX_before = rightPanel.HorizontalScroll.Value;
            float scrollY_before = rightPanel.VerticalScroll.Value;

            // Calculate world coordinates of the point under the mouse
            float worldX = (mousePos.X + scrollX_before) / _zoomFactor;
            float worldY = (mousePos.Y + scrollY_before) / _zoomFactor;

            // Update zoom factor and PictureBox size
            _zoomFactor = newZoomFactor;
            UpdatePictureBoxSizeAndPosition();

            // Calculate new scroll positions to keep the point under the mouse
            int scrollX_after = (int)(worldX * _zoomFactor - mousePos.X);
            int scrollY_after = (int)(worldY * _zoomFactor - mousePos.Y);

            // Apply new scroll positions
            rightPanel.AutoScrollPosition = new Point(scrollX_after, scrollY_after);

            _pictureBox.Invalidate();
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                _panStartPoint = e.Location;
                _pictureBox.Cursor = Cursors.SizeAll;
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                // Calculate the new scroll position
                int dx = e.X - _panStartPoint.X;
                int dy = e.Y - _panStartPoint.Y;
                int newX = Math.Max(0, -rightPanel.AutoScrollPosition.X - dx);
                int newY = Math.Max(0, -rightPanel.AutoScrollPosition.Y - dy);
                rightPanel.AutoScrollPosition = new Point(newX, newY);
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                _isPanning = false;
                _pictureBox.Cursor = Cursors.Default;
            }
        }

        private class FlagInfo
        {
            public string Name { get; set; }
            public List<LayerInfo> Layers { get; set; } = new List<LayerInfo>();
            public Rectangle BoundingBox { get; set; }
            public Size LayoutSize { get; set; }
        }

        private class LayoutInfo
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private class BoardMetadata
        {
            public LayoutInfo FullBoardLayout { get; set; }
            public LayoutInfo UnitLayout { get; set; }
            public int OriginOffsetX { get; set; }
            public int OriginOffsetY { get; set; }
        }

        private static Color GetRandomColor()
        {
            return Color.FromArgb(150, _random.Next(128, 256), _random.Next(128, 256), _random.Next(128, 256));
        }
    }
} 
 