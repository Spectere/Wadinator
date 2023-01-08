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
    /// The filenames sharing the same SHA1 hash.
    /// </summary>
    public List<string> Filenames { get; set; } = new();
    
    /// <summary>
    /// The number of times the lump with this hash has been played.
    /// </summary>
    public int SelectionCount { get; set; }
}
