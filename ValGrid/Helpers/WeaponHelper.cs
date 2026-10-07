using System;

namespace ValGrid.Helpers;

public static class WeaponHelper
{
    public static string GetTurkishWeaponName(string weapon)
    {
        if (string.IsNullOrWhiteSpace(weapon))
            return "";

        var trimmed = weapon.Trim();
        var lower = trimmed.ToLowerInvariant();

        if (lower.StartsWith("standart "))
        {
            var sub = lower.Substring(9).Trim();
            if (sub == "melee" || sub == "knife" || sub == "bıçak")
                return "Standart Yakın Dövüş Silahı";
            return "Standart " + GetTurkishWeaponName(sub);
        }

        return lower switch
        {
            "melee" or "knife" => "Yakın Dövüş Silahı",
            "classic" => "Classic",
            "shorty" => "Shorty",
            "frenzy" => "Frenzy",
            "ghost" => "Ghost",
            "bandit" or "bandits" => "Bandit",
            "sheriff" => "Sheriff",
            "stinger" => "Stinger",
            "spectre" => "Spectre",
            "bucky" => "Bucky",
            "judge" => "Judge",
            "bulldog" => "Bulldog",
            "guardian" => "Guardian",
            "warden" => "Warden",
            "phantom" => "Phantom",
            "vandal" => "Vandal",
            "marshal" => "Marshal",
            "outlaw" => "Outlaw",
            "operator" => "Operator",
            "ares" => "Ares",
            "odin" => "Odin",
            _ => trimmed
        };
    }

    public static string FormatSkinTitle(string chromaName, string skinName, string fallbackName = null, string weaponName = null)
    {
        string title = null;

        if (!string.IsNullOrWhiteSpace(chromaName))
            title = chromaName;
        else if (!string.IsNullOrWhiteSpace(fallbackName))
            title = fallbackName;
        else if (!string.IsNullOrWhiteSpace(skinName))
            title = skinName;
        else if (!string.IsNullOrWhiteSpace(weaponName))
            title = GetTurkishWeaponName(weaponName);
        else
            title = "";

        // Clean newline characters and multiple whitespace
        title = title.Replace("\r\n", " ").Replace("\n", " ").Trim();

        // Translate fallback standard melee if needed
        if (title.Equals("Melee", StringComparison.OrdinalIgnoreCase) ||
            title.Equals("Knife", StringComparison.OrdinalIgnoreCase))
        {
            title = "Yakın Dövüş Silahı";
        }
        else if (title.Equals("Standart Melee", StringComparison.OrdinalIgnoreCase) ||
                 title.Equals("Standart Knife", StringComparison.OrdinalIgnoreCase))
        {
            title = "Standart Yakın Dövüş Silahı";
        }

        return title;
    }

