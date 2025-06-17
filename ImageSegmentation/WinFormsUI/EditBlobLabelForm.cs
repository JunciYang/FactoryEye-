using System;
using System.Windows.Forms;

namespace ImageSegmentation.WinFormsUI
{
    public partial class EditBlobLabelForm : Form
    {
        public string NewLabelName { get; private set; }

        public EditBlobLabelForm(string currentLabel)
        {
            InitializeComponent();
            tBNewLabelName.Text = currentLabel;
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            btnOK.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(tBNewLabelName.Text))
                {
                    NewLabelName = tBNewLabelName.Text;
                    DialogResult = DialogResult.OK;
                }
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
            };

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
} 