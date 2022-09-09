using System.Text.RegularExpressions;

namespace Wadinator;

/// <summary>
/// Responsible for analyzing WADs and reporting the results to the user.
/// </summary>
public class Analyzer {
    private List<WadDirectoryEntry>? _mapList;
    private readonly WadReader _wad;
    
    // Map List Regex
    private static readonly Regex AllMapsRegEx = new("^(E.M.|MAP..)$");
    private static readonly Regex EpisodeAndMapRegEx = new("^(E.M.)$");
    private static readonly Regex UltimateDoomMapRegEx = new Regex("^(E4M.)$");

    // Map Lump Information
    private List<WadDirectoryEntry> MapList =>
        _mapList ??= _wad.Lumps.Where(x => AllMapsRegEx.IsMatch(x.Name)).ToList();

    private bool HasUltimateDoomMaps => MapList.Any(map => UltimateDoomMapRegEx.IsMatch(map.Name));
    private bool UsesEpisodeAndMap => MapList.Any(x => EpisodeAndMapRegEx.IsMatch(x.Name));
    private bool UsesMapOnly => MapList.Any(x => x.Name.StartsWith("MAP"));

    /// <summary>
    /// Initializes the wad analyzer.
    /// </summary>
    /// <param name="wadReader">A <see cref="WadReader"/> containing a loaded WAD.</param>
    public Analyzer(in WadReader wadReader) {
        _wad = wadReader;
    }

    /// <summary>
    /// Analyzes the loaded WAD file.
    /// </summary>
    /// <returns>An <see cref="AnalysisResults"/> object containing information about the loaded WAD.</returns>
    public AnalysisResults AnalyzeWad() {
        var (compLevel, hasMismatchedBosses) = DetectCompLevelAndMismatchedBosses();

        return new AnalysisResults(
            CompLevel: compLevel,
            ContainsExMxMaps: UsesEpisodeAndMap,
            ContainsMapXxMaps: UsesMapOnly,
            HasMismatchedBosses: hasMismatchedBosses,
            MapList: MapList
        );
    }

    /// <summary>
    /// Attempts to guess the ideal complevel for this WAD. This also returns whether a map contains unexpected boss enemies in final maps.
    /// </summary>
    /// <returns>The detected complevel and a boolean indicating whether or not unexpected boss enemies were found.</returns>
    private (CompLevel, bool) DetectCompLevelAndMismatchedBosses() {
        var compLevel = CompLevel.Doom19;
        var hasMismatchedBosses = false;
        
        // Perform a linedef/sector analysis to guess the correct complevel.
        foreach(var lump in _wad.Lumps) {
            // Indiscriminately check all supported lumps.
            var resultComplevel = lump.Name switch {
                "DEHACKED" => Lumpalyzer.AnalyzeDeHackEd(_wad.GetLump(lump)),
                "LINEDEFS" => Lumpalyzer.AnalyzeLinedefs(_wad.GetLump(lump)),
                "SECTORS" => Lumpalyzer.AnalyzeSectors(_wad.GetLump(lump)),
                "THINGS" => Lumpalyzer.AnalyzeThings(_wad.GetLump(lump)),
                _ => compLevel
            };

            compLevel = compLevel.Promote(resultComplevel);
        }

        /*
         * Perform any necessary complevel adjustments.
         */
        if(compLevel < CompLevel.Boom && !UsesMapOnly) {
            // If Ultimate Doom maps are detected, we already have a good idea which
            // complevel this should be.
            if(HasUltimateDoomMaps) {
                return (CompLevel.UltimateDoom, hasMismatchedBosses);
            }

            hasMismatchedBosses = DetectMismatchedBosses();
            compLevel = compLevel.Promote(CompLevel.UltimateDoom, !hasMismatchedBosses);
        }
        
        return (compLevel, hasMismatchedBosses);
    }

    /// <summary>
    /// Detects whether or not the WAD contains unexpected boss enemies in E1M8.
    /// </summary>
    /// <returns><c>true</c> if the WAD has unexpected bosses in E1M8, or <c>false</c> if it does not, or if the WAD does not
    /// contain an E1M8.</returns>
    private bool DetectMismatchedBosses() {
        // Check for mismatched boss enemies (Cyberdemon/Mastermind on E1M8).
        if(!MapList.Any(lump => lump.Name == "E1M8")) {
            return false;
        }

        var mapLumps = GetMapLumps("E1M8");
        var sectorLump = mapLumps.FirstOrDefault(x => x.Name == "SECTORS");
        var thingLump = mapLumps.FirstOrDefault(x => x.Name == "THINGS");

        if(sectorLump is null || thingLump is null) {
            return false;
        }

        var sectorStream = _wad.GetLump(sectorLump);
        var thingStream = _wad.GetLump(thingLump);

        return Lumpalyzer.HasMismatchedBossEncounter("E1M8", sectorStream, thingStream);
    }

    /// <summary>
    /// Fetches a list of map lumps associated with a particular level slot.
    /// </summary>
    /// <param name="mapName">The name of the map to analyze.</param>
    /// <returns>A list of lumps related to the given map, or an empty list if the map does not exist in the WAD.</returns>
    private List<WadDirectoryEntry> GetMapLumps(string mapName) {
        // NOTE: If we need to fetch more items, expand this list accordingly.
        var candidates = new List<string> {
            "LINEDEFS",
            "SECTORS",
            "THINGS"
        };

        var mapFound = false;
        var mapLumps = new List<WadDirectoryEntry>();
        foreach(var lump in _wad.Lumps) {
            if(!mapFound) {
                if(lump.Name != mapName) continue;

                mapFound = true;
                continue;
            }

            // Check for candidates and add them to the list.
            if(candidates.Contains(lump.Name))
                mapLumps.Add(lump);

            // If we run into a marker lump, we've gone too far.
            if(lump.Size == 0) break;
        }

        return mapLumps;
    }
}
