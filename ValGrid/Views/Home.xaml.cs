using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FontAwesome6.Fonts;
using ValGrid.Helpers;
using ValGrid.ViewModels;

namespace ValGrid.Views;

public partial class Home : UserControl
{
    public static ImageAwesome ValorantStatus;
    public static ImageAwesome AccountStatus;
    public static ImageAwesome MatchStatus;

    public Home()
    {
        InitializeComponent();
        DataContextChanged += DataContextChangedHandler;

        ValorantStatus = ValorantStatusView;
        AccountStatus = AccountStatusView;
        MatchStatus = MatchStatusView;

        Loaded += (s, e) =>
        {
            ApplyLocalization();
            var win = Window.GetWindow(this);
            if (win != null)
            {
                win.PreviewKeyDown += (ws, we) =>
                {
                    if (we.Key == System.Windows.Input.Key.Escape)
                    {
                        if (PlayerListOverlay != null && PlayerListOverlay.Visibility == Visibility.Visible)
                        {
                            PlayerListOverlay.Visibility = Visibility.Collapsed;
                            PlayerListOverlay.IsHitTestVisible = false;
                            we.Handled = true;
                            return;
                        }

                        if (CareerOverlay != null && CareerOverlay.Visibility == Visibility.Visible)
                        {
                            CareerOverlay.Visibility = Visibility.Collapsed;
                            CareerOverlay.IsHitTestVisible = false;
                            we.Handled = true;
                            return;
                        }

                        if (StoreOverlay != null && StoreOverlay.Visibility == Visibility.Visible)
                        {
                            StoreOverlay.CloseStore();
                            we.Handled = true;
                            return;
                        }
                    }
                };
            }
        };
    }

    private void ApplyLocalization()
    {
        try
        {
            if (YourPartyTitleText != null) YourPartyTitleText.Text = L10n.Get("YourParty");
            if (StatusTitleText != null) StatusTitleText.Text = L10n.Get("Status");

            if (SettingsNavBtn != null) SettingsNavBtn.ToolTip = L10n.Get("NavSettingsTooltip");
            if (InfoNavBtn != null) InfoNavBtn.ToolTip = L10n.Get("NavInfoTooltip");

            if (PlayerListNavText != null) PlayerListNavText.Text = L10n.Get("ListButton");
            if (PlayerListNavBtn != null) PlayerListNavBtn.ToolTip = L10n.Get("ListToolTip");

            if (CareerNavText != null) CareerNavText.Text = L10n.Get("CareerButton");
            if (CareerNavBtn != null) CareerNavBtn.ToolTip = L10n.Get("CareerToolTip");

            if (DailyStoreNavText != null) DailyStoreNavText.Text = L10n.Get("StoreButton");
            if (DailyStoreNavBtn != null) DailyStoreNavBtn.ToolTip = L10n.Get("StoreToolTip");

            if (StatusValorantLabel != null) StatusValorantLabel.Text = L10n.Get("ValorantStatus");
            if (StatusAccountLabel != null) StatusAccountLabel.Text = L10n.Get("AccountStatus");
            if (StatusMatchLabel != null) StatusMatchLabel.Text = L10n.Get("MatchStatus");

            if (RefreshTitleLabel != null) RefreshTitleLabel.Text = L10n.Get("Refresh");
            if (RefreshingInLabel != null) RefreshingInLabel.Text = L10n.Get("RefreshingIn");

            if (EmptyPartyTitleText != null) EmptyPartyTitleText.Text = L10n.Get("EmptyPartyTitle");
            if (EmptyPartyDescText != null) EmptyPartyDescText.Text = L10n.Get("EmptyPartyDesc");
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ApplyLocalization failed in Home");
        }
    }

    private void OpenPlayerList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Constants.Log?.Information("OpenPlayerList_Click in Home triggered. PlayerListOverlay is null? {n}", PlayerListOverlay == null);
            if (PlayerListOverlay != null)
            {
                PlayerListOverlay.IsHitTestVisible = true;
                PlayerListOverlay.Visibility = Visibility.Visible;
                PlayerListOverlay.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenPlayerList_Click failed in Home");
        }
    }

    private void OpenCareer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Constants.Log?.Information("OpenCareer_Click in Home triggered. CareerOverlay is null? {n}", CareerOverlay == null);
            if (CareerOverlay != null)
            {
                CareerOverlay.IsHitTestVisible = true;
                CareerOverlay.Visibility = Visibility.Visible;
                CareerOverlay.RefreshMatches();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenCareer_Click failed in Home");
        }
    }

    private void OpenStore_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Constants.Log?.Information("OpenStore_Click in Home triggered. StoreOverlay is null? {n}", StoreOverlay == null);
            if (StoreOverlay != null)
            {
                StoreOverlay.IsHitTestVisible = true;
                StoreOverlay.Visibility = Visibility.Visible;
                StoreOverlay.LoadStore();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenStore_Click failed in Home");
        }
    }

    private void DataContextChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not HomeViewModel viewModel)
            return;
        viewModel.GoMatchEvent += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (GoMatch.Command.CanExecute(null))
                    GoMatch.Command.Execute(null);
            });
        };
    }
}

