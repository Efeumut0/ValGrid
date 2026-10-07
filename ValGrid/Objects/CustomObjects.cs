using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using ValGrid.Helpers;

namespace ValGrid.Objects;


public class IgnData : ObservableObject
{
    private Visibility _trackerDisabled = Visibility.Visible;
    public Visibility TrackerDisabled
    {
        get => _trackerDisabled;
        set => SetProperty(ref _trackerDisabled, value);
    }

    private Visibility _trackerEnabled = Visibility.Collapsed;
    public Visibility TrackerEnabled
    {
        get => _trackerEnabled;
        set => SetProperty(ref _trackerEnabled, value);
    }

    private Uri _trackerUri;
    public Uri TrackerUri
    {
        get => _trackerUri;
        set => SetProperty(ref _trackerUri, value);
    }

    private string _username = "";
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private bool _isStreamerMode;
    public bool IsStreamerMode
    {
        get => _isStreamerMode;
        set => SetProperty(ref _isStreamerMode, value);
    }

    private bool _isUnmaskedFromMemory;
    public bool IsUnmaskedFromMemory
    {
        get => _isUnmaskedFromMemory;
        set => SetProperty(ref _isUnmaskedFromMemory, value);
    }

    private string _tooltip;
    public string Tooltip
    {
        get => _tooltip ?? (TrackerEnabled == Visibility.Visible ? Properties.Resources.TrackerToolTip : null);
        set => SetProperty(ref _tooltip, value);
    }
}

public class IdentityData : ObservableObject
{
    private Uri _image;
    public Uri Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    private string _name = "";
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}

public class PlayerUIData : ObservableObject
{
    private string _backgroundColour = "#252A40";
    public string BackgroundColour
    {
        get => _backgroundColour;
        set => SetProperty(ref _backgroundColour, value);
    }

    private string _partyColour = "Transparent";
    public string PartyColour
    {
        get => _partyColour;
        set => SetProperty(ref _partyColour, value);
    }

    private Guid _partyUuid;
    public Guid PartyUuid
    {
        get => _partyUuid;
        set => SetProperty(ref _partyUuid, value);
    }

    private Guid _puuid;
    public Guid Puuid
    {
        get => _puuid;
        set => SetProperty(ref _puuid, value);
    }

    private string _partyName = "";
    public string PartyName
    {
        get => _partyName;
        set => SetProperty(ref _partyName, value);
    }

    private string _partyTooltip = "";
    public string PartyTooltip
    {
        get => _partyTooltip;
        set => SetProperty(ref _partyTooltip, value);
    }

    private string _partyMemberNames = "";
    public string PartyMemberNames
    {
        get => _partyMemberNames;
        set => SetProperty(ref _partyMemberNames, value);
    }

    private int _partySize;
    public int PartySize
    {
        get => _partySize;
        set => SetProperty(ref _partySize, value);
    }

    private bool _isInParty;
    public bool IsInParty
    {
        get => _isInParty;
        set
        {
            if (SetProperty(ref _isInParty, value))
            {
                OnPropertyChanged(nameof(PartyBorderVisibility));
            }
        }
    }

    public Visibility PartyBorderVisibility => IsInParty ? Visibility.Visible : Visibility.Collapsed;
}


public class SeasonData : ObservableObject
{
    private Guid _currentSeason;
    public Guid CurrentSeason
    {
        get => _currentSeason;
        set => SetProperty(ref _currentSeason, value);
    }

    private Guid _previouspreviouspreviousSeason;
    public Guid PreviouspreviouspreviousSeason
    {
        get => _previouspreviouspreviousSeason;
        set => SetProperty(ref _previouspreviouspreviousSeason, value);
    }

    private Guid _previouspreviousSeason;
    public Guid PreviouspreviousSeason
    {
        get => _previouspreviousSeason;
        set => SetProperty(ref _previouspreviousSeason, value);
    }

    private Guid _previousSeason;
    public Guid PreviousSeason
    {
        get => _previousSeason;
        set => SetProperty(ref _previousSeason, value);
    }
}

public class SkinData : ObservableObject
{
    private Uri _aresImage;
    public Uri AresImage { get => _aresImage; set => SetProperty(ref _aresImage, value); }
    private string _aresName = "";
    public string AresName { get => _aresName; set => SetProperty(ref _aresName, value); }

    private Uri _buckyImage;
    public Uri BuckyImage { get => _buckyImage; set => SetProperty(ref _buckyImage, value); }
    private string _buckyName = "";
    public string BuckyName { get => _buckyName; set => SetProperty(ref _buckyName, value); }

    private Uri _banditImage;
    public Uri BanditImage { get => _banditImage; set => SetProperty(ref _banditImage, value); }
    private string _banditName = "";
    public string BanditName { get => _banditName; set => SetProperty(ref _banditName, value); }

    private Uri _wardenImage;
    public Uri WardenImage { get => _wardenImage; set => SetProperty(ref _wardenImage, value); }
    private string _wardenName = "";
    public string WardenName { get => _wardenName; set => SetProperty(ref _wardenName, value); }

    private Uri _bulldogImage;
    public Uri BulldogImage { get => _bulldogImage; set => SetProperty(ref _bulldogImage, value); }
    private string _bulldogName = "";
    public string BulldogName { get => _bulldogName; set => SetProperty(ref _bulldogName, value); }

    private Uri _cardImage;
    public Uri CardImage { get => _cardImage; set => SetProperty(ref _cardImage, value); }
    private string _cardName = "";
    public string CardName { get => _cardName; set => SetProperty(ref _cardName, value); }

    private Uri _classicImage;
    public Uri ClassicImage { get => _classicImage; set => SetProperty(ref _classicImage, value); }
    private string _classicName = "";
    public string ClassicName { get => _classicName; set => SetProperty(ref _classicName, value); }

    private Uri _frenzyImage;
    public Uri FrenzyImage { get => _frenzyImage; set => SetProperty(ref _frenzyImage, value); }
    private string _frenzyName = "";
    public string FrenzyName { get => _frenzyName; set => SetProperty(ref _frenzyName, value); }

    private Uri _ghostImage;
    public Uri GhostImage { get => _ghostImage; set => SetProperty(ref _ghostImage, value); }
    private string _ghostName = "";
    public string GhostName { get => _ghostName; set => SetProperty(ref _ghostName, value); }

