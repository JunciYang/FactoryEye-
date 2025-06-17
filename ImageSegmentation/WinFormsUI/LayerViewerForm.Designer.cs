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
            menuStrip1 = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            processToolStripMenuItem = new ToolStripMenuItem();
            binaryToolStripMenuItem = new ToolStripMenuItem();
            openImageToolStripMenuItem = new ToolStripMenuItem();
            mainLayoutPanel.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
            menuStrip1.SuspendLayout();
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
            mainLayoutPanel.Location = new Point(0, 25);
            mainLayoutPanel.Name = "mainLayoutPanel";
            mainLayoutPanel.RowCount = 1;
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayoutPanel.Size = new Size(800, 425);
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
            leftPanel.Size = new Size(294, 419);
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
            _layerTreeView.Size = new Size(288, 173);
            _layerTreeView.TabIndex = 1;
            // 
            // _loadButton
            // 
            _loadButton.Dock = DockStyle.Fill;
            _loadButton.Location = new Point(3, 222);
            _loadButton.Name = "_loadButton";
            _loadButton.Size = new Size(288, 44);
            _loadButton.TabIndex = 2;
            // 
            // _colorButton
            // 
            _colorButton.Dock = DockStyle.Fill;
            _colorButton.Location = new Point(3, 272);
            _colorButton.Name = "_colorButton";
            _colorButton.Size = new Size(288, 44);
            _colorButton.TabIndex = 3;
            // 
            // _processButton
            // 
            _processButton.Dock = DockStyle.Fill;
            _processButton.Location = new Point(3, 322);
            _processButton.Name = "_processButton";
            _processButton.Size = new Size(288, 44);
            _processButton.TabIndex = 4;
            // 
            // _startButton
            // 
            _startButton.Dock = DockStyle.Fill;
            _startButton.Location = new Point(3, 372);
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
            rightPanel.Size = new Size(494, 419);
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
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, processToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 25);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { openImageToolStripMenuItem });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(39, 21);
            toolStripMenuItem1.Text = "File";
            // 
            // processToolStripMenuItem
            // 
            processToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { binaryToolStripMenuItem });
            processToolStripMenuItem.Name = "processToolStripMenuItem";
            processToolStripMenuItem.Size = new Size(65, 21);
            processToolStripMenuItem.Text = "Process";
            // 
            // binaryToolStripMenuItem
            // 
            binaryToolStripMenuItem.Name = "binaryToolStripMenuItem";
            binaryToolStripMenuItem.Size = new Size(180, 22);
            binaryToolStripMenuItem.Text = "Binary";
            binaryToolStripMenuItem.Click += binaryToolStripMenuItem_Click;
            // 
            // openImageToolStripMenuItem
            // 
            openImageToolStripMenuItem.Name = "openImageToolStripMenuItem";
            openImageToolStripMenuItem.Size = new Size(180, 22);
            openImageToolStripMenuItem.Text = "Open Image";
            openImageToolStripMenuItem.Click += openImageToolStripMenuItem_Click;
            // 
            // LayerViewerForm
            // 
            ClientSize = new Size(800, 450);
            Controls.Add(mainLayoutPanel);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "LayerViewerForm";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            mainLayoutPanel.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem processToolStripMenuItem;
        private ToolStripMenuItem binaryToolStripMenuItem;
        private ToolStripMenuItem openImageToolStripMenuItem;
    }
} 