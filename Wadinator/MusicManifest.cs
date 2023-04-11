namespace Wadinator; 

/// <summary>
/// Describes a directory of music.
/// </summary>
public class MusicManifest {
    /// <summary>
    /// A collection of user-provided metadata for a directory of music. The key of each
    /// entry is the filename, with the value being a <see cref="MusicMetadata"/>.
    /// </summary>
    public Dictionary<string, MusicMetadata>? Entries { get; set; }
}
