using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ValGrid.Helpers;

public static class CurrencyHelper
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly CultureInfo TrCulture = new("tr-TR");

    // Default fallback rate in Turkey: 1 VP ≈ 0.285 TL (as of 2026 store pricing: 1700 VP = ~500 TL)
    private const double FallbackVpToTry = 0.285;
    private static double _vpToTryRate = FallbackVpToTry;
    private static DateTime _lastUpdated = DateTime.MinValue;
    private static readonly object Lock = new();

    public static double VpToTryRate
    {
        get
        {
            lock (Lock)
            {
                return _vpToTryRate;
            }
        }
        private set
        {
            lock (Lock)
            {
                _vpToTryRate = value;
            }
        }
    }

    /// <summary>
    /// Fetches live USD to TRY exchange rate from public API and updates VP to TRY calculation dynamically.
    /// Runs asynchronously in background, never throws or blocks.
    /// </summary>
    public static async Task RefreshExchangeRateAsync()
    {
        try
        {
            if (DateTime.UtcNow - _lastUpdated < TimeSpan.FromHours(4))
                return;

            var json = await HttpClient.GetStringAsync("https://open.er-api.com/v6/latest/USD").ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty("TRY", out var tryRateElement) &&
                tryRateElement.TryGetDouble(out var usdToTry))
            {
                if (usdToTry > 0)
                {
                    // In Global Riot pricing: 1000 VP ≈ $9.99 USD -> 1 VP = 0.00999 USD.
                    // Regional Turkey pricing factor is historically ~0.58 of US dollar conversion.
                    var calculatedRate = usdToTry * 0.00999 * 0.58;
                    if (calculatedRate >= 0.10 && calculatedRate <= 2.0)
                    {
                        VpToTryRate = calculatedRate;
                        _lastUpdated = DateTime.UtcNow;
                        Constants.Log.Information("CurrencyHelper: Updated VP to TRY rate: {rate:F4} (USD/TRY: {usd:F2})", calculatedRate, usdToTry);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Warning("CurrencyHelper: Failed to fetch live exchange rate, using fallback rate {rate}: {err}", _vpToTryRate, ex.Message);
        }
    }

    public static int VpToTl(int vp)
    {
        if (vp <= 0) return 0;
        return (int)Math.Round(vp * VpToTryRate);
    }

    public static string FormatVp(int vp)
    {
        return $"{vp.ToString("N0", TrCulture)} VP";
    }

    public static string FormatPrice(int vp)
    {
        if (vp <= 0) return L10n.IsEnglish ? "$0.00" : "0 TL";
        if (L10n.IsEnglish)
        {
            var usd = Math.Round(vp / 100.0, 2);
            return $"${usd.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        return FormatTl(vp);
    }

    public static string FormatApproximatePrice(int vp)
    {
        if (vp <= 0) return L10n.IsEnglish ? "$0.00" : "0 TL";
        if (L10n.IsEnglish)
        {
            var usd = Math.Round(vp / 100.0, 2);
            return $"~${usd.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        return $"~{FormatTl(vp)}";
    }

    public static string FormatTl(int vp)
    {
        if (L10n.IsEnglish)
        {
            var usd = Math.Round(vp / 100.0, 2);
            return $"${usd.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        var tl = VpToTl(vp);
        return $"{tl.ToString("N0", TrCulture)} TL";
    }

    public static string FormatTlCompact(int vp)
    {
        if (L10n.IsEnglish)
        {
            var usd = Math.Round(vp / 100.0, 2);
            return $"${usd.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        var tl = VpToTl(vp);
        if (tl >= 1000)
        {
            var k = tl / 1000;
            return $"{k}bin TL";
        }
        return $"{tl} TL";
    }

    public static string FormatVpAndTl(int vp)
    {
        if (vp <= 0) return L10n.IsEnglish ? "0 VP ($0.00)" : "0 VP (0 TL)";
        if (L10n.IsEnglish)
        {
            return $"{FormatVp(vp)} ({FormatApproximatePrice(vp)})";
        }
        return $"{FormatVp(vp)} (~{FormatTl(vp)})";
    }
}

