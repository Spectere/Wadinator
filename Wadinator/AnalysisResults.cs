namespace Wadinator;

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
public record AnalysisResults(
    CompLevel CompLevel,
    bool ContainsExMxMaps,
    bool ContainsMapXxMaps,
    bool HasMismatchedBosses,
    List<WadDirectoryEntry> MapList,
    List<WadDirectoryEntry> MusicList,
    bool IsDeathmatchWad
);
