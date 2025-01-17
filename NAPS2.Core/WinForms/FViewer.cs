using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAPS2.Config;
using NAPS2.Lang.Resources;
using NAPS2.Operation;
using NAPS2.Platform;
using NAPS2.Scan.Images;
using NAPS2.Util;

using NAPS2.Recovery;
using NAPS2.Scan;
using NAPS2.Scan.Exceptions;

using NAPS2.Scan.Wia;
using NAPS2.Scan.Wia.Native;

namespace NAPS2.WinForms
{
    public class FViewer : FormBase
    {
        private readonly Container components = null;
        private ToolStripContainer toolStripContainer1;
        private ToolStrip toolStrip1;
        private ToolStripTextBox tbPageCurrent;
        private ToolStripLabel lblPageTotal;
        private ToolStripButton tsPrev;
        private ToolStripButton tsNext;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripDropDownButton tsdRotate;
        private ToolStripMenuItem tsRotateLeft;
        private ToolStripMenuItem tsRotateRight;
        private ToolStripMenuItem tsFlip;
        private ToolStripMenuItem tsCustomRotation;
        private ToolStripButton tsCrop;
        private ToolStripButton tsBrightnessContrast;
        private ToolStripButton tsDelete;
        private TiffViewerCtl tiffViewer1;
        private ToolStripMenuItem tsDeskew;
        private readonly ChangeTracker changeTracker;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton tsSavePDF;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton tsSaveImage;
        private readonly IOperationFactory operationFactory;
        private readonly WinFormsExportHelper exportHelper;
        private readonly AppConfigManager appConfigManager;
        private ToolStripButton tsHueSaturation;
        private ToolStripButton tsBlackWhite;
        private ToolStripButton tsSharpen;
        private readonly ScannedImageRenderer scannedImageRenderer;
        private readonly KeyboardShortcutManager ksm;
        private readonly IUserConfigManager userConfigManager;
        private ToolStrip toolStrip2;
        private ToolStripButton Scan;
        private ToolStripButton toolStripButton1;
        private ToolStripButton toolStripButton2;
        private ToolStripButton toolStripButton3;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private readonly IOperationProgress operationProgress;

        //CC Use features from the main interfaces (Scanning)
        private FDesktop fDesktop;
        

        public FViewer(FDesktop fDesktop, ChangeTracker changeTracker, IOperationFactory operationFactory, WinFormsExportHelper exportHelper, AppConfigManager appConfigManager, ScannedImageRenderer scannedImageRenderer, KeyboardShortcutManager ksm, IUserConfigManager userConfigManager, IOperationProgress operationProgress)
        {
            this.changeTracker = changeTracker;
            this.operationFactory = operationFactory;
            this.exportHelper = exportHelper;
            this.appConfigManager = appConfigManager;
            this.scannedImageRenderer = scannedImageRenderer;
            this.ksm = ksm;
            this.userConfigManager = userConfigManager;
            this.operationProgress = operationProgress;
            this.fDesktop = fDesktop;
            InitializeComponent();
        }

        public ScannedImageList ImageList { get; set; }
        public int ImageIndex { get; set; }
        public Action DeleteCallback { get; set; }
        public Action<int> SelectCallback { get; set; }
        public FDesktop Fdesktop { get; set; }


        protected override async void OnLoad(object sender, EventArgs e)
        {
            tbPageCurrent.Visible = PlatformCompat.Runtime.IsToolbarTextboxSupported;
            if (appConfigManager.Config.HideSavePdfButton)
            {
                toolStrip1.Items.Remove(tsSavePDF);
            }
            if (appConfigManager.Config.HideSaveImagesButton)
            {
                toolStrip1.Items.Remove(tsSaveImage);
            }

            AssignKeyboardShortcuts();
            UpdatePage();
            await UpdateImage();
        }

        private async Task GoTo(int index)
        {
            if (index == ImageIndex || index < 0 || index >= ImageList.Images.Count)
            {
                return;
            }
            ImageIndex = index;
            UpdatePage();
            SelectCallback(index);
            await UpdateImage();
        }

