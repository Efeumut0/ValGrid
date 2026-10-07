using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ValGrid.Helpers;

namespace ValGrid.Objects;

public class DailyStoreOffer : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
{
    public Guid ItemId { get; set; }
    public Guid OfferId { get; set; }
    public string Name { get; set; } = "Bilinmeyen Skin";
    public Uri Image { get; set; }
    public string WeaponType { get; set; } = "Silah";
    public int VpCost { get; set; }
    public string VpCostFormatted => $"{VpCost:N0} VP";
    public string TlCostFormatted => $"~{CurrencyHelper.FormatTl(VpCost)}";
    public string TierDevName { get; set; } = "Standard";
    public string TierDisplayName { get; set; } = "";
    public string TierColor { get; set; } = "#7f8c8d";
    public string TierIconUrl { get; set; } = "";
    public string VpIconUrl => StoreHelper.VpIconUrl;

    public Uri CardLargeArt { get; set; }
    public Uri CardWideArt { get; set; }
    public Uri CardSmallArt { get; set; }
    public bool IsPlayerCard => string.Equals(WeaponType, "OYUNCU KARTI", StringComparison.OrdinalIgnoreCase)
        || CardLargeArt != null || CardWideArt != null;

    public Brush TierBorderBrush
    {
        get
        {
            try
            {
                if (!string.IsNullOrEmpty(TierColor))
                {
                    var col = (Color)ColorConverter.ConvertFromString(TierColor);
                    return new SolidColorBrush(col);
                }
            }
            catch { }
            return new SolidColorBrush(Color.FromRgb(0x4a, 0x55, 0x68));
        }
    }

    public Brush TierBackgroundBrush
    {
        get
        {
            try
            {
                if (!string.IsNullOrEmpty(TierColor))
                {
                    var col = (Color)ColorConverter.ConvertFromString(TierColor);
                    // 15% opacity tint for modern card glow
                    return new SolidColorBrush(Color.FromArgb(38, col.R, col.G, col.B));
                }
            }
            catch { }
            return new SolidColorBrush(Color.FromArgb(30, 0x1d, 0x23, 0x3a));
        }
    }

    public bool IsRare { get; set; }
    public string RarityLabel { get; set; } = "";
    public Visibility RareVisibility => IsRare ? Visibility.Visible : Visibility.Collapsed;

    // For Night Market (Gece Pazarı)
    public bool IsNightMarket { get; set; }
    public int DiscountPercentage { get; set; }
    public string DiscountPercentText => $"-{DiscountPercentage}%";
    public Visibility DiscountVisibility => IsNightMarket && DiscountPercentage > 0 ? Visibility.Visible : Visibility.Collapsed;
    public int OriginalVpCost { get; set; }
    public string OriginalVpCostFormatted => OriginalVpCost > 0 ? $"{OriginalVpCost:N0} VP" : "";
    public Visibility OriginalCostVisibility => IsNightMarket && OriginalVpCost > 0 ? Visibility.Visible : Visibility.Collapsed;

    // Bundle / Special Set Discount properties
    public int BundleDiscountedCost { get; set; }
    public string BundleDiscountedCostFormatted => BundleDiscountedCost > 0 ? $"{BundleDiscountedCost:N0} VP" : "";
    public string BundleDiscountedTlCostFormatted => BundleDiscountedCost > 0 ? $"~{CurrencyHelper.FormatTl(BundleDiscountedCost)}" : "";
    public bool HasBundleDiscount => BundleDiscountedCost > 0 && BundleDiscountedCost < VpCost;
    public Visibility BundleDiscountVisibility => HasBundleDiscount ? Visibility.Visible : Visibility.Collapsed;

    private bool _isRevealed = true;
    public bool IsRevealed
    {
        get => _isRevealed;
        set
        {
            if (SetProperty(ref _isRevealed, value))
            {
                OnPropertyChanged(nameof(CardBackVisibility));
                OnPropertyChanged(nameof(CardFrontVisibility));
            }
        }
    }

    public Visibility CardBackVisibility => (!IsNightMarket || IsRevealed) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility CardFrontVisibility => (!IsNightMarket || IsRevealed) ? Visibility.Visible : Visibility.Collapsed;
}

public class FeaturedBundleOffer : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
{
    public Guid BundleId { get; set; }
    public Guid DataAssetId { get; set; }
    public string Name { get; set; } = "Öne Çıkan Paket";
    public Uri DisplayIcon { get; set; }
    public Uri DisplayIcon2 { get; set; }
    public Uri VerticalPromoImage { get; set; }
    public string Description { get; set; } = "";
    public string ExtraDescription { get; set; } = "";
    public int VpCost { get; set; }
    public string VpCostFormatted => $"{VpCost:N0} VP";
    public string TlCostFormatted => $"~{CurrencyHelper.FormatTl(VpCost)}";
    public int BaseVpCost { get; set; }
    public string BaseVpCostFormatted => BaseVpCost > 0 ? $"{BaseVpCost:N0} VP" : "";
    public int DiscountPercent { get; set; }
    public string DiscountPercentText => DiscountPercent > 0 ? $"-{DiscountPercent}%" : "";
    public Visibility DiscountVisibility => DiscountPercent > 0 ? Visibility.Visible : Visibility.Collapsed;
    public int RemainingDurationSeconds { get; set; }
    public string RemainingTimeFormatted { get; set; } = "";
    public string VideoUrl { get; set; } = "";
    public bool HasVideo => !string.IsNullOrEmpty(VideoUrl);
    public string VpIconUrl => StoreHelper.VpIconUrl;
    public List<DailyStoreOffer> Items { get; set; } = new();
    public int ItemsCount => Items?.Count ?? 0;
    public bool HasItems => ItemsCount > 0;
    public Visibility ItemsVisibility => HasItems ? Visibility.Visible : Visibility.Collapsed;

    private bool _isItemsExpanded;
    public bool IsItemsExpanded
    {
        get => _isItemsExpanded;
        set => SetProperty(ref _isItemsExpanded, value);
    }
}

public class DailyStoreData
{
    public List<FeaturedBundleOffer> FeaturedBundles { get; set; } = new();
    public bool HasFeaturedBundles => FeaturedBundles != null && FeaturedBundles.Count > 0;
    public Visibility FeaturedBundlesVisibility => HasFeaturedBundles ? Visibility.Visible : Visibility.Collapsed;

    public List<DailyStoreOffer> DailyOffers { get; set; } = new();
    public List<DailyStoreOffer> NightMarketOffers { get; set; } = new();

    public bool HasNightMarket => NightMarketOffers != null && NightMarketOffers.Count > 0;
    public Visibility NightMarketVisibility => HasNightMarket ? Visibility.Visible : Visibility.Collapsed;
    public string NightMarketTitle { get; set; } = "GECE PAZARI (NIGHT MARKET)";
    public string NightMarketSubtitle { get; set; } = "• İndirimli Özel Teklifler (Kartı çevirmek ve incelemek için tıklayın)";
    public bool IsNightMarketArchived { get; set; }

    public int RemainingDurationSeconds { get; set; }
    public string RemainingTimeFormatted { get; set; } = "--:--:--";

    public int VpBalance { get; set; }
    public int RadianiteBalance { get; set; }
    public int KcBalance { get; set; }

    public string VpBalanceFormatted => $"{VpBalance:N0} VP";
    public string RadianiteBalanceFormatted => $"{RadianiteBalance:N0} RP";
    public string KcBalanceFormatted => $"{KcBalance:N0} KC";

    public string VpIconUrl => StoreHelper.VpIconUrl;
    public string RpIconUrl => StoreHelper.RpIconUrl;
    public string KcIconUrl => StoreHelper.KcIconUrl;
}

