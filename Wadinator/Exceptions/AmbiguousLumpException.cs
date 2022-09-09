namespace Wadinator.Exceptions; 

/// <summary>
/// Thrown when multiple identically named lumps are found.
/// </summary>
public class AmbiguousLumpException : Exception {
    public AmbiguousLumpException() {}
    public AmbiguousLumpException(string lumpName) : base($"Multiple instances of '{lumpName}' found!") {}
    public AmbiguousLumpException(string lumpName, Exception inner) : base($"Multiple instances of '{lumpName}' found!", inner) {}
}
