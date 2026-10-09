using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ValGrid.Helpers;

namespace ValGrid.Objects;

public class SkinLevelModel
{
    public Guid Uuid { get; set; }
    public string DisplayName { get; set; } = "";
    public string LevelItem { get; set; } = "";
    public int LevelNumber { get; set; } = 1;
    public string LevelTypeName { get; set; } = "1. Seviye";
    public string DisplayIcon { get; set; }
    public string VideoUrl { get; set; }
    public bool HasVideo => !string.IsNullOrEmpty(VideoUrl);
    public string LevelIcon => HasVideo ? "🎬" : "🔹";
    public bool IsSelected { get; set; }
}

public class SkinChromaModel
{
    public Guid Uuid { get; set; }
    public string DisplayName { get; set; } = "";
    public string CleanStyleName { get; set; } = "Standart";
    public string DisplayIcon { get; set; }
    public string FullRenderUrl { get; set; }
    public string SwatchUrl { get; set; }
    public string VideoUrl { get; set; }
    public bool HasVideo => !string.IsNullOrEmpty(VideoUrl);
    public bool IsSelected { get; set; }
}

public class SkinInspectDetail
{
    public Guid SkinUuid { get; set; }
    public string Name { get; set; } = "Bilinmeyen Skin";
    public string WeaponType { get; set; } = "SİLAH";
    public int VpCost { get; set; }
    public string VpCostFormatted => $"{VpCost:N0} VP";
    public string TlCostFormatted => CurrencyHelper.FormatApproximatePrice(VpCost);
    public string TierDevName { get; set; } = "Standard";
    public string TierDisplayName { get; set; } = "STANDART";
    public string TierColor { get; set; } = "#7f8c8d";
    public string TierIconUrl { get; set; } = "";
    public string WallpaperUrl { get; set; }
    public string DisplayIcon { get; set; }

    public SolidColorBrush TierBrush
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

    public SolidColorBrush TierBackgroundBrush
    {
        get
        {
            try
            {
                if (!string.IsNullOrEmpty(TierColor))
                {
                    var col = (Color)ColorConverter.ConvertFromString(TierColor);
                    return new SolidColorBrush(Color.FromArgb(38, col.R, col.G, col.B));
                }
            }
            catch { }
            return new SolidColorBrush(Color.FromArgb(30, 0x1d, 0x23, 0x3a));
        }
    }

    public List<SkinLevelModel> Levels { get; set; } = new();
    public List<SkinChromaModel> Chromas { get; set; } = new();

    public bool HasAnyVideo => Levels.Any(l => l.HasVideo) || Chromas.Any(c => c.HasVideo);
    public bool HasLevels => Levels.Count > 1;
    public bool HasChromas => Chromas.Count > 1;
}

