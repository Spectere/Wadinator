using Tomlet.Attributes;

namespace Wadinator.Configuration; 

/// <summary>
/// Represents a Wadinator config file.
/// </summary>
public class WadinatorConfig {
    /// <summary>
    /// Specifies the user's default path.
    /// </summary>
    [TomlProperty("default-path")]
    public string DefaultPath { get; set; } = "";

    /// <summary>
    /// Indicates whether or not paths are recursed by default.
    /// </summary>
    [TomlProperty("default-recurse")]
    public bool DefaultRecurse { get; set; } = false;

    /// <summary>
    /// An object containing game-specific configuration.
    /// </summary>
    [TomlProperty("games")]
    public Games Games { get; set; } = new();

    /// <summary>
    /// An object containing paths to IWAD files.
    /// </summary>
    [TomlProperty("iwads")]
    public Iwads Iwads { get; set; } = new();
}
