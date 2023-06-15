using System.Text.RegularExpressions;

namespace Wadinator;

/// <summary>
/// Responsible for analyzing WADs and reporting the results to the user.
/// </summary>
public class Analyzer {
    private List<WadDirectoryEntry>? _mapList;
    private readonly WadReader _wad;
    private AnalysisSettings _analysisSettings;

    // Map List Regex
    private static readonly Regex AllMapsRegEx = new("^(E.M.|MAP..)");
    private static readonly Regex EpisodeAndMapRegEx = new("^(E.M.)");
    private static readonly Regex UltimateDoomMapRegEx = new Regex("^(E4M.)");

    // Map Lump Information
    private List<WadDirectoryEntry> MapList =>
        _mapList ??= _wad.Lumps.Where(x => AllMapsRegEx.IsMatch(x.Name)).ToList();

    private bool HasUltimateDoomMaps => MapList.Any(map => UltimateDoomMapRegEx.IsMatch(map.Name));
    private bool UsesEpisodeAndMap => MapList.Any(x => EpisodeAndMapRegEx.IsMatch(x.Name));
    private bool UsesMapOnly => MapList.Any(x => x.Name.StartsWith("MAP"));
    
    // Map Lump Directory Entries
    private struct MapLumpCollection {
        public WadDirectoryEntry Linedefs;
        public WadDirectoryEntry Sectors;
        public WadDirectoryEntry Things;
    }

    /// <summary>
    /// Initializes the wad analyzer.
    /// </summary>
    /// <param name="wadReader">A <see cref="WadReader"/> containing a loaded WAD.</param>
    /// <param name="analysisSettings">Settings for the WAD analyzer.</param>
    public Analyzer(in WadReader wadReader, AnalysisSettings analysisSettings) {
        _wad = wadReader;
        _analysisSettings = analysisSettings;
    }

    /// <summary>
    /// Analyzes the loaded WAD file.
    /// </summary>
    /// <returns>An <see cref="AnalysisResults"/> object containing information about the loaded WAD.</returns>
    public AnalysisResults AnalyzeWad() {
        // Detect complevel and mismatched bosses.
        var (compLevel, hasMismatchedBosses) = DetectCompLevelAndMismatchedBosses();

        // Detect deathmatch WADs.
        var isDeathmatchWad = false;
        if(_analysisSettings.DetectDeathmatchWads) {
            isDeathmatchWad = DetectDeathmatchWad();
        }
        
        // Detect maps with no exit.
        var mapsWithNoExit = DetectMapsWithNoExit();
        
        // Return everything.
        return new AnalysisResults(
            CompLevel: compLevel,
            ContainsExMxMaps: UsesEpisodeAndMap,
            ContainsMapXxMaps: UsesMapOnly,
            HasMismatchedBosses: hasMismatchedBosses,
            MapList: MapList,
            MapsWithNoExit: mapsWithNoExit,
            MusicList: _wad.Lumps.Where(x => x.Name.StartsWith("D_")).ToList(),
            IsDeathmatchWad: isDeathmatchWad
        );
    }

