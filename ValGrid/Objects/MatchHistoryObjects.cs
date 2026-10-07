using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using ValGrid.Helpers;

namespace ValGrid.Objects;

public class MatchHistoryWeaponItem
{
    public string WeaponName { get; set; } = "";
    public string SkinName { get; set; } = "";
    public string SkinImage { get; set; } = "";
    public string TierColor { get; set; } = "#7f8c8d";
    public string RarityLabel { get; set; } = "";
    public bool IsRare { get; set; }
    public int VpCost { get; set; }
}

public class MatchHistoryPlayerItem : ObservableObject
{
    public Guid Puuid { get; set; }
    public string Username { get; set; } = "";
    public string AgentName { get; set; } = "";
    public string AgentIcon { get; set; } = "";
    public string RankName { get; set; } = "";
    public string RankIcon { get; set; } = "";
    public int RankTier { get; set; }

    private string _accountLevel = "-";
    public string AccountLevel
    {
        get => _accountLevel;
        set
        {
            if (SetProperty(ref _accountLevel, value))
            {
                OnPropertyChanged(nameof(HasAccountLevel));
                OnPropertyChanged(nameof(AccountLevelVisibility));
            }
        }
    }
    [JsonIgnore]
    public bool HasAccountLevel => !string.IsNullOrEmpty(AccountLevel) && AccountLevel != "-" && AccountLevel != "0";
    [JsonIgnore]
    public Visibility AccountLevelVisibility => HasAccountLevel ? Visibility.Visible : Visibility.Collapsed;

    private string _peakRankName = "";
    public string PeakRankName
    {
        get => _peakRankName;
        set
        {
            if (SetProperty(ref _peakRankName, value))
            {
                OnPropertyChanged(nameof(HasPeakRank));
                OnPropertyChanged(nameof(PeakRankVisibility));
                OnPropertyChanged(nameof(PeakRankDisplay));
                OnPropertyChanged(nameof(PeakRankIconVisibility));
            }
        }
    }

    private string _peakRankIcon = "";
    public string PeakRankIcon
    {
        get => _peakRankIcon;
        set
        {
            if (SetProperty(ref _peakRankIcon, value))
            {
                OnPropertyChanged(nameof(HasPeakRankIcon));
                OnPropertyChanged(nameof(PeakRankIconVisibility));
            }
        }
    }

    private string _peakRankTooltip = "";
    public string PeakRankTooltip
    {
        get => _peakRankTooltip;
        set => SetProperty(ref _peakRankTooltip, value);
    }

