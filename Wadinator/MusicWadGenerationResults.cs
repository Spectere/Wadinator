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
}
