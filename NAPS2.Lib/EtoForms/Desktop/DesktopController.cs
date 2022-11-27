using System.Threading;
using Eto.Forms;
using NAPS2.ImportExport;
using NAPS2.ImportExport.Images;
using NAPS2.Platform.Windows;
using NAPS2.Recovery;
using NAPS2.Remoting;
using NAPS2.Scan;
using NAPS2.Update;

namespace NAPS2.EtoForms.Desktop;

// TODO: We undoubtedly want to decompose this file even further.
// We almost certainly want a DesktopScanController for the scanning-related logic.
// We could have a DesktopPipesController that depends on DesktopScanController.
// Specifically each line in Initialize might make sense as a sub-controller.
// We also need to think about how to pass the Form instance around as needed. (e.g. to Activate it). Maybe this should be something injectable, and could also be used by UpdateOperation instead of searching through open forms.
// i.e. (I)DesktopFormProvider
public class DesktopController
{
    private readonly ScanningContext _scanningContext;
    private readonly UiImageList _imageList;
    private readonly RecoveryStorageManager _recoveryStorageManager;
    private readonly ThumbnailController _thumbnailController;
    private readonly OperationProgress _operationProgress;
    private readonly Naps2Config _config;
    private readonly IOperationFactory _operationFactory;
    private readonly StillImage _stillImage;
    private readonly IUpdateChecker _updateChecker;
    private readonly INotificationManager _notify;
    private readonly ImageTransfer _imageTransfer;
    private readonly ImageClipboard _imageClipboard;
    private readonly ImageListActions _imageListActions;
    private readonly IExportController _exportController;
    private readonly DialogHelper _dialogHelper;
    private readonly DesktopImagesController _desktopImagesController;
    private readonly IDesktopScanController _desktopScanController;
    private readonly DesktopFormProvider _desktopFormProvider;
    private readonly IScannedImagePrinter _scannedImagePrinter;

    private bool _closed;
    private bool _initialized;
    private bool _suspended;

    public DesktopController(ScanningContext scanningContext, UiImageList imageList,
        RecoveryStorageManager recoveryStorageManager, ThumbnailController thumbnailController,
        OperationProgress operationProgress, Naps2Config config, IOperationFactory operationFactory,
        StillImage stillImage,
        IUpdateChecker updateChecker, INotificationManager notify, ImageTransfer imageTransfer,
        ImageClipboard imageClipboard, ImageListActions imageListActions, IExportController exportController,
        DialogHelper dialogHelper,
        DesktopImagesController desktopImagesController, IDesktopScanController desktopScanController,
        DesktopFormProvider desktopFormProvider, IScannedImagePrinter scannedImagePrinter)
    {
        _scanningContext = scanningContext;
        _imageList = imageList;
        _recoveryStorageManager = recoveryStorageManager;
        _thumbnailController = thumbnailController;
        _operationProgress = operationProgress;
        _config = config;
        _operationFactory = operationFactory;
        _stillImage = stillImage;
        _updateChecker = updateChecker;
        _notify = notify;
        _imageTransfer = imageTransfer;
        _imageClipboard = imageClipboard;
        _imageListActions = imageListActions;
        _exportController = exportController;
        _dialogHelper = dialogHelper;
        _desktopImagesController = desktopImagesController;
        _desktopScanController = desktopScanController;
        _desktopFormProvider = desktopFormProvider;
        _scannedImagePrinter = scannedImagePrinter;
    }

    public bool SkipRecoveryCleanup { get; set; }

