using NUnit.Framework;
using DfTools.Desktop.Models;
using DfTools.Desktop.Services;
using DfTools.Desktop.Converters;
using DfTools.Desktop.ViewModels;
using DfTools.Diff;

namespace DfTools.Tests.DesktopTests;

[TestFixture]
public class DesktopAppTests
{
    [Test]
    public void AppSettings_HasDefaultValues()
    {
        var settings = new AppSettings();
        Assert.That(settings.Theme.BackgroundColor, Is.EqualTo("#000000"));
        Assert.That(settings.Text.FormatButtonText, Is.EqualTo("[F5] FORMAT"));
        Assert.That(settings.DiffTool.DefaultOldText, Is.Not.Empty);
    }

    [Test]
    public void GeneratedAppSettings_HasValuesFromAppSettingsJson()
    {
        var generated = DfTools.Desktop.Generated.GeneratedAppSettings.Default;
        Assert.That(generated, Is.Not.Null);
        Assert.That(generated.Text.AppTitle, Is.EqualTo("DFTOOLS"));
        Assert.That(generated.Theme.BackgroundColor, Is.EqualTo("#000000"));
        Assert.That(generated.SqlFormatter.IndentString, Is.EqualTo("  "));
    }

    [Test]
    public void SettingsService_CanLoadAndSaveSettings()
    {
        var service = new SettingsService();
        var settings = service.LoadSettings();
        Assert.That(settings, Is.Not.Null);

        var saved = service.SaveSettings(settings);
        Assert.That(saved, Is.True);
    }

    [Test]
    public void IntEqualsConverter_EvaluatesCorrectly()
    {
        var converter = IntEqualsConverter.Instance;
        Assert.That(converter.Convert(0, typeof(bool), "0", System.Globalization.CultureInfo.InvariantCulture), Is.True);
        Assert.That(converter.Convert(0, typeof(bool), "1", System.Globalization.CultureInfo.InvariantCulture), Is.False);
    }

    [Test]
    public void MainViewModel_CompareDiff_ComputesDiffResult()
    {
        var vm = new MainViewModel();
        vm.DiffOldInput = "Line A";
        vm.DiffNewInput = "Line B";
        vm.CompareDiff();

        Assert.That(vm.DiffResult, Is.Not.Null);
        Assert.That(vm.DiffResult!.HasDifferences, Is.True);
        Assert.That(vm.IsDiffEditMode, Is.False);
    }

    [Test]
    public void DiffChangeTypeToBrushConverter_ReturnsBrushesForChangeTypes()
    {
        var converter = DiffChangeTypeToBrushConverter.Instance;

        var bgBrush = converter.Convert(DiffChangeType.Inserted, typeof(Avalonia.Media.IBrush), "Background", System.Globalization.CultureInfo.InvariantCulture);
        var fgBrush = converter.Convert(DiffChangeType.Inserted, typeof(Avalonia.Media.IBrush), "Foreground", System.Globalization.CultureInfo.InvariantCulture);

        Assert.That(bgBrush, Is.Not.Null);
        Assert.That(fgBrush, Is.Not.Null);
    }
}
