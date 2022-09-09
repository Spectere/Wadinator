namespace Wadinator.Exceptions; 

/// <summary>
/// Thrown when a lump cannot be found in a WAD.
/// </summary>
public class LumpNotFoundException : Exception {
    public LumpNotFoundException() {}
    public LumpNotFoundException(string lumpName) : base($"'{lumpName}' could not be found!") {}
    public LumpNotFoundException(string lumpName, Exception inner) : base($"'{lumpName}' could not be found!", inner) {}
}

