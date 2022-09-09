namespace Wadinator; 

/// <summary>
/// Indicates the type of WAD.
/// </summary>
public enum WadType {
    /// <summary>
    /// An unknown WAD type.
    /// </summary>
    Unknown,

    /// <summary>
    /// A patch WAD. This is the most common type of third-party WAD.
    /// </summary>
    Pwad,
    
    /// <summary>
    /// An official WAD. This is generally only used for the official Doom, Doom II, Final Doom, and Heretic WADs.
    /// </summary>
    Iwad
}
