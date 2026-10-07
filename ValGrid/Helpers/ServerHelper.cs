using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ValGrid.Objects;
using RestSharp;
using RiotPing = ValGrid.Objects.Ping;

namespace ValGrid.Helpers;

public static class ServerHelper
{
    public static RiotPing[] CurrentUserPings { get; set; }

    public static int GetPingForPod(string podId)
    {
        return GetPingForPod(CurrentUserPings, podId);
    }

    public static int GetPingForPod(RiotPing[] pings, string podId)
    {
        if (pings == null || pings.Length == 0 || string.IsNullOrWhiteSpace(podId))
            return -1;

        // 1. Exact match
        foreach (var p in pings)
        {
            if (p != null && string.Equals(p.GamePodId, podId, StringComparison.OrdinalIgnoreCase) && p.PingPing > 0)
                return (int)p.PingPing;
        }

        // 2. City / location keyword match
        var podLower = podId.ToLowerInvariant();
        string[] cities = new[]
        {
            "istanbul", "frankfurt", "paris", "london", "warsaw", "madrid", "stockholm",
            "bahrain", "tokyo", "seoul", "singapore", "sydney", "mumbai", "ashburn",
            "virginia", "chicago", "santiago", "saopaulo", "brazil", "capetown", "riyadh",
            "ist", "fra", "par", "lon", "waw", "mad", "sto"
        };

        foreach (var city in cities)
        {
            if (podLower.Contains(city))
            {
                foreach (var p in pings)
                {
                    if (p != null && p.PingPing > 0 && !string.IsNullOrWhiteSpace(p.GamePodId))
                    {
                        if (p.GamePodId.ToLowerInvariant().Contains(city))
                            return (int)p.PingPing;
                    }
                }
            }
        }

        return -1;
    }

    public static async Task<int> FetchRiotPodPingAsync(string podId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(podId) || string.IsNullOrEmpty(Constants.Shard) || string.IsNullOrEmpty(Constants.Region))
                return -1;

            var client = new RestClient(
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/parties/v1/players/{Constants.Ppuuid}"
            );
            var request = new RestRequest();
            Login.AddAuthToRequest(request);
            var res = await client.ExecuteGetAsync<PartyIdResponse>(request).ConfigureAwait(false);
            if (!res.IsSuccessful || res.Data?.CurrentPartyId == null || res.Data.CurrentPartyId == Guid.Empty)
                return -1;

            var partyClient = new RestClient(
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/parties/v1/parties/{res.Data.CurrentPartyId}"
            );
            var partyReq = new RestRequest();
            Login.AddAuthToRequest(partyReq);
            var partyRes = await partyClient.ExecuteGetAsync(partyReq).ConfigureAwait(false);
            if (!partyRes.IsSuccessful || string.IsNullOrEmpty(partyRes.Content))
                return -1;

