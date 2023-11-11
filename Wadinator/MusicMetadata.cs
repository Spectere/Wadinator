namespace Wadinator; 

/// <summary>
/// User-defined metadata about a music file.
/// </summary>
public class MusicMetadata {
    /// <summary>
    /// The title of the song.
    /// </summary>
    public string? Title { get; set; } = null;

    /// <summary>
    /// The author of the song.
    /// </summary>
    public string? Artist { get; set; } = null;

    /// <summary>
    /// The artist who transcribed this song into the given format.
    /// </summary>
    public string? Sequencer { get; set; } = null;

    /// <summary>
    /// If this is set to <c>true</c>, the track will be flagged as potentially being
    /// flagged by an automated copyright bot on YouTube, Twitch, etc. This should be
    /// set to <c>true</c> for all MIDI or MOD covers of commercial music, just to be
    /// on the safe side.
    /// </summary>
    public bool? Copyright { get; set; } = null;
}
