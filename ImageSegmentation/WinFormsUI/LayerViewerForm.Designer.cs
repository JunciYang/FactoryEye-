namespace ImageSegmentation.WinFormsUI
{
    partial class LayerViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            mainLayoutPanel = new TableLayoutPanel();
            leftPanel = new TableLayoutPanel();
            leftPanelHeader = new Label();
            _layerTreeView = new TreeView();
            _loadButton = new Button();
            _colorButton = new Button();
            _processButton = new Button();
            _startButton = new Button();
            rightPanel = new Panel();
            _pictureBox = new PictureBox();
            mainLayoutPanel.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
            SuspendLayout();
            // 
            // mainLayoutPanel
            // 
            mainLayoutPanel.ColumnCount = 2;
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayoutPanel.Controls.Add(leftPanel, 0, 0);
            mainLayoutPanel.Controls.Add(rightPanel, 1, 0);
            mainLayoutPanel.Dock = DockStyle.Fill;
            mainLayoutPanel.Location = new Point(0, 0);
            mainLayoutPanel.Name = "mainLayoutPanel";
            mainLayoutPanel.RowCount = 1;
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayoutPanel.Size = new Size(800, 450);
            mainLayoutPanel.TabIndex = 0;
            // 
            // leftPanel
            // 
            leftPanel.ColumnCount = 1;
            leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftPanel.Controls.Add(leftPanelHeader, 0, 0);
            leftPanel.Controls.Add(_layerTreeView, 0, 1);
            leftPanel.Controls.Add(_loadButton, 0, 2);
            leftPanel.Controls.Add(_colorButton, 0, 3);
            leftPanel.Controls.Add(_processButton, 0, 4);
            leftPanel.Controls.Add(_startButton, 0, 5);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(3, 3);
            leftPanel.Name = "leftPanel";
            leftPanel.RowCount = 6;
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            leftPanel.Size = new Size(294, 444);
            leftPanel.TabIndex = 0;
            // 
            // leftPanelHeader
            // 
            leftPanelHeader.Dock = DockStyle.Fill;
            leftPanelHeader.Location = new Point(3, 0);
            leftPanelHeader.Name = "leftPanelHeader";
            leftPanelHeader.Size = new Size(288, 40);
            leftPanelHeader.TabIndex = 0;
            // 
            // _layerTreeView
            // 
            _layerTreeView.Dock = DockStyle.Fill;
            _layerTreeView.Location = new Point(3, 43);
            _layerTreeView.Name = "_layerTreeView";
            _layerTreeView.Size = new Size(288, 198);
            _layerTreeView.TabIndex = 1;
            // 
            // _loadButton
            // 
            _loadButton.Dock = DockStyle.Fill;
            _loadButton.Location = new Point(3, 247);
            _loadButton.Name = "_loadButton";
            _loadButton.Size = new Size(288, 44);
            _loadButton.TabIndex = 2;
            // 
            // _colorButton
            // 
            _colorButton.Dock = DockStyle.Fill;
            _colorButton.Location = new Point(3, 297);
            _colorButton.Name = "_colorButton";
            _colorButton.Size = new Size(288, 44);
            _colorButton.TabIndex = 3;
            // 
            // _processButton
            // 
            _processButton.Dock = DockStyle.Fill;
            _processButton.Location = new Point(3, 347);
            _processButton.Name = "_processButton";
            _processButton.Size = new Size(288, 44);
            _processButton.TabIndex = 4;
            // 
            // _startButton
            // 
            _startButton.Dock = DockStyle.Fill;
            _startButton.Location = new Point(3, 397);
            _startButton.Name = "_startButton";
            _startButton.Size = new Size(288, 44);
            _startButton.TabIndex = 5;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(_pictureBox);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(303, 3);
            rightPanel.Name = "rightPanel";
            rightPanel.Size = new Size(494, 444);
            rightPanel.TabIndex = 1;
            // 
            // _pictureBox
            // 
            _pictureBox.Location = new Point(0, 0);
            _pictureBox.Name = "_pictureBox";
            _pictureBox.Size = new Size(100, 50);
            _pictureBox.TabIndex = 0;
            _pictureBox.TabStop = false;
            // 
            // LayerViewerForm
            // 
            ClientSize = new Size(800, 450);
            Controls.Add(mainLayoutPanel);
            Name = "LayerViewerForm";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            mainLayoutPanel.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel leftPanel;
        private System.Windows.Forms.Label leftPanelHeader;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.TreeView _layerTreeView;
        private System.Windows.Forms.PictureBox _pictureBox;
        private System.Windows.Forms.Button _loadButton;
        private System.Windows.Forms.Button _processButton;
        private System.Windows.Forms.Button _colorButton;
        private System.Windows.Forms.Button _startButton;
    }
} 