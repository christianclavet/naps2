﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAPS2.Config;
using NAPS2.ImportExport;
using NAPS2.Operation;
using NAPS2.Recovery;
using NAPS2.Scan.Images;
using NAPS2.WinForms;
using NAPS2;
using CsvHelper;
using NAPS2.Util;
using NAPS2.ImportExport.Images;
using System.IO;

namespace NAPS2.WinForms
{
    public partial class FExport : FormBase
    {
        private readonly FileNamePlaceholders fileNamePlaceholders;
        private readonly WinFormsExportHelper exportHelper;
        private readonly FDesktop fdesktop;
        private readonly ChangeTracker changeTracker;
        private readonly DialogHelper dialogHelper;
        private string filename;
        private readonly RecoveryIndex recoveryIndex;

        public ImageSettingsContainer imageSettingsContainer;

        public readonly ImageSettingsContainer imageSettingsContainer;

        public FExport(FDesktop fdesktop, FileNamePlaceholders fileNamePlaceholders, WinFormsExportHelper exportHelper, DialogHelper dialogHelper, ChangeTracker changeTracker, ImageSettingsContainer imageSettingsContainer, RecoveryIndex recoveryIndex)
        {
            this.fileNamePlaceholders = fileNamePlaceholders;
            this.exportHelper = exportHelper;
            this.fdesktop = fdesktop;
            this.changeTracker = changeTracker;
            this.dialogHelper = dialogHelper;
            this.imageSettingsContainer = imageSettingsContainer;
            this.recoveryIndex = recoveryIndex;
            InitializeComponent();
            
        }

        public string projectName { get; set; }

        public NotificationManager notify { get; set; }

        public void SetData(ImageSettings imageSettings)
        {
            imageSettingsContainer.ImageSettings = imageSettings;
        }

        public void SetName(string name) 
        {
            projectName = UserConfigManager.Config.project;
            tb_ExportPath.Text = "$(nnnnnnnn).jpg";
            //filename = fdesktop.imageSettings.DefaultFileName+tb_ExportPath.Text;
           /* if (name == null) 
            {
                name = fdesktop.imageSettings.CSVFileName;
            }

            tb_exportFilename.Text = name + ".csv";
            cb_CSVEnabler.Checked = fdesktop.imageSettings.UseCSVExport;
            if (fdesktop.CSVExpression == null) 
            {
                tb_CSVExpression.Text = "TEST,$(barcode),$(sheetside),$(filename)"; 
            } else 
            { 
                tb_CSVExpression.Text = fdesktop.imageSettings.CSVExpression;
            }
            */
        }

        private void BTN_File_Click(object sender, EventArgs e)
        {
            if (dialogHelper.PromptToSaveImage(tb_ExportPath.Text, out string newPath))
            {
                tb_ExportPath.Text = newPath;
                filename = newPath;
            }

        }

        private void btn_Expression_Click(object sender, EventArgs e)
        {
            var form = FormFactory.Create<FPlaceholders>();
            form.FileName = tb_ExportPath.Text;
            if (form.ShowDialog() == DialogResult.OK)
            {
                tb_ExportPath.Text = form.FileName;
            }
        }

        private void tb_ExportPath_TextChanged(object sender, EventArgs e)
        {
            filename = tb_ExportPath.Text;
            var fileExample = fileNamePlaceholders.SubstitutePlaceholders(tb_ExportPath.Text, DateTime.Now, true);
            var file = Path.GetFileName(fileExample);
            fileExample = Path.Combine(Path.GetDirectoryName(fileExample), projectName);
            fileExample = Path.Combine(fileExample,file);
            LBL_Exemple.Text = fileExample;
        }

        private void cb_CSVEnabler_CheckedChanged(object sender, EventArgs e)
        {
            this.groupBox1.Enabled = cb_CSVEnabler.Checked;
        }

        private void tb_CSVExpression_TextChanged(object sender, EventArgs e)
        {
            string text = tb_CSVExpression.Text.Replace("$(filename)", fileNamePlaceholders.SubstitutePlaceholders(filename, DateTime.Now, true));
            text = text.Replace("$(barcode)", "1234-5678");
            text = text.Replace("$(sheetside)", "1=front-2=back");
            lbl_meta.Text = text;
        }

        private void tb_exportFilename_TextChanged(object sender, EventArgs e)
        {

        }

        private void BTN_Cancel_Click(object sender, EventArgs e)
        {
            imageSettingsContainer.ImageSettings = new ImageSettings
            {
                UseCSVExport = false,
                SkipSavePrompt = false,
            };
            Close();
        }

        private void BTN_Export_Click(object sender, EventArgs e)
        {
            imageSettingsContainer.ImageSettings = new ImageSettings
            {
                ProjectName = this.projectName,
                DefaultFileName = tb_ExportPath.Text,
                CSVExpression = tb_CSVExpression.Text,
                CSVFileName = tb_exportFilename.Text,
                SkipSavePrompt = true,
                UseCSVExport = cb_CSVEnabler.Checked,
            };

<<<<<<< HEAD
            //Return this to the main prg
            fdesktop.setImageContainer.ImageSettings = imageSettingsContainer.ImageSettings;

            //SaveImages(imagesList.Images);
            /*
            imageSettingsContainer.ImageSettings = new ImageSettings
            {
                UseCSVExport = false,
                SkipSavePrompt = false,
            };
            */
=======
            fdesktop.imageSettingsContainer = imageSettingsContainer; //Try to push back the content to the desktop class
>>>>>>> 50491094c8dd15bd32cf61a44879111e7dd764c0

            Close();
        }

        private async void SaveImages(List<ScannedImage> images)
        {            
            if (await exportHelper.SaveImages(images, notify))
            {

               changeTracker.Made();
               
            }
        }
    }
}