    private Uri _guardianImage;
    public Uri GuardianImage { get => _guardianImage; set => SetProperty(ref _guardianImage, value); }
    private string _guardianName = "";
    public string GuardianName { get => _guardianName; set => SetProperty(ref _guardianName, value); }

    private Uri _judgeImage;
    public Uri JudgeImage { get => _judgeImage; set => SetProperty(ref _judgeImage, value); }
    private string _judgeName = "";
    public string JudgeName { get => _judgeName; set => SetProperty(ref _judgeName, value); }

    private Uri _largeCardImage;
    public Uri LargeCardImage { get => _largeCardImage; set => SetProperty(ref _largeCardImage, value); }

    private Uri _marshalImage;
    public Uri MarshalImage { get => _marshalImage; set => SetProperty(ref _marshalImage, value); }
    private string _marshalName = "";
    public string MarshalName { get => _marshalName; set => SetProperty(ref _marshalName, value); }

    private Uri _outlawImage;
    public Uri OutlawImage { get => _outlawImage; set => SetProperty(ref _outlawImage, value); }
    private string _outlawName = "";
    public string OutlawName { get => _outlawName; set => SetProperty(ref _outlawName, value); }

    private Uri _meleeImage;
    public Uri MeleeImage { get => _meleeImage; set => SetProperty(ref _meleeImage, value); }
    private string _meleeName = "";
    public string MeleeName { get => _meleeName; set => SetProperty(ref _meleeName, value); }

    private Uri _odinImage;
    public Uri OdinImage { get => _odinImage; set => SetProperty(ref _odinImage, value); }
    private string _odinName = "";
    public string OdinName { get => _odinName; set => SetProperty(ref _odinName, value); }

    private Uri _operatorImage;
    public Uri OperatorImage { get => _operatorImage; set => SetProperty(ref _operatorImage, value); }
    private string _operatorName = "";
    public string OperatorName { get => _operatorName; set => SetProperty(ref _operatorName, value); }

    private Uri _phantomImage;
    public Uri PhantomImage { get => _phantomImage; set => SetProperty(ref _phantomImage, value); }
    private string _phantomName = "";
    public string PhantomName { get => _phantomName; set => SetProperty(ref _phantomName, value); }

    private Uri _sheriffImage;
    public Uri SheriffImage { get => _sheriffImage; set => SetProperty(ref _sheriffImage, value); }
    private string _sheriffName = "";
    public string SheriffName { get => _sheriffName; set => SetProperty(ref _sheriffName, value); }

    private Uri _shortyImage;
    public Uri ShortyImage { get => _shortyImage; set => SetProperty(ref _shortyImage, value); }
    private string _shortyName = "";
    public string ShortyName { get => _shortyName; set => SetProperty(ref _shortyName, value); }

    private Uri _spectreImage;
    public Uri SpectreImage { get => _spectreImage; set => SetProperty(ref _spectreImage, value); }
    private string _spectreName = "";
    public string SpectreName { get => _spectreName; set => SetProperty(ref _spectreName, value); }

    private Uri _spray1Image;
    public Uri Spray1Image { get => _spray1Image; set => SetProperty(ref _spray1Image, value); }
    private string _spray1Name = "";
    public string Spray1Name { get => _spray1Name; set => SetProperty(ref _spray1Name, value); }

    private Uri _spray2Image;
    public Uri Spray2Image { get => _spray2Image; set => SetProperty(ref _spray2Image, value); }
    private string _spray2Name = "";
    public string Spray2Name { get => _spray2Name; set => SetProperty(ref _spray2Name, value); }

    private Uri _spray3Image;
    public Uri Spray3Image { get => _spray3Image; set => SetProperty(ref _spray3Image, value); }
    private string _spray3Name = "";
    public string Spray3Name { get => _spray3Name; set => SetProperty(ref _spray3Name, value); }

    private Uri _spray4Image;
    public Uri Spray4Image { get => _spray4Image; set => SetProperty(ref _spray4Image, value); }
    private string _spray4Name = "";
    public string Spray4Name { get => _spray4Name; set => SetProperty(ref _spray4Name, value); }

    private Uri _stingerImage;
    public Uri StingerImage { get => _stingerImage; set => SetProperty(ref _stingerImage, value); }
    private string _stingerName = "";
    public string StingerName { get => _stingerName; set => SetProperty(ref _stingerName, value); }

    private Uri _vandalImage;
    public Uri VandalImage { get => _vandalImage; set => SetProperty(ref _vandalImage, value); }
    private string _vandalName = "";
    public string VandalName { get => _vandalName; set => SetProperty(ref _vandalName, value); }

    private Uri _slot1Image;
    public Uri Slot1Image { get => _slot1Image; set => SetProperty(ref _slot1Image, value); }
    private string _slot1Name = "";
    public string Slot1Name { get => _slot1Name; set => SetProperty(ref _slot1Name, value); }

    private Uri _slot2Image;
    public Uri Slot2Image { get => _slot2Image; set => SetProperty(ref _slot2Image, value); }
    private string _slot2Name = "";
    public string Slot2Name { get => _slot2Name; set => SetProperty(ref _slot2Name, value); }

    private Uri _slot3Image;
    public Uri Slot3Image { get => _slot3Image; set => SetProperty(ref _slot3Image, value); }
    private string _slot3Name = "";
    public string Slot3Name { get => _slot3Name; set => SetProperty(ref _slot3Name, value); }

    private Uri _slot4Image;
    public Uri Slot4Image { get => _slot4Image; set => SetProperty(ref _slot4Image, value); }
    private string _slot4Name = "";
    public string Slot4Name { get => _slot4Name; set => SetProperty(ref _slot4Name, value); }

    // Inventory Value & Rare Skin properties
    private int _totalInventoryValueVp;
    public int TotalInventoryValueVp
    {
        get => _totalInventoryValueVp;
        set
        {
            if (SetProperty(ref _totalInventoryValueVp, value))
            {
                TotalValueFormatted = CurrencyHelper.FormatVp(value);
                TotalValueTlFormatted = CurrencyHelper.FormatTl(value);
                InventoryValueVisibility = Visibility.Visible;
            }
        }
    }

