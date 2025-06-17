using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageSegmentation.WinFormsUI
{
    public partial class ThresholdAdjustForm : Form
    {
        private Image originalImage;
        private PictureBox previewBox;
        private TrackBar thresholdTrackBar;
        private Label thresholdLabel;
        private Button okButton;
        private Button cancelButton;
        private int selectedThreshold;

        public int SelectedThreshold => selectedThreshold;

        public ThresholdAdjustForm(Image image)
        {
            InitializeComponent();
            originalImage = image;
            selectedThreshold = 128; // 默认阈值
            InitializeControls();
            ApplyThreshold(selectedThreshold);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ThresholdAdjustForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "ThresholdAdjustForm";
            this.Text = "Threshold Adjustment";
            this.ResumeLayout(false);
        }

        private void InitializeControls()
        {
            // 预览图片框
            previewBox = new PictureBox
            {
                Location = new Point(12, 12),
                Size = new Size(600, 400),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 阈值滑动条
            thresholdTrackBar = new TrackBar
            {
                Location = new Point(12, 430),
                Size = new Size(600, 45),
                Maximum = 255,
                Value = selectedThreshold,
                TickFrequency = 25
            };
            thresholdTrackBar.ValueChanged += ThresholdTrackBar_ValueChanged;

            // 阈值标签
            thresholdLabel = new Label
            {
                Location = new Point(12, 480),
                Size = new Size(100, 20),
                Text = $"阈值: {selectedThreshold}"
            };

            // OK按钮
            okButton = new Button
            {
                Location = new Point(500, 520),
                Size = new Size(100, 30),
                Text = "OK",
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            // Cancel按钮
            cancelButton = new Button
            {
                Location = new Point(620, 520),
                Size = new Size(100, 30),
                Text = "Cancel",
                DialogResult = DialogResult.Cancel
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] { 
                previewBox, 
                thresholdTrackBar, 
                thresholdLabel, 
                okButton, 
                cancelButton 
            });
        }

        private void ThresholdTrackBar_ValueChanged(object sender, EventArgs e)
        {
            selectedThreshold = thresholdTrackBar.Value;
            thresholdLabel.Text = $"阈值: {selectedThreshold}";
            ApplyThreshold(selectedThreshold);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ApplyThreshold(int threshold)
        {
            if (originalImage == null) return;

            using (var bitmap = new Bitmap(originalImage))
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        int gray = (int)((pixel.R * 0.3) + (pixel.G * 0.59) + (pixel.B * 0.11));
                        Color newColor = gray > threshold ? Color.White : Color.Black;
                        bitmap.SetPixel(x, y, newColor);
                    }
                }

                previewBox.Image = bitmap;
            }
        }
    }
} 