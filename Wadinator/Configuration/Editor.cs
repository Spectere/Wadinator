using Tomlet.Attributes;

namespace Wadinator.Configuration; 

/// <summary>
/// Contains configuration for text editors.
/// </summary>
public class Editor {
    /// <summary>
    /// The path to the text editor.
    /// </summary>
    [TomlProperty("filename")]
    public string ExecutableName { get; set; } = "";

    /// <summary>
    /// The argument passed to the text editor to open the file in read-only mode.
    /// </summary>
    [TomlProperty("read-only-arg")]
    public string ReadOnlyArg { get; set; } = "";
}