            // 1. Direct resilient JsonDocument parsing (immune to schema additions / type mismatch)
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(partyRes.Content);
                if (doc.RootElement.TryGetProperty("Members", out var membersEl))
                {
                    foreach (var memberEl in membersEl.EnumerateArray())
                    {
                        if (memberEl.TryGetProperty("Subject", out var subProp) &&
                            subProp.TryGetGuid(out var subGuid) && subGuid == Constants.Ppuuid)
                        {
                            if (memberEl.TryGetProperty("Pings", out var pingsEl))
                            {
                                var pList = new System.Collections.Generic.List<RiotPing>();
                                foreach (var pEl in pingsEl.EnumerateArray())
                                {
                                    long pingVal = 0;
                                    string podVal = "";
                                    if (pEl.TryGetProperty("Ping", out var pingProp))
                                        pingVal = pingProp.GetInt64();
                                    if (pEl.TryGetProperty("GamePodID", out var podProp))
                                        podVal = podProp.GetString();

                                    if (!string.IsNullOrEmpty(podVal))
                                        pList.Add(new RiotPing { PingPing = pingVal, GamePodId = podVal });
                                }
                                if (pList.Count > 0)
                                {
                                    CurrentUserPings = pList.ToArray();
                                    return GetPingForPod(podId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception jEx)
            {
                Constants.Log?.Warning("FetchRiotPodPingAsync JsonDocument parse error: {e}", jEx.Message);
            }

            // 2. Typed deserialization fallback
            try
            {
                var partyObj = System.Text.Json.JsonSerializer.Deserialize<PartyResponse>(partyRes.Content);
                var me = partyObj?.Members?.FirstOrDefault(m => m.Subject == Constants.Ppuuid);
                if (me?.Pings != null)
                {
                    CurrentUserPings = me.Pings;
                    return GetPingForPod(podId);
                }
            }
            catch (Exception dEx)
            {
                Constants.Log?.Warning("FetchRiotPodPingAsync Deserialize fallback error: {e}", dEx.Message);
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("FetchRiotPodPingAsync failed: {e}", ex.Message);
        }
        return -1;
    }

    public static string GetPodLocationName(string podId)
    {
        if (string.IsNullOrWhiteSpace(podId))
            return "Bilinmeyen Sunucu";

        var lower = podId.ToLowerInvariant();

        if (lower.Contains("istanbul"))
            return "🇹🇷 İstanbul (TR)";
        if (lower.Contains("frankfurt"))
            return "🇩🇪 Frankfurt (Almanya)";
        if (lower.Contains("paris"))
            return "🇫🇷 Paris (Fransa)";
        if (lower.Contains("london"))
            return "🇬🇧 Londra (İngiltere)";
        if (lower.Contains("madrid"))
            return "🇪🇸 Madrid (İspanya)";
        if (lower.Contains("warsaw"))
            return "🇵🇱 Varşova (Polonya)";
        if (lower.Contains("stockholm"))
            return "🇸🇪 Stockholm (İsveç)";
        if (lower.Contains("bahrain"))
            return "🇧🇭 Bahreyn (Orta Doğu)";
        if (lower.Contains("tokyo"))
            return "🇯🇵 Tokyo (Japonya)";
        if (lower.Contains("seoul"))
            return "🇰🇷 Seul (Kore)";
        if (lower.Contains("singapore"))
            return "🇸🇬 Singapur";
        if (lower.Contains("sydney"))
            return "🇦🇺 Sidney (Avustralya)";
        if (lower.Contains("mumbai"))
            return "🇮🇳 Mumbai (Hindistan)";
        if (lower.Contains("ashburn") || lower.Contains("virginia"))
            return "🇺🇸 Doğu ABD (K. Virginia)";
        if (lower.Contains("chicago"))
            return "🇺🇸 Orta ABD (Chicago)";
        if (lower.Contains("santiago"))
            return "🇨🇱 Santiago (Şili)";
        if (lower.Contains("saopaulo") || lower.Contains("brazil"))
            return "🇧🇷 São Paulo (Brezilya)";

        // Fallback: clean up aresriot.aws-rsoe-xyz
        var parts = podId.Split(new[] { '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (!part.Equals("aresriot", StringComparison.OrdinalIgnoreCase) &&
                !part.Equals("aws", StringComparison.OrdinalIgnoreCase) &&
                !part.Equals("rsoe", StringComparison.OrdinalIgnoreCase) &&
                !part.Equals("gp", StringComparison.OrdinalIgnoreCase) &&
                !part.Equals("eu", StringComparison.OrdinalIgnoreCase) &&
                !part.Equals("na", StringComparison.OrdinalIgnoreCase) &&
                !part.Equals("ap", StringComparison.OrdinalIgnoreCase) &&
                !char.IsDigit(part[0]))
            {
                return $"🌐 {char.ToUpperInvariant(part[0])}{part.Substring(1)}";
            }
        }

        return $"🌐 {podId}";
    }

    public static async Task<int> MeasurePingAsync(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            return -1;

        // 1. Try TCP handshake measurement if port is specified
        if (port > 0)
        {
            try
            {
                using var tcp = new TcpClient();
                var sw = Stopwatch.StartNew();
                var connectTask = tcp.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(1200)).ConfigureAwait(false) == connectTask && tcp.Connected)
                {
                    sw.Stop();
                    return (int)sw.ElapsedMilliseconds;
                }
            }
            catch
            {
                // Fall through to ICMP ping
            }
        }

        // 2. Try ICMP Ping
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(host, 1200).ConfigureAwait(false);
            if (reply.Status == IPStatus.Success)
            {
                return (int)reply.RoundtripTime;
            }
        }
        catch
        {
            // Fall through
        }

        return -1;
    }

    public static string GetPingColor(int pingMs)
    {
        if (pingMs <= 0)
            return "#8e9bb5"; // Unknown / Gray
        if (pingMs < 35)
            return "#32e2b2"; // Excellent / Teal
        if (pingMs < 70)
            return "#f0b232"; // Good / Gold
        return "#f05454";     // High / Red
    }

    public static string MaskPlayerKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "--";
        if (key.Length <= 10)
            return key;
        return $"{key.Substring(0, 4)}...{key.Substring(key.Length - 4)}";
    }

    public static string FormatServerTooltip(string region, string podId, string host, int port, int ping, string playerKey)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🌐 Sunucu & Bağlantı Bilgisi");
        sb.AppendLine($"• Konum / Bölge: {region}");
        if (!string.IsNullOrWhiteSpace(podId))
            sb.AppendLine($"• Sunucu Pod: {podId}");
        if (!string.IsNullOrWhiteSpace(host))
        {
            var ipText = port > 0 ? $"{host}:{port}" : host;
            sb.AppendLine($"• Sunucu IP & Port: {ipText}");
        }
        if (ping > 0)
        {
            var quality = ping < 35 ? "Mükemmel" : (ping < 70 ? "İyi" : "Yüksek Gecikme");
            sb.AppendLine($"• Gecikme (Ping): {ping} ms ({quality})");
        }
        else
        {
            sb.AppendLine("• Gecikme (Ping): Ölçülüyor / ICMP Yanıtsız");
        }
        if (!string.IsNullOrWhiteSpace(playerKey))
        {
            sb.AppendLine();
            sb.AppendLine("🔐 Şifreli Bağlantı Anahtarı (PlayerKey):");
            sb.AppendLine(playerKey);
        }
        sb.AppendLine();
        sb.AppendLine("(📋 Butonuna tıklayarak bu bilgileri panoya kopyalayabilirsiniz)");
        return sb.ToString().TrimEnd();
    }
}

