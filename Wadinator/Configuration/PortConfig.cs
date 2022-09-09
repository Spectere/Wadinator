using Tomlet.Attributes;

namespace Wadinator.Configuration; 

public class PortConfig {
    [TomlProperty("filename")]
    public string ExecutableName { get; set; } = "";

    [TomlProperty("uses-complevels")]
    public bool UsesComplevels { get; set; } = false;
}
