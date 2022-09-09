namespace Wadinator; 

/// <summary>
/// Describes a single directory entry in a WAD.
/// </summary>
/// <param name="Position">The position of the data.</param>
/// <param name="Size">The size of the data.</param>
/// <param name="Name">The filename associated with the entry.</param>
public record WadDirectoryEntry(
    int Position,
    int Size,
    string Name
);
