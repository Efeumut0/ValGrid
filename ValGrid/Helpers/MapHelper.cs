using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ValGrid.Objects;

namespace ValGrid.Helpers;

public static class MapHelper
{
    private static readonly Dictionary<string, string> MapCodenameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Ascent", "Ascent" },
        { "Duality", "Bind" },
        { "Bind", "Bind" },
        { "Bonsai", "Split" },
        { "Split", "Split" },
        { "Triad", "Haven" },
        { "Haven", "Haven" },
        { "Port", "Icebox" },
        { "Icebox", "Icebox" },
        { "Foxtrot", "Breeze" },
        { "Breeze", "Breeze" },
        { "Canyon", "Fracture" },
        { "Fracture", "Fracture" },
        { "Pitt", "Pearl" },
        { "Pearl", "Pearl" },
        { "Jam", "Lotus" },
        { "Lotus", "Lotus" },
        { "Juliett", "Sunset" },
        { "Sunset", "Sunset" },
        { "Infinity", "Abyss" },
        { "Abyss", "Abyss" },
        { "Ebis", "Abyss" },
        { "Rook", "Corrode" },
        { "Corrode", "Corrode" },
        { "Plummet", "Summit" },
        { "Summit", "Summit" },
        { "HURM_Alley", "District" },
        { "District", "District" },
        { "HURM_Bowl", "Kasbah" },
        { "Kasbah", "Kasbah" },
        { "HURM_Helix", "Drift" },
        { "Drift", "Drift" },
        { "HURM_HighTide", "Glitch" },
        { "Glitch", "Glitch" },
        { "HURM_Yard", "Piazza" },
        { "Piazza", "Piazza" },
        { "Range", "Poligon" },
        { "RangeV2", "Poligon" },
        { "Poligon", "Poligon" },
        { "Poveglia", "Poligon" },
        { "PovegliaV2", "Poligon" },
        { "AbilityDraft", "Gauntlet" },
        { "Gauntlet", "Gauntlet" },
        { "Skirmish_A", "Çarpışma A" },
        { "Skirmish_B", "Çarpışma B" },
        { "Skirmish_C", "Çarpışma C" },
        { "Skirmish_D", "Çarpışma D" },
        { "Skirmish_E", "Çarpışma E" }
    };

    private static readonly Dictionary<Guid, (string Name, string CodeName)> KnownMapGuids = new()
    {
        { Guid.Parse("224b0a95-48b9-f703-1bd8-67aca101a61f"), ("Abyss", "Infinity") },
        { Guid.Parse("7eaecc1b-4337-bbf6-6ab9-04b8f06b3319"), ("Ascent", "Ascent") },
        { Guid.Parse("d960549e-485c-e861-8d71-aa9d1aed12a2"), ("Split", "Bonsai") },
        { Guid.Parse("b529448b-4d60-346e-e89e-00a4c527a405"), ("Fracture", "Canyon") },
        { Guid.Parse("2c9d57ec-4431-9c5e-2939-8f9ef6dd5cba"), ("Bind", "Duality") },
        { Guid.Parse("2fb9a4fd-47b8-4e7d-a969-74b4046ebd53"), ("Breeze", "Foxtrot") },
        { Guid.Parse("690b3ed2-4dff-945b-8223-6da834e30d24"), ("District", "HURM_Alley") },
        { Guid.Parse("12452a9d-48c3-0b02-e7eb-0381c3520404"), ("Kasbah", "HURM_Bowl") },
        { Guid.Parse("2c09d728-42d5-30d8-43dc-96a05cc7ee9d"), ("Drift", "HURM_Helix") },
        { Guid.Parse("d6336a5a-428f-c591-98db-c8a291159134"), ("Glitch", "HURM_HighTide") },
        { Guid.Parse("de28aa9b-4cbe-1003-320e-6cb3ec309557"), ("Piazza", "HURM_Yard") },
        { Guid.Parse("2fe4ed3a-450a-948b-6d6b-e89a78e680a9"), ("Lotus", "Jam") },
        { Guid.Parse("92584fbe-486a-b1b2-9faa-39b0f486b498"), ("Sunset", "Juliett") },
        { Guid.Parse("fd267378-4d1d-484f-ff52-77821ed10dc2"), ("Pearl", "Pitt") },
        { Guid.Parse("756da597-416b-c0f2-f47b-afbdf28670bc"), ("Summit", "Plummet") },
        { Guid.Parse("e2ad5c54-4114-a870-9641-8ea21279579a"), ("Icebox", "Port") },
        { Guid.Parse("ee613ee9-28b7-4beb-9666-08db13bb2244"), ("Poligon", "Range") },
        { Guid.Parse("5914d1e0-40c4-cfdd-6b88-eba06347686c"), ("Poligon", "RangeV2") },
        { Guid.Parse("1c18ab1f-420d-0d8b-71d0-77ad3c439115"), ("Corrode", "Rook") },
        { Guid.Parse("2bee0dc9-4ffe-519b-1cbd-7fbe763a6047"), ("Haven", "Triad") },
        { Guid.Parse("dd3a1cd9-41b1-50ea-3bd6-a7bb3c5978bd"), ("Gauntlet", "AbilityDraft") }
    };

    private static Dictionary<string, ValMap> _mapsCache;
    private static Dictionary<Guid, ValMap> _uuidToMap;
    private static readonly object CacheLock = new();

    public static string ResolveMapName(string rawMap)
    {
        if (string.IsNullOrWhiteSpace(rawMap))
            return "Bilinmeyen Harita";

        var trimmed = rawMap.Trim();
        if (trimmed.Equals("Bilinmeyen Harita", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Undefined", StringComparison.OrdinalIgnoreCase))
            return "Bilinmeyen Harita";

        // Check if rawMap is a GUID
        if (Guid.TryParse(trimmed, out var mapGuid))
        {
            if (KnownMapGuids.TryGetValue(mapGuid, out var known))
                return known.Name;

            var uuidMap = GetUuidMap();
            if (uuidMap != null && uuidMap.TryGetValue(mapGuid, out var valMap) && !string.IsNullOrWhiteSpace(valMap.Name))
            {
                if (MapCodenameMap.TryGetValue(valMap.Name, out var friendly))
                    return friendly;
                return valMap.Name;
            }
        }

        // 1. Extract codename from path e.g. "/Game/Maps/Juliett/Juliett" -> "Juliett"
        var slashIdx = trimmed.LastIndexOf('/');
        var codeName = slashIdx >= 0 ? trimmed.Substring(slashIdx + 1) : trimmed;

        // Check if codeName directly matches a friendly name in codename dictionary
        if (MapCodenameMap.TryGetValue(codeName, out var friendlyName))
            return friendlyName;

        // Check if trimmed itself matches a known codename
        if (MapCodenameMap.TryGetValue(trimmed, out var friendlyFromTrimmed))
            return friendlyFromTrimmed;

        // 2. Try maps.json dictionary
        try
        {
            var maps = GetMapsDict();
            if (maps != null)
            {
                if (maps.TryGetValue(trimmed, out var valMap) && !string.IsNullOrWhiteSpace(valMap?.Name))
                {
                    if (MapCodenameMap.TryGetValue(valMap.Name, out var friendlyFromVal))
                        return friendlyFromVal;
                    return valMap.Name;
                }

                foreach (var kvp in maps)
                {
                    if (kvp.Key.EndsWith("/" + codeName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kvp.Value?.Name))
                    {
                        if (MapCodenameMap.TryGetValue(kvp.Value.Name, out var friendlyFromKvp))
                            return friendlyFromKvp;
                        return kvp.Value.Name;
                    }

                    if (kvp.Value?.Name?.Equals(codeName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (MapCodenameMap.TryGetValue(kvp.Value.Name, out var friendlyFromVal))
                            return friendlyFromVal;
                        return kvp.Value.Name;
                    }
                }
            }
        }
        catch { }

        return !string.IsNullOrWhiteSpace(codeName) ? codeName : "Bilinmeyen Harita";
    }

    public static Uri ResolveMapImage(string rawMap)
    {
        if (string.IsNullOrWhiteSpace(rawMap))
            return null;

        var trimmed = rawMap.Trim();
        Guid targetGuid = Guid.Empty;

        // 1. Direct GUID check
        if (Guid.TryParse(trimmed, out var parsedGuid))
        {
            targetGuid = parsedGuid;
        }

        // 2. Check KnownMapGuids for matching name or codename
        if (targetGuid == Guid.Empty)
        {
            var slashIdx = trimmed.LastIndexOf('/');
            var codeName = slashIdx >= 0 ? trimmed.Substring(slashIdx + 1) : trimmed;

            foreach (var kvp in KnownMapGuids)
            {
                if (kvp.Value.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Value.Name.Equals(codeName, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Value.CodeName.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Value.CodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase))
                {
                    targetGuid = kvp.Key;
                    break;
                }
            }

            // Check friendly name mapping (e.g. EBIS -> Abyss)
            if (targetGuid == Guid.Empty && MapCodenameMap.TryGetValue(codeName, out var friendly))
            {
                foreach (var kvp in KnownMapGuids)
                {
                    if (kvp.Value.Name.Equals(friendly, StringComparison.OrdinalIgnoreCase))
                    {
                        targetGuid = kvp.Key;
                        break;
                    }
                }
            }
        }

        // 3. Fallback to maps.json
        if (targetGuid == Guid.Empty)
        {
            try
            {
                var maps = GetMapsDict();
                if (maps != null)
                {
                    var slashIdx = trimmed.LastIndexOf('/');
                    var codeName = slashIdx >= 0 ? trimmed.Substring(slashIdx + 1) : trimmed;

                    if (maps.TryGetValue(trimmed, out var valMap) && valMap.UUID != Guid.Empty)
                    {
                        targetGuid = valMap.UUID;
                    }
                    else
                    {
                        foreach (var kvp in maps)
                        {
                            if (kvp.Key.EndsWith("/" + codeName, StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.EndsWith("/" + trimmed, StringComparison.OrdinalIgnoreCase) ||
                                (kvp.Value?.Name != null && (
                                    kvp.Value.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                                    kvp.Value.Name.Equals(codeName, StringComparison.OrdinalIgnoreCase))))
                            {
                                if (kvp.Value != null && kvp.Value.UUID != Guid.Empty)
                                {
                                    targetGuid = kvp.Value.UUID;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        if (targetGuid != Guid.Empty)
        {
            try
            {
                var imgPath = (string.IsNullOrEmpty(Constants.LocalAppDataPath)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
                    : Constants.LocalAppDataPath) + $"\\ValAPI\\mapsimg\\{targetGuid}.png";

                if (File.Exists(imgPath))
                    return new Uri(imgPath);

                // Fallback to Riot CDN if local file is missing
                return new Uri($"https://media.valorant-api.com/maps/{targetGuid}/splash.png");
            }
            catch { }
        }

        return null;
    }

    public static string ResolveMapNameFromImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        try
        {
            var clean = imageUrl.Trim();
            var lastSlash = clean.LastIndexOf('/');
            var fileName = lastSlash >= 0 ? clean.Substring(lastSlash + 1) : clean;
            var dotIdx = fileName.IndexOf('.');
            var guidStr = dotIdx >= 0 ? fileName.Substring(0, dotIdx) : fileName;

            if (Guid.TryParse(guidStr, out var mapGuid))
            {
                if (KnownMapGuids.TryGetValue(mapGuid, out var known))
                    return known.Name;

                var uuidMap = GetUuidMap();
                if (uuidMap != null && uuidMap.TryGetValue(mapGuid, out var valMap) && !string.IsNullOrWhiteSpace(valMap.Name))
                {
                    return ResolveMapName(valMap.Name);
                }
            }
        }
        catch { }

        return null;
    }

    private static Dictionary<string, ValMap> GetMapsDict()
    {
        lock (CacheLock)
        {
            if (_mapsCache != null && _mapsCache.Count > 0)
                return _mapsCache;

            try
            {
                var localPath = string.IsNullOrEmpty(Constants.LocalAppDataPath)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
                    : Constants.LocalAppDataPath;
                var mapsPath = localPath + "\\ValAPI\\maps.json";

                if (File.Exists(mapsPath))
                {
                    var json = File.ReadAllText(mapsPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, ValMap>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (dict != null)
                    {
                        _mapsCache = new Dictionary<string, ValMap>(dict, StringComparer.OrdinalIgnoreCase);
                        _uuidToMap = new Dictionary<Guid, ValMap>();
                        foreach (var kvp in _mapsCache)
                        {
                            if (kvp.Value != null && kvp.Value.UUID != Guid.Empty)
                            {
                                _uuidToMap[kvp.Value.UUID] = kvp.Value;
                            }
                        }
                        return _mapsCache;
                    }
                }
            }
            catch { }
        }
        return null;
    }

    private static Dictionary<Guid, ValMap> GetUuidMap()
    {
        lock (CacheLock)
        {
            if (_uuidToMap != null && _uuidToMap.Count > 0)
                return _uuidToMap;

            GetMapsDict();
            return _uuidToMap;
        }
    }
}