    public async Task Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        StartPipesServer();
        ShowStartupMessages();
        ShowRecoveryPrompt();
        InitThumbnailRendering();
        await RunStillImageEvents();
        SetFirstRunDate();
        ShowDonationPrompt();
        ShowUpdatePrompt();
    }

    private void ShowDonationPrompt()
    {
        // Show a donation prompt after a month of use
#if !INSTALLER_MSI
        if (!_config.Get(c => c.HiddenButtons).HasFlag(ToolbarButtons.Donate) &&
            !_config.Get(c => c.HasBeenPromptedForDonation) &&
            DateTime.Now - _config.Get(c => c.FirstRunDate) > TimeSpan.FromDays(30))
        {
            var transact = _config.User.BeginTransaction();
            transact.Set(c => c.HasBeenPromptedForDonation, true);
            transact.Set(c => c.LastDonatePromptDate, DateTime.Now);
            transact.Commit();
            _notify.DonatePrompt();
        }
#endif
    }

    private void ShowUpdatePrompt()
    {
#if !INSTALLER_MSI
        if (_config.Get(c => c.CheckForUpdates) &&
            !_config.Get(c => c.NoUpdatePrompt) &&
            (!_config.Get(c => c.HasCheckedForUpdates) ||
             _config.Get(c => c.LastUpdateCheckDate) < DateTime.Now - UpdateChecker.CheckInterval))
        {
            _updateChecker.CheckForUpdates().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Log.ErrorException("Error checking for updates", task.Exception!);
                }
                else
                {
                    var transact = _config.User.BeginTransaction();
                    transact.Set(c => c.HasCheckedForUpdates, true);
                    transact.Set(c => c.LastUpdateCheckDate, DateTime.Now);
                    transact.Commit();
                }
                var update = task.Result;
                if (update != null)
                {
                    _notify.UpdateAvailable(_updateChecker, update);
                }
            }).AssertNoAwait();
        }
