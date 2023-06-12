using System.Text;

namespace Wadinator;

/// <summary>
/// Contains the results of a WAD analysis.
/// </summary>
public record AnalysisResults {
    /// <summary>The detected <see cref="CompLevel"/> for this WAD.</summary>
    public CompLevel CompLevel { get; }

    /// <summary><c>true</c> if this WAD contains maps with ExMx map lumps, otherwise <c>false</c>.</summary>
    public bool ContainsExMxMaps { get; }

    /// <summary><c>true</c> if this WAD contains maps with MAPxx map lumps, otherwise <c>false</c>.</summary>
    public bool ContainsMapXxMaps { get; }

    /// <summary><c>true</c> if this WAD contains Cyberdemon(s) or Spider Mastermind(s), as well as sectors with
    /// tag 666, in E1M8.</summary>
    public bool HasMismatchedBosses { get; }

    /// <summary>A list of maps lumps contained in this WAD.</summary>
    public List<WadDirectoryEntry> MapList { get; }

    /// <summary>A list of music lumps contained in this WAD.</summary>
    public List<WadDirectoryEntry> MusicList { get; }

    /// <summary><c>true</c> if this WAD is flagged as a deathmatch WAD. This will be set to <c>false</c> if
    /// deathmatch WAD detection is disabled, or if the check returns negative.</summary>
    public bool IsDeathmatchWad { get; }
    
    /// <summary>
    /// Contains the results of a WAD analysis.
    /// </summary>
    /// <param name="CompLevel">The detected <see cref="CompLevel"/> for this WAD.</param>
    /// <param name="ContainsExMxMaps"><c>true</c> if this WAD contains maps with ExMx map lumps, otherwise <c>false</c>.</param>
    /// <param name="ContainsMapXxMaps"><c>true</c> if this WAD contains maps with MAPxx map lumps, otherwise <c>false</c>.</param>
    /// <param name="HasMismatchedBosses"><c>true</c> if this WAD contains Cyberdemon(s) or Spider Mastermind(s), as well as sectors with
    /// tag 666, in E1M8.</param>
    /// <param name="MapList">A list of maps lumps contained in this WAD.</param>
    /// <param name="MusicList">A list of music lumps contained in this WAD.</param>
    /// <param name="IsDeathmatchWad"><c>true</c> if this WAD is flagged as a deathmatch WAD. This will be set to <c>false</c> if
    /// deathmatch WAD detection is disabled, or if the check returns negative.</param>
    public AnalysisResults(CompLevel CompLevel,
                           bool ContainsExMxMaps,
                           bool ContainsMapXxMaps,
                           bool HasMismatchedBosses,
                           List<WadDirectoryEntry> MapList,
                           List<WadDirectoryEntry> MusicList,
                           bool IsDeathmatchWad) {
        this.CompLevel = CompLevel;
        this.ContainsExMxMaps = ContainsExMxMaps;
        this.ContainsMapXxMaps = ContainsMapXxMaps;
        this.HasMismatchedBosses = HasMismatchedBosses;
        this.MapList = MapList;
        this.MusicList = MusicList;
        this.IsDeathmatchWad = IsDeathmatchWad;
    }

    /// <summary>
    /// This adjusts the Doom II MAPxx name so that the episodes are separated appropriately.
    /// If we don't do this, we'll end up with MAP01-09 (9 maps) on one line and MAP10-19 (10
    /// maps) on the next.
    /// </summary>
    /// <param name="mapName">The name of the map to adjust.</param>
    /// <returns>The adjusted map name. This will be the normal map name with the map number
    /// decremented by one. If an error occurs, the original map name will be returned.</returns>
    private string AdjustDoom2MapNameForEpisodeSort(string mapName) =>
        int.TryParse(mapName.AsSpan(3, 2), out var mapNumber)
            ? $"MAP{mapNumber - 1:00}"
            : mapName;

    /// <summary>
    /// Returns a formatted list of maps.
    /// </summary>
    /// <param name="padding">The number of spaces of padding to put before each line.</param>
    /// <returns>A string containing a formatted list of maps.</returns>
    public string GetFormattedMapList(int padding) {
        StringBuilder output = new();
        var pad = new string(' ', padding);
        
        if(ContainsExMxMaps) {
            // Put each episode on its own line.
            var prefixes = MapList.Select(x => x.Name)
                                  .Where(x => x.StartsWith("E"))
                                  .Select(x => x[..2])
                                  .Distinct()
                                  .OrderBy(x => x);

            foreach(var prefix in prefixes) {
                // Find the maps in each episode, trim the strings to remove any excess junk, and display them in a nice list.
                var episodeMaps = MapList.Select(x => x.Name)
                                         .Where(x => x.StartsWith(prefix))
                                         .Select(x => x[..4])
                                         .Distinct()
                                         .OrderBy(x => x);

                output.Append(pad);
                output.AppendLine(string.Join(", ", episodeMaps));
            }
        }

        if(ContainsMapXxMaps) {
            // Put each "episode" (10 map set) on its own line.
            var prefixes = MapList.Select(x => x.Name)
                                  .Where(x => x.StartsWith("MAP"))
                                  .Select(x => AdjustDoom2MapNameForEpisodeSort(x)[..4])
                                  .Distinct()
                                  .OrderBy(x => x);

            foreach(var prefix in prefixes) {
                // Find the maps in each "episode," trim the strings down to remove any excess junk, and display them in a nice list.
                var episodeMaps = MapList.Select(x => x.Name)
                                         .Where(x => AdjustDoom2MapNameForEpisodeSort(x).StartsWith(prefix))
                                         .Select(x => x[..5])
                                         .Distinct()
                                         .OrderBy(x => x);

                output.Append(pad);
                output.AppendLine(string.Join(", ", episodeMaps));
            }
        }

        return output.ToString();
    }
}
