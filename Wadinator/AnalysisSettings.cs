namespace Wadinator; 

/// <summary>
/// Contains settings to configure the WAD analysis engine.
/// </summary>
public class AnalysisSettings {
    /// <summary>
    /// If this is set to <c>true</c>, the analyzer will attempt to detect deathmatch-only WADs. 
    /// </summary>
    public bool DetectDeathmatchWads { get; set; } = false;

    /// <summary>
    /// If deathmatch WAD detection is enabled, a WAD will be considered deathmatch-only if there are this,
    /// or fewer, enemies.
    /// </summary>
    public int DeathmatchMapEnemyThreshold { get; set; } = 0;

    /// <summary>
    /// <c>true</c> if the target WAD is a Heretic WAD, otherwise <c>false</c>.
    /// </summary>
    public bool IsHeretic { get; set; } = false;
}
