using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAPS2.Config.Experimental;
using NAPS2.ImportExport.Images;
using NAPS2.ImportExport.Pdf;
using NAPS2.Lang.Resources;
using NAPS2.Logging;
using NAPS2.Ocr;
using NAPS2.Operation;
using NAPS2.Scan;
using NAPS2.Images;
using NAPS2.Images.Storage;
using NAPS2.Util;
using NAPS2.WinForms;

namespace NAPS2.ImportExport
{
    public class AutoSaver
    {
        private readonly ConfigProvider<PdfSettings> pdfSettingsProvider;
        private readonly ConfigProvider<ImageSettings> imageSettingsProvider;
        private readonly OcrEngineManager ocrEngineManager;
        private readonly OcrRequestQueue ocrRequestQueue;
        private readonly ErrorOutput errorOutput;
        private readonly DialogHelper dialogHelper;
        private readonly OperationProgress operationProgress;
        private readonly ISaveNotify notify;
        private readonly PdfExporter pdfExporter;
        private readonly OverwritePrompt overwritePrompt;
        private readonly BitmapRenderer bitmapRenderer;
        private readonly ConfigProvider<CommonConfig> configProvider;

        public AutoSaver()
        {
            // TODO: Need to make SDK provider use safe for null defaults (maybe a helper method that reuses a subset of InternalDefaults somehow)
            pdfSettingsProvider = new StubConfigProvider<PdfSettings>(new PdfSettings());
            imageSettingsProvider = new StubConfigProvider<ImageSettings>(new ImageSettings());
            ocrEngineManager = OcrEngineManager.Default;
            ocrRequestQueue = OcrRequestQueue.Default;
            errorOutput = ErrorOutput.Default;
            dialogHelper = DialogHelper.Default;
            operationProgress = OperationProgress.Default;
            notify = null;
            pdfExporter = PdfExporter.Default;
            overwritePrompt = OverwritePrompt.Default;
            bitmapRenderer = new BitmapRenderer(ImageContext.Default);
            configProvider = ConfigScopes.Current.Provider;
        }

        public AutoSaver(ConfigProvider<PdfSettings> pdfSettingsProvider, ConfigProvider<ImageSettings> imageSettingsProvider, OcrEngineManager ocrEngineManager, OcrRequestQueue ocrRequestQueue, ErrorOutput errorOutput, DialogHelper dialogHelper, OperationProgress operationProgress, ISaveNotify notify, PdfExporter pdfExporter, OverwritePrompt overwritePrompt, BitmapRenderer bitmapRenderer, ConfigProvider<CommonConfig> configProvider)
        {
            this.pdfSettingsProvider = pdfSettingsProvider;
            this.imageSettingsProvider = imageSettingsProvider;
            this.ocrEngineManager = ocrEngineManager;
            this.ocrRequestQueue = ocrRequestQueue;
            this.errorOutput = errorOutput;
            this.dialogHelper = dialogHelper;
            this.operationProgress = operationProgress;
            this.notify = notify;
            this.pdfExporter = pdfExporter;
            this.overwritePrompt = overwritePrompt;
            this.bitmapRenderer = bitmapRenderer;
            this.configProvider = configProvider;
        }

        public ScannedImageSource Save(AutoSaveSettings settings, ScannedImageSource source)
        {
            var sink = new ScannedImageSink();

            if (!settings.ClearImagesAfterSaving)
            {
                // Basic auto save, so keep track of images as we pipe them and try to auto save afterwards
                var imageList = new List<ScannedImage>();
                source.ForEach(img =>
                {
                    sink.PutImage(img);
                    imageList.Add(img);
                }).ContinueWith(async t =>
                {
                    try
                    {
                        await InternalSave(settings, imageList);
                        if (t.IsFaulted && t.Exception != null)
                        {
                            sink.SetError(t.Exception.InnerException);
                        }
                    }
                    finally
                    {
                        sink.SetCompleted();
                    }
                });
                return sink.AsSource();
            }

            // Auto save without piping images
            // TODO: This may fail if PropagateErrors is true
            source.ToList().ContinueWith(async t =>
            {
                if (await InternalSave(settings, t.Result))
                {
                    foreach (ScannedImage img in t.Result)
                    {
                        img.Dispose();
                    }
                }
                else
                {
                    // Fallback in case auto save failed; pipe all the images back at once
                    foreach (ScannedImage img in t.Result)
                    {
                        sink.PutImage(img);
                    }
                }

                sink.SetCompleted();
            });
            return sink.AsSource();
        }

        private async Task<bool> InternalSave(AutoSaveSettings settings, List<ScannedImage> images)
        {
            try
            {
                bool ok = true;
                var placeholders = Placeholders.All.WithDate(DateTime.Now);
                int i = 0;
                string firstFileSaved = null;
                var scans = SaveSeparatorHelper.SeparateScans(new[] { images }, settings.Separator).ToList();
                foreach (var imageList in scans)
                {
                    (bool success, string filePath) = await SaveOneFile(settings, placeholders, i++, imageList, scans.Count == 1);
                    if (!success)
                    {
                        ok = false;
                    }
                    if (success && firstFileSaved == null)
                    {
                        firstFileSaved = filePath;
                    }
                }
                // TODO: Shouldn't this give duplicate notifications?
                if (notify != null && scans.Count > 1 && ok)
                {
                    // Can't just do images.Count because that includes patch codes
                    int imageCount = scans.SelectMany(x => x).Count();
                    notify.ImagesSaved(imageCount, firstFileSaved);
                }
                return ok;
            }
            catch (Exception ex)
            {
                Log.ErrorException(MiscResources.AutoSaveError, ex);
                errorOutput.DisplayError(MiscResources.AutoSaveError, ex);
                return false;
            }
        }
        
        private async Task<(bool, string)> SaveOneFile(AutoSaveSettings settings, Placeholders placeholders, int i, List<ScannedImage> images, bool doNotify)
        {
            if (images.Count == 0)
            {
                return (true, null);
            }
            string subPath = placeholders.Substitute(settings.FilePath, true, i);
            if (settings.PromptForFilePath)
            {
                if (dialogHelper.PromptToSavePdfOrImage(subPath, out string newPath))
                {
                    subPath = placeholders.Substitute(newPath, true, i);
                }
            }
            var extension = Path.GetExtension(subPath);
            if (extension != null && extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
            {
                if (File.Exists(subPath))
                {
                    subPath = placeholders.Substitute(subPath, true, 0, 1);
                }
                var op = new SavePdfOperation(pdfExporter, overwritePrompt);
                var ocrContext = new OcrContext(configProvider.DefaultOcrParams(), ocrEngineManager, ocrRequestQueue);
                if (op.Start(subPath, placeholders, images, pdfSettingsProvider, ocrContext))
                {
                    operationProgress.ShowProgress(op);
                }
                bool success = await op.Success;
                if (success && doNotify)
                {
                    notify?.PdfSaved(subPath);
                }
                return (success, subPath);
            }
            else
            {
                var op = new SaveImagesOperation(overwritePrompt, bitmapRenderer, new TiffHelper(bitmapRenderer));
                if (op.Start(subPath, placeholders, images, imageSettingsProvider))
                {
                    operationProgress.ShowProgress(op);
                }
                bool success = await op.Success;
                if (success && doNotify)
                {
                    notify?.ImagesSaved(images.Count, op.FirstFileSaved);
                }
                return (success, subPath);
            }
        }
    }
}