using Tomlet.Attributes;

namespace Wadinator.Configuration; 

public class WadinatorConfig {
    [TomlProperty("default-path")]
    public string DefaultPath { get; set; } = "";

    [TomlProperty("default-recurse")]
    public bool DefaultRecurse { get; set; } = false;

    [TomlProperty("games")]
    public Games Games { get; set; } = new();

    [TomlProperty("iwads")]
    public Iwads Iwads { get; set; } = new();
}
