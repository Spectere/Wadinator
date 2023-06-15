using Tomlet.Attributes;

namespace Wadinator.Configuration; 

/// <summary>
/// Contains settings for the WAD analysis engine.
/// </summary>
public class Analysis {
    /// <summary>
    /// If this is set to <c>true</c>, WADs containing a given number of enemies or less
    /// (determined using the <see cref="SkipDeathmatchThreshold"/> setting) will be
    /// skipped.
    /// </summary>
    [TomlProperty("skip-deathmatch-maps")]
    public bool SkipDeathmatchMaps { get; set; } = true;

    /// <summary>
    /// If <see cref="SkipDeathmatchMaps"/> is enabled, WADs will be skipped if they
    /// contain this number of enemies or fewer. This option defaults to 0 and does
    /// nothing if <see cref="SkipDeathmatchMaps"/> is set to <c>false</c>. Note that
    /// the number of enemies in a WAD will be added together and compared to this
    /// value.
    /// </summary>
    [TomlProperty("skip-deathmatch-threshold")]
    public int SkipDeathmatchThreshold { get; set; } = 0;

    /// <summary>
    /// If this is set to <c>true</c>, maps that are skipped will be logged into the
    /// "played" file. This option defaults to <c>false</c>.
    /// </summary>
    [TomlProperty("record-skipped-maps")]
    public bool RecordSkippedMaps { get; set; } = false;

    /// <summary>
    /// If this is set to <c>true</c>, a message will be displayed when a map or maps
    /// lack a way for the player to exit the level.
    /// </summary>
    [TomlProperty("report-maps-with-no-exit")]
    public bool ReportMapsWithNoExit { get; set; } = true;
}