    private string _totalValueFormatted = "0 VP";
    public string TotalValueFormatted
    {
        get => _totalValueFormatted;
        set => SetProperty(ref _totalValueFormatted, value);
    }

    private string _totalValueTlFormatted = "0 TL";
    public string TotalValueTlFormatted
    {
        get => _totalValueTlFormatted;
        set => SetProperty(ref _totalValueTlFormatted, value);
    }

    private string _inventorySummaryTooltip = "";
    public string InventorySummaryTooltip
    {
        get => _inventorySummaryTooltip;
        set => SetProperty(ref _inventorySummaryTooltip, value);
    }

    private Visibility _inventoryValueVisibility = Visibility.Collapsed;
    public Visibility InventoryValueVisibility
    {
        get => _inventoryValueVisibility;
        set => SetProperty(ref _inventoryValueVisibility, value);
    }

    private bool _hasUltraRareSkin;
    public bool HasUltraRareSkin
    {
        get => _hasUltraRareSkin;
        set
        {
            if (SetProperty(ref _hasUltraRareSkin, value))
            {
                RareBadgeVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                RarityBorderBrush = value ? "#ffd700" : "#3a4266";
                RarityTextBrush = value ? "#ffd700" : "#32e2b2";
            }
        }
    }

    private string _rareBadgeIcon = "⭐";
    public string RareBadgeIcon
    {
        get => _rareBadgeIcon;
        set => SetProperty(ref _rareBadgeIcon, value);
    }

    private string _collectionType = "Koleksiyon";
    public string CollectionType
    {
        get => _collectionType;
        set => SetProperty(ref _collectionType, value);
    }


    private Visibility _rareBadgeVisibility = Visibility.Collapsed;
    public Visibility RareBadgeVisibility
    {
        get => _rareBadgeVisibility;
        set => SetProperty(ref _rareBadgeVisibility, value);
    }

    private string _rarityBorderBrush = "#3a4266";
    public string RarityBorderBrush
    {
        get => _rarityBorderBrush;
        set => SetProperty(ref _rarityBorderBrush, value);
    }

    private string _rarityTextBrush = "#32e2b2";
    public string RarityTextBrush
    {
        get => _rarityTextBrush;
        set => SetProperty(ref _rarityTextBrush, value);
    }

    // Per-slot tooltips and rare indicators
    private string _slot1Tooltip = "";
    public string Slot1Tooltip { get => _slot1Tooltip; set => SetProperty(ref _slot1Tooltip, value); }
    private bool _slot1IsRare;
    public bool Slot1IsRare { get => _slot1IsRare; set => SetProperty(ref _slot1IsRare, value); }
    private string _slot1BorderBrush = "Transparent";
    public string Slot1BorderBrush { get => _slot1BorderBrush; set => SetProperty(ref _slot1BorderBrush, value); }
    private Thickness _slot1BorderThickness = new(0);
    public Thickness Slot1BorderThickness { get => _slot1BorderThickness; set => SetProperty(ref _slot1BorderThickness, value); }
    private Visibility _slot1StarVisibility = Visibility.Collapsed;
    public Visibility Slot1StarVisibility { get => _slot1StarVisibility; set => SetProperty(ref _slot1StarVisibility, value); }

    private string _slot2Tooltip = "";
    public string Slot2Tooltip { get => _slot2Tooltip; set => SetProperty(ref _slot2Tooltip, value); }
    private bool _slot2IsRare;
    public bool Slot2IsRare { get => _slot2IsRare; set => SetProperty(ref _slot2IsRare, value); }
    private string _slot2BorderBrush = "Transparent";
    public string Slot2BorderBrush { get => _slot2BorderBrush; set => SetProperty(ref _slot2BorderBrush, value); }
    private Thickness _slot2BorderThickness = new(0);
    public Thickness Slot2BorderThickness { get => _slot2BorderThickness; set => SetProperty(ref _slot2BorderThickness, value); }
    private Visibility _slot2StarVisibility = Visibility.Collapsed;
    public Visibility Slot2StarVisibility { get => _slot2StarVisibility; set => SetProperty(ref _slot2StarVisibility, value); }

    private string _slot3Tooltip = "";
    public string Slot3Tooltip { get => _slot3Tooltip; set => SetProperty(ref _slot3Tooltip, value); }
    private bool _slot3IsRare;
    public bool Slot3IsRare { get => _slot3IsRare; set => SetProperty(ref _slot3IsRare, value); }
    private string _slot3BorderBrush = "Transparent";
    public string Slot3BorderBrush { get => _slot3BorderBrush; set => SetProperty(ref _slot3BorderBrush, value); }
    private Thickness _slot3BorderThickness = new(0);
    public Thickness Slot3BorderThickness { get => _slot3BorderThickness; set => SetProperty(ref _slot3BorderThickness, value); }
    private Visibility _slot3StarVisibility = Visibility.Collapsed;
    public Visibility Slot3StarVisibility { get => _slot3StarVisibility; set => SetProperty(ref _slot3StarVisibility, value); }

    private string _slot4Tooltip = "";
    public string Slot4Tooltip { get => _slot4Tooltip; set => SetProperty(ref _slot4Tooltip, value); }
    private bool _slot4IsRare;
    public bool Slot4IsRare { get => _slot4IsRare; set => SetProperty(ref _slot4IsRare, value); }
    private string _slot4BorderBrush = "Transparent";
    public string Slot4BorderBrush { get => _slot4BorderBrush; set => SetProperty(ref _slot4BorderBrush, value); }
    private Thickness _slot4BorderThickness = new(0);
    public Thickness Slot4BorderThickness { get => _slot4BorderThickness; set => SetProperty(ref _slot4BorderThickness, value); }
    private Visibility _slot4StarVisibility = Visibility.Collapsed;
    public Visibility Slot4StarVisibility { get => _slot4StarVisibility; set => SetProperty(ref _slot4StarVisibility, value); }

    public Dictionary<string, ValSkinMeta> WeaponMetas { get; set; } = new();
    public Dictionary<string, WeaponBuddyInfo> WeaponBuddies { get; set; } = new();

