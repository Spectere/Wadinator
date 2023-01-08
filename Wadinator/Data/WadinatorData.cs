namespace Wadinator.Data; 

/// <summary>
/// A collection of data stored by the Wadinator, such as played WADs.
/// </summary>
public class WadinatorData {
    /// <summary>
    /// WADs that were selected by the Wadinator.
    /// </summary>
    public List<SelectedWad> SelectedWads { get; set; } = new();
}
