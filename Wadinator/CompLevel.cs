namespace Wadinator;

public enum CompLevel {
    Doom12 = 0,
    Doom1666 = 1,
    Doom19 = 2,
    UltimateDoom = 3,
    FinalDoom = 4,
    Boom = 9,
    Mbf = 11,
    Mbf21 = 21
}

public static class ComplevelExtension {
    public static CompLevel Promote(this CompLevel currentLevel, CompLevel targetLevel) =>
        targetLevel > currentLevel ? targetLevel : currentLevel;


    public static CompLevel Promote(this CompLevel currentLevel, CompLevel targetLevel, bool condition) =>
        condition && targetLevel > currentLevel ? targetLevel : currentLevel;
}