    /// <summary>
    /// Attempts to guess the ideal complevel for this WAD. This also returns whether a map contains unexpected boss enemies in final maps.
    /// </summary>
    /// <returns>The detected complevel and a boolean indicating whether or not unexpected boss enemies were found.</returns>
    private (CompLevel, bool) DetectCompLevelAndMismatchedBosses() {
        var compLevel = CompLevel.Doom19;
        var hasMismatchedBosses = false;
        
        // Look for a DEHACKED lump, and analyze that if it exists.
        var dehackedLump = _wad.Lumps.FirstOrDefault(x => x.Name == "DEHACKED");
        if(dehackedLump is not null) {
            compLevel = compLevel.Promote(Lumpalyzer.AnalyzeDeHackEd(_wad.GetLump(dehackedLump)));
        }

        // Next, analyze the sectors, linedefs, and things for each detected map.
        foreach(var mapName in MapList) {
            var mapLumps = GetMapLumps(mapName.Name);

            if(mapLumps is null) {
                continue;
            }

            compLevel = compLevel.Promote(Lumpalyzer.AnalyzeLinedefs(_wad.GetLump(mapLumps.Value.Linedefs)));
            compLevel = compLevel.Promote(Lumpalyzer.AnalyzeSectors(_wad.GetLump(mapLumps.Value.Sectors)));
            compLevel = compLevel.Promote(Lumpalyzer.AnalyzeThings(_wad.GetLump(mapLumps.Value.Things)));
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
    /// Detects whether or not the WAD is a deathmatch WAD.
    /// </summary>
    /// <returns><c>true</c> if the WAD is determined to be a deathmatch WAD, otherwise <c>false</c>.</returns>
    private bool DetectDeathmatchWad() {
        var thingLumps = _wad.Lumps.Where(x => x.Name == "THINGS");
        
        var totalEnemyCount = 0;

        foreach(var thingLump in thingLumps) {
            // Check for a player start.
            if(!Lumpalyzer.HasPlayer1Start(_wad.GetLump(thingLump))) {
                // This WAD will not be fully playable in single-player. Abort here.
                return true;
            }

            // Check enemy count.
            totalEnemyCount += Lumpalyzer.GetEnemyCount(_wad.GetLump(thingLump), _analysisSettings.IsHeretic);
        }

        if(_analysisSettings.DeathmatchMapEnemyThreshold >= totalEnemyCount) {
            return true;
        };

        return false;
    }

    /// <summary>
    /// Detects maps that do not have an exit.
    /// </summary>
    /// <returns>A list of maps that lack an exit.</returns>
    private List<WadDirectoryEntry> DetectMapsWithNoExit() {
        var mapsWithNoExit = new List<WadDirectoryEntry>();

        foreach(var map in MapList) {
            var hasExit = false;
            var mapLumpsNullable = GetMapLumps(map.Name);

            if(!mapLumpsNullable.HasValue) {
                // This shouldn't happen, but handle it just in case.
                Console.WriteLine($"DetectMapsWithNoEdit: unable to find map lumps for {map.Name}");
                continue;
            }
            var mapLumps = mapLumpsNullable.Value;
            
            //
            // LINEDEFS
            //
            var linedefIds = new List<ushort> {
                11,   // S1 Exit
                51,   // S1 Secret Exit
                52,   // W1 Exit
                124,  // W1 Secret Exit
                197,  // G1 Exit (Boom)
                198   // G1 Secret Exit (Boom)
            };
            var linedefStream = _wad.GetLump(mapLumps.Linedefs);

            if(Lumpalyzer.LinedefTypeExists(linedefStream, linedefIds)) {
                hasExit = true;
            }
            
            //
            // SECTORS
            //
            var sectorStream = _wad.GetLump(mapLumps.Sectors);
            
            // Type 11: 20% damage, exit
            if(Lumpalyzer.SectorTypeExists(sectorStream, 11)) {
                hasExit = true;
            }
            
            // Generalized types: 12+6 (kill all players, exit), 12+6+5 (kill all players, secret exit)
            var sectorIds = new List<ushort> {
                0b0001_0000_0100_0000,  //     12 + 6: kill all players, exit
                0b0001_0000_0110_0000,  // 12 + 6 + 5: kill all players, secret exit
            };
            sectorStream = _wad.GetLump(mapLumps.Sectors);
            if(Lumpalyzer.SectorTypeExists(sectorStream, sectorIds, true)) {
                hasExit = true;
            }

            //
            // THINGS
            //
            var thingIds = new List<ushort>();
            var thingsStream = _wad.GetLump(mapLumps.Things);

            // SPECIAL CASE: Romero's Head (Thing ID: 88)
            thingIds.Add(88);
            
            // SPECIAL CASE: Cyberdemon on E2M8 (Thing ID: 16)
            if(map.Name.ToUpper().StartsWith("E2M8")) {
                thingIds.Add(16);
            }

            // SPECIAL CASE: Spider Mastermind on E3M8 (Thing ID: 7)
            if(map.Name.ToUpper().StartsWith("E3M8")) {
                thingIds.Add(7);
            }

            if(Lumpalyzer.ThingTypeExists(thingsStream, thingIds)) {
                hasExit = true;
            }
            
            // Done. Did we find an exit?
            if(!hasExit) {
                // Nope. Add it to the list.
                mapsWithNoExit.Add(map);
            }
        }

        return mapsWithNoExit;
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

        if(mapLumps is null) {
            return false;
        }

        var sectorLump = mapLumps.Value.Sectors;
        var thingLump = mapLumps.Value.Things;

        var sectorStream = _wad.GetLump(sectorLump);
        var thingStream = _wad.GetLump(thingLump);

        return Lumpalyzer.HasMismatchedBossEncounter("E1M8", sectorStream, thingStream);
    }

    /// <summary>
    /// Fetches a list of map lumps associated with a particular level slot.
    /// </summary>
    /// <param name="mapName">The name of the map to analyze.</param>
    /// <returns>A list of lumps related to the given map, or <c>null</c> if the map does not exist in the WAD.</returns>
    private MapLumpCollection? GetMapLumps(string mapName) {
        // Some early WAD editors mangle the names of the lumps, so we use indexes instead of lump names now.
        const int linedefsOffset = 2;
        const int sectorsOffset = 8;
        const int thingsOffset = 1;

        var result = new MapLumpCollection();

        var mapFound = false;
        var mapIndex = -1;

        // Fetch the base index of the map lump.
        for(var i = 0; i < _wad.Lumps.Count; i++) {
            if(!_wad.Lumps[i].Name.StartsWith(mapName)) {
                continue;
            }

            mapFound = true;
            mapIndex = i;
        }

        if(!mapFound) {
            return null;
        }

        // Populate the collection and return the results.
        result.Linedefs = _wad.Lumps[mapIndex + linedefsOffset];
        result.Sectors = _wad.Lumps[mapIndex + sectorsOffset];
        result.Things = _wad.Lumps[mapIndex + thingsOffset];

        return result;
    }
}
