using System.Text;

namespace Wadinator;

/// <summary>
/// Used to specify lumps that should be loaded into a new WAD via the <see cref="WadWriter"/>.
/// </summary>
public class WadWriterLump {
    /// <summary>
    /// The name of the new lump.
    /// </summary>
    public string LumpName { get; }

    /// <summary>
    /// The file that should be loaded into the WAD.
    /// </summary>
    public string DataFilename { get; }

    /// <summary>
    /// The starting byte of the lump within the WAD. This is used by <see cref="WadWriter"/> and will be
    /// overwritten during the WAD's creation.
    /// </summary>
    public int StartingByte { get; set; }

    /// <summary>
    /// The length of the file.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Creates a new output lump.
    /// </summary>
    /// <param name="lumpName">The name of the lump in the new WAD file. If this is greater than 8 characters in length, it will
    /// be truncated.</param>
    /// <param name="dataFilename">The file containing the data that should be loaded into the WAD.</param>
    public WadWriterLump(string lumpName, string dataFilename) {
        LumpName = (lumpName.Length > 8 ? lumpName[..8] : lumpName).ToUpper();
        DataFilename = dataFilename;

        Length = (int)new FileInfo(dataFilename).Length;
    }

    /// <summary>
    /// Retrieves the name of this lump in a format that the WAD file format expects.
    /// </summary>
    /// <returns></returns>
    public byte[] GetWadLumpName() => Encoding.GetEncoding(437).GetBytes(LumpName.PadRight(8, '\0'));
}
