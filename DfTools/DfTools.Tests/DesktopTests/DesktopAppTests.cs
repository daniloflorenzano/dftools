using NUnit.Framework;
using DfTools.Desktop.Models;
using DfTools.Desktop.Services;
using DfTools.Desktop.Converters;

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
}
