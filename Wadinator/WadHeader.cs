namespace Wadinator; 

/// <summary>
/// The header of the WAD file.
/// </summary>
/// <param name="Magic">The file magic (first four bytes).</param>
/// <param name="Entries">The number of directory entries in the WAD file.</param>
/// <param name="DirectoryPosition">The position of the WAD's directory.</param>
public record WadHeader(
    uint Magic,
    int Entries,
    int DirectoryPosition
);