    public static void EnsureStandardWeaponFallbacks(Objects.SkinData sd)
    {
        if (sd == null) return;

        static void Fallback(ref Uri img, ref string name, string defaultName, string defaultUrl)
        {
            if (img == null && string.IsNullOrEmpty(name))
            {
                if (Uri.TryCreate(defaultUrl, UriKind.Absolute, out var uri))
                    img = uri;
                name = defaultName;
            }
        }

        var classicUri = sd.ClassicImage; var classicName = sd.ClassicName;
        Fallback(ref classicUri, ref classicName, "Standart Classic", "https://media.valorant-api.com/weapons/29a0cfab-485b-f5d5-779a-b59f85e204a8/displayicon.png");
        sd.ClassicImage = classicUri; sd.ClassicName = classicName;

        var shortyUri = sd.ShortyImage; var shortyName = sd.ShortyName;
        Fallback(ref shortyUri, ref shortyName, "Standart Shorty", "https://media.valorant-api.com/weapons/42da8ccc-40d5-affc-beec-15aa47b42eda/displayicon.png");
        sd.ShortyImage = shortyUri; sd.ShortyName = shortyName;

        var frenzyUri = sd.FrenzyImage; var frenzyName = sd.FrenzyName;
        Fallback(ref frenzyUri, ref frenzyName, "Standart Frenzy", "https://media.valorant-api.com/weapons/44d4e95c-4157-0037-81b2-17841bf2e8e3/displayicon.png");
        sd.FrenzyImage = frenzyUri; sd.FrenzyName = frenzyName;

        var ghostUri = sd.GhostImage; var ghostName = sd.GhostName;
        Fallback(ref ghostUri, ref ghostName, "Standart Ghost", "https://media.valorant-api.com/weapons/1baa85b4-4c70-1284-64bb-6481dfc3bb4e/displayicon.png");
        sd.GhostImage = ghostUri; sd.GhostName = ghostName;

        var banditUri = sd.BanditImage; var banditName = sd.BanditName;
        Fallback(ref banditUri, ref banditName, "Standart Bandit", "https://media.valorant-api.com/weapons/410b2e0b-4ceb-1321-1727-20858f7f3477/displayicon.png");
        sd.BanditImage = banditUri; sd.BanditName = banditName;

        var sheriffUri = sd.SheriffImage; var sheriffName = sd.SheriffName;
        Fallback(ref sheriffUri, ref sheriffName, "Standart Sheriff", "https://media.valorant-api.com/weapons/e336c6b8-418d-9340-d77f-7a9e4cfe0702/displayicon.png");
        sd.SheriffImage = sheriffUri; sd.SheriffName = sheriffName;

        var stingerUri = sd.StingerImage; var stingerName = sd.StingerName;
        Fallback(ref stingerUri, ref stingerName, "Standart Stinger", "https://media.valorant-api.com/weapons/f7e1b454-4ad4-1063-ec0a-159e56b58941/displayicon.png");
        sd.StingerImage = stingerUri; sd.StingerName = stingerName;

        var spectreUri = sd.SpectreImage; var spectreName = sd.SpectreName;
        Fallback(ref spectreUri, ref spectreName, "Standart Spectre", "https://media.valorant-api.com/weapons/462080d1-4035-2937-7c09-27aa2a5c27a7/displayicon.png");
        sd.SpectreImage = spectreUri; sd.SpectreName = spectreName;

        var buckyUri = sd.BuckyImage; var buckyName = sd.BuckyName;
        Fallback(ref buckyUri, ref buckyName, "Standart Bucky", "https://media.valorant-api.com/weapons/910be174-449b-c412-ab22-d0873436b21b/displayicon.png");
        sd.BuckyImage = buckyUri; sd.BuckyName = buckyName;

        var judgeUri = sd.JudgeImage; var judgeName = sd.JudgeName;
        Fallback(ref judgeUri, ref judgeName, "Standart Judge", "https://media.valorant-api.com/weapons/ec845bf4-4f79-ddda-a3da-0db3774b2794/displayicon.png");
        sd.JudgeImage = judgeUri; sd.JudgeName = judgeName;

        var bulldogUri = sd.BulldogImage; var bulldogName = sd.BulldogName;
        Fallback(ref bulldogUri, ref bulldogName, "Standart Bulldog", "https://media.valorant-api.com/weapons/ae3de142-4d85-2547-dd26-4e90bed35cf7/displayicon.png");
        sd.BulldogImage = bulldogUri; sd.BulldogName = bulldogName;

        var guardianUri = sd.GuardianImage; var guardianName = sd.GuardianName;
        Fallback(ref guardianUri, ref guardianName, "Standart Guardian", "https://media.valorant-api.com/weapons/4ade7faa-4cf1-8376-95ef-39884480959b/displayicon.png");
        sd.GuardianImage = guardianUri; sd.GuardianName = guardianName;

        var wardenUri = sd.WardenImage; var wardenName = sd.WardenName;
        Fallback(ref wardenUri, ref wardenName, "Standart Warden", "https://media.valorant-api.com/weapons/8db0a1bf-4a50-832a-4566-faaaa6d250ca/displayicon.png");
        sd.WardenImage = wardenUri; sd.WardenName = wardenName;

        var phantomUri = sd.PhantomImage; var phantomName = sd.PhantomName;
        Fallback(ref phantomUri, ref phantomName, "Standart Phantom", "https://media.valorant-api.com/weapons/ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a/displayicon.png");
        sd.PhantomImage = phantomUri; sd.PhantomName = phantomName;

        var vandalUri = sd.VandalImage; var vandalName = sd.VandalName;
        Fallback(ref vandalUri, ref vandalName, "Standart Vandal", "https://media.valorant-api.com/weapons/9c82e19d-4575-0200-1a81-3eacf00cf872/displayicon.png");
        sd.VandalImage = vandalUri; sd.VandalName = vandalName;

        var marshalUri = sd.MarshalImage; var marshalName = sd.MarshalName;
        Fallback(ref marshalUri, ref marshalName, "Standart Marshal", "https://media.valorant-api.com/weapons/c4883e50-4494-202c-3ec3-6b8a9284f00b/displayicon.png");
        sd.MarshalImage = marshalUri; sd.MarshalName = marshalName;

        var outlawUri = sd.OutlawImage; var outlawName = sd.OutlawName;
        Fallback(ref outlawUri, ref outlawName, "Standart Outlaw", "https://media.valorant-api.com/weapons/5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c/displayicon.png");
        sd.OutlawImage = outlawUri; sd.OutlawName = outlawName;

        var opUri = sd.OperatorImage; var opName = sd.OperatorName;
        Fallback(ref opUri, ref opName, "Standart Operator", "https://media.valorant-api.com/weapons/a03b24d3-4319-996d-0f8c-94bbfba1dfc7/displayicon.png");
        sd.OperatorImage = opUri; sd.OperatorName = opName;

        var aresUri = sd.AresImage; var aresName = sd.AresName;
        Fallback(ref aresUri, ref aresName, "Standart Ares", "https://media.valorant-api.com/weapons/55d8a0f4-4274-ca67-fe2c-06ab45efdf58/displayicon.png");
        sd.AresImage = aresUri; sd.AresName = aresName;

        var odinUri = sd.OdinImage; var odinName = sd.OdinName;
        Fallback(ref odinUri, ref odinName, "Standart Odin", "https://media.valorant-api.com/weapons/63e6c2b6-4a8e-869c-3d4c-e38355226584/displayicon.png");
        sd.OdinImage = odinUri; sd.OdinName = odinName;

        var meleeUri = sd.MeleeImage; var meleeName = sd.MeleeName;
        Fallback(ref meleeUri, ref meleeName, "Standart Yakın Dövüş", "https://media.valorant-api.com/weapons/2f59173c-4bed-b6c3-2191-dea9b58be9c7/displayicon.png");
        sd.MeleeImage = meleeUri; sd.MeleeName = meleeName;

        if (sd.LargeCardImage == null)
        {
            if (sd.CardImage != null)
            {
                sd.LargeCardImage = sd.CardImage;
            }
            else if (Uri.TryCreate("https://media.valorant-api.com/playercards/9fb34818-41e4-bb6e-9530-84be96b04111/largeart.png", UriKind.Absolute, out var cardUri))
            {
                sd.LargeCardImage = cardUri;
                sd.CardImage = cardUri;
                sd.CardName = "Standart Kart";
            }
        }

        static void FallbackSpray(ref Uri img, ref string name)
        {
            if (img == null)
            {
                if (Uri.TryCreate("https://media.valorant-api.com/sprays/27c5fd11-4f69-031a-6ee0-6da838ed7998/fulltransparenticon.png", UriKind.Absolute, out var spUri))
                    img = spUri;
                if (string.IsNullOrEmpty(name))
                    name = "Standart Sprey";
            }
        }

        var s1Uri = sd.Spray1Image; var s1Name = sd.Spray1Name;
        FallbackSpray(ref s1Uri, ref s1Name);
        sd.Spray1Image = s1Uri; sd.Spray1Name = s1Name;

        var s2Uri = sd.Spray2Image; var s2Name = sd.Spray2Name;
        FallbackSpray(ref s2Uri, ref s2Name);
        sd.Spray2Image = s2Uri; sd.Spray2Name = s2Name;

        var s3Uri = sd.Spray3Image; var s3Name = sd.Spray3Name;
        FallbackSpray(ref s3Uri, ref s3Name);
        sd.Spray3Image = s3Uri; sd.Spray3Name = s3Name;

        var s4Uri = sd.Spray4Image; var s4Name = sd.Spray4Name;
        FallbackSpray(ref s4Uri, ref s4Name);
        sd.Spray4Image = s4Uri; sd.Spray4Name = s4Name;
    }
}

