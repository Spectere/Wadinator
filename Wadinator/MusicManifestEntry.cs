namespace Wadinator; 

/// <summary>
/// User-defined metadata about a music file.
/// </summary>
public class MusicManifestEntry {
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
}
