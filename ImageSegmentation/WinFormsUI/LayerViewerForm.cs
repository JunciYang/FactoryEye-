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
        private List<FlagInfo> _flagInfos = new List<FlagInfo>();
        private Size _fullBoardSize = Size.Empty;
        private Size _unitLayoutSize = Size.Empty;
        private Size _boardSize = Size.Empty;
        private Point _originOffset = Point.Empty;
        private string _currentViewMode = BoardInfo.BOARDSIDE.ToString(); // "BOARDSIDE" or "UNIT"

        // Fields for Zoom and Pan
        private float _zoomFactor = 1.0f;
        private PointF _viewOffset = PointF.Empty; // Represents the top-left of the view in world coordinates
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
            public Point? LayoutPosition { get; set; } // Add layout position tracking
        }

        // Add selected blob tracking
        private (LayerInfo Layer, ContourData Contour)? _selectedBlob = null;
        private (LayerInfo Layer, ContourData Contour)? _hoveredBlob = null;
        private ToolTip _toolTip;

        // Add layout information
        private int _unitWidth;
        private int _unitHeight;
        private int _totalRows;
        private int _totalCols;
        private int _blocks;
        private int[] _cutUnit;

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
            
            // Initialize tooltip
            _toolTip = new ToolTip();
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 500;
            _toolTip.ReshowDelay = 500;
            _toolTip.ShowAlways = true;
            _toolTip.UseAnimation = true;
            _toolTip.UseFading = true;
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
            _pictureBox.BackColor = Color.Black; // Set a background color
            _pictureBox.Dock = DockStyle.Fill;

            // Add Event Handlers for Zoom and Pan
            _pictureBox.Paint += PictureBox_Paint;
            _pictureBox.MouseWheel += PictureBox_MouseWheel;
            _pictureBox.MouseDown += PictureBox_MouseDown;
            _pictureBox.MouseMove += PictureBox_MouseMove;
            _pictureBox.MouseUp += PictureBox_MouseUp;
            _pictureBox.MouseDoubleClick += PictureBox_MouseDoubleClick;

            // Disable AutoScroll on the container panel
            rightPanel.AutoScroll = false;
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
            if (_boardSize.IsEmpty || _pictureBox.ClientSize.Width == 0 || _pictureBox.ClientSize.Height == 0) return;

            float panelAspect = (float)_pictureBox.ClientSize.Width / _pictureBox.ClientSize.Height;
            float imageAspect = (float)_boardSize.Width / _boardSize.Height;

            if (panelAspect > imageAspect)
            {
                _zoomFactor = (float)_pictureBox.ClientSize.Height / _boardSize.Height;
            }
            else
            {
                _zoomFactor = (float)_pictureBox.ClientSize.Width / _boardSize.Width;
            }

            // Center the image
            _viewOffset.X = (_boardSize.Width / 2f) - (_pictureBox.ClientSize.Width / 2f) / _zoomFactor;
            _viewOffset.Y = (_boardSize.Height / 2f) - (_pictureBox.ClientSize.Height / 2f) / _zoomFactor;

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
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the directory containing layer data";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var selectedPath = folderDialog.SelectedPath;
                    LoadLayersFromPath(selectedPath);
                }
            }
        }

        private void LoadLayersFromPath(string selectedPath)
        {
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
                //_originOffset = new Point(0, 0);
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

                    // Create FlagInfo for this flag
                    var flagInfo = new FlagInfo { Name = flagName, Layers = new List<LayerInfo>() };
                    _flagInfos.Add(flagInfo);

                    var layerFiles = Directory.GetFiles(flagDir, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var layerFile in layerFiles)
                    {
                        var layerName = Path.GetFileNameWithoutExtension(layerFile);
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

                        // Add layer to FlagInfo
                        flagInfo.Layers.Add(layerInfo);
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
            if (_currentViewMode == BoardInfo.UNIT.ToString())
            {
                _boardSize = _unitLayoutSize;
            }
            else // Default to BOARDSIDE
            {
                _boardSize = _fullBoardSize;
            }

            // Set initial view for the selected board layout to 100% zoom, centered.
            _zoomFactor = 1.0f;
            if (!_boardSize.IsEmpty && _pictureBox.ClientSize.Width > 0 && _pictureBox.ClientSize.Height > 0)
            {
                _viewOffset.X = (_boardSize.Width / 2f) - (_pictureBox.ClientSize.Width / 2f);
                _viewOffset.Y = (_boardSize.Height / 2f) - (_pictureBox.ClientSize.Height / 2f);
            }
            _pictureBox.Invalidate();
        }

        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            if (_boardSize.IsEmpty) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // Apply transformations for pan and zoom
            g.ScaleTransform(_zoomFactor, _zoomFactor);
            g.TranslateTransform(-_viewOffset.X, -_viewOffset.Y);

            // Draw a border to indicate the bounds of the board layout
            using (var borderPen = new Pen(Color.DarkGray, 1f / _zoomFactor))
            {
                borderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(borderPen, 0, 0, _boardSize.Width, _boardSize.Height);
            }

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
                                        // Check if this is the selected blob
                                        if (_selectedBlob.HasValue && _selectedBlob.Value.Layer == layer && _selectedBlob.Value.Contour == contourData)
                                        {
                                            // Draw selected blob with a different color and border
                                            using (var selectedBrush = new SolidBrush(Color.FromArgb(200, layer.DisplayColor)))
                                            {
                                                g.FillPolygon(selectedBrush, drawingPoints);
                                            }
                                            using (var selectedPen = new Pen(Color.Yellow, 2f / _zoomFactor))
                                            {
                                                g.DrawPolygon(selectedPen, drawingPoints);
                                            }
                                        }
                                        // Check if this is the hovered blob
                                        else if (_hoveredBlob.HasValue && _hoveredBlob.Value.Layer == layer && _hoveredBlob.Value.Contour == contourData)
                                        {
                                            // Draw hovered blob with a different color and border
                                            using (var hoverBrush = new SolidBrush(Color.FromArgb(180, layer.DisplayColor)))
                                            {
                                                g.FillPolygon(hoverBrush, drawingPoints);
                                            }
                                            using (var hoverPen = new Pen(Color.White, 1.5f / _zoomFactor))
                                            {
                                                g.DrawPolygon(hoverPen, drawingPoints);
                                            }
                                        }
                                        else
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
        }

        private RectangleF GetVisibleWorldRect()
        {
            if (_pictureBox.ClientSize.Width == 0 || _pictureBox.ClientSize.Height == 0)
            {
                return RectangleF.Empty;
            }
            float worldX = _viewOffset.X;
            float worldY = _viewOffset.Y;
            float worldWidth = _pictureBox.ClientSize.Width / _zoomFactor;
            float worldHeight = _pictureBox.ClientSize.Height / _zoomFactor;
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
            string boardPath;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择要处理的电路板数据文件夹";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                boardPath = dialog.SelectedPath;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                _startButton.Enabled = false;
                _startButton.Text = "Processing...";

                var unitImgPath = Path.Combine(boardPath, "Unit.png");

                var layoutEngine = new UnitToBoardLayoutEngine();
                layoutEngine.Layout(boardPath, unitImgPath);

                MessageBox.Show("Layout processing completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // 自动重新加载图层
                //LoadLayersFromPath(boardPath);
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
            if (e is HandledMouseEventArgs h) h.Handled = true;

            PointF mousePos = e.Location;

            // Calculate world coordinates under the mouse cursor before zooming
            float worldX = _viewOffset.X + mousePos.X / _zoomFactor;
            float worldY = _viewOffset.Y + mousePos.Y / _zoomFactor;

            // Calculate new zoom factor
            float scale = e.Delta > 0 ? 1.25f : 0.8f;
            var newZoomFactor = _zoomFactor * scale;
            newZoomFactor = Math.Max(0.05f, Math.Min(newZoomFactor, 50.0f)); // Clamp zoom level

            if (Math.Abs(newZoomFactor - _zoomFactor) < 0.001) return;

            _zoomFactor = newZoomFactor;

            // Adjust view offset to keep the point under the mouse stationary
            _viewOffset.X = worldX - mousePos.X / _zoomFactor;
            _viewOffset.Y = worldY - mousePos.Y / _zoomFactor;

            _pictureBox.Invalidate();
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _panStartPoint = e.Location;
                _pictureBox.Cursor = Cursors.SizeAll;
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Convert mouse coordinates to world coordinates
                var worldX = (e.Location.X / _zoomFactor) + _viewOffset.X;
                var worldY = (e.Location.Y / _zoomFactor) + _viewOffset.Y;

                Point currentOffset = (_currentViewMode == BoardInfo.BOARDSIDE.ToString()) ? _originOffset : Point.Empty;
                TreeNode? activeFlagNode = _layerTreeView.Nodes
                    .OfType<TreeNode>()
                     .FirstOrDefault(n => n.Text.Equals(_currentViewMode, StringComparison.OrdinalIgnoreCase));

                // Check if click is within any contour
                foreach (var flagInfo in _flagInfos)
                {
                    if (flagInfo.Name != _currentViewMode) continue;

                    foreach (var layer in flagInfo.Layers)
                    {
                        foreach (var contourData in layer.Contours)
                        {
                            var drawingPoints = contourData.Points.Select(p => new Point(p.X + currentOffset.X, p.Y + currentOffset.Y)).ToArray();
                            if (IsPointInPolygon(new PointF(worldX, worldY), drawingPoints))
                            {
                                EditBlobLabel(layer, contourData);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void EditBlobLabel(LayerInfo layer, ContourData contour)
        {
            using (var form = new Form())
            {
                form.Text = "Edit Blob Label";
                form.Size = new Size(500, 300);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var label = new Label
                {
                    Text = "New Label Name:",
                    Location = new Point(10, 20),
                    AutoSize = true
                };

                var textBox = new TextBox
                {
                    Text = layer.Name,
                    Location = new Point(10, 40),
                    Width = 260
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(120, 100)
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(200, 100)
                };

                form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // Create new layer with the new name
                    var newLayer = new LayerInfo
                    {
                        Name = textBox.Text,
                        DisplayColor = layer.DisplayColor,
                        Contours = new List<ContourData> { contour }
                    };

                    // Add new layer to the current flag
                    var currentFlag = _flagInfos.FirstOrDefault(f => f.Name == _currentViewMode);
                    if (currentFlag != null)
                    {
                        currentFlag.Layers.Add(newLayer);

                        // Add new node to tree view
                        var flagNode = _layerTreeView.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == _currentViewMode);
                        if (flagNode != null)
                        {
                            var newNode = new TreeNode(textBox.Text) { Tag = newLayer };
                            flagNode.Nodes.Add(newNode);
                        }

                        _pictureBox.Invalidate();
                    }
                }
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var dx = e.Location.X - _panStartPoint.X;
                var dy = e.Location.Y - _panStartPoint.Y;
                _viewOffset = new PointF(_viewOffset.X - dx / _zoomFactor, _viewOffset.Y - dy / _zoomFactor);
                _panStartPoint = e.Location;
                _pictureBox.Invalidate();
                return;
            }
            else
            {
                // 鼠标悬停的像素坐标
                float worldX = _viewOffset.X + e.X / _zoomFactor;
                float worldY = _viewOffset.Y + e.Y / _zoomFactor;
                Point worldPoint = new Point((int)worldX, (int)worldY);

                // 判断是整板还是单颗
                Point currentOffset = (_currentViewMode == BoardInfo.BOARDSIDE.ToString()) ? _originOffset : Point.Empty;
                TreeNode? activeFlagNode = _layerTreeView.Nodes
                    .OfType<TreeNode>()
                     .FirstOrDefault(n => n.Text.Equals(_currentViewMode, StringComparison.OrdinalIgnoreCase));
                
                if (activeFlagNode != null)
                {
                    _hoveredBlob = null; // Reset hover state
                    foreach (TreeNode layerNode in activeFlagNode.Nodes)
                    {
                        if (layerNode.Checked && layerNode.Tag is LayerInfo layer)
                        {
                            for (int i = 0; i < layer.Contours.Count; i++) 
                            {
                                var contourData = layer.Contours[i];
                                var drawingPoints = contourData.Points.Select(p => new Point(p.X + currentOffset.X, p.Y + currentOffset.Y)).ToArray();
                                if (IsPointInPolygon(worldPoint, drawingPoints))
                                {
                                    var num = i;
                                    _hoveredBlob = (layer, contourData);
                                    _toolTip.SetToolTip(_pictureBox, $"{layerNode.Text}{num + 1}");
                                    _pictureBox.Invalidate();
                                    return;
                                }
                            }
                        }
                    }
                    _toolTip.SetToolTip(_pictureBox, "");
                    _pictureBox.Invalidate();
                }
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
                _pictureBox.Cursor = Cursors.Default;
            }
        }

        private void PictureBox_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            FitImageToPanel();
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

        private class LayoutData
        {
            public UnitLayoutInfo UnitLayout { get; set; }
            public FullBoardLayoutInfo FullBoardLayout { get; set; }
            public CutPositionInfo CutPosition { get; set; }
        }

        private class UnitLayoutInfo
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private class FullBoardLayoutInfo
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int Rows { get; set; }
            public int Cols { get; set; }
            public int Blocks { get; set; }
        }

        private class CutPositionInfo
        {
            public int[] Unit { get; set; }
        }

        private static Color GetRandomColor()
        {
            return Color.FromArgb(150, _random.Next(128, 256), _random.Next(128, 256), _random.Next(128, 256));
        }

        private bool IsPointInPolygon(PointF point, Point[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
} 
 