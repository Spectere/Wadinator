using Tomlet.Attributes;

namespace Wadinator.Configuration; 

/// <summary>
/// Contains configuration for source ports.
/// </summary>
public class PortConfig {
    /// <summary>
    /// The path to the source port.
    /// </summary>
    [TomlProperty("filename")]
    public string ExecutableName { get; set; } = "";

    /// <summary>
    /// <c>true</c> if the port makes use of complevels (such as PrBoom-based ports), otherwise <c>false</c>.
    /// </summary>
    [TomlProperty("uses-complevels")]
    public bool UsesComplevels { get; set; } = false;
}
