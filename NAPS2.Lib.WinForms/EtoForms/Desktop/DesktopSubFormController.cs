using NAPS2.EtoForms.Ui;
using NAPS2.Images;
using NAPS2.Ocr;
using NAPS2.WinForms;

namespace NAPS2.EtoForms.Desktop;

public class DesktopSubFormController : IDesktopSubFormController
{
    private readonly IFormFactory _formFactory;
    private readonly UiImageList _imageList;
    private readonly DesktopImagesController _desktopImagesController;
    private readonly TesseractLanguageManager _tesseractLanguageManager;

    public DesktopSubFormController(IFormFactory formFactory, UiImageList imageList,
        DesktopImagesController desktopImagesController, TesseractLanguageManager tesseractLanguageManager)
    {
        _formFactory = formFactory;
        _imageList = imageList;
        _desktopImagesController = desktopImagesController;
        _tesseractLanguageManager = tesseractLanguageManager;
    }

    public void ShowCropForm() => ShowImageForm<CropForm>();
    public void ShowBrightnessContrastForm() => ShowImageForm<BrightContForm>();
    public void ShowHueSaturationForm() => ShowImageForm<HueSatForm>();
    public void ShowBlackWhiteForm() => ShowImageForm<BlackWhiteForm>();
    public void ShowSharpenForm() => ShowImageForm<SharpenForm>();
    public void ShowRotateForm() => ShowImageForm<RotateForm>();

    private void ShowImageForm<T>() where T : ImageFormBase
    {
        var selection = _imageList.Selection;
        if (selection.Any())
        {
            var form = _formFactory.Create<T>();
            form.Image = selection.First();
            form.SelectedImages = selection.ToList();
            form.ShowModal();
        }
    }

    public void ShowProfilesForm()
    {
        var form = _formFactory.Create<ProfilesForm>();
        form.ImageCallback = _desktopImagesController.ReceiveScannedImage();
        form.ShowModal();
    }

    public void ShowOcrForm()
    {
        if (_tesseractLanguageManager.InstalledLanguages.Any())
        {
            _formFactory.Create<OcrSetupForm>().ShowModal();
        }
        else
        {
            _formFactory.Create<FOcrLanguageDownload>().ShowDialog();
            if (_tesseractLanguageManager.InstalledLanguages.Any())
            {
                _formFactory.Create<OcrSetupForm>().ShowModal();
            }
        }
    }

    public void ShowBatchScanForm()
    {
        var form = _formFactory.Create<FBatchScan>();
        form.ImageCallback = _desktopImagesController.ReceiveScannedImage();
        form.ShowDialog();
    }

    public void ShowViewerForm()
    {
        var selected = _imageList.Selection.FirstOrDefault();
        if (selected != null)
        {
            using var viewer = _formFactory.Create<PreviewForm>();
            viewer.CurrentImage = selected;
            viewer.ShowModal();
        }
    }

    public void ShowPdfSettingsForm()
    {
        _formFactory.Create<FPdfSettings>().ShowDialog();
    }

    public void ShowImageSettingsForm()
    {
        _formFactory.Create<FImageSettings>().ShowDialog();
    }

    public void ShowEmailSettingsForm()
    {
        _formFactory.Create<FEmailSettings>().ShowDialog();
    }

    public void ShowAboutForm()
    {
        _formFactory.Create<AboutForm>().ShowModal();
    }

    public void ShowSettingsForm()
    {
        // FormFactory.Create<FSettings>().ShowDialog();
    }
}