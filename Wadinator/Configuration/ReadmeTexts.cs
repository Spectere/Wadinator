using Tomlet.Attributes;

namespace Wadinator.Configuration;

/// <summary>
/// Contains the text file finder settings.
/// </summary>
public class ReadmeTexts {
    /// <summary>
    /// Should Wadinator attempt to search for the WAD's matching text file.
    /// </summary>
    [TomlProperty("search-for-text")]
    public bool SearchForText { get; set; } = true;

    /// <summary>
    /// Should the matching text file's contents be printed?
    /// </summary>
    [TomlProperty("print-contents")]
    public bool PrintContents { get; set; } = false;

    /// <summary>
    /// Should the finder attempt to accommodate for D!Zone?
    /// </summary>
    [TomlProperty("dzone-compat")]
    public bool DZoneCompat { get; set; } = true;
}
