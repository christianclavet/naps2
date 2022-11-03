using System.Collections.ObjectModel;
using NAPS2.App.Tests.Verification;
using OpenQA.Selenium.Appium.Windows;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace NAPS2.App.Tests.Appium;

[Collection("appium")]
public class LanguageSelectionTests : AppiumTests
{
    // TODO: Verify why zh-TW isn't here (and that hi still hasn't been translated)
    private static readonly HashSet<string> ExpectedMissingLanguages = new() { "zh-TW", "hi" };

    [VerifyFact]
    public void OpenLanguageDropdown()
    {
        // Open the Language dropdown
        ClickAtName("Language");
        var menuItems = GetMenuItems();
    
        // Verify all expected languages have menu items
        var menuItemTexts = menuItems.Select(x => x.Text).ToHashSet();
        var allLanguages = GetAllLanguages();
        var missingLanguages = allLanguages
            .Where(x => !menuItemTexts.Contains(x.langName) && !ExpectedMissingLanguages.Contains(x.langCode)).ToList();
        Assert.True(missingLanguages.Count == 0, $"Missing languages: {string.Join(",", missingLanguages)}");
    
        // Verify French (fr) translation as a standard language example
        ClickAtName("Français");
        ClickAtName("Langue");
        
        // Verify Portuguese (pt-BR) translation as a country-specific language example
        ClickAtName("Português (Brasil)");
        ClickAtName("Idioma");
        
        // Verify Hebrew translation as a RTL language example
        ClickAtName("עברית");
        // Toggling RTL causes a new window to be created
        ResetMainWindow();
        ClickAtName("שפה");
    
        // And back to English
        ClickAtName("English");
        ResetMainWindow();
        ClickAtName("Language");
    
        AppTestHelper.AssertNoErrorLog(FolderPath);
    }

    private List<(string langCode, string langName)> GetAllLanguages()
    {
        return new CultureHelper(Naps2Config.Stub()).GetAllCultures().ToList();
    }

    private ReadOnlyCollection<WindowsElement> GetMenuItems()
    {
        return _session.FindElementsByTagName("MenuItem");
    }

    // TODO: As part of language switching, check several languages (one LTR: fr, one RTL: he, one country-specific LTR: pt-BR); and check several strings (from each NAPS2.Sdk, NAPS2.Lib, NAPS2.Lib.WinForms)
}