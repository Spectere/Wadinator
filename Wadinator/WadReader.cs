using System.Collections.ObjectModel;
using System.Text;

namespace Wadinator;

public class WadReader : IDisposable {
    public class LumpNotFoundException : Exception {
        public LumpNotFoundException() {}
        public LumpNotFoundException(string lumpName) : base($"{lumpName} could not be found!") {}
        public LumpNotFoundException(string lumpName, Exception inner) : base($"{lumpName} could not be found!", inner) {}
    }

    public enum WadType {
        Pwad,
        Iwad,
        Unknown
    }

    /// <summary>
    /// Describes a single directory entry in a WAD.
    /// </summary>
    /// <param name="Position">The position of the data.</param>
    /// <param name="Size">The size of the data.</param>
    /// <param name="Name">The filename associated with the entry.</param>
    public record WadDirectoryEntry(int Position, int Size, string Name);

    /// <summary>
    /// The header of the WAD file.
    /// </summary>
    /// <param name="Magic">The file magic (first four bytes).</param>
    /// <param name="Entries">The number of directory entries in the WAD file.</param>
    /// <param name="DirectoryPosition">The position of the WAD's directory.</param>
    public record WadHeader(uint Magic, int Entries, int DirectoryPosition);

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
                Encoding.ASCII.GetString(_binaryReader.ReadBytes(8)).TrimEnd('\0')
            ));
        }

        Lumps = new ReadOnlyCollection<WadDirectoryEntry>(entriesList);
    }

    public Stream GetLump(WadDirectoryEntry entry) {
        _fileStream.Seek(entry.Position, SeekOrigin.Begin);
        var data = _binaryReader.ReadBytes(entry.Size);
        return new MemoryStream(data);
    }

    public Stream GetLump(string name) {
        var entry = Lumps.SingleOrDefault(x => x.Name == name);

        if(entry is null)
            throw new LumpNotFoundException(name);

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
