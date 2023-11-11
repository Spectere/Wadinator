using Tomlet.Attributes;

namespace Wadinator.Configuration; 

public class MusicRandomizerConfig {
    /// <summary>
    /// If this is set to <c>true</c>, a music WAD will be generated so that the player
    /// doesn't have to constantly endure D_E1M1 and D_RUNNIN. This setting defaults to
    /// <c>false</c>.
    /// </summary>
    [TomlProperty("generate-music-wad")]
    public bool GenerateMusicWad { get; set; }

    /// <summary>
    /// The filename of the music WAD. This setting defaults to "WadinatorMusic.wad".
    /// </summary>
    [TomlProperty("music-wad-filename")]
    public string MusicWadFilename { get; set; } = "WadinatorMusic.wad";

    /// <summary>
    /// The directory containing the music lumps that should be used by the randomizer.
    /// This setting defaults to "MusicLumps".
    /// </summary>
    [TomlProperty("source-lump-path")]
    public string SourceLumpPath { get; set; } = "MusicLumps";

    /// <summary>
    /// If this is set to <c>true</c>, a music lump should be generated even if the WAD
    /// already has custom music in that slot. This setting defaults to <c>false</c>.
    /// </summary>
    [TomlProperty("replace-existing-music")]
    public bool ReplaceExistingMusic { get; set; } = false;
    
    /// <summary>
    /// If this is set to <c>true</c>, random music will be used for the intermission
    /// screen as well. Note that even if this is set to <c>true</c>, the value of
    /// <see cref="ReplaceExistingMusic"/> will still be considered. This setting defaults
    /// to <c>true</c>.
    /// </summary>
    [TomlProperty("replace-intermission-music")]
    public bool ReplaceIntermissionMusic { get; set; } = true;

    /// <summary>
    /// If this is set to <c>true</c>, a music lump chosen for the intermission screen music
    /// will be tallied. This will impact the likelihood that it will be picked again, as
    /// the music randomizer puts a higher weight on songs that have been picked fewer times.
    /// This setting defaults to <c>false</c>.
    /// </summary>
    [TomlProperty("track-intermission-music-usage")]
    public bool TrackIntermissionMusicUsage { get; set; } = false;

    /// <summary>
    /// If this is set to <c>true</c>, the chosen music will be displayed when the Wadinator is run.
    /// </summary>
    [TomlProperty("display-selected-tracks")]
    public bool DisplaySelectedTracks { get; set; } = true;

    /// <summary>
    /// If this is set to <c>true</c>, songs that have the "copyright" flag configured in the
    /// manifest will be eligible for selection. This should be set to <c>false</c> (or overridden
    /// using the command line parameter) if you plan to broadcast yourself playing random
    /// WADs on Twitch or YouTube. Please note that the effectiveness of this setting depends
    /// on the music manifest being properly configured. The Wadinator has no way of knowing
    /// if a MIDI is going to detected by a copyright bot. 
    /// </summary>
    [TomlProperty("allow-copyrighted-tracks")]
    public bool AllowCopyrightedTracks { get; set; } = true;
}