    public void ApplySlots()
    {
        (Slot1Image, Slot1Name) = Resolve(ValGrid.Properties.Settings.Default.Weapon1);
        (Slot2Image, Slot2Name) = Resolve(ValGrid.Properties.Settings.Default.Weapon2);
        (Slot3Image, Slot3Name) = Resolve(ValGrid.Properties.Settings.Default.Weapon3);
        (Slot4Image, Slot4Name) = Resolve(ValGrid.Properties.Settings.Default.Weapon4);

        ApplySlotMeta(1, ValGrid.Properties.Settings.Default.Weapon1);
        ApplySlotMeta(2, ValGrid.Properties.Settings.Default.Weapon2);
        ApplySlotMeta(3, ValGrid.Properties.Settings.Default.Weapon3);
        ApplySlotMeta(4, ValGrid.Properties.Settings.Default.Weapon4);
    }

    private void ApplySlotMeta(int slot, string weapon)
    {
        ValSkinMeta meta = null;
        if (WeaponMetas != null)
            WeaponMetas.TryGetValue(weapon, out meta);

        var name = slot switch { 1 => Slot1Name, 2 => Slot2Name, 3 => Slot3Name, _ => Slot4Name };
        var isRare = meta?.IsRare ?? false;
        var skinTitle = WeaponHelper.FormatSkinTitle(meta?.ChromaName, meta?.SkinName, name, weapon);
        var tooltip = meta != null && meta.VpCost > 0
            ? (isRare
                ? $"⭐ [{meta.RarityLabel}] {skinTitle}\nFiyat: {CurrencyHelper.FormatVpAndTl(meta.VpCost)} | Seri: {meta.TierDevName} Edition"
                : $"{skinTitle}\nFiyat: {CurrencyHelper.FormatVpAndTl(meta.VpCost)} | Seri: {meta.TierDevName} Edition")
            : (!string.IsNullOrEmpty(skinTitle) ? skinTitle : WeaponHelper.GetTurkishWeaponName(weapon));

        var borderBrush = isRare ? "#ffd700" : "Transparent";
        var borderThickness = isRare ? new Thickness(1.5) : new Thickness(0);
        var starVis = isRare ? Visibility.Visible : Visibility.Collapsed;

        switch (slot)
        {
            case 1:
                Slot1Tooltip = tooltip;
                Slot1IsRare = isRare;
                Slot1BorderBrush = borderBrush;
                Slot1BorderThickness = borderThickness;
                Slot1StarVisibility = starVis;
                break;
            case 2:
                Slot2Tooltip = tooltip;
                Slot2IsRare = isRare;
                Slot2BorderBrush = borderBrush;
                Slot2BorderThickness = borderThickness;
                Slot2StarVisibility = starVis;
                break;
            case 3:
                Slot3Tooltip = tooltip;
                Slot3IsRare = isRare;
                Slot3BorderBrush = borderBrush;
                Slot3BorderThickness = borderThickness;
                Slot3StarVisibility = starVis;
                break;
            case 4:
                Slot4Tooltip = tooltip;
                Slot4IsRare = isRare;
                Slot4BorderBrush = borderBrush;
                Slot4BorderThickness = borderThickness;
                Slot4StarVisibility = starVis;
                break;
        }
    }


    private (Uri, string) Resolve(string weapon) =>
        weapon switch
        {
            "Classic" => (ClassicImage, ClassicName),
            "Shorty" => (ShortyImage, ShortyName),
            "Frenzy" => (FrenzyImage, FrenzyName),
            "Ghost" => (GhostImage, GhostName),
            "Bandit" => (BanditImage, BanditName),
            "Sheriff" => (SheriffImage, SheriffName),
            "Stinger" => (StingerImage, StingerName),
            "Spectre" => (SpectreImage, SpectreName),
            "Bucky" => (BuckyImage, BuckyName),
            "Judge" => (JudgeImage, JudgeName),
            "Bulldog" => (BulldogImage, BulldogName),
            "Guardian" => (GuardianImage, GuardianName),
            "Warden" => (WardenImage, WardenName),
            "Phantom" => (PhantomImage, PhantomName),
            "Vandal" => (VandalImage, VandalName),
            "Marshal" => (MarshalImage, MarshalName),
            "Outlaw" => (OutlawImage, OutlawName),
            "Operator" => (OperatorImage, OperatorName),
            "Ares" => (AresImage, AresName),
            "Odin" => (OdinImage, OdinName),
            _ => (VandalImage, VandalName)
        };
}

public class RankData : ObservableObject
{
    private int _maxRr = 100;
    public int MaxRr { get => _maxRr; set => SetProperty(ref _maxRr, value); }

    private Uri[] _rankImages;
    public Uri[] RankImages { get => _rankImages; set => SetProperty(ref _rankImages, value); }

    private string[] _rankNames;
    public string[] RankNames { get => _rankNames; set => SetProperty(ref _rankNames, value); }

    private Uri _peakRankImage;
    public Uri PeakRankImage { get => _peakRankImage; set => SetProperty(ref _peakRankImage, value); }

    private string _peakRankName = "UNRATED";
    public string PeakRankName { get => _peakRankName; set => SetProperty(ref _peakRankName, value); }

    private string _peakRankTooltip = "Peak: UNRATED";
    public string PeakRankTooltip { get => _peakRankTooltip; set => SetProperty(ref _peakRankTooltip, value); }
}

public class MatchHistoryData : ObservableObject
{
    private int[] _previousGames;
    public int[] PreviousGames { get => _previousGames; set => SetProperty(ref _previousGames, value); }

    private string[] _previousGameColours;
    public string[] PreviousGameColours { get => _previousGameColours; set => SetProperty(ref _previousGameColours, value); }

    private int _rankProgress;
    public int RankProgress { get => _rankProgress; set => SetProperty(ref _rankProgress, value); }
}

public class ValMap
{
    public string Name { get; set; } = "";
    public Guid UUID { get; set; }
}

public class ValCard
{
    public string Name { get; set; } = "Undefined";
    public Uri Image { get; set; } =
        new Uri("https://media.valorant-api.com/sprays/472693f9-4d87-416b-9def-3fbe2d310cc0/displayicon.png");
    public Uri FullImage { get; set; } =
        new Uri("https://media.valorant-api.com/sprays/472693f9-4d87-416b-9def-3fbe2d310cc0/displayicon.png");
}