#endif
    }

    private void SetFirstRunDate()
    {
        if (!_config.Get(c => c.HasBeenRun))
        {
            var transact = _config.User.BeginTransaction();
            transact.Set(c => c.HasBeenRun, true);
            transact.Set(c => c.FirstRunDate, DateTime.Now);
            transact.Commit();
        }
    }

    private async Task RunStillImageEvents()
    {
        // If NAPS2 was started by the scanner button, do the appropriate actions automatically
        if (_stillImage.ShouldScan)
        {
            await _desktopScanController.ScanWithDevice(_stillImage.DeviceID!);
        }
    }

    public void Cleanup()
    {
        if (_suspended) return;
        Pipes.KillServer();
        if (!SkipRecoveryCleanup)
        {
            try
            {
                // TODO: Figure out and fix undisposed processed images
                _scanningContext.Dispose();
                _recoveryStorageManager.Dispose();
            }
            catch (Exception ex)
            {
                Log.ErrorException("Recovery cleanup failed", ex);
            }
        }
        _closed = true;
        _thumbnailController.Dispose();
    }

    public bool PrepareForClosing(bool userClosing)
    {
        if (_closed) return true;

        if (_operationProgress.ActiveOperations.Any())
        {
            if (userClosing)
            {
                if (_operationProgress.ActiveOperations.Any(x => !x.SkipExitPrompt))
                {
                    var result = MessageBox.Show(_desktopFormProvider.DesktopForm,
                        MiscResources.ExitWithActiveOperations,
                        MiscResources.ActiveOperations,
                        MessageBoxButtons.YesNo, MessageBoxType.Warning, MessageBoxDefaultButton.No);
                    if (result != DialogResult.Yes)
                    {
                        return false;
                    }
                }
            }
            else
            {
                SkipRecoveryCleanup = true;
            }
        }
        else if (_imageList.Images.Any() && _imageList.SavedState != _imageList.CurrentState)
        {
            if (userClosing && !SkipRecoveryCleanup)
            {
                var result = MessageBox.Show(_desktopFormProvider.DesktopForm, MiscResources.ExitWithUnsavedChanges,
                    MiscResources.UnsavedChanges,
                    MessageBoxButtons.YesNo, MessageBoxType.Warning, MessageBoxDefaultButton.No);
                if (result != DialogResult.Yes)
                {
                    return false;
                }
                _imageList.SavedState = _imageList.CurrentState;
            }
            else
            {
                SkipRecoveryCleanup = true;
            }
        }

        if (_operationProgress.ActiveOperations.Any())
        {
            _operationProgress.ActiveOperations.ForEach(op => op.Cancel());
            _desktopFormProvider.DesktopForm.Visible = false;
            _desktopFormProvider.DesktopForm.ShowInTaskbar = false;
            Task.Run(() =>
            {
                var timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));
                try
                {
                    _operationProgress.ActiveOperations.ForEach(op => op.Wait(timeoutCts.Token));
                }
                catch (OperationCanceledException)
                {
                }
                _closed = true;
                Invoker.Current.SafeInvoke(_desktopFormProvider.DesktopForm.Close);
            });
            return false;
        }

        return true;
    }

    private void StartPipesServer()
    {
        // Receive messages from other processes
        Pipes.StartServer(msg =>
        {
            if (msg.StartsWith(Pipes.MSG_SCAN_WITH_DEVICE, StringComparison.InvariantCulture))
            {
                Invoker.Current.SafeInvoke(async () =>
                    await _desktopScanController.ScanWithDevice(msg.Substring(Pipes.MSG_SCAN_WITH_DEVICE.Length)));
            }
            if (msg.Equals(Pipes.MSG_ACTIVATE))
            {
                Invoker.Current.SafeInvoke(() =>
                {
                    // TODO: xplat
                    var formOnTop = Application.Instance.Windows.Last();
                    if (formOnTop.WindowState == WindowState.Minimized)
                    {
                        Win32.ShowWindow(formOnTop.NativeHandle, Win32.ShowWindowCommands.Restore);
                    }
                    formOnTop.BringToFront();
                });
            }
        });
    }

    private void ShowStartupMessages()
    {
        // If configured (e.g. by a business), show a customizable message box on application startup.
        if (!string.IsNullOrWhiteSpace(_config.Get(c => c.StartupMessageText)))
        {
            MessageBox.Show(_config.Get(c => c.StartupMessageText), _config.Get(c => c.StartupMessageTitle),
                MessageBoxButtons.OK,
                _config.Get(c => c.StartupMessageIcon).ToEto());
        }
    }

    private void ShowRecoveryPrompt()
    {
        // Allow scanned images to be recovered in case of an unexpected close
        // TODO: Eto implementation
        // var op = _operationFactory.Create<RecoveryOperation>();
        // if (op.Start(_desktopImagesController.ReceiveScannedImage(),
        //         new RecoveryParams { ThumbnailSize = _config.ThumbnailSize() }))
        // {
        //     _operationProgress.ShowProgress(op);
        // }
    }

    private void InitThumbnailRendering()
    {
        _thumbnailController.Init(_imageList);
    }

    public void ImportFiles(IEnumerable<string> files)
    {
        var op = _operationFactory.Create<ImportOperation>();
        if (op.Start(OrderFiles(files), _desktopImagesController.ReceiveScannedImage(),
                new ImportParams { ThumbnailSize = _thumbnailController.RenderSize }))
        {
            _operationProgress.ShowProgress(op);
        }
    }

    private List<string> OrderFiles(IEnumerable<string> files)
    {
        // Custom ordering to account for numbers so that e.g. "10" comes after "2"
        var filesList = files.ToList();
        filesList.Sort(new NaturalStringComparer());
        return filesList;
    }

    public void ImportDirect(ImageTransferData data, bool copy)
    {
        var op = _operationFactory.Create<DirectImportOperation>();
        if (op.Start(data, copy, _desktopImagesController.ReceiveScannedImage(),
                new DirectImportParams { ThumbnailSize = _thumbnailController.RenderSize }))
        {
            _operationProgress.ShowProgress(op);
        }
    }

    public void Paste()
    {
        if (_imageTransfer.IsInClipboard())
        {
            ImportDirect(_imageTransfer.GetFromClipboard(), true);
        }
    }

    public async Task Copy()
    {
        using var imagesToCopy = _imageList.Selection.Select(x => x.GetClonedImage()).ToDisposableList();
        await _imageClipboard.Write(imagesToCopy.InnerList, true);
    }


    public void Clear()
    {
        if (_imageList.Images.Count > 0)
        {
            if (MessageBox.Show(_desktopFormProvider.DesktopForm,
                    string.Format(MiscResources.ConfirmClearItems, _imageList.Images.Count),
                    MiscResources.Clear, MessageBoxButtons.OKCancel,
                    MessageBoxType.Question) == DialogResult.Ok)
            {
                _imageListActions.DeleteAll();
            }
        }
    }

    public void Delete()
    {
        // TODO: Move to ImageListActions and use a null parent form
        if (_imageList.Selection.Any())
        {
            if (MessageBox.Show(_desktopFormProvider.DesktopForm,
                    string.Format(MiscResources.ConfirmDeleteItems, _imageList.Selection.Count),
                    MiscResources.Delete, MessageBoxButtons.OKCancel,
                    MessageBoxType.Question) == DialogResult.Ok)
            {
                _imageListActions.DeleteSelected();
            }
        }
    }

    public void RunDocumentCorrection()
    {
        foreach (var image in _imageList.Selection)
        {
            image.AddTransform(new CorrectionTransform(CorrectionMode.Document));
        }
    }

    public void ResetImage()
    {
        if (_imageList.Selection.Any())
        {
            if (MessageBox.Show(_desktopFormProvider.DesktopForm,
                    string.Format(MiscResources.ConfirmResetImages, _imageList.Selection.Count),
                    MiscResources.ResetImage,
                    MessageBoxButtons.OKCancel, MessageBoxType.Question) == DialogResult.Ok)
            {
                _imageListActions.ResetTransforms();
            }
        }
    }

    public async Task SavePdf()
    {
        var action = _config.Get(c => c.SaveButtonDefaultAction);

        if (action == SaveButtonDefaultAction.AlwaysPrompt
            || action == SaveButtonDefaultAction.PromptIfSelected && _imageList.Selection.Any())
        {
            // TODO
            // tsdSavePDF.ShowDropDown();
        }
        else if (action == SaveButtonDefaultAction.SaveSelected && _imageList.Selection.Any())
        {
            await _imageListActions.SaveSelectedAsPdf();
        }
        else
        {
            await _imageListActions.SaveAllAsPdf();
        }
    }

    public async Task SaveImages()
    {
        var action = _config.Get(c => c.SaveButtonDefaultAction);

        if (action == SaveButtonDefaultAction.AlwaysPrompt
            || action == SaveButtonDefaultAction.PromptIfSelected && _imageList.Selection.Any())
        {
            // TODO
            // tsdSaveImages.ShowDropDown();
        }
        else if (action == SaveButtonDefaultAction.SaveSelected && _imageList.Selection.Any())
        {
            await _imageListActions.SaveSelectedAsImages();
        }
        else
        {
            await _imageListActions.SaveAllAsImages();
        }
    }

    public async Task EmailPdf()
    {
        var action = _config.Get(c => c.SaveButtonDefaultAction);

        if (action == SaveButtonDefaultAction.AlwaysPrompt
            || action == SaveButtonDefaultAction.PromptIfSelected && _imageList.Selection.Any())
        {
            // TODO
            // tsdEmailPDF.ShowDropDown();
        }
        else if (action == SaveButtonDefaultAction.SaveSelected && _imageList.Selection.Any())
        {
            await _imageListActions.EmailSelectedAsPdf();
        }
        else
        {
            await _imageListActions.EmailAllAsPdf();
        }
    }

    public async Task Print()
    {
        var state = _imageList.CurrentState;
        using var allImages = _imageList.Images.Select(x => x.GetClonedImage()).ToDisposableList();
        using var selectedImages = _imageList.Selection.Select(x => x.GetClonedImage()).ToDisposableList();
        if (await _scannedImagePrinter.PromptToPrint(allImages.InnerList, selectedImages.InnerList))
        {
            _imageList.SavedState = state;
        }
    }

    public void Import()
    {
        if (_dialogHelper.PromptToImport(out var fileNames))
        {
            ImportFiles(fileNames!);
        }
    }

    public void Suspend()
    {
        _suspended = true;
    }

    public void Resume()
    {
        _suspended = false;
    }
}