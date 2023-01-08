using System.Collections.ObjectModel;
using System.Text;
using Wadinator.Exceptions;

namespace Wadinator;

/// <summary>
/// Doom WAD file reader.
/// </summary>
public class WadReader : IDisposable {
    /// <summary>
    /// The header of this WAD file.
    /// </summary>
    public readonly WadHeader Header;

    /// <summary>
    /// A read-only collection of this WAD's directory entries.
    /// </summary>
    public readonly IReadOnlyList<WadDirectoryEntry> Lumps;

    private readonly FileStream _fileStream;
    private readonly BinaryReader _binaryReader;

    /// <summary>
    /// Gets the type of this WAD file.
    /// </summary>
    public WadType Type => Header.Magic switch {
        0x44415749 => WadType.Iwad,
        0x44415750 => WadType.Pwad,
        _          => WadType.Unknown
    };

    /// <summary>
    /// Creates a new <see cref="WadReader"/> object from the given file.
    /// </summary>
    /// <param name="filename">The WAD file to open.</param>
    /// <returns>A <see cref="WadReader"/> object containing data from the loaded file.</returns>
    public WadReader(string filename) {
        _fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        _binaryReader = new BinaryReader(_fileStream);

        // Read the WAD header.
        Header = new WadHeader(
            _binaryReader.ReadUInt32(),  // Magic
            _binaryReader.ReadInt32(),  // Entries
            _binaryReader.ReadInt32()  // Directory Position
        );

        // If the file type is unknown, do not attempt to read the directory.
        // Create a blank Entries collection and return.
        if(Type == WadType.Unknown) {
            Lumps = new ReadOnlyCollection<WadDirectoryEntry>(new List<WadDirectoryEntry>());
            return;
        }

        // Seek to the appropriate spot, read all of the entries, and return.
        _fileStream.Seek(Header.DirectoryPosition, SeekOrigin.Begin);
        var entriesList = new List<WadDirectoryEntry>(Header.Entries);
        for(var i = 0; i < Header.Entries; i++) {
            entriesList.Add(new WadDirectoryEntry(
                _binaryReader.ReadInt32(),
                _binaryReader.ReadInt32(),
                Encoding.ASCII.GetString(_binaryReader.ReadBytes(8)).Split('\0')[0]
            ));
        }

        Lumps = new ReadOnlyCollection<WadDirectoryEntry>(entriesList);
    }

    /// <summary>
    /// Returns a <see cref="Stream"/> containing the contents of a WAD lump.
    /// </summary>
    /// <param name="entry">A <see cref="WadDirectoryEntry"/> describing the lump.</param>
    /// <returns>A <see cref="Stream"/> containing the contents of a WAD lump.</returns>
    public Stream GetLump(WadDirectoryEntry entry) {
        _fileStream.Seek(entry.Position, SeekOrigin.Begin);
        var data = _binaryReader.ReadBytes(entry.Size);
        return new MemoryStream(data);
    }

    /// <summary>
    /// Returns a <see cref="Stream"/> containing the contents of a WAD lump.
    /// </summary>
    /// <param name="name">The name of the lump to locate.</param>
    /// <returns>A <see cref="Stream"/> containing the contents of a WAD lump.</returns>
    /// <exception cref="AmbiguousLumpException">Thrown when multiple identically named lumps are found.</exception>
    /// <exception cref="LumpNotFoundException">Thrown when the lump could not be found.</exception>
    public Stream GetLump(string name) {
        var entry = Lumps.SingleOrDefault(x => x.Name == name);

        if(entry is null) {
            // Figure out exactly what went wrong.
            if(Lumps.Count(x => x.Name == name) > 1) {
                throw new AmbiguousLumpException(name);
            }
            
            throw new LumpNotFoundException(name);
        }

        return GetLump(entry);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose() {
        try {
            _binaryReader.Close();
            _fileStream.Close();
        } catch {
            // ignored
        }

        _binaryReader.Dispose();
        _fileStream.Dispose();
    }
}
