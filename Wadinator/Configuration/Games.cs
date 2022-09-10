using Tomlet.Attributes;

namespace Wadinator.Configuration; 

/// <summary>
/// Contains game-specific information.
/// </summary>
public class Games {
    /// <summary>
    /// Contains configuration information specific to Doom.
    /// </summary>
    [TomlProperty("doom")]
    public PortConfig Doom { get; set; } = new();

    /// <summary>
    /// Contains configuration information specific to Heretic.
    /// </summary>
    [TomlProperty("heretic")]
    public PortConfig Heretic { get; set; } = new();
}