    [JsonIgnore]
    public bool HasPeakRank => !string.IsNullOrWhiteSpace(PeakRankName) &&
                               !PeakRankName.Equals("UNRATED", StringComparison.OrdinalIgnoreCase) &&
                               !PeakRankName.Equals("Derecesiz", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore]
    public Visibility PeakRankVisibility => HasPeakRank ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public bool HasPeakRankIcon => !string.IsNullOrWhiteSpace(PeakRankIcon);
    [JsonIgnore]
    public Visibility PeakRankIconVisibility => (HasPeakRank && HasPeakRankIcon) ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public string PeakRankDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PeakRankName)) return "";
            if (PeakRankName.StartsWith("Peak", StringComparison.OrdinalIgnoreCase))
                return PeakRankName;
            return $"Peak: {PeakRankName}";
        }
    }

    private int _rankProgress;
    public int RankProgress
    {
        get => _rankProgress;
        set
        {
            if (SetProperty(ref _rankProgress, value))
            {
                OnPropertyChanged(nameof(HasRankProgress));
                OnPropertyChanged(nameof(RankRrFormatted));
            }
        }
    }
    [JsonIgnore]
    public bool HasRankProgress => RankProgress > 0;
    [JsonIgnore]
    public string RankRrFormatted => HasRankProgress ? $"{RankProgress} RR" : "";
    public string TeamId { get; set; } = "";
    public bool IsAlly { get; set; }
    public string TeamName { get; set; } = "";
    public string TeamColor { get; set; } = "#5b92e5";
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
    public string KdaFormatted => $"{Kills}/{Deaths}/{Assists}";
    public int TotalInventoryValueVp { get; set; }
    public string TotalValueFormatted { get; set; } = "";
    public string TotalValueTlFormatted { get; set; } = "";

    private string _note = "";
    public string Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
            {
                OnPropertyChanged(nameof(HasNote));
                OnPropertyChanged(nameof(NoteVisibility));
                OnPropertyChanged(nameof(NoteDisplayText));
            }
        }
    }

    [JsonIgnore]
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
    [JsonIgnore]
    public Visibility NoteVisibility => HasNote ? Visibility.Visible : Visibility.Collapsed;
    [JsonIgnore]
    public string NoteDisplayText => HasNote ? $"\"{Note}\"" : "Not Yok";

    private int _encounterCount;
    public int EncounterCount
    {
        get => _encounterCount;
        set
        {
            if (SetProperty(ref _encounterCount, value))
            {
                OnPropertyChanged(nameof(EncounterVisibility));
            }
        }
    }

    private string _encounterBadgeText = "";
    public string EncounterBadgeText
    {
        get => _encounterBadgeText;
        set => SetProperty(ref _encounterBadgeText, value);
    }

    private string _encounterTooltip = "";
    public string EncounterTooltip
    {
        get => _encounterTooltip;
        set => SetProperty(ref _encounterTooltip, value);
    }

    [JsonIgnore]
    public Visibility EncounterVisibility => EncounterCount > 0 ? Visibility.Visible : Visibility.Collapsed;

    private static readonly Brush InactiveBlacklistBg = new SolidColorBrush(Color.FromRgb(0x22, 0x13, 0x16));
    private static readonly Brush ActiveBlacklistBg = new SolidColorBrush(Color.FromRgb(0xdc, 0x26, 0x26));
    private static readonly Brush InactiveBlacklistBorder = new SolidColorBrush(Color.FromRgb(0x4a, 0x1d, 0x24));
    private static readonly Brush ActiveBlacklistBorder = new SolidColorBrush(Color.FromRgb(0xfc, 0xa5, 0xa5));
    private static readonly Brush InactiveBlacklistIcon = new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44));
    private static readonly Brush ActiveBlacklistIcon = Brushes.White;

    private static readonly Brush InactiveFemaleBg = new SolidColorBrush(Color.FromRgb(0x23, 0x15, 0x20));
    private static readonly Brush ActiveFemaleBg = new SolidColorBrush(Color.FromRgb(0xd6, 0x33, 0x84));
    private static readonly Brush InactiveFemaleBorder = new SolidColorBrush(Color.FromRgb(0x4a, 0x1d, 0x35));
    private static readonly Brush ActiveFemaleBorder = new SolidColorBrush(Color.FromRgb(0xff, 0xb6, 0xc1));
    private static readonly Brush InactiveFemaleIcon = new SolidColorBrush(Color.FromRgb(0xff, 0x79, 0xc6));
    private static readonly Brush ActiveFemaleIcon = Brushes.White;

    private static readonly Brush InactiveMaleBg = new SolidColorBrush(Color.FromRgb(0x13, 0x1b, 0x2e));
    private static readonly Brush ActiveMaleBg = new SolidColorBrush(Color.FromRgb(0x16, 0x2a, 0x45));
    private static readonly Brush InactiveMaleBorder = new SolidColorBrush(Color.FromRgb(0x1e, 0x3a, 0x5f));
    private static readonly Brush ActiveMaleBorder = new SolidColorBrush(Color.FromRgb(0x38, 0xbd, 0xf8));
    private static readonly Brush InactiveMaleIcon = new SolidColorBrush(Color.FromRgb(0x38, 0xbd, 0xf8));
    private static readonly Brush ActiveMaleIcon = Brushes.White;

    private static readonly Brush DefaultAllyRowBorder = new SolidColorBrush(Color.FromRgb(0x2f, 0x3a, 0x5e));
    private static readonly Brush DefaultEnemyRowBorder = new SolidColorBrush(Color.FromRgb(0x47, 0x2b, 0x38));
    private static readonly Brush BlacklistRowBorder = new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44));
    private static readonly Brush FemaleRowBorder = new SolidColorBrush(Color.FromRgb(0xff, 0x79, 0xc6));
    private static readonly Brush MaleRowBorder = new SolidColorBrush(Color.FromRgb(0x38, 0xbd, 0xf8));

    private static readonly Brush DefaultAllyRowBg = new SolidColorBrush(Color.FromRgb(0x21, 0x28, 0x42));
    private static readonly Brush DefaultEnemyRowBg = new SolidColorBrush(Color.FromRgb(0x26, 0x1e, 0x2b));
    private static readonly Brush FemaleRowBg = new SolidColorBrush(Color.FromRgb(0x35, 0x18, 0x29));

    static MatchHistoryPlayerItem()
    {
        InactiveBlacklistBg.Freeze();
        ActiveBlacklistBg.Freeze();
        InactiveBlacklistBorder.Freeze();
        ActiveBlacklistBorder.Freeze();
        InactiveBlacklistIcon.Freeze();

        InactiveFemaleBg.Freeze();
        ActiveFemaleBg.Freeze();
        InactiveFemaleBorder.Freeze();
        ActiveFemaleBorder.Freeze();
        InactiveFemaleIcon.Freeze();

        InactiveMaleBg.Freeze();
        ActiveMaleBg.Freeze();
        InactiveMaleBorder.Freeze();
        ActiveMaleBorder.Freeze();
        InactiveMaleIcon.Freeze();

        DefaultAllyRowBorder.Freeze();
        DefaultEnemyRowBorder.Freeze();
        BlacklistRowBorder.Freeze();
        FemaleRowBorder.Freeze();
        MaleRowBorder.Freeze();

        DefaultAllyRowBg.Freeze();
        DefaultEnemyRowBg.Freeze();
        FemaleRowBg.Freeze();
    }

    private bool _isFemale;
    public bool IsFemale
    {
        get => _isFemale;
        set
        {
            if (SetProperty(ref _isFemale, value))
            {
                OnPropertyChanged(nameof(FemaleBackground));
                OnPropertyChanged(nameof(FemaleBorder));
                OnPropertyChanged(nameof(FemaleIconBrush));
                OnPropertyChanged(nameof(FemaleGlowOpacity));
                OnPropertyChanged(nameof(FemaleTooltip));
                OnPropertyChanged(nameof(RowBackground));
                OnPropertyChanged(nameof(RowBorderBrush));
                OnPropertyChanged(nameof(RowBorderThickness));
            }
        }
    }

    [JsonIgnore]
    public Brush RowBackground
    {
        get
        {
            if (IsFemale) return FemaleRowBg;
            return IsAlly ? DefaultAllyRowBg : DefaultEnemyRowBg;
        }
    }

    [JsonIgnore]
    public Brush FemaleBackground => IsFemale ? ActiveFemaleBg : InactiveFemaleBg;
    [JsonIgnore]
    public Brush FemaleBorder => IsFemale ? ActiveFemaleBorder : InactiveFemaleBorder;
    [JsonIgnore]
    public Brush FemaleIconBrush => IsFemale ? ActiveFemaleIcon : InactiveFemaleIcon;
    [JsonIgnore]
    public double FemaleGlowOpacity => IsFemale ? 0.9 : 0.0;
    [JsonIgnore]
    public string FemaleTooltip => IsFemale
        ? "Kız Oyuncu Etiketini Kaldır"
        : "Kız Oyuncu Olarak Kaydet";

    private bool _isMale;
    public bool IsMale
    {
        get => _isMale;
        set
        {
            if (SetProperty(ref _isMale, value))
            {
                OnPropertyChanged(nameof(MaleBackground));
                OnPropertyChanged(nameof(MaleBorder));
                OnPropertyChanged(nameof(MaleIconBrush));
                OnPropertyChanged(nameof(MaleGlowOpacity));
                OnPropertyChanged(nameof(MaleTooltip));
                OnPropertyChanged(nameof(RowBorderBrush));
                OnPropertyChanged(nameof(RowBorderThickness));
            }
        }
    }

    [JsonIgnore]
    public Brush MaleBackground => IsMale ? ActiveMaleBg : InactiveMaleBg;
    [JsonIgnore]
    public Brush MaleBorder => IsMale ? ActiveMaleBorder : InactiveMaleBorder;
    [JsonIgnore]
    public Brush MaleIconBrush => IsMale ? ActiveMaleIcon : InactiveMaleIcon;
    [JsonIgnore]
    public double MaleGlowOpacity => IsMale ? 0.9 : 0.0;
    [JsonIgnore]
    public string MaleTooltip => IsMale
        ? "Erkek Oyuncu Etiketini Kaldır"
        : "Erkek Oyuncu Olarak Kaydet";

    private bool _isBlacklisted;
    public bool IsBlacklisted
    {
        get => _isBlacklisted;
        set
        {
            if (SetProperty(ref _isBlacklisted, value))
            {
                OnPropertyChanged(nameof(BlacklistBackground));
                OnPropertyChanged(nameof(BlacklistBorder));
                OnPropertyChanged(nameof(BlacklistIconBrush));
                OnPropertyChanged(nameof(BlacklistGlowOpacity));
                OnPropertyChanged(nameof(BlacklistTooltip));
                OnPropertyChanged(nameof(RowBorderBrush));
                OnPropertyChanged(nameof(RowBorderThickness));
            }
        }
    }

    [JsonIgnore]
    public Brush BlacklistBackground => IsBlacklisted ? ActiveBlacklistBg : InactiveBlacklistBg;
    [JsonIgnore]
    public Brush BlacklistBorder => IsBlacklisted ? ActiveBlacklistBorder : InactiveBlacklistBorder;
    [JsonIgnore]
    public Brush BlacklistIconBrush => IsBlacklisted ? ActiveBlacklistIcon : InactiveBlacklistIcon;
    [JsonIgnore]
    public double BlacklistGlowOpacity => IsBlacklisted ? 0.9 : 0.0;
    [JsonIgnore]
    public string BlacklistTooltip => IsBlacklisted
        ? "Engeli Kaldır (Kara Listeden Çıkar)"
        : "Bu Oyuncuyu Engelle (Kara Listeye Ekle)";

    [JsonIgnore]
    public Brush RowBorderBrush
    {
        get
        {
            if (IsBlacklisted) return BlacklistRowBorder;
            if (IsFemale) return FemaleRowBorder;
            if (IsMale) return MaleRowBorder;
            return IsAlly ? DefaultAllyRowBorder : DefaultEnemyRowBorder;
        }
    }

    [JsonIgnore]
    public Thickness RowBorderThickness
    {
        get
        {
            if (IsBlacklisted) return new Thickness(1.3);
            if (IsFemale) return new Thickness(1.6);
            if (IsMale) return new Thickness(1.3);
            return new Thickness(1);
        }
    }

    public ObservableCollection<MatchHistoryWeaponItem> Weapons { get; set; } = new();

    public PlayerFullSkinDataDto FullSkinData { get; set; }
    public string CardImage { get; set; } = "";
    public string CardName { get; set; } = "";
    public string Spray1Image { get; set; } = "";
    public string Spray1Name { get; set; } = "";
    public string Spray2Image { get; set; } = "";
    public string Spray2Name { get; set; } = "";
    public string Spray3Image { get; set; } = "";
    public string Spray3Name { get; set; } = "";
    public string Spray4Image { get; set; } = "";
    public string Spray4Name { get; set; } = "";

    [JsonIgnore]
    public SkinData SkinData { get; set; }

    public SkinData GetEffectiveSkinData()
    {
        if (SkinData != null)
            return SkinData;

        var sd = new SkinData
        {
            TotalInventoryValueVp = TotalInventoryValueVp,
            TotalValueFormatted = TotalValueFormatted,
            TotalValueTlFormatted = TotalValueTlFormatted,
            InventoryValueVisibility = TotalInventoryValueVp > 0 ? Visibility.Visible : Visibility.Collapsed
        };

        if (FullSkinData != null)
        {
            if (!string.IsNullOrEmpty(FullSkinData.LargeCardImage) && Uri.TryCreate(FullSkinData.LargeCardImage, UriKind.RelativeOrAbsolute, out var lcUri))
                sd.LargeCardImage = lcUri;
            else if (!string.IsNullOrEmpty(CardImage) && Uri.TryCreate(CardImage, UriKind.RelativeOrAbsolute, out var cImgUri))
                sd.LargeCardImage = cImgUri;

            if (!string.IsNullOrEmpty(FullSkinData.CardImage) && Uri.TryCreate(FullSkinData.CardImage, UriKind.RelativeOrAbsolute, out var cUri))
                sd.CardImage = cUri;
            sd.CardName = !string.IsNullOrEmpty(FullSkinData.CardName) ? FullSkinData.CardName : (!string.IsNullOrEmpty(CardName) ? CardName : "Oyuncu Kartı");

            if (!string.IsNullOrEmpty(FullSkinData.Spray1Image) && Uri.TryCreate(FullSkinData.Spray1Image, UriKind.RelativeOrAbsolute, out var s1Uri))
                sd.Spray1Image = s1Uri;
            sd.Spray1Name = FullSkinData.Spray1Name ?? "";

            if (!string.IsNullOrEmpty(FullSkinData.Spray2Image) && Uri.TryCreate(FullSkinData.Spray2Image, UriKind.RelativeOrAbsolute, out var s2Uri))
                sd.Spray2Image = s2Uri;
            sd.Spray2Name = FullSkinData.Spray2Name ?? "";

            if (!string.IsNullOrEmpty(FullSkinData.Spray3Image) && Uri.TryCreate(FullSkinData.Spray3Image, UriKind.RelativeOrAbsolute, out var s3Uri))
                sd.Spray3Image = s3Uri;
            sd.Spray3Name = FullSkinData.Spray3Name ?? "";

            if (!string.IsNullOrEmpty(FullSkinData.Spray4Image) && Uri.TryCreate(FullSkinData.Spray4Image, UriKind.RelativeOrAbsolute, out var s4Uri))
                sd.Spray4Image = s4Uri;
            sd.Spray4Name = FullSkinData.Spray4Name ?? "";

            sd.WeaponMetas = new Dictionary<string, ValSkinMeta>(StringComparer.OrdinalIgnoreCase);
            sd.WeaponBuddies = new Dictionary<string, WeaponBuddyInfo>(StringComparer.OrdinalIgnoreCase);

            void AssignWeapon(string key, Action<Uri, string> assigner)
            {
                if (FullSkinData.WeaponSkins != null && FullSkinData.WeaponSkins.TryGetValue(key, out var wDto) && wDto != null)
                {
                    Uri wUri = null;
                    if (!string.IsNullOrEmpty(wDto.SkinImage))
                        Uri.TryCreate(wDto.SkinImage, UriKind.RelativeOrAbsolute, out wUri);

                    assigner(wUri, wDto.SkinName ?? "");

                    sd.WeaponMetas[key] = new ValSkinMeta
                    {
                        SkinName = wDto.SkinName,
                        TierColor = wDto.TierColor,
                        TierDevName = wDto.TierDevName,
                        RarityLabel = wDto.RarityLabel,
                        VpCost = wDto.VpCost,
                        IsRare = wDto.IsRare
                    };

                    if (!string.IsNullOrEmpty(wDto.BuddyName) || !string.IsNullOrEmpty(wDto.BuddyImage))
                    {
                        Uri bUri = null;
                        if (!string.IsNullOrEmpty(wDto.BuddyImage))
                            Uri.TryCreate(wDto.BuddyImage, UriKind.RelativeOrAbsolute, out bUri);

                        sd.WeaponBuddies[key] = new WeaponBuddyInfo
                        {
                            Name = wDto.BuddyName,
                            Image = bUri
                        };
                    }
                }
            }

            AssignWeapon("Classic", (u, n) => { sd.ClassicImage = u; sd.ClassicName = n; });
            AssignWeapon("Shorty", (u, n) => { sd.ShortyImage = u; sd.ShortyName = n; });
            AssignWeapon("Frenzy", (u, n) => { sd.FrenzyImage = u; sd.FrenzyName = n; });
            AssignWeapon("Ghost", (u, n) => { sd.GhostImage = u; sd.GhostName = n; });
            AssignWeapon("Bandit", (u, n) => { sd.BanditImage = u; sd.BanditName = n; });
            AssignWeapon("Sheriff", (u, n) => { sd.SheriffImage = u; sd.SheriffName = n; });
            AssignWeapon("Stinger", (u, n) => { sd.StingerImage = u; sd.StingerName = n; });
            AssignWeapon("Spectre", (u, n) => { sd.SpectreImage = u; sd.SpectreName = n; });
            AssignWeapon("Bucky", (u, n) => { sd.BuckyImage = u; sd.BuckyName = n; });
            AssignWeapon("Judge", (u, n) => { sd.JudgeImage = u; sd.JudgeName = n; });
            AssignWeapon("Bulldog", (u, n) => { sd.BulldogImage = u; sd.BulldogName = n; });
            AssignWeapon("Guardian", (u, n) => { sd.GuardianImage = u; sd.GuardianName = n; });
            AssignWeapon("Warden", (u, n) => { sd.WardenImage = u; sd.WardenName = n; });
            AssignWeapon("Phantom", (u, n) => { sd.PhantomImage = u; sd.PhantomName = n; });
            AssignWeapon("Vandal", (u, n) => { sd.VandalImage = u; sd.VandalName = n; });
            AssignWeapon("Marshal", (u, n) => { sd.MarshalImage = u; sd.MarshalName = n; });
            AssignWeapon("Outlaw", (u, n) => { sd.OutlawImage = u; sd.OutlawName = n; });
            AssignWeapon("Operator", (u, n) => { sd.OperatorImage = u; sd.OperatorName = n; });
            AssignWeapon("Ares", (u, n) => { sd.AresImage = u; sd.AresName = n; });
            AssignWeapon("Odin", (u, n) => { sd.OdinImage = u; sd.OdinName = n; });
            AssignWeapon("Melee", (u, n) => { sd.MeleeImage = u; sd.MeleeName = n; });
        }
        else if (Weapons != null)
        {
            if (!string.IsNullOrEmpty(CardImage) && Uri.TryCreate(CardImage, UriKind.RelativeOrAbsolute, out var cUri))
            {
                sd.LargeCardImage = cUri;
                sd.CardImage = cUri;
                sd.CardName = !string.IsNullOrEmpty(CardName) ? CardName : "Oyuncu Kartı";
            }

            sd.WeaponMetas = new Dictionary<string, ValSkinMeta>(StringComparer.OrdinalIgnoreCase);

            foreach (var w in Weapons)
            {
                if (w == null) continue;
                Uri imgUri = null;
                if (!string.IsNullOrEmpty(w.SkinImage))
                    Uri.TryCreate(w.SkinImage, UriKind.RelativeOrAbsolute, out imgUri);

                var sName = w.SkinName ?? "";
                var wName = w.WeaponName ?? "";

                void SetSlot(string key, Action<Uri, string> assigner)
                {
                    assigner(imgUri, sName);
                    sd.WeaponMetas[key] = new ValSkinMeta
                    {
                        SkinName = sName,
                        TierColor = !string.IsNullOrEmpty(w.TierColor) ? w.TierColor : "#7f8c8d",
                        TierDevName = "Standard",
                        RarityLabel = w.RarityLabel ?? "",
                        VpCost = w.VpCost,
                        IsRare = w.IsRare
                    };
                }

                if (sName.Contains("Vandal", StringComparison.OrdinalIgnoreCase) || wName.Contains("Vandal", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Vandal", (u, n) => { sd.VandalImage = u; sd.VandalName = n; });
                }
                else if (sName.Contains("Phantom", StringComparison.OrdinalIgnoreCase) || wName.Contains("Phantom", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Phantom", (u, n) => { sd.PhantomImage = u; sd.PhantomName = n; });
                }
                else if (sName.Contains("Sheriff", StringComparison.OrdinalIgnoreCase) || wName.Contains("Sheriff", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Sheriff", (u, n) => { sd.SheriffImage = u; sd.SheriffName = n; });
                }
                else if (sName.Contains("Spectre", StringComparison.OrdinalIgnoreCase) || wName.Contains("Spectre", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Spectre", (u, n) => { sd.SpectreImage = u; sd.SpectreName = n; });
                }
                else if (sName.Contains("Ghost", StringComparison.OrdinalIgnoreCase) || wName.Contains("Ghost", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Ghost", (u, n) => { sd.GhostImage = u; sd.GhostName = n; });
                }
                else if (sName.Contains("Operator", StringComparison.OrdinalIgnoreCase) || wName.Contains("Operator", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Operator", (u, n) => { sd.OperatorImage = u; sd.OperatorName = n; });
                }
                else if (sName.Contains("Classic", StringComparison.OrdinalIgnoreCase) || wName.Contains("Classic", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Classic", (u, n) => { sd.ClassicImage = u; sd.ClassicName = n; });
                }
                else if (sName.Contains("Marshal", StringComparison.OrdinalIgnoreCase) || wName.Contains("Marshal", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Marshal", (u, n) => { sd.MarshalImage = u; sd.MarshalName = n; });
                }
                else if (sName.Contains("Judge", StringComparison.OrdinalIgnoreCase) || wName.Contains("Judge", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Judge", (u, n) => { sd.JudgeImage = u; sd.JudgeName = n; });
                }
                else if (sName.Contains("Bucky", StringComparison.OrdinalIgnoreCase) || wName.Contains("Bucky", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Bucky", (u, n) => { sd.BuckyImage = u; sd.BuckyName = n; });
                }
                else if (sName.Contains("Odin", StringComparison.OrdinalIgnoreCase) || wName.Contains("Odin", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Odin", (u, n) => { sd.OdinImage = u; sd.OdinName = n; });
                }
                else if (sName.Contains("Ares", StringComparison.OrdinalIgnoreCase) || wName.Contains("Ares", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Ares", (u, n) => { sd.AresImage = u; sd.AresName = n; });
                }
                else if (sName.Contains("Outlaw", StringComparison.OrdinalIgnoreCase) || wName.Contains("Outlaw", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Outlaw", (u, n) => { sd.OutlawImage = u; sd.OutlawName = n; });
                }
                else if (sName.Contains("Bulldog", StringComparison.OrdinalIgnoreCase) || wName.Contains("Bulldog", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Bulldog", (u, n) => { sd.BulldogImage = u; sd.BulldogName = n; });
                }
                else if (sName.Contains("Guardian", StringComparison.OrdinalIgnoreCase) || wName.Contains("Guardian", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Guardian", (u, n) => { sd.GuardianImage = u; sd.GuardianName = n; });
                }
                else if (sName.Contains("Stinger", StringComparison.OrdinalIgnoreCase) || wName.Contains("Stinger", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Stinger", (u, n) => { sd.StingerImage = u; sd.StingerName = n; });
                }
                else if (sName.Contains("Frenzy", StringComparison.OrdinalIgnoreCase) || wName.Contains("Frenzy", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Frenzy", (u, n) => { sd.FrenzyImage = u; sd.FrenzyName = n; });
                }
                else if (sName.Contains("Shorty", StringComparison.OrdinalIgnoreCase) || wName.Contains("Shorty", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Shorty", (u, n) => { sd.ShortyImage = u; sd.ShortyName = n; });
                }
                else if (sName.Contains("Melee", StringComparison.OrdinalIgnoreCase) || wName.Contains("Melee", StringComparison.OrdinalIgnoreCase) ||
                         sName.Contains("Bıçak", StringComparison.OrdinalIgnoreCase) || wName.Contains("Bıçak", StringComparison.OrdinalIgnoreCase) ||
                         sName.Contains("Karambit", StringComparison.OrdinalIgnoreCase) || sName.Contains("Hançer", StringComparison.OrdinalIgnoreCase))
                {
                    SetSlot("Melee", (u, n) => { sd.MeleeImage = u; sd.MeleeName = n; });
                }
            }
        }

        // Fill standard silhouettes for any unequipped/missing weapons
        WeaponHelper.EnsureStandardWeaponFallbacks(sd);

        sd.ApplySlots();
        SkinData = sd;
        return sd;
    }
}

public class PlayerSkinItemDto
{
    public string WeaponKey { get; set; } = "";
    public string SkinName { get; set; } = "";
    public string SkinImage { get; set; } = "";
    public string TierColor { get; set; } = "#7f8c8d";
    public string TierDevName { get; set; } = "Standard";
    public string RarityLabel { get; set; } = "";
    public int VpCost { get; set; }
    public bool IsRare { get; set; }
    public string BuddyName { get; set; }
    public string BuddyImage { get; set; }
}

public class PlayerFullSkinDataDto
{
    public string CardId { get; set; }
    public string CardImage { get; set; }
    public string LargeCardImage { get; set; }
    public string CardName { get; set; }

    public string Spray1Image { get; set; }
    public string Spray1Name { get; set; }
    public string Spray2Image { get; set; }
    public string Spray2Name { get; set; }
    public string Spray3Image { get; set; }
    public string Spray3Name { get; set; }
    public string Spray4Image { get; set; }
    public string Spray4Name { get; set; }

    public Dictionary<string, PlayerSkinItemDto> WeaponSkins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int TotalInventoryValueVp { get; set; }
}

public class MatchHistoryItem : ObservableObject
{
    public string MatchId { get; set; } = "";
    public string Date { get; set; } = "";
    public string RelativeTime { get; set; } = "";
    public long Timestamp { get; set; }
    public string MapName { get; set; } = "";
    public string MapImage { get; set; } = "";
    public string GameMode { get; set; } = "";
    public string GameModeIcon { get; set; } = "";
    public string Score { get; set; } = "";
    public string Result { get; set; } = "OYNANDI";
    public string ResultColor { get; set; } = "#32e2b2";
    public string ResultBadgeBg { get; set; } = "#1c2b38";
    public int TotalInventoryVp { get; set; }
    public int TotalInventoryTl { get; set; }
    public string TotalInventoryCombinedText { get; set; } = "";
    public string MyAgentName { get; set; } = "";
    public string MyAgentIcon { get; set; } = "";
    public string MyKda { get; set; } = "";

    public ObservableCollection<MatchHistoryPlayerItem> Players { get; set; } = new();

    public IEnumerable<MatchHistoryPlayerItem> Allies => Players.Where(p => p.IsAlly);
    public IEnumerable<MatchHistoryPlayerItem> Enemies => Players.Where(p => !p.IsAlly);

    public int AllyTotalVp => Allies.Sum(p => p.TotalInventoryValueVp);
    public int EnemyTotalVp => Enemies.Sum(p => p.TotalInventoryValueVp);
}

