using Wadinator.Configuration;

namespace Wadinator; 

/// <summary>
/// Performs configuration sanity checks.
/// </summary>
public static class ConfigSanity {
    /// <summary>
    /// Checks the sanity of the Wadinator config file.
    /// </summary>
    /// <param name="config">A <see cref="WadinatorConfig"/> instance.</param>
    /// <returns><c>true</c> if the config file looks fine, or <c>false</c> if an error has been detected.</returns>
    public static bool Check(WadinatorConfig config) {
        var success = true;
        
        if(!string.IsNullOrWhiteSpace(config.DefaultPath) && !Directory.Exists(config.DefaultPath) && !File.Exists(config.DefaultPath)) {
            Console.WriteLine("[!!] default-path is set to a non-existant path");
            success = false;
        }

        if(config.MusicRandomizerConfig.GenerateMusicWad && !Directory.Exists(config.MusicRandomizerConfig.SourceLumpPath)) {
            Console.WriteLine("[!!] music-randomizer/source-lump-path is set to a non-existant path");
            success = false;
        }
        
        return success;
    }
}
