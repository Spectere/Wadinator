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
    public bool RecurseDirectories { get; set; } = false;
    
    /// <summary>
    /// Specifies whether randomly picked WADs are logged to the "played" file.
    /// </summary>
    [TomlProperty("log-rng-results")]
    public bool LogRandomWadResults { get; set; } = true;

    /// <summary>
    /// Specifies that the input WADs should be played with Heretic.
    /// </summary>
    [TomlProperty("use-heretic")]
    public bool UseHeretic { get; set; } = false;

    /// <summary>
    /// Should the matching text file's contents be printed?
    /// </summary>
    [TomlProperty("print-contents")]
    public bool PrintContents { get; set; } = false;

    /// <summary>
    /// An object containing text editor configuration.
    /// </summary>
    [TomlProperty("editor")]
    public Editor Editor { get; set; } = new();

    /// <summary>
    /// An object containing game-specific configuration.
    /// </summary>
    [TomlProperty("games")]
    public Games Games { get; set; } = new();
}
