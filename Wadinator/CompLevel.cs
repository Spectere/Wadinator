namespace Wadinator;

/// <summary>
/// Represents a PrBoom complevel.
/// </summary>
public enum CompLevel {
    /// <summary>
    /// Doom 1.2 compatibility.
    /// </summary>
    Doom12 = 0,
    
    /// <summary>
    /// Doom and Doom II 1.666 compatibility.
    /// </summary>
    Doom1666 = 1,
    
    /// <summary>
    /// Doom and Doom II 1.9 compatibility.
    /// </summary>
    Doom19 = 2,
    
    /// <summary>
    /// Ultimate Doom 1.9 compatibility.
    /// </summary>
    UltimateDoom = 3,
    
    /// <summary>
    /// Final Doom compatibility.
    /// </summary>
    FinalDoom = 4,
    
    /// <summary>
    /// Boom 2.05 compatibility.
    /// </summary>
    Boom = 9,
    
    /// <summary>
    /// MBF compatibility.
    /// </summary>
    Mbf = 11,
    
    /// <summary>
    /// MBF21 compatibility.
    /// </summary>
    Mbf21 = 21
}

public static class CompLevelExtension {
    /// <summary>
    /// Unconditionally promotes a complevel to a greater one.
    /// </summary>
    /// <param name="currentLevel">The current complevel.</param>
    /// <param name="targetLevel">The target complevel.</param>
    /// <returns>The promoted complevel.</returns>
    public static CompLevel Promote(this CompLevel currentLevel, CompLevel targetLevel) =>
        targetLevel > currentLevel ? targetLevel : currentLevel;
    
    /// <summary>
    /// Promotes the complevel if a condition returns <c>true</c>.
    /// </summary>
    /// <param name="currentLevel">The current complevel.</param>
    /// <param name="targetLevel">The target complevel.</param>
    /// <param name="condition">If <c>true</c>, the complevel is promoted. Otherwise, it remains the same.</param>
    /// <returns>The resulting complevel.</returns>
    public static CompLevel Promote(this CompLevel currentLevel, CompLevel targetLevel, bool condition) =>
        condition && targetLevel > currentLevel ? targetLevel : currentLevel;
}
