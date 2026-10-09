using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using ValGrid.Helpers;

namespace ValGrid.Views;

public partial class Info : UserControl
{
    public Info()
    {
        InitializeComponent();
        Loaded += (s, e) => ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        try
        {
            if (InfoHomeBtnText != null) InfoHomeBtnText.Text = L10n.Get("HomeTitle");
            if (InfoHomeBtn != null) InfoHomeBtn.ToolTip = L10n.Get("HomeTitle");
            if (InfoSettingsBtn != null) InfoSettingsBtn.ToolTip = L10n.Get("NavSettingsTooltip");
            if (InfoAppSubtitleText != null) InfoAppSubtitleText.Text = L10n.Get("InfoAppSubtitle");
            if (InfoVersionText != null) InfoVersionText.Text = Constants.AppVersion;
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ApplyLocalization failed in Info");
        }
    }

    private void HandleLinkClickAsync(object sender, RequestNavigateEventArgs e)
    {
        var link = (Hyperlink)sender;
        var navigateUri = link.NavigateUri.ToString();
        Process.Start(new ProcessStartInfo(navigateUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void ImageClickAsync(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        Process.Start(new ProcessStartInfo(button.Tag.ToString()) { UseShellExecute = true });
        e.Handled = true;
    }
}

