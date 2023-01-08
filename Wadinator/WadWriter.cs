namespace Wadinator; 

// At some point this and WadReader should probably be merged.

/// <summary>
/// Doom WAD file writer. This thing is honestly kind of lame, but it does exactly as much as it needs to do.
/// </summary>
public static class WadWriter {
    private const uint PwadMagic = 0x44415750;
    private const int DirectoryPositionOffset = 0x08;

    /// <summary>
    /// Creates a WAD file with the specified files.
    /// </summary>
    /// <param name="wadFilename">The name of the new WAD.</param>
    /// <param name="inputLumps">A list of <see cref="WadWriterLump"/> objects that should be created.</param>
    public static void Create(string wadFilename, List<WadWriterLump> inputLumps) {
        var wadFileStream = File.Create(wadFilename);
        var writer = new BinaryWriter(wadFileStream);

        /**************
         * WAD header *
         **************/
        writer.Write(PwadMagic);         // Magic
        writer.Write(inputLumps.Count);  // Lump count.
        writer.Write(0);                 // Directory offset (we'll fill this in later).
        
        
        /*************
         * Lump data *
         *************/
        foreach(var lump in inputLumps) {
            lump.StartingByte = (int)wadFileStream.Position;
            
            var lumpStream = File.OpenRead(lump.DataFilename);
            lumpStream.CopyTo(wadFileStream);
            lumpStream.Close();
        }
        
        
        /*****************
         * WAD directory *
         *****************/
        var directoryPosition = (int)wadFileStream.Position;

        foreach(var lump in inputLumps) {
            writer.Write(lump.StartingByte);       // Starting byte.
            writer.Write(lump.Length);             // Lump length.
            writer.Write(lump.GetWadLumpName());  // Lump name.
        }
        
        // Go back and write the directory offset.
        writer.Seek(DirectoryPositionOffset, SeekOrigin.Begin);
        writer.Write(directoryPosition);
        
        
        /****************************************
         * Close 'em up, boys! We're done here. *
         ****************************************/
        writer.Flush();
        writer.Close();
        wadFileStream.Close();
    }
}