public class ValNameImage
{
    public string Name { get; set; } = "Kuşanılmadı";
    public Uri Image { get; set; } = null;
}

public class MatchDetails : ObservableObject
{
    private string _gameMode = "";
    public string GameMode { get => _gameMode; set => SetProperty(ref _gameMode, value); }

    private Uri _gameModeImage;
    public Uri GameModeImage { get => _gameModeImage; set => SetProperty(ref _gameModeImage, value); }

    private string _map = "";
    public string Map { get => _map; set => SetProperty(ref _map, value); }

    private Uri _mapImage;
    public Uri MapImage { get => _mapImage; set => SetProperty(ref _mapImage, value); }

    private int _lobbyTotalValueVp;
    public int LobbyTotalValueVp
    {
        get => _lobbyTotalValueVp;
        set
        {
            if (SetProperty(ref _lobbyTotalValueVp, value))
            {
                LobbyTotalValueVpText = CurrencyHelper.FormatVp(value);
                LobbyTotalValueTlText = CurrencyHelper.FormatTl(value);
                LobbyValueVisibility = value > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private string _lobbyTotalValueVpText = "0 VP";
    public string LobbyTotalValueVpText
    {
        get => _lobbyTotalValueVpText;
        set => SetProperty(ref _lobbyTotalValueVpText, value);
    }

    private string _lobbyTotalValueTlText = "0 TL";
    public string LobbyTotalValueTlText
    {
        get => _lobbyTotalValueTlText;
        set => SetProperty(ref _lobbyTotalValueTlText, value);
    }

    private string _teamBlueLabel = "Sol Takım: ";
    public string TeamBlueLabel
    {
        get => _teamBlueLabel;
        set
        {
            if (SetProperty(ref _teamBlueLabel, value))
            {
                RecalculateTeamRatios();
            }
        }
    }

    private string _teamRedLabel = "Sağ Takım: ";
    public string TeamRedLabel
    {
        get => _teamRedLabel;
        set
        {
            if (SetProperty(ref _teamRedLabel, value))
            {
                RecalculateTeamRatios();
            }
        }
    }

    private int _teamBlueValueVp;
    public int TeamBlueValueVp
    {
        get => _teamBlueValueVp;
        set
        {
            if (SetProperty(ref _teamBlueValueVp, value))
            {
                TeamBlueValueText = CurrencyHelper.FormatVp(value);
                TeamBlueValueCombinedText = $"{CurrencyHelper.FormatVp(value)} (~{CurrencyHelper.FormatTl(value)})";
                RecalculateTeamRatios();
            }
        }
    }

    private string _teamBlueValueText = "0 VP";
    public string TeamBlueValueText
    {
        get => _teamBlueValueText;
        set => SetProperty(ref _teamBlueValueText, value);
    }

    private string _teamBlueValueCombinedText = "0 VP (0 TL)";
    public string TeamBlueValueCombinedText
    {
        get => _teamBlueValueCombinedText;
        set => SetProperty(ref _teamBlueValueCombinedText, value);
    }

    private int _teamRedValueVp;
    public int TeamRedValueVp
    {
        get => _teamRedValueVp;
        set
        {
            if (SetProperty(ref _teamRedValueVp, value))
            {
                TeamRedValueText = CurrencyHelper.FormatVp(value);
                TeamRedValueCombinedText = $"{CurrencyHelper.FormatVp(value)} (~{CurrencyHelper.FormatTl(value)})";
                RecalculateTeamRatios();
            }
        }
    }

    private string _teamRedValueText = "0 VP";
    public string TeamRedValueText
    {
        get => _teamRedValueText;
        set => SetProperty(ref _teamRedValueText, value);
    }

    private string _teamRedValueCombinedText = "0 VP (0 TL)";
    public string TeamRedValueCombinedText
    {
        get => _teamRedValueCombinedText;
        set => SetProperty(ref _teamRedValueCombinedText, value);
    }

    private double _teamBluePercent = 50.0;
    public double TeamBluePercent
    {
        get => _teamBluePercent;
        set => SetProperty(ref _teamBluePercent, value);
    }

    private double _teamRedPercent = 50.0;
    public double TeamRedPercent
    {
        get => _teamRedPercent;
        set => SetProperty(ref _teamRedPercent, value);
    }

    private string _teamRatioTooltip = "";
    public string TeamRatioTooltip
    {
        get => _teamRatioTooltip;
        set => SetProperty(ref _teamRatioTooltip, value);
    }

    private Visibility _teamRatioVisibility = Visibility.Collapsed;
    public Visibility TeamRatioVisibility
    {
        get => _teamRatioVisibility;
        set => SetProperty(ref _teamRatioVisibility, value);
    }

    public void RecalculateTeamRatios()
    {
        int total = _teamBlueValueVp + _teamRedValueVp;
        if (total > 0)
        {
            TeamBluePercent = Math.Round((double)_teamBlueValueVp / total * 100.0, 1);
            TeamRedPercent = Math.Round(100.0 - TeamBluePercent, 1);
            TeamRatioTooltip = $"Takım Envanter Değeri Dağılımı:\n• {TeamBlueLabel}%{TeamBluePercent:F0} ({CurrencyHelper.FormatVp(_teamBlueValueVp)})\n• {TeamRedLabel}%{TeamRedPercent:F0} ({CurrencyHelper.FormatVp(_teamRedValueVp)})";
            TeamRatioVisibility = Visibility.Visible;
        }
        else
        {
            TeamBluePercent = 50.0;
            TeamRedPercent = 50.0;
            TeamRatioTooltip = "Henüz envanter verisi hesaplanmadı";
            TeamRatioVisibility = Visibility.Collapsed;
        }
    }


    private Visibility _lobbyValueVisibility = Visibility.Collapsed;
    public Visibility LobbyValueVisibility
    {
        get => _lobbyValueVisibility;
        set => SetProperty(ref _lobbyValueVisibility, value);
    }

    // Server & Connection Details
    private string _serverRegion = "";
    public string ServerRegion { get => _serverRegion; set => SetProperty(ref _serverRegion, value); }

    private string _serverPodId = "";
    public string ServerPodId { get => _serverPodId; set => SetProperty(ref _serverPodId, value); }

    private string _serverIpPort = "";
    public string ServerIpPort { get => _serverIpPort; set => SetProperty(ref _serverIpPort, value); }

    private string _serverHost = "";
    public string ServerHost { get => _serverHost; set => SetProperty(ref _serverHost, value); }

    private int _serverPort;
    public int ServerPort { get => _serverPort; set => SetProperty(ref _serverPort, value); }

    private string _serverPing = "-- ms";
    public string ServerPing { get => _serverPing; set => SetProperty(ref _serverPing, value); }

    private string _serverPingColor = "#32e2b2";
    public string ServerPingColor { get => _serverPingColor; set => SetProperty(ref _serverPingColor, value); }

    private string _playerKey = "";
    public string PlayerKey { get => _playerKey; set => SetProperty(ref _playerKey, value); }

    private string _playerKeyMasked = "";
    public string PlayerKeyMasked { get => _playerKeyMasked; set => SetProperty(ref _playerKeyMasked, value); }

    private Visibility _playerKeyVisibility = Visibility.Collapsed;
    public Visibility PlayerKeyVisibility { get => _playerKeyVisibility; set => SetProperty(ref _playerKeyVisibility, value); }

    private Visibility _serverInfoVisibility = Visibility.Collapsed;
    public Visibility ServerInfoVisibility { get => _serverInfoVisibility; set => SetProperty(ref _serverInfoVisibility, value); }

    private string _serverTooltip = "";
    public string ServerTooltip { get => _serverTooltip; set => SetProperty(ref _serverTooltip, value); }

    // Leaderboard (Sırasıyla Liderlik) Badge Properties
    private Visibility _leaderboardVisibility = Visibility.Collapsed;
    public Visibility LeaderboardVisibility
    {
        get => _leaderboardVisibility;
        set => SetProperty(ref _leaderboardVisibility, value);
    }

    private string _leaderboardTopSummary = "";
    public string LeaderboardTopSummary
    {
        get => _leaderboardTopSummary;
        set => SetProperty(ref _leaderboardTopSummary, value);
    }

    private string _leaderboardPreviewText = "";
    public string LeaderboardPreviewText
    {
        get => _leaderboardPreviewText;
        set => SetProperty(ref _leaderboardPreviewText, value);
    }

    private string _leaderboardTooltip = "";
    public string LeaderboardTooltip
    {
        get => _leaderboardTooltip;
        set => SetProperty(ref _leaderboardTooltip, value);
    }

    private string _leaderboardChatText = "";
    public string LeaderboardChatText
    {
        get => _leaderboardChatText;
        set => SetProperty(ref _leaderboardChatText, value);
    }

    private string _leaderboardMultiLineText = "";
    public string LeaderboardMultiLineText
    {
        get => _leaderboardMultiLineText;
        set => SetProperty(ref _leaderboardMultiLineText, value);
    }

    // Blacklist / Threat Alert Badge Properties
    private bool _hasBlacklistedPlayerInMatch;
    public bool HasBlacklistedPlayerInMatch
    {
        get => _hasBlacklistedPlayerInMatch;
        set => SetProperty(ref _hasBlacklistedPlayerInMatch, value);
    }

    private Visibility _blacklistAlertVisibility = Visibility.Collapsed;
    public Visibility BlacklistAlertVisibility
    {
        get => _blacklistAlertVisibility;
        set => SetProperty(ref _blacklistAlertVisibility, value);
    }

    private string _blacklistAlertText = "";
    public string BlacklistAlertText
    {
        get => _blacklistAlertText;
        set => SetProperty(ref _blacklistAlertText, value);
    }

    private string _blacklistAlertTooltip = "";
    public string BlacklistAlertTooltip
    {
        get => _blacklistAlertTooltip;
        set => SetProperty(ref _blacklistAlertTooltip, value);
    }
}


public class EncounterMatch : ObservableObject
{
    private string _matchId = "";
    public string MatchId { get => _matchId; set => SetProperty(ref _matchId, value); }

    private string _date = "";
    public string Date { get => _date; set => SetProperty(ref _date, value); }

    private string _relativeTime = "";
    public string RelativeTime
    {
        get => _relativeTime;
        set
        {
            if (SetProperty(ref _relativeTime, value))
            {
                OnPropertyChanged(nameof(RelativeTimeVisibility));
            }
        }
    }

    public Visibility RelativeTimeVisibility =>
        string.IsNullOrEmpty(RelativeTime) ? Visibility.Collapsed : Visibility.Visible;

    private string _map = "";
    public string Map { get => _map; set => SetProperty(ref _map, value); }

    private Uri _mapImage;
    public Uri MapImage
    {
        get => _mapImage;
        set
        {
            if (SetProperty(ref _mapImage, value))
            {
                OnPropertyChanged(nameof(MapImageVisibility));
                OnPropertyChanged(nameof(MapEmojiVisibility));
            }
        }
    }

    public Visibility MapImageVisibility => MapImage != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MapEmojiVisibility => MapImage == null ? Visibility.Visible : Visibility.Collapsed;

    private string _agentName = "";
    public string AgentName { get => _agentName; set => SetProperty(ref _agentName, value); }

    private Uri _agentIcon;
    public Uri AgentIcon { get => _agentIcon; set => SetProperty(ref _agentIcon, value); }

    private bool _isAlly;
    public bool IsAlly
    {
        get => _isAlly;
        set
        {
            if (SetProperty(ref _isAlly, value))
            {
                OnPropertyChanged(nameof(TeamText));
                OnPropertyChanged(nameof(TeamColor));
                OnPropertyChanged(nameof(AllyLogoVisibility));
                OnPropertyChanged(nameof(EnemyLogoVisibility));
            }
        }
    }

    private bool? _won;
    public bool? Won
    {
        get => _won;
        set
        {
            if (SetProperty(ref _won, value))
            {
                OnPropertyChanged(nameof(ResultText));
                OnPropertyChanged(nameof(ResultColor));
            }
        }
    }

    private string _score = "";
    public string Score
    {
        get => _score;
        set
        {
            if (SetProperty(ref _score, value))
            {
                OnPropertyChanged(nameof(ResultText));
            }
        }
    }

    public string ResultText
    {
        get
        {
            if (Won == true)
                return string.IsNullOrEmpty(Score) ? "KAZANDIK" : $"KAZANDIK ({Score})";
            if (Won == false)
                return string.IsNullOrEmpty(Score) ? "KAYBETTİK" : $"KAYBETTİK ({Score})";
            return string.IsNullOrEmpty(Score) ? "OYNANDI" : Score;
        }
    }
    public string ResultColor => Won == true ? "#32e2b2" : (Won == false ? "#f05454" : "#9f97b0");

    public string TeamText => IsAlly ? "Dost Takım" : "Rakip Takım";
    public string TeamColor => IsAlly ? "#32e2b2" : "#f0b232";

    public Visibility AllyLogoVisibility => IsAlly ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EnemyLogoVisibility => !IsAlly ? Visibility.Visible : Visibility.Collapsed;
}

public class EncounterData : ObservableObject
{
    private Visibility _visible = Visibility.Collapsed;
    public Visibility Visible { get => _visible; set => SetProperty(ref _visible, value); }

    private string _summary = "";
    public string Summary { get => _summary; set => SetProperty(ref _summary, value); }

    private string _tooltip = "";
    public string Tooltip { get => _tooltip; set => SetProperty(ref _tooltip, value); }

    private string _background = "#4c5b6e";
    public string Background
    {
        get => _background;
        set
        {
            if (SetProperty(ref _background, value))
            {
                TextColor = _background == "#4c5b6e" ? "#FFFFFF" : "#161926";
            }
        }
    }

    private string _textColor = "#FFFFFF";
    public string TextColor { get => _textColor; set => SetProperty(ref _textColor, value); }

    private string _note = "";
    public string Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
            {
                HasNote = !string.IsNullOrWhiteSpace(_note);
            }
        }
    }

    private bool _hasNote;
    public bool HasNote
    {
        get => _hasNote;
        set
        {
            if (SetProperty(ref _hasNote, value))
            {
                OnPropertyChanged(nameof(NoteVisibility));
            }
        }
    }

    public Visibility NoteVisibility => HasNote ? Visibility.Visible : Visibility.Collapsed;

    private Guid _puuid;
    public Guid Puuid { get => _puuid; set => SetProperty(ref _puuid, value); }

    private ObservableCollection<EncounterMatch> _recentMatches = new();
    public ObservableCollection<EncounterMatch> RecentMatches
    {
        get => _recentMatches;
        set
        {
            if (SetProperty(ref _recentMatches, value))
            {
                HasMatches = _recentMatches != null && _recentMatches.Count > 0;
            }
        }
    }

    private bool _hasMatches;
    public bool HasMatches
    {
        get => _hasMatches;
        set
        {
            if (SetProperty(ref _hasMatches, value))
            {
                OnPropertyChanged(nameof(NoMatchesVisibility));
            }
        }
    }

    public Visibility NoMatchesVisibility => HasMatches ? Visibility.Collapsed : Visibility.Visible;
}

public class Player : ObservableObject
{
    private static int _globalBlacklistCounter = 0;

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

