using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ValGrid.Helpers;
using ValGrid.Objects;

namespace ValGrid.Controls;

public partial class InventoryControl : UserControl
{
    public static readonly DependencyProperty SkinDataProperty = DependencyProperty.Register(
        nameof(SkinDataObject),
        typeof(SkinData),
        typeof(InventoryControl),
        new PropertyMetadata(new SkinData(), OnSkinDataChanged)
    );

    public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register(
        nameof(Username),
        typeof(string),
        typeof(InventoryControl),
        new PropertyMetadata(null)
    );

    public static readonly RoutedEvent CloseButtonEvent = EventManager.RegisterRoutedEvent(
        "SettingConfirmedEvent",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(InventoryControl)
    );

    public InventoryControl(SkinData skinData, string username)
    {
        InitializeComponent();
        SkinDataObject = skinData ?? new SkinData();
        Username = username;
        Focusable = true;
        Loaded += (s, e) =>
        {
            ApplyWeaponStyles();
            Focus();
        };
        ApplyWeaponStyles();
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            RaiseEvent(new RoutedEventArgs(CloseButtonEvent));
            e.Handled = true;
        }
    }

    private static void OnSkinDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InventoryControl control)
        {
            control.ApplyWeaponStyles();
        }
    }

    public SkinData SkinDataObject
    {
        get => (SkinData)GetValue(SkinDataProperty);
        set => SetValue(SkinDataProperty, value);
    }

    public string Username
    {
        get => (string)GetValue(UsernameProperty);
        set => SetValue(UsernameProperty, value);
    }

    public event RoutedEventHandler CloseButton
    {
        add => AddHandler(CloseButtonEvent, value);
        remove => RemoveHandler(CloseButtonEvent, value);
    }

    private void CloseBtnClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CloseButtonEvent));
    }

    private void Backdrop_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CloseButtonEvent));
        e.Handled = true;
    }

    private void InnerBorder_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void ApplyWeaponStyles()
    {
        if (SkinDataObject == null) return;

        // Ensure standard weapon fallbacks are populated for all 18 weapons, card, and sprays
        WeaponHelper.EnsureStandardWeaponFallbacks(SkinDataObject);

        var metas = SkinDataObject.WeaponMetas ?? new Dictionary<string, ValSkinMeta>();

        void SetupSlot(InventoryEntryControl entry, string weaponName, Uri img, string fallbackName)
        {
            if (entry == null) return;
            entry.Image = img;

            metas.TryGetValue(weaponName, out var meta);
            var skinTitle = WeaponHelper.FormatSkinTitle(meta?.ChromaName, meta?.SkinName, fallbackName, weaponName);
            var isStandard = IsStandardWeapon(skinTitle, weaponName) || IsStandardWeapon(fallbackName, weaponName);

            if (isStandard)
            {
                entry.Opacity = 0.55;
                entry.IsRare = false;
                entry.BorderBrush = System.Windows.Media.Brushes.Transparent;
                entry.BorderThickness = new Thickness(0);
                entry.TooltipName = $"[Varsayılan] {WeaponHelper.GetTurkishWeaponName(weaponName)}";
            }
            else
            {
                entry.Opacity = 1.0;
                var isRare = meta?.IsRare ?? false;
                entry.IsRare = isRare;

                if (isRare)
                {
                    // Sadece uyarı çıkartan silahlarda (Champions, Arcane, Kuronami, Ultra Edition vb.) renkli çerçeve ve parlama
                    System.Windows.Media.Color color = System.Windows.Media.Color.FromRgb(0xff, 0xd7, 0x00);
                    if (meta != null && !string.IsNullOrEmpty(meta.TierColor) && meta.TierColor != "Transparent")
                    {
                        try
                        {
                            color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(meta.TierColor);
                        }
                        catch
                        {
                            color = System.Windows.Media.Color.FromRgb(0xff, 0xd7, 0x00);
                        }
                    }

                    entry.BorderBrush = new System.Windows.Media.SolidColorBrush(color);
                    entry.BorderThickness = new Thickness(1.8);
                    entry.GlowColor = color;
                    entry.GlowBlur = 12.0;
                    entry.GlowOpacity = 0.65;
                }
                else
                {
                    // Normal silahlarda gereksiz renklendirme yok
                    entry.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    entry.BorderThickness = new Thickness(0);
                    entry.GlowOpacity = 0;
                }

                if (meta != null && meta.VpCost > 0)
                {
                    entry.TooltipName = meta.IsRare
                        ? $"⭐ [{meta.RarityLabel}] {skinTitle}\nFiyat: {CurrencyHelper.FormatVpAndTl(meta.VpCost)} | Seri: {meta.TierDevName} Edition"
                        : $"{skinTitle}\nFiyat: {CurrencyHelper.FormatVpAndTl(meta.VpCost)} | Seri: {meta.TierDevName} Edition";
                }
                else
                {
                    entry.TooltipName = skinTitle;
                }
            }

            if (SkinDataObject.WeaponBuddies != null && SkinDataObject.WeaponBuddies.TryGetValue(weaponName, out var buddy) && buddy?.Image != null)
            {
                entry.BuddyImage = buddy.Image;
                entry.BuddyName = !string.IsNullOrWhiteSpace(buddy.Name) ? $"Uğurluk: {buddy.Name}" : "Uğurluk";
                entry.BuddyVisibility = Visibility.Visible;
            }
            else
            {
                entry.BuddyImage = null;
                entry.BuddyName = null;
                entry.BuddyVisibility = Visibility.Collapsed;
            }
        }

        SetupSlot(EntryClassic, "Classic", SkinDataObject.ClassicImage, SkinDataObject.ClassicName);
        SetupSlot(EntryShorty, "Shorty", SkinDataObject.ShortyImage, SkinDataObject.ShortyName);
        SetupSlot(EntryFrenzy, "Frenzy", SkinDataObject.FrenzyImage, SkinDataObject.FrenzyName);
        SetupSlot(EntryGhost, "Ghost", SkinDataObject.GhostImage, SkinDataObject.GhostName);
        SetupSlot(EntryBandit, "Bandit", SkinDataObject.BanditImage, SkinDataObject.BanditName);
        SetupSlot(EntrySheriff, "Sheriff", SkinDataObject.SheriffImage, SkinDataObject.SheriffName);
        SetupSlot(EntryStinger, "Stinger", SkinDataObject.StingerImage, SkinDataObject.StingerName);
        SetupSlot(EntrySpectre, "Spectre", SkinDataObject.SpectreImage, SkinDataObject.SpectreName);
        SetupSlot(EntryBucky, "Bucky", SkinDataObject.BuckyImage, SkinDataObject.BuckyName);
        SetupSlot(EntryJudge, "Judge", SkinDataObject.JudgeImage, SkinDataObject.JudgeName);
        SetupSlot(EntryBulldog, "Bulldog", SkinDataObject.BulldogImage, SkinDataObject.BulldogName);
        SetupSlot(EntryGuardian, "Guardian", SkinDataObject.GuardianImage, SkinDataObject.GuardianName);
        SetupSlot(EntryWarden, "Warden", SkinDataObject.WardenImage, SkinDataObject.WardenName);
        SetupSlot(EntryPhantom, "Phantom", SkinDataObject.PhantomImage, SkinDataObject.PhantomName);
        SetupSlot(EntryVandal, "Vandal", SkinDataObject.VandalImage, SkinDataObject.VandalName);
        SetupSlot(EntryMarshal, "Marshal", SkinDataObject.MarshalImage, SkinDataObject.MarshalName);
        SetupSlot(EntryOutlaw, "Outlaw", SkinDataObject.OutlawImage, SkinDataObject.OutlawName);
        SetupSlot(EntryOperator, "Operator", SkinDataObject.OperatorImage, SkinDataObject.OperatorName);
        SetupSlot(EntryAres, "Ares", SkinDataObject.AresImage, SkinDataObject.AresName);
        SetupSlot(EntryOdin, "Odin", SkinDataObject.OdinImage, SkinDataObject.OdinName);
        SetupSlot(EntryMelee, "Melee", SkinDataObject.MeleeImage, SkinDataObject.MeleeName);
    }

    private bool _isCopyingSkins = false;

    private async void CopyPlayerName_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Username)) return;
        var clean = Username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
        var success = await ClipboardHelper.SetTextAsync(clean);
        if (sender is TextBlock tb)
        {
            var oldText = tb.Text;
            tb.Text = success ? $"✓ {clean}" : oldText;
            await Task.Delay(1200);
            tb.Text = oldText;
        }
    }

    private async void CopySkins_Click(object sender, RoutedEventArgs e)
    {
        if (_isCopyingSkins) return;
        _isCopyingSkins = true;

        try
        {
            var text = BuildInventorySkinsSummary(SkinDataObject, Username, includeUsername: true, includeValue: false, includeBuddies: false);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopySkinsIcon != null && CopySkinsBtn != null)
            {
                CopySkinsIcon.Text = success ? "✓" : "❌";
                if (CopySkinsLabel != null)
                    CopySkinsLabel.Text = success ? "Kopyalandı!" : "Hata!";
                CopySkinsBtn.ToolTip = success ? "İsim ve Silah Skinleri Kopyalandı!" : "Kopyalama başarısız (Pano meşgul)";
                await Task.Delay(1800);
                CopySkinsIcon.Text = "📋";
                if (CopySkinsLabel != null)
                    CopySkinsLabel.Text = "Kopyala";
                CopySkinsBtn.ToolTip = "Sol Tık: İsim ve Skinleri Kopyala\nOrta Tık: Toplam Değerli (VP & TL) Kopyala\nSağ Tık: Sadece Uğurlukları Kopyala";
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopySkins_Click failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingSkins = false;
        }
    }

    private void CopySkins_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Middle)
        {
            CopySkins_MiddleClick(sender, e);
        }
    }

    private async void CopySkins_MiddleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isCopyingSkins) return;
        _isCopyingSkins = true;

        try
        {
            var text = BuildInventorySkinsSummary(SkinDataObject, Username, includeUsername: true, includeValue: true, includeBuddies: false);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopySkinsIcon != null && CopySkinsBtn != null)
            {
                CopySkinsIcon.Text = success ? "✓" : "❌";
                if (CopySkinsLabel != null)
                    CopySkinsLabel.Text = success ? "Kopyalandı!" : "Hata!";
                CopySkinsBtn.ToolTip = success ? "Değerli (VP & TL) Envanter Kopyalandı!" : "Kopyalama başarısız (Pano meşgul)";
                await Task.Delay(1800);
                CopySkinsIcon.Text = "📋";
                if (CopySkinsLabel != null)
                    CopySkinsLabel.Text = "Kopyala";
                CopySkinsBtn.ToolTip = "Sol Tık: İsim ve Skinleri Kopyala\nOrta Tık: Toplam Değerli (VP & TL) Kopyala\nSağ Tık: Sadece Uğurlukları Kopyala";
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopySkins_MiddleClick failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingSkins = false;
        }
    }

    private async void CopySkins_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isCopyingSkins) return;
        _isCopyingSkins = true;

        try
        {
            var text = BuildInventoryBuddiesSummary(SkinDataObject, Username, includeUsername: true);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopySkinsIcon != null && CopySkinsBtn != null)
            {
                CopySkinsIcon.Text = success ? "✓" : "❌";
                if (CopySkinsLabel != null)
                    CopySkinsLabel.Text = success ? "Kopyalandı!" : "Hata!";
                CopySkinsBtn.ToolTip = success ? "Sadece Uğurluklar Kopyalandı!" : "Kopyalama başarısız (Pano meşgul)";
                await Task.Delay(1800);
                CopySkinsIcon.Text = "📋";
                if (CopySkinsLabel != null)
                    CopySkinsLabel.Text = "Kopyala";
                CopySkinsBtn.ToolTip = "Sol Tık: İsim ve Skinleri Kopyala\nOrta Tık: Toplam Değerli (VP & TL) Kopyala\nSağ Tık: Sadece Uğurlukları Kopyala";
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopySkins_RightClick failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingSkins = false;
        }
    }

    private async void CopyBuddies_Click(object sender, RoutedEventArgs e)
    {
        if (_isCopyingSkins) return;
        _isCopyingSkins = true;

        try
        {
            var text = BuildInventoryBuddiesSummary(SkinDataObject, Username, includeUsername: true);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyBuddiesIcon != null && CopyBuddiesBtn != null)
            {
                CopyBuddiesIcon.Text = success ? "✓" : "❌";
                if (CopyBuddiesLabel != null)
                    CopyBuddiesLabel.Text = success ? "Kopyalandı!" : "Hata!";
                CopyBuddiesBtn.ToolTip = success ? "Sadece Uğurluklar Kopyalandı!" : "Kopyalama başarısız (Pano meşgul)";
                await Task.Delay(1800);
                CopyBuddiesIcon.Text = "🧿";
                if (CopyBuddiesLabel != null)
                    CopyBuddiesLabel.Text = "Uğurluklar";
                CopyBuddiesBtn.ToolTip = "Sadece Takılı Uğurlukları Kopyala\nÖrn: Oyuncu (Uğurluklar): RGX 11z Pro, Radyant Çiçeği...";
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CopyBuddies_Click failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingSkins = false;
        }
    }

    // KULLANICI TERCİHİ: Silahlar popülerlik / önem sırasına göre kopyalanır (Vandal, Phantom, Bıçak, Op, Sheriff...).
    // Bu sıralama sabittir ve değiştirilmemelidir.
    public static readonly (string WeaponKey, Func<SkinData, string> FallbackGetter)[] WeaponSlots = new[]
    {
        ("Vandal", (Func<SkinData, string>)(s => s.VandalName)),
        ("Phantom", s => s.PhantomName),
        ("Melee", s => s.MeleeName),
        ("Operator", s => s.OperatorName),
        ("Sheriff", s => s.SheriffName),
        ("Ghost", s => s.GhostName),
        ("Classic", s => s.ClassicName),
        ("Outlaw", s => s.OutlawName),
        ("Marshal", s => s.MarshalName),
        ("Odin", s => s.OdinName),
        ("Spectre", s => s.SpectreName),
        ("Guardian", s => s.GuardianName),
        ("Bulldog", s => s.BulldogName),
        ("Ares", s => s.AresName),
        ("Frenzy", s => s.FrenzyName),
        ("Shorty", s => s.ShortyName),
        ("Judge", s => s.JudgeName),
        ("Bucky", s => s.BuckyName),
        ("Stinger", s => s.StingerName),
        ("Bandit", s => s.BanditName),
        ("Warden", s => s.WardenName)
    };

    public static string CleanSkinName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "";
        var name = rawName.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();

        // 1. Remove level prefixes like "1. Seviye ", "4. Seviye ", "Level 1 ", "Level 4 "
        name = Regex.Replace(name, @"^\d+\.\s*Seviye\s+", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"^Level\s*\d+\s+", "", RegexOptions.IgnoreCase);

        // 2. Remove style / variant suffix in parentheses like "(Stil 2)", "(Stil 3 - Mavi)", "(Variant 1)", "(Renk 2)"
        name = Regex.Replace(name, @"\s*\((?:Stil|Style|Variant|Renk)\s*\d+[^)]*\)", "", RegexOptions.IgnoreCase);

        return name.Trim();
    }

    public static string CleanBuddyName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "";
        var name = rawName.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();

        // 1. Remove level prefixes
        name = Regex.Replace(name, @"^\d+\.\s*Seviye\s+", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"^Level\s*\d+\s+", "", RegexOptions.IgnoreCase);

        // 2. Remove common suffixes like " Uğuru", " Uğurluğu", " Uğur", " Gun Buddy", " Buddy", " Charm"
        name = Regex.Replace(name, @"\s+(?:Uğurluğu|Uğuru|Uğur|Gun\s*Buddy|Buddy|Charm)\s*$", "", RegexOptions.IgnoreCase);

        return name.Trim();
    }

    private static readonly System.Globalization.CultureInfo TurkishCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    public static bool EqualsTurkish(string a, string b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
        try
        {
            return TurkishCulture.CompareInfo.Compare(a.Trim(), b.Trim(), System.Globalization.CompareOptions.IgnoreCase) == 0;
        }
        catch
        {
            return string.Equals(a.Trim(), b.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public static bool StartsWithTurkish(string source, string prefix)
    {
        if (source == null || prefix == null) return false;
        if (source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        try
        {
            return TurkishCulture.CompareInfo.IsPrefix(source.Trim(), prefix.Trim(), System.Globalization.CompareOptions.IgnoreCase);
        }
        catch
        {
            return source.Trim().StartsWith(prefix.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public static bool IsStandardWeapon(string skinName, string weaponKey)
    {
        if (string.IsNullOrWhiteSpace(skinName)) return true;
        var trimmed = skinName.Trim();

        // 1. Standart variations exact or prefix
        if (EqualsTurkish(trimmed, "Standart") || EqualsTurkish(trimmed, "Standard") ||
            EqualsTurkish(trimmed, "STANDART") || EqualsTurkish(trimmed, "STANDARD") ||
            StartsWithTurkish(trimmed, "Standart ") || StartsWithTurkish(trimmed, "Standard ") ||
            StartsWithTurkish(trimmed, "STANDART ") || StartsWithTurkish(trimmed, "STANDARD ") ||
            StartsWithTurkish(trimmed, "Standart-") || StartsWithTurkish(trimmed, "Standard-"))
        {
            return true;
        }

        // 2. Equals the weapon key or English/Turkish name (e.g. "Vandal", "Classic", "Odin", "Frenzy", "Ares")
        if (EqualsTurkish(trimmed, weaponKey) || EqualsTurkish(trimmed, WeaponHelper.GetTurkishWeaponName(weaponKey)))
            return true;

        // 3. Standart + WeaponKey variations (e.g. "STANDART ODIN", "Standart Frenzy", "Standart Ares")
        if (trimmed.IndexOf(weaponKey, StringComparison.OrdinalIgnoreCase) >= 0 &&
            (trimmed.IndexOf("standart", StringComparison.OrdinalIgnoreCase) >= 0 ||
             trimmed.IndexOf("standard", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }

        // 4. Melee standard variations (including uppercase "YAKIN DÖVÜŞ SİLAHI" from Riot API)
        if (EqualsTurkish(trimmed, "Yakın Dövüş Silahı") ||
            EqualsTurkish(trimmed, "Yakın Dövüş") ||
            EqualsTurkish(trimmed, "Standart Yakın Dövüş Silahı") ||
            EqualsTurkish(trimmed, "Bıçak") ||
            EqualsTurkish(trimmed, "Knife") ||
            EqualsTurkish(trimmed, "Melee") ||
            string.Equals(trimmed, "YAKIN DÖVÜŞ SİLAHI", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "YAKIN DOVUS SILAHI", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Yakın Dövüş Silahı", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static List<string> GetSkinNames(SkinData skinData, bool onlyCustom = true, bool includeBuddies = false)
    {
        if (skinData == null) return new List<string>();

        var metas = skinData.WeaponMetas ?? new Dictionary<string, ValSkinMeta>();
        var buddies = skinData.WeaponBuddies ?? new Dictionary<string, WeaponBuddyInfo>();
        var result = new List<string>();

        foreach (var (weaponKey, getter) in WeaponSlots)
        {
            metas.TryGetValue(weaponKey, out var meta);
            var rawName = !string.IsNullOrWhiteSpace(meta?.SkinName)
                ? meta.SkinName
                : (getter != null ? getter(skinData) : null);

            var cleanName = CleanSkinName(rawName);
            if (string.IsNullOrWhiteSpace(cleanName))
                continue;

            if (onlyCustom && IsStandardWeapon(cleanName, weaponKey))
                continue;

            if (includeBuddies && buddies.TryGetValue(weaponKey, out var buddy) && !string.IsNullOrWhiteSpace(buddy?.Name))
            {
                var cleanBuddy = CleanBuddyName(buddy.Name);
                if (!string.IsNullOrWhiteSpace(cleanBuddy))
                {
                    result.Add($"{cleanName} ({cleanBuddy})");
                }
                else
                {
                    result.Add(cleanName);
                }
            }
            else
            {
                result.Add(cleanName);
            }
        }

        return result;
    }

    public static string BuildInventorySkinsSummary(
        SkinData skinData,
        string username = null,
        bool includeUsername = false,
        string separator = ", ",
        bool includeValue = false,
        bool includeBuddies = false)
    {
        var cleanUser = string.IsNullOrWhiteSpace(username)
            ? ""
            : username.Replace(" 🔗", "").Replace(" 👁", "").Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();

        var prefix = (includeUsername && !string.IsNullOrWhiteSpace(cleanUser))
            ? $"{cleanUser}: "
            : "";

        var valStr = (includeValue && skinData != null && skinData.TotalInventoryValueVp > 0)
            ? $" (Toplam: {skinData.TotalValueFormatted} / ~{skinData.TotalValueTlFormatted})"
            : "";

        var allSkins = GetSkinNames(skinData, onlyCustom: true, includeBuddies: includeBuddies);

        if (allSkins.Count == 0)
        {
            var noSkinsText = $"Özel skin bulunmuyor{valStr}";
            var emptyRes = string.IsNullOrEmpty(prefix) ? noSkinsText : $"{prefix}{noSkinsText}";
            return SanitizeChatMessage(emptyRes);
        }

        var fullText = prefix + string.Join(separator, allSkins) + valStr;
        return SanitizeChatMessage(fullText);
    }

    public static List<string> GetBuddyNames(SkinData skinData, bool distinct = true)
    {
        if (skinData?.WeaponBuddies == null) return new List<string>();

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan in priority weapon order: Vandal, Phantom, Operator, Sheriff, Ghost, Classic, etc.
        foreach (var (weaponKey, _) in WeaponSlots)
        {
            if (skinData.WeaponBuddies.TryGetValue(weaponKey, out var buddy) && !string.IsNullOrWhiteSpace(buddy?.Name))
            {
                var cleanBuddy = CleanBuddyName(buddy.Name);
                if (!string.IsNullOrWhiteSpace(cleanBuddy))
                {
                    if (!distinct || seen.Add(cleanBuddy))
                    {
                        result.Add(cleanBuddy);
                    }
                }
            }
        }

        // Catch any remaining weapons in WeaponBuddies
        foreach (var kvp in skinData.WeaponBuddies)
        {
            if (kvp.Value != null && !string.IsNullOrWhiteSpace(kvp.Value.Name))
            {
                var cleanBuddy = CleanBuddyName(kvp.Value.Name);
                if (!string.IsNullOrWhiteSpace(cleanBuddy))
                {
                    if (!distinct || seen.Add(cleanBuddy))
                    {
                        result.Add(cleanBuddy);
                    }
                }
            }
        }

        return result;
    }

    public static string BuildInventoryBuddiesSummary(
        SkinData skinData,
        string username = null,
        bool includeUsername = true,
        string separator = ", ")
    {
        var cleanUser = string.IsNullOrWhiteSpace(username)
            ? ""
            : username.Replace(" 🔗", "").Replace(" 👁", "").Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();

        var prefix = (includeUsername && !string.IsNullOrWhiteSpace(cleanUser))
            ? $"{cleanUser} (Uğurluklar): "
            : "Uğurluklar: ";

        var allBuddies = GetBuddyNames(skinData, distinct: true);

        if (allBuddies.Count == 0)
        {
            var noBuddiesText = "Takılı uğurluk bulunmuyor";
            var emptyRes = string.IsNullOrEmpty(prefix) ? noBuddiesText : $"{prefix}{noBuddiesText}";
            return SanitizeChatMessage(emptyRes);
        }

        var fullText = prefix + string.Join(separator, allBuddies);
        return SanitizeChatMessage(fullText);
    }

    public static string SanitizeChatMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        // Eliminate any newlines or carriage returns (crucial for Valorant in-game chat)
        text = text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
        // Collapse multiple spaces into one
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}

