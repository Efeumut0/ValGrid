using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using ValGrid.Helpers;
using ValGrid.Objects;

namespace ValGrid.Controls;

public partial class PlayerControl : UserControl
{
    /// <summary>
    /// Static flag: true while any NotePopup is open in the match view.
    /// MatchViewModel checks this to defer refresh.
    /// </summary>
    internal static bool IsAnyNotePopupOpen { get; set; } = false;

    public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(
        "PlayerCell",
        typeof(Player),
        typeof(PlayerControl),
        new PropertyMetadata(new Player(), OnPlayerChanged)
    );

    private static void OnPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PlayerControl pc && e.NewValue is Player p)
        {
            p.RefreshPlayerTags();
        }
    }

    public PlayerControl()
    {
        InitializeComponent();
        AddHandler(
            InventoryControl.CloseButtonEvent,
            new RoutedEventHandler(ClosePopupEventHandlerMethod)
        );
        Loaded += (s, e) =>
        {
            PlayerCell?.RefreshPlayerTags();
            FemaleTagManager.FemaleTagsChanged -= OnFemaleTagsChanged;
            FemaleTagManager.FemaleTagsChanged += OnFemaleTagsChanged;
            MaleTagManager.MaleTagsChanged -= OnMaleTagsChanged;
            MaleTagManager.MaleTagsChanged += OnMaleTagsChanged;
            BlacklistManager.BlacklistChanged -= OnBlacklistChanged;
            BlacklistManager.BlacklistChanged += OnBlacklistChanged;
        };
        Unloaded += (s, e) =>
        {
            FemaleTagManager.FemaleTagsChanged -= OnFemaleTagsChanged;
            MaleTagManager.MaleTagsChanged -= OnMaleTagsChanged;
            BlacklistManager.BlacklistChanged -= OnBlacklistChanged;
        };
    }

    private void OnFemaleTagsChanged()
    {
        Dispatcher.InvokeAsync(() => PlayerCell?.RefreshFemaleStatus());
    }

    private void OnMaleTagsChanged()
    {
        Dispatcher.InvokeAsync(() => PlayerCell?.RefreshMaleStatus());
    }

    private void OnBlacklistChanged()
    {
        Dispatcher.InvokeAsync(() => PlayerCell?.RefreshBlacklistStatus());
    }

    public Player PlayerCell
    {
        get => (Player)GetValue(PlayerProperty);
        set => SetValue(PlayerProperty, value);
    }

    private void HandleLinkClick(object sender, RequestNavigateEventArgs e)
    {
        var hl = (Hyperlink)sender;
        var navigateUri = hl.NavigateUri.ToString();
        Process.Start(new ProcessStartInfo(navigateUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void FemaleButton_Click(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = (s?.Tag as Player) ?? PlayerCell ?? (s?.DataContext as Player);
        if (player != null)
        {
            player.IsFemale = !player.IsFemale;
            if (player.IsFemale && player.IsMale)
            {
                player.IsMale = false;
            }
            var puuid = player.PlayerUiData?.Puuid ?? Guid.Empty;
            var username = player.IgnData?.Username;
            FemaleTagManager.SetFemale(puuid, username, player.IsFemale);
        }
        e.Handled = true;
    }

    private void MaleButton_Click(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = (s?.Tag as Player) ?? PlayerCell ?? (s?.DataContext as Player);
        if (player != null)
        {
            player.IsMale = !player.IsMale;
            if (player.IsMale && player.IsFemale)
            {
                player.IsFemale = false;
            }
            var puuid = player.PlayerUiData?.Puuid ?? Guid.Empty;
            var username = player.IgnData?.Username;
            MaleTagManager.SetMale(puuid, username, player.IsMale);
        }
        e.Handled = true;
    }

    private void BlacklistButton_Click(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = PlayerCell ?? (s?.DataContext as Player);
        if (player != null)
        {
            player.IsBlacklisted = !player.IsBlacklisted;
            var puuid = player.PlayerUiData?.Puuid ?? Guid.Empty;
            var username = player.IgnData?.Username;
            BlacklistManager.SetBlacklisted(puuid, username, player.IsBlacklisted);
        }
        e.Handled = true;
    }

    private bool _isCopyingPlayerInv = false;

    private async void CopyPlayerInventory_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (_isCopyingPlayerInv) return;
        _isCopyingPlayerInv = true;

        try
        {
            var btn = sender as FrameworkElement;
            var player = (btn?.Tag as Player) ?? PlayerCell ?? (btn?.DataContext as Player);
            if (player == null) return;

            var displayName = FormatPlayerDisplayName(player, this);

            // Normal Sol Tık: İsim ve Skinleri Kopyala
            // Shift + Sol Tık: Sadece Oyuncu İsmini (# Etiketiyle) Kopyala
            bool shiftOnlyName = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            var textToCopy = shiftOnlyName
                ? displayName
                : InventoryControl.BuildInventorySkinsSummary(player.SkinData, displayName, includeUsername: true, includeValue: false);

            var success = await ClipboardHelper.SetTextAsync(textToCopy);

            await ShowCopyFeedbackAsync(btn, success ? (shiftOnlyName ? "İsim Kopyalandı!" : "İsim ve Silahlar Kopyalandı!") : "Kopyalama başarısız!");
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopyPlayerInventory_Click failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingPlayerInv = false;
        }
    }

    private async void PlayerName_RightClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var fe = sender as FrameworkElement;
        var player = (fe?.DataContext as Player) ?? PlayerCell;
        if (player == null) return;

        var cleanName = FormatPlayerDisplayName(player, this);
        var success = await ClipboardHelper.SetTextAsync(cleanName);
        if (CopyPlayerInvBtn != null)
        {
            await ShowCopyFeedbackAsync(CopyPlayerInvBtn, success ? $"'{cleanName}' Kopyalandı!" : "Kopyalama başarısız!");
        }
    }

    private async void CopyPlayerInventory_MiddleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (_isCopyingPlayerInv) return;
        _isCopyingPlayerInv = true;

        try
        {
            var btn = sender as FrameworkElement;
            var player = (btn?.Tag as Player) ?? PlayerCell ?? (btn?.DataContext as Player);
            if (player == null) return;

            var displayName = FormatPlayerDisplayName(player, this);
            var textToCopy = InventoryControl.BuildInventorySkinsSummary(player.SkinData, displayName, includeUsername: true, includeValue: true);

            var success = await ClipboardHelper.SetTextAsync(textToCopy);

            await ShowCopyFeedbackAsync(btn, success ? "Değerli (VP & TL) Envanter Kopyalandı!" : "Kopyalama başarısız!");
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopyPlayerInventory_MiddleClick failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingPlayerInv = false;
        }
    }

    private void CopyPlayerInventory_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            CopyPlayerInventory_MiddleClick(sender, e);
        }
    }

    private async void CopyPlayerInventory_RightClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (_isCopyingPlayerInv) return;
        _isCopyingPlayerInv = true;

        try
        {
            var btn = sender as FrameworkElement;
            var player = (btn?.Tag as Player) ?? PlayerCell ?? (btn?.DataContext as Player);
            if (player == null) return;

            var displayName = FormatPlayerDisplayName(player, this);
            var textToCopy = InventoryControl.BuildInventoryBuddiesSummary(player.SkinData, displayName, includeUsername: true);

            var success = await ClipboardHelper.SetTextAsync(textToCopy);

            await ShowCopyFeedbackAsync(btn, success ? "Uğurluklar Kopyalandı!" : "Kopyalama başarısız!");
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopyPlayerInventory_RightClick failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingPlayerInv = false;
        }
    }

    private async System.Threading.Tasks.Task ShowCopyFeedbackAsync(FrameworkElement btn, string tooltipText)
    {
        if (CopyPlayerInvNormalState != null && CopyPlayerInvSuccessState != null)
        {
            CopyPlayerInvNormalState.Visibility = Visibility.Collapsed;
            CopyPlayerInvSuccessState.Visibility = Visibility.Visible;
            if (btn != null)
            {
                btn.ToolTip = tooltipText;
            }
            await System.Threading.Tasks.Task.Delay(1500);
            CopyPlayerInvSuccessState.Visibility = Visibility.Collapsed;
            CopyPlayerInvNormalState.Visibility = Visibility.Visible;
            if (btn != null)
            {
                btn.ToolTip = "Sol Tık: İsim ve Skinleri Kopyala\nShift + Sol Tık: Sadece Oyuncu İsmini Kopyala\nOrta Tık: Toplam Değerli (VP & TL) Kopyala\nSağ Tık: Sadece Uğurlukları Kopyala";
            }
        }
    }

    private void ButtonUpHandler(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = s?.DataContext as Player;
        if (player == null) return;

        var win = Window.GetWindow(this);
        if (win != null)
        {
            popup.Width = win.ActualWidth > 0 ? win.ActualWidth : win.Width;
            popup.Height = win.ActualHeight > 0 ? win.ActualHeight : win.Height;
            popup.PlacementTarget = win;
        }

        var displayName = FormatPlayerDisplayName(player, this);
        popup.Child = new InventoryControl(player.SkinData, displayName);
        popup.IsOpen = true;
        e.Handled = true;
    }

    public static string FormatPlayerDisplayName(Player player, FrameworkElement element = null)
    {
        if (player == null) return "oyuncu";

        var username = player.IgnData?.Username;
        if (!string.IsNullOrWhiteSpace(username) && username != "----")
        {
            var clean = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
            if (!string.IsNullOrWhiteSpace(clean))
                return clean;
        }

        var agentName = !string.IsNullOrWhiteSpace(player.IdentityData?.Name)
            ? player.IdentityData.Name.ToLowerInvariant()
            : "oyuncu";

        bool? isOurTeam = null;

        if (element != null)
        {
            var vm = FindMatchViewModel(element);
            if (vm != null)
            {
                if (vm.LeftPlayerList != null && vm.LeftPlayerList.Contains(player))
                    isOurTeam = true;
                else if (vm.RightPlayerList != null && vm.RightPlayerList.Contains(player))
                    isOurTeam = false;
            }
        }

        if (!isOurTeam.HasValue && player.PlayerUiData != null && player.PlayerUiData.Puuid == Constants.Ppuuid)
        {
            isOurTeam = true;
        }

        var teamPrefix = isOurTeam.HasValue
            ? (isOurTeam.Value ? "bizim" : "sizin")
            : "bizim";

        return $"{teamPrefix} {agentName}";
    }

    private static ViewModels.MatchViewModel FindMatchViewModel(DependencyObject current)
    {
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is ViewModels.MatchViewModel mvm)
                return mvm;
            if (current is Window win && win.DataContext is ViewModels.MatchViewModel wvm)
                return wvm;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void ClosePopupEventHandlerMethod(object sender, RoutedEventArgs e)
    {
        popup.IsOpen = false;
        e.Handled = true;
    }

    private System.Windows.Threading.DispatcherTimer _popupCloseTimer;

    private void EncounterBadge_Click(object sender, MouseButtonEventArgs e)
    {
        _popupCloseTimer?.Stop();
        HistoryPopup.IsOpen = false;
        NoteInputBox.Text = PlayerCell?.EncounterData?.Note ?? "";
        NotePopup.IsOpen = true;
        IsAnyNotePopupOpen = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
        {
            NoteInputBox.Focus();
            NoteInputBox.SelectAll();
        });
        e.Handled = true;
    }

    private void CloseNotePopup_Click(object sender, MouseButtonEventArgs e)
    {
        SaveNoteInternal();
        NotePopup.IsOpen = false;
        IsAnyNotePopupOpen = false;
        e.Handled = true;
    }

    private void NoteInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveNoteInternal();
            NotePopup.IsOpen = false;
            IsAnyNotePopupOpen = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            NotePopup.IsOpen = false;
            IsAnyNotePopupOpen = false;
            e.Handled = true;
        }
    }

    private void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        SaveNoteInternal();
        NotePopup.IsOpen = false;
        IsAnyNotePopupOpen = false;
    }

    private void NotePopup_Closed(object sender, EventArgs e)
    {
        SaveNoteInternal();
        IsAnyNotePopupOpen = false;
    }

    private void SaveNoteInternal()
    {
        if (PlayerCell?.PlayerUiData == null) return;
        var puuid = PlayerCell.PlayerUiData.Puuid;
        var note = NoteInputBox.Text.Trim();
        EncounterTracker.SetNote(puuid, note);
        if (PlayerCell.EncounterData != null)
        {
            PlayerCell.EncounterData.Note = note;
            PlayerCell.EncounterData.HasNote = !string.IsNullOrWhiteSpace(note);
            var summary = PlayerCell.EncounterData.Summary ?? "";
            var parts = summary.Split(' ');
            var count = parts.Length > 1 ? parts[1] : (parts.Length > 0 ? parts[0] : "");
            if (!count.StartsWith("↻")) count = "↻ " + count;
            PlayerCell.EncounterData.Summary = count + (PlayerCell.EncounterData.HasNote ? " 📝" : "");
            var tt = "";
            if (PlayerCell.EncounterData.HasNote)
                tt += $"📝 Not: {note}\n------------------------------\n";
            tt += $"{PlayerCell.EncounterData.Summary} (Not bırakmak / düzenlemek için tıklayın)";
            PlayerCell.EncounterData.Tooltip = tt;
        }
    }

    private void EncounterBadge_MouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer?.Stop();
        if (!NotePopup.IsOpen)
        {
            if (PlayerCell?.EncounterData?.RecentMatches != null)
            {
                EncounterTracker.RefreshRelativeTimes(PlayerCell.EncounterData.RecentMatches);
            }
            HistoryPopup.IsOpen = true;
        }
    }

    private void EncounterBadge_MouseLeave(object sender, MouseEventArgs e)
    {
        StartPopupCloseTimer();
    }

    private void HistoryPopup_MouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer?.Stop();
    }

    private void HistoryPopup_MouseLeave(object sender, MouseEventArgs e)
    {
        StartPopupCloseTimer();
    }

    private void StartPopupCloseTimer()
    {
        _popupCloseTimer?.Stop();
        _popupCloseTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _popupCloseTimer.Tick += (s, args) =>
        {
            _popupCloseTimer.Stop();
            if (!EncounterBadge.IsMouseOver && !HistoryPopup.IsMouseOver)
            {
                HistoryPopup.IsOpen = false;
            }
        };
        _popupCloseTimer.Start();
    }
}

