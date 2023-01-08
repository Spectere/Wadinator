namespace Wadinator.Data; 

/// <summary>
/// A WAD that was selected by the Wadinator.
/// </summary>
public class SelectedWad {
    /// <summary>
    /// The filename of the WAD.
    /// </summary>
    public string Filename { get; set; } = "";

    /// <summary>
    /// <c>true</c> if the WAD was automatically skipped, otherwise <c>false</c>.
    /// </summary>
    public bool Skipped { get; set; }
}
