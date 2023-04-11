namespace Wadinator.Data; 

/// <summary>
/// Data about music lumps. This is used by the randomizer.
/// </summary>
public class MusicLump {
    /// <summary>
    /// The SHA1 hash for the lump.
    /// </summary>
    public string Sha1 { get; set; } = "";
    
    /// <summary>
    /// If this is <c>true</c>, the music file exists on disk. This will be automatically changed to <c>false</c> if
    /// the Wadinator detects that the file has been deleted.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// The title of this music track. If this is unknown, this should be either <c>null</c> or an empty/whitespace string.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The artist of the track. If this is unknown, this should be either <c>null</c> or an empty/whitespace string.
    /// </summary>
    public string? Artist { get; set; }
    
    /// <summary>
    /// The sequencer of this track. This is intended for music tracks that were transcribed to a supported format (such as MIDI).
    /// </summary>
    public string? Sequencer { get; set; }

    /// <summary>
    /// The number of times the lump with this hash has been played.
    /// </summary>
    public int SelectionCount { get; set; }
}
