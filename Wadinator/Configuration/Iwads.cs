using Tomlet.Attributes;

namespace Wadinator.Configuration; 

public class Iwads {
    [TomlProperty("heretic")]
    public string Heretic { get; set; } = "HERETIC.WAD";
}
