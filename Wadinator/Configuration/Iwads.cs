using Tomlet.Attributes;

namespace Wadinator.Configuration; 

/// <summary>
/// Contains paths pointing to IWADs.
/// </summary>
public class Iwads {
    /// <summary>
    /// The path to Heretic's IWAD file.
    /// </summary>
    [TomlProperty("heretic")]
    public string Heretic { get; set; } = "HERETIC.WAD";
}
