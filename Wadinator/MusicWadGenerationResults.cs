using Wadinator.Data;

namespace Wadinator;

/// <summary>
/// Contains the results of the <see cref="MusicRandomizer.GenerateWad"/> method.
/// </summary>
public class MusicWadGenerationResults {
    /// <summary>
    /// <c>true</c> if the WAD generation process was successful, otherwise <c>false</c>.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The music lumps data object, with the selection counts updated appropriately.
    /// </summary>
    public List<MusicLump> MusicLumps { get; set; } = new();

    /// <summary>
    /// A dictionary containing the map whose music was replaced, as well as information
    /// about the lump that replaced it. This can be used to report the selected tracks
    /// back to the user.
    /// </summary>
    public Dictionary<string, MusicLump> SelectedLumps { get; set; } = new();
}