    private static readonly Brush DefaultCardBg = new SolidColorBrush(Color.FromRgb(0x25, 0x2a, 0x40));
    private static readonly Brush FemaleCardBg = new SolidColorBrush(Color.FromRgb(0x35, 0x18, 0x29));
    private static readonly Brush FemaleBorderBrush = new SolidColorBrush(Color.FromRgb(0xff, 0x79, 0xc6));
    private static readonly Brush MaleCardBorderBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xbd, 0xf8));
    private static readonly Brush BlacklistCardBorderBrush = new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44));
    private static readonly Brush TransparentBrush = Brushes.Transparent;

    static Player()
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

        DefaultCardBg.Freeze();
        FemaleCardBg.Freeze();
        FemaleBorderBrush.Freeze();
        MaleCardBorderBrush.Freeze();
        BlacklistCardBorderBrush.Freeze();
    }

    private bool _isBlacklisted;
    public bool IsBlacklisted
    {
        get => _isBlacklisted;
        set
        {
            if (SetProperty(ref _isBlacklisted, value))
            {
                if (value)
                {
                    BlacklistOrder = System.Threading.Interlocked.Increment(ref _globalBlacklistCounter);
                }
                else
                {
                    BlacklistOrder = 0;
                }
                OnPropertyChanged(nameof(BlacklistBackground));
                OnPropertyChanged(nameof(BlacklistBorder));
                OnPropertyChanged(nameof(BlacklistIconBrush));
                OnPropertyChanged(nameof(BlacklistGlowOpacity));
                OnPropertyChanged(nameof(BlacklistTooltip));
                OnPropertyChanged(nameof(CardBorderBrush));
                OnPropertyChanged(nameof(CardBorderThickness));
                OnPropertyChanged(nameof(CardShadowBlur));
                OnPropertyChanged(nameof(CardShadowDepth));
                OnPropertyChanged(nameof(CardShadowColor));
                OnPropertyChanged(nameof(CardShadowOpacity));
                OnPropertyChanged(nameof(CardBackground));
            }
        }
    }

    public int BlacklistOrder { get; set; }

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
                OnPropertyChanged(nameof(CardBackground));
                OnPropertyChanged(nameof(CardBorderBrush));
                OnPropertyChanged(nameof(CardBorderThickness));
                OnPropertyChanged(nameof(CardShadowBlur));
                OnPropertyChanged(nameof(CardShadowDepth));
                OnPropertyChanged(nameof(CardShadowColor));
                OnPropertyChanged(nameof(CardShadowOpacity));
            }
        }
    }

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
                OnPropertyChanged(nameof(CardBorderBrush));
                OnPropertyChanged(nameof(CardBorderThickness));
            }
        }
    }

    private bool _isInMatch = false;
    [JsonIgnore]
    public bool IsInMatch
    {
        get => _isInMatch;
        set
        {
            if (SetProperty(ref _isInMatch, value))
            {
                OnPropertyChanged(nameof(FemaleButtonVisibility));
                OnPropertyChanged(nameof(MaleButtonVisibility));
                OnPropertyChanged(nameof(BlacklistButtonVisibility));
                OnPropertyChanged(nameof(WeaponBarVisibility));
                OnPropertyChanged(nameof(LobbyInfoVisibility));
                OnPropertyChanged(nameof(PartyOwnerVisibility));
                OnPropertyChanged(nameof(MatchDetailsVisibility));
            }
        }
    }

    private bool _isPartyOwner = false;
    [JsonIgnore]
    public bool IsPartyOwner
    {
        get => _isPartyOwner;
        set
        {
            if (SetProperty(ref _isPartyOwner, value))
            {
                OnPropertyChanged(nameof(PartyOwnerVisibility));
            }
        }
    }

    [JsonIgnore]
    public Visibility FemaleButtonVisibility => IsInMatch ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility MaleButtonVisibility => IsInMatch ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility BlacklistButtonVisibility => IsInMatch ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility WeaponBarVisibility => IsInMatch ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility LobbyInfoVisibility => !IsInMatch ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility PartyOwnerVisibility => (!IsInMatch && IsPartyOwner) ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility MatchDetailsVisibility => IsInMatch ? Visibility.Visible : Visibility.Collapsed;

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
        ? "Kız oyuncu etiketini kaldır (Etiketlendi ♀)"
        : "Kız oyuncu olarak kaydet (Kartı pembeleştir)";

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
        ? "Erkek oyuncu etiketini kaldır (Etiketlendi ♂)"
        : "Erkek oyuncu olarak kaydet (Mavi kenarlık ♂)";

    [JsonIgnore]
    public Brush CardBackground
    {
        get
        {
            if (IsFemale) return FemaleCardBg;
            if (PlayerUiData != null && !string.IsNullOrEmpty(PlayerUiData.BackgroundColour))
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(PlayerUiData.BackgroundColour);
                }
                catch { }
            }
            return DefaultCardBg;
        }
    }

    [JsonIgnore]
    public Brush CardBorderBrush
    {
        get
        {
            if (IsBlacklisted) return BlacklistCardBorderBrush;
            if (IsFemale) return FemaleBorderBrush;
            if (IsMale) return MaleCardBorderBrush;
            return TransparentBrush;
        }
    }

    [JsonIgnore]
    public Thickness CardBorderThickness
    {
        get
        {
            if (IsFemale) return new Thickness(1.8);
            if (IsBlacklisted) return new Thickness(1.3);
            if (IsMale) return new Thickness(1.3);
            return new Thickness(0);
        }
    }

    [JsonIgnore]
    public double CardShadowBlur => IsFemale ? 12 : 5;

    [JsonIgnore]
    public double CardShadowDepth => IsFemale ? 0 : 3;

    [JsonIgnore]
    public Color CardShadowColor => IsFemale ? Color.FromRgb(0xff, 0x79, 0xc6) : Colors.Black;

    [JsonIgnore]
    public double CardShadowOpacity => IsFemale ? 0.6 : 0.5;

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
        ? "Maç sonu mesajından çıkar (Seçildi)"
        : "Bu oyuncuyu maç sonu mesajına ekle (Engel)";

    private EncounterData _encounterData = new();
    public EncounterData EncounterData { get => _encounterData; set => SetProperty(ref _encounterData, value); }

    private string _accountLevel = "-";
    public string AccountLevel { get => _accountLevel; set => SetProperty(ref _accountLevel, value); }

    private Visibility _active = Visibility.Collapsed;
    public Visibility Active { get => _active; set => SetProperty(ref _active, value); }

    private IdentityData _identityData = new();
    public IdentityData IdentityData { get => _identityData; set => SetProperty(ref _identityData, value); }

    public void RefreshBlacklistStatus()
    {
        var puuid = PlayerUiData?.Puuid ?? Guid.Empty;
        var username = IgnData?.Username;
        var isBlocked = Helpers.BlacklistManager.IsBlacklisted(puuid, username);
        if (IsBlacklisted != isBlocked) IsBlacklisted = isBlocked;
    }

    public void RefreshFemaleStatus()
    {
        var puuid = PlayerUiData?.Puuid ?? Guid.Empty;
        var username = IgnData?.Username;
        var female = Helpers.FemaleTagManager.IsFemale(puuid, username);
        if (female && puuid != Guid.Empty && !string.IsNullOrWhiteSpace(username))
        {
            Helpers.FemaleTagManager.AssociateUsername(puuid, username);
        }
        if (IsFemale != female) IsFemale = female;
    }

    public void RefreshMaleStatus()
    {
        var puuid = PlayerUiData?.Puuid ?? Guid.Empty;
        var username = IgnData?.Username;
        var male = Helpers.MaleTagManager.IsMale(puuid, username);
        if (male && puuid != Guid.Empty && !string.IsNullOrWhiteSpace(username))
        {
            Helpers.MaleTagManager.AssociateUsername(puuid, username);
        }
        if (IsMale != male) IsMale = male;
    }

    public void RefreshPlayerTags()
    {
        RefreshBlacklistStatus();
        RefreshFemaleStatus();
        RefreshMaleStatus();
    }

    private IgnData _ignData = new();
    public IgnData IgnData
    {
        get => _ignData;
        set
        {
            if (SetProperty(ref _ignData, value))
            {
                RefreshPlayerTags();
            }
        }
    }

    private MatchHistoryData _matchHistoryData = new();
    public MatchHistoryData MatchHistoryData { get => _matchHistoryData; set => SetProperty(ref _matchHistoryData, value); }

    private PlayerUIData _playerUiData = new();
    public PlayerUIData PlayerUiData
    {
        get => _playerUiData;
        set
        {
            if (SetProperty(ref _playerUiData, value))
            {
                RefreshPlayerTags();
                OnPropertyChanged(nameof(CardBackground));
            }
        }
    }

    private RankData _rankData = new();
    public RankData RankData { get => _rankData; set => SetProperty(ref _rankData, value); }

    private SkinData _skinData = new();
    public SkinData SkinData { get => _skinData; set => SetProperty(ref _skinData, value); }

    private string _teamId = "";
    public string TeamId { get => _teamId; set => SetProperty(ref _teamId, value); }
}

public class LoadingOverlay : ObservableObject
{
    private string _content = "";
    public string Content { get => _content; set => SetProperty(ref _content, value); }

    private string _header = "";
    public string Header { get => _header; set => SetProperty(ref _header, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    private int _progress;
    public int Progress { get => _progress; set => SetProperty(ref _progress, value); }
}

