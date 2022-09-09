using Tomlet.Attributes;

namespace Wadinator.Configuration; 

public class Games {
    [TomlProperty("doom")]
    public PortConfig Doom { get; set; } = new();

    [TomlProperty("heretic")]
    public PortConfig Heretic { get; set; } = new();
}