        private void UpdatePage()
        {
            tbPageCurrent.Text = (ImageIndex + 1).ToString(CultureInfo.CurrentCulture);
            lblPageTotal.Text = string.Format(MiscResources.OfN, ImageList.Images.Count);
            
            // Get the informations about the current image and display in the status bar
            String text2 = ImageList.Images[ImageIndex].infoResolution;
            String text3 = ImageList.Images[ImageIndex].BarCodeData;
            String text4 = "";
            if (text3 != "")
                text4 = "Barcode: " + text3;
            String format = ImageList.Images[ImageIndex].infoFormat;
            
            toolStripStatusLabel1.Text = "Size: " + text2 + " - " + text4 + " - " + format;

            if (!PlatformCompat.Runtime.IsToolbarTextboxSupported)
            {
                lblPageTotal.Text = tbPageCurrent.Text + ' ' + lblPageTotal.Text;
            }
        }

        private async Task UpdateImage()
        {
            tiffViewer1.Image?.Dispose();
            tiffViewer1.Image = null;
            var newImage = await scannedImageRenderer.Render(ImageList.Images[ImageIndex]);
            tiffViewer1.Image = newImage;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                tiffViewer1?.Image?.Dispose();
                tiffViewer1?.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FViewer));
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tiffViewer1 = new NAPS2.WinForms.TiffViewerCtl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tbPageCurrent = new System.Windows.Forms.ToolStripTextBox();
            this.lblPageTotal = new System.Windows.Forms.ToolStripLabel();
            this.tsPrev = new System.Windows.Forms.ToolStripButton();
            this.tsNext = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsdRotate = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsRotateLeft = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRotateRight = new System.Windows.Forms.ToolStripMenuItem();
            this.tsFlip = new System.Windows.Forms.ToolStripMenuItem();
            this.tsDeskew = new System.Windows.Forms.ToolStripMenuItem();
            this.tsCustomRotation = new System.Windows.Forms.ToolStripMenuItem();
            this.tsCrop = new System.Windows.Forms.ToolStripButton();
            this.tsBrightnessContrast = new System.Windows.Forms.ToolStripButton();
            this.tsHueSaturation = new System.Windows.Forms.ToolStripButton();
            this.tsBlackWhite = new System.Windows.Forms.ToolStripButton();
            this.tsSharpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsSavePDF = new System.Windows.Forms.ToolStripButton();
            this.tsSaveImage = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.Scan = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.tiffViewer1);
            resources.ApplyResources(this.toolStripContainer1.ContentPanel, "toolStripContainer1.ContentPanel");
            resources.ApplyResources(this.toolStripContainer1, "toolStripContainer1");
            this.toolStripContainer1.Name = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip2);
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Name = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            resources.ApplyResources(this.toolStripStatusLabel1, "toolStripStatusLabel1");
            // 
            // tiffViewer1
            // 
            resources.ApplyResources(this.tiffViewer1, "tiffViewer1");
            this.tiffViewer1.Image = null;
            this.tiffViewer1.Name = "tiffViewer1";
            this.tiffViewer1.Load += new System.EventHandler(this.tiffViewer1_Load);
            this.tiffViewer1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tiffViewer1_KeyDown);
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbPageCurrent,
            this.lblPageTotal,
            this.tsPrev,
            this.tsNext,
            this.toolStripSeparator1,
            this.tsdRotate,
            this.tsCrop,
            this.tsBrightnessContrast,
            this.tsHueSaturation,
            this.tsBlackWhite,
            this.tsSharpen,
            this.toolStripSeparator3,
            this.tsSavePDF,
            this.tsSaveImage,
            this.toolStripSeparator2,
            this.tsDelete});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // tbPageCurrent
            // 
            resources.ApplyResources(this.tbPageCurrent, "tbPageCurrent");
            this.tbPageCurrent.Name = "tbPageCurrent";
            this.tbPageCurrent.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbPageCurrent_KeyDown);
            this.tbPageCurrent.TextChanged += new System.EventHandler(this.tbPageCurrent_TextChanged);
            // 
            // lblPageTotal
            // 
            this.lblPageTotal.Name = "lblPageTotal";
            resources.ApplyResources(this.lblPageTotal, "lblPageTotal");
            // 
            // tsPrev
            // 
            this.tsPrev.Image = global::NAPS2.Icons.arrow_left1;
            resources.ApplyResources(this.tsPrev, "tsPrev");
            this.tsPrev.Name = "tsPrev";
            this.tsPrev.Click += new System.EventHandler(this.tsPrev_Click);
            // 
            // tsNext
            // 
            this.tsNext.Image = global::NAPS2.Icons.arrow_right1;
            resources.ApplyResources(this.tsNext, "tsNext");
            this.tsNext.Name = "tsNext";
            this.tsNext.Click += new System.EventHandler(this.tsNext_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // tsdRotate
            // 
            this.tsdRotate.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRotateLeft,
            this.tsRotateRight,
            this.tsFlip,
            this.tsDeskew,
            this.tsCustomRotation});
            this.tsdRotate.Image = global::NAPS2.Icons.arrow_rotate_anticlockwise;
            resources.ApplyResources(this.tsdRotate, "tsdRotate");
            this.tsdRotate.Name = "tsdRotate";
            this.tsdRotate.ShowDropDownArrow = false;
            // 
            // tsRotateLeft
            // 
            this.tsRotateLeft.Image = global::NAPS2.Icons.arrow_rotate_anticlockwise;
            this.tsRotateLeft.Name = "tsRotateLeft";
            resources.ApplyResources(this.tsRotateLeft, "tsRotateLeft");
            this.tsRotateLeft.Click += new System.EventHandler(this.tsRotateLeft_Click);
            // 
            // tsRotateRight
            // 
            this.tsRotateRight.Image = global::NAPS2.Icons.arrow_rotate_clockwise;
            this.tsRotateRight.Name = "tsRotateRight";
            resources.ApplyResources(this.tsRotateRight, "tsRotateRight");
            this.tsRotateRight.Click += new System.EventHandler(this.tsRotateRight_Click);
            // 
            // tsFlip
            // 
            this.tsFlip.Image = global::NAPS2.Icons.arrow_switch;
            this.tsFlip.Name = "tsFlip";
            resources.ApplyResources(this.tsFlip, "tsFlip");
            this.tsFlip.Click += new System.EventHandler(this.tsFlip_Click);
            // 
            // tsDeskew
            // 
            this.tsDeskew.Image = global::NAPS2.Icons.transform_shear;
            this.tsDeskew.Name = "tsDeskew";
            resources.ApplyResources(this.tsDeskew, "tsDeskew");
            this.tsDeskew.Click += new System.EventHandler(this.tsDeskew_Click);
            // 
            // tsCustomRotation
            // 
            this.tsCustomRotation.Name = "tsCustomRotation";
            resources.ApplyResources(this.tsCustomRotation, "tsCustomRotation");
            this.tsCustomRotation.Click += new System.EventHandler(this.tsCustomRotation_Click);
            // 
            // tsCrop
            // 
            this.tsCrop.Image = global::NAPS2.Icons.transform_crop1;
            resources.ApplyResources(this.tsCrop, "tsCrop");
            this.tsCrop.Name = "tsCrop";
            this.tsCrop.Click += new System.EventHandler(this.tsCrop_Click);
            // 
            // tsBrightnessContrast
            // 
            this.tsBrightnessContrast.Image = global::NAPS2.Icons.color_adjustment;
            resources.ApplyResources(this.tsBrightnessContrast, "tsBrightnessContrast");
            this.tsBrightnessContrast.Name = "tsBrightnessContrast";
            this.tsBrightnessContrast.Click += new System.EventHandler(this.tsBrightnessContrast_Click);
            // 
            // tsHueSaturation
            // 
            this.tsHueSaturation.Image = global::NAPS2.Icons.color_management1;
            resources.ApplyResources(this.tsHueSaturation, "tsHueSaturation");
            this.tsHueSaturation.Name = "tsHueSaturation";
            this.tsHueSaturation.Click += new System.EventHandler(this.tsHueSaturation_Click);
            // 
            // tsBlackWhite
            // 
            this.tsBlackWhite.Image = global::NAPS2.Icons.contrast_high1;
            resources.ApplyResources(this.tsBlackWhite, "tsBlackWhite");
            this.tsBlackWhite.Name = "tsBlackWhite";
            this.tsBlackWhite.Click += new System.EventHandler(this.tsBlackWhite_Click);
            // 
            // tsSharpen
            // 
            this.tsSharpen.Image = global::NAPS2.Icons.sharpen1;
            resources.ApplyResources(this.tsSharpen, "tsSharpen");
            this.tsSharpen.Name = "tsSharpen";
            this.tsSharpen.Click += new System.EventHandler(this.tsSharpen_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // tsSavePDF
            // 
            this.tsSavePDF.Image = global::NAPS2.Icons.file_extension_pdf;
            resources.ApplyResources(this.tsSavePDF, "tsSavePDF");
            this.tsSavePDF.Name = "tsSavePDF";
            this.tsSavePDF.Click += new System.EventHandler(this.tsSavePDF_Click);
            // 
            // tsSaveImage
            // 
            this.tsSaveImage.Image = global::NAPS2.Icons.picture_save;
            resources.ApplyResources(this.tsSaveImage, "tsSaveImage");
            this.tsSaveImage.Name = "tsSaveImage";
            this.tsSaveImage.Click += new System.EventHandler(this.tsSaveImage_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // tsDelete
            // 
            this.tsDelete.Image = global::NAPS2.Icons.cross;
            resources.ApplyResources(this.tsDelete, "tsDelete");
            this.tsDelete.Name = "tsDelete";
            this.tsDelete.Click += new System.EventHandler(this.tsDelete_Click);
            // 
            // toolStrip2
            // 
            resources.ApplyResources(this.toolStrip2, "toolStrip2");
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Scan,
            this.toolStripButton1,
            this.toolStripButton2,
            this.toolStripButton3});
            this.toolStrip2.Name = "toolStrip2";
            // 
            // Scan
            // 
            this.Scan.Image = global::NAPS2.Icons.control_play_blue;
            resources.ApplyResources(this.Scan, "Scan");
            this.Scan.Name = "Scan";
            this.Scan.Click += new System.EventHandler(this.tsScan_ButtonClick);
            // 
            // toolStripButton1
            // 
            resources.ApplyResources(this.toolStripButton1, "toolStripButton1");
            this.toolStripButton1.Image = global::NAPS2.Icons.arrow_repeat;
            this.toolStripButton1.Name = "toolStripButton1";
            // 
            // toolStripButton2
            // 
            resources.ApplyResources(this.toolStripButton2, "toolStripButton2");
            this.toolStripButton2.Image = global::NAPS2.Icons.add1;
            this.toolStripButton2.Name = "toolStripButton2";
            // 
            // toolStripButton3
            // 
            resources.ApplyResources(this.toolStripButton3, "toolStripButton3");
            this.toolStripButton3.Image = global::NAPS2.Icons.arrow_undo;
            this.toolStripButton3.Name = "toolStripButton3";
            // 
            // FViewer
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "FViewer";
            this.ShowInTaskbar = false;
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private async void tbPageCurrent_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(tbPageCurrent.Text, out int indexOffBy1))
            {
                await GoTo(indexOffBy1 - 1);
            }
        }

        private async void tsNext_Click(object sender, EventArgs e)
        {
            await GoTo(ImageIndex + 1);
        }

        private async void tsPrev_Click(object sender, EventArgs e)
        {
            await GoTo(ImageIndex - 1);
        }

        private async void tsRotateLeft_Click(object sender, EventArgs e)
        {
            await ImageList.RotateFlip(Enumerable.Range(ImageIndex, 1), RotateFlipType.Rotate270FlipNone);
            await UpdateImage();
        }

        private async void tsRotateRight_Click(object sender, EventArgs e)
        {
            await ImageList.RotateFlip(Enumerable.Range(ImageIndex, 1), RotateFlipType.Rotate90FlipNone);
            await UpdateImage();
        }

        private async void tsFlip_Click(object sender, EventArgs e)
        {
            await ImageList.RotateFlip(Enumerable.Range(ImageIndex, 1), RotateFlipType.Rotate180FlipNone);
            await UpdateImage();
        }

        private async void tsDeskew_Click(object sender, EventArgs e)
        {
            var op = operationFactory.Create<DeskewOperation>();
            if (op.Start(new[] { ImageList.Images[ImageIndex] }))
            {
                operationProgress.ShowProgress(op);
                await UpdateImage();
            }
        }

        private async void tsCustomRotation_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FRotate>();
            form.Image = ImageList.Images[ImageIndex];
            form.ShowDialog();
            await UpdateImage();
        }

        private async void tsCrop_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FCrop>();
            form.Image = ImageList.Images[ImageIndex];
            form.ShowDialog();
            await UpdateImage();
        }

        private async void tsBrightnessContrast_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FBrightnessContrast>();
            form.Image = ImageList.Images[ImageIndex];
            form.ShowDialog();
            await UpdateImage();
        }

        private async void tsHueSaturation_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FHueSaturation>();
            form.Image = ImageList.Images[ImageIndex];
            form.ShowDialog();
            await UpdateImage();
        }

        private async void tsBlackWhite_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FBlackWhite>();
            form.Image = ImageList.Images[ImageIndex];
            form.ShowDialog();
            await UpdateImage();
        }

        private async void tsSharpen_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FSharpen>();
            form.Image = ImageList.Images[ImageIndex];
            form.ShowDialog();
            await UpdateImage();
        }

        private async void tsDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(string.Format(MiscResources.ConfirmDeleteItems, 1), MiscResources.Delete, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                await DeleteCurrentImage();
            }
        }

        private async Task DeleteCurrentImage()
        {
            // Need to dispose the bitmap first to avoid file access issues
            tiffViewer1.Image?.Dispose();
            // Actually delete the image
            ImageList.Delete(Enumerable.Range(ImageIndex, 1));
            // Update FDesktop in the background
            DeleteCallback();

            if (ImageList.Images.Any())
            {
                changeTracker.Made();
                // Update the GUI for the newly displayed image
                if (ImageIndex >= ImageList.Images.Count)
                {
                    await GoTo(ImageList.Images.Count - 1);
                }
                else
                {
                    await UpdateImage();
                }
                lblPageTotal.Text = string.Format(MiscResources.OfN, ImageList.Images.Count);
            }
            else
            {
                changeTracker.Clear();
                // No images left to display, so no point keeping the form open
                Close();
            }
        }

        private async void tsSavePDF_Click(object sender, EventArgs e)
        {
            if (await exportHelper.SavePDF(new List<ScannedImage> { ImageList.Images[ImageIndex] }, null))
            {
                if (appConfigManager.Config.DeleteAfterSaving)
                {
                    await DeleteCurrentImage();
                }
            }
        }

        private async void tsSaveImage_Click(object sender, EventArgs e)
        {
            if (await exportHelper.SaveImages(new List<ScannedImage> { ImageList.Images[ImageIndex] }, null))
            {
                if (appConfigManager.Config.DeleteAfterSaving)
                {
                    await DeleteCurrentImage();
                }
            }
        }

        private async void tiffViewer1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Control || e.Shift || e.Alt))
            {
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        Close();
                        return;
                    case Keys.PageDown:
                    case Keys.Right:
                    case Keys.Down:
                        await GoTo(ImageIndex + 1);
                        return;
                    case Keys.PageUp:
                    case Keys.Left:
                    case Keys.Up:
                        await GoTo(ImageIndex - 1);
                        return;
                }
            }

            ksm.Perform(e);
        }

        private async void tbPageCurrent_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Control || e.Shift || e.Alt))
            {
                switch (e.KeyCode)
                {
                    case Keys.PageDown:
                    case Keys.Right:
                    case Keys.Down:
                        await GoTo(ImageIndex + 1);
                        return;
                    case Keys.PageUp:
                    case Keys.Left:
                    case Keys.Up:
                        await GoTo(ImageIndex - 1);
                        return;
                }
            }

            ksm.Perform(e);
        }

        private void AssignKeyboardShortcuts()
        {
            // Defaults

            ksm.Assign("Del", tsDelete);

            // Configured

            var ks = userConfigManager.Config.KeyboardShortcuts ?? appConfigManager.Config.KeyboardShortcuts ?? new KeyboardShortcuts();

            ksm.Assign(ks.Delete, tsDelete);
            ksm.Assign(ks.ImageBlackWhite, tsBlackWhite);
            ksm.Assign(ks.ImageBrightness, tsBrightnessContrast);
            ksm.Assign(ks.ImageContrast, tsBrightnessContrast);
            ksm.Assign(ks.ImageCrop, tsCrop);
            ksm.Assign(ks.ImageHue, tsHueSaturation);
            ksm.Assign(ks.ImageSaturation, tsHueSaturation);
            ksm.Assign(ks.ImageSharpen, tsSharpen);

            ksm.Assign(ks.RotateCustom, tsCustomRotation);
            ksm.Assign(ks.RotateFlip, tsFlip);
            ksm.Assign(ks.RotateLeft, tsRotateLeft);
            ksm.Assign(ks.RotateRight, tsRotateRight);
            ksm.Assign(ks.SaveImages, tsSaveImage);
            ksm.Assign(ks.SavePDF, tsSavePDF);
        }

        private void tiffViewer1_Load(object sender, EventArgs e)
        {

        }

        private async void tsScan_ButtonClick(object sender, EventArgs e)
        {
            // The feature of the button work as it call the function. But the function cannot work in the context.
            // Need rework to work in this context.
            await fDesktop.ScanDefault();
        }

    }
}
