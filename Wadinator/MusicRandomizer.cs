using System.Security.Cryptography;
using Wadinator.Data;

namespace Wadinator; 

/// <summary>
/// Randomly picks music for each map.
/// </summary>
public class MusicRandomizer {
    private readonly Configuration.MusicRandomizerConfig _config;

    private readonly List<string> _musicIntermission = new() {
        "D_INTER",  // Ultimate Doom
        "D_DM2INT"  // Doom II / Final Doom
    };

    // Key: Map lump
    // Value: Music lump
    private readonly Dictionary<string, string> _musicMap = new() {
        // Ultimate Doom
        { "E1M1", "D_E1M1" },
        { "E1M2", "D_E1M2" },
        { "E1M3", "D_E1M3" },
        { "E1M4", "D_E1M4" },
        { "E1M5", "D_E1M5" },
        { "E1M6", "D_E1M6" },
        { "E1M7", "D_E1M7" },
        { "E1M8", "D_E1M8" },
        { "E1M9", "D_E1M9" },
        
        { "E2M1", "D_E2M1" },
        { "E2M2", "D_E2M2" },
        { "E2M3", "D_E2M3" },
        { "E2M4", "D_E2M4" },
        { "E2M5", "D_E2M5" },
        { "E2M6", "D_E2M6" },
        { "E2M7", "D_E2M7" },
        { "E2M8", "D_E2M8" },
        { "E2M9", "D_E2M9" },

        { "E3M1", "D_E3M1" },
        { "E3M2", "D_E3M2" },
        { "E3M3", "D_E3M3" },
        { "E3M4", "D_E3M4" },
        { "E3M5", "D_E3M5" },
        { "E3M6", "D_E3M6" },
        { "E3M7", "D_E3M7" },
        { "E3M8", "D_E3M8" },
        { "E3M9", "D_E3M9" },

        { "E4M1", "D_E3M4" },
        { "E4M2", "D_E3M2" },
        { "E4M3", "D_E3M3" },
        { "E4M4", "D_E1M5" },
        { "E4M5", "D_E2M7" },
        { "E4M6", "D_E2M4" },
        { "E4M7", "D_E2M6" },
        { "E4M8", "D_E2M5" },
        { "E4M9", "D_E1M9" },

        // Doom II / Final Doom
        { "MAP01", "D_RUNNIN" },
        { "MAP02", "D_STALKS" },
        { "MAP03", "D_COUNTD" },
        { "MAP04", "D_BETWEE" },
        { "MAP05", "D_DOOM" },
        { "MAP06", "D_THE_DA" },
        { "MAP07", "D_SHAWN" },
        { "MAP08", "D_DDTBLU" },
        { "MAP09", "D_IN_CIT" },
        { "MAP10", "D_DEAD" },

        { "MAP11", "D_STLKS2" },
        { "MAP12", "D_THEDA2" },
        { "MAP13", "D_DOOM2" },
        { "MAP14", "D_DDTBL2" },
        { "MAP15", "D_RUNNI2" },
        { "MAP16", "D_DEAD2" },
        { "MAP17", "D_STLKS3" },
        { "MAP18", "D_ROMERO" },
        { "MAP19", "D_SHAWN2" },
        { "MAP20", "D_MESSAG" },
        
        { "MAP21", "D_COUNT2" },
        { "MAP22", "D_DDTBL3" },
        { "MAP23", "D_AMPIE" },
        { "MAP24", "D_THEDA3" },
        { "MAP25", "D_ADRIAN" },
        { "MAP26", "D_MESSG2" },
        { "MAP27", "D_ROMER2" },
        { "MAP28", "D_TENSE" },
        { "MAP29", "D_SHAWN3" },
        { "MAP30", "D_OPENIN" },

        { "MAP31", "D_EVIL" },
        { "MAP32", "D_ULTIMA" }
    };

    /// <summary>
    /// Keeps track of some helpful statistics we use to figure out how to weight each song.
    /// </summary>
    private class MusicStats {
        /// <summary>
        /// The hash representing the candidate track.
        /// </summary>
        public string Sha1 = "";

        /// <summary>
        /// The number of times this track was picked.
        /// </summary>
        public int SelectionCount;

        /// <summary>
        /// A percentage representing how many times this track was picked.
        /// </summary>
        public double Percentage;

        /// <summary>
        /// This candidate's weight in the selection process.
        /// </summary>
        public int Weight;
    }

    /// <summary>
    /// Tracks the final selections.
    /// </summary>
    private class Selection {
        /// <summary>
        /// The lump to be populated.
        /// </summary>
        public string Lump = "";

        /// <summary>
        /// The hash of the song to use.
        /// </summary>
        public string Hash = "";
    }

    /// <summary>
    /// Initializes the music randomizer.
    /// </summary>
    /// <param name="configuration">The configuration for the randomizer.</param>
    public MusicRandomizer(Configuration.MusicRandomizerConfig configuration) {
        _config = configuration;
    }

    /// <summary>
    /// Generates a music WAD.
    /// </summary>
    /// <param name="musicLumps">A list of eligible music lumps.</param>
    /// <param name="mapList">All of the maps contained in the WAD file.</param>
    /// <param name="existingMusic">All of the music (D_*) lumps in the WAD file. This will be ignored if "ReplaceExistingMusic"
    /// is enabled.</param>
    /// <returns>The results of the generation process.</returns>
    public MusicWadGenerationResults GenerateWad(List<MusicLump> musicLumps, List<string> mapList, List<string> existingMusic) {
        var results = new MusicWadGenerationResults {
            Success = true,
            MusicLumps = musicLumps
        };
        
        // Figure out which lumps we need to populate.
        var musicLumpsToFill = new List<string>();
        foreach(var map in mapList) {
            if(!_musicMap.ContainsKey(map) || musicLumpsToFill.Contains(_musicMap[map])) {
                continue;
            }

            musicLumpsToFill.Add(_musicMap[map]);
        }

        if(_config.ReplaceIntermissionMusic) {
            // Add the intermission music to the mix.
            musicLumpsToFill.Add(_musicIntermission[0]);  // Only add one--we'll take care of the other one later.
        }
        
        // Remove existing ones (if ReplaceExistingMusic is disabled).
        if(!_config.ReplaceExistingMusic) {
            existingMusic.ForEach(x => musicLumpsToFill.Remove(x));

            if(_config.ReplaceIntermissionMusic && existingMusic.Contains(_musicIntermission[0])) {
                // Or perhaps we won't. :)
                musicLumpsToFill.Remove(_musicIntermission[0]);
            }
        }
        
        // Do a few quick checks before we begin.
        if(musicLumpsToFill.Count == 0) {
            results.Success = false;
            return results;
        }
        
        if(musicLumps.Count == 0) {
            Console.WriteLine("warning: No valid music candidates found!");
            
            results.Success = false;
            return results;
        }

        if(musicLumps.Count < musicLumpsToFill.Count + (_config.ReplaceIntermissionMusic ? 1 : 0)) {
            Console.WriteLine("warning: Not enough music candidates to fulfill the selected WAD.");
            
            results.Success = false;
            return results;
        }
        
        /*
         * So here's how we're gonna do it (reminder: update this if this thing changes!):
         *
         *     1. Start everything with a weight of 100.
         *     2. If no selections have been made, leave it at that.
         *     3. Determine the percentage of plays a particular song has.
         *         a. If a particular song has *never* been played, add 25 to the weight.
         *         b. Otherwise, reduce the weight by its percentage.
         *     4. Clamp the values to (20, 125).
         *
         * I'm sure this is statistically fair (insert a dozen rofl emojis here).
         */
        
        // 1. Start everything with a weight of 100.
        var candidates = musicLumps.Where(x => x.Filenames.Count > 0)
                                   .Select(x => new MusicStats {
                                       Sha1 = x.Sha1,
                                       SelectionCount = x.SelectionCount,
                                       Weight = 100
                                   }).ToList();
        
        // 2. If no selections have been made, leave it at that.
        var totalSelections = musicLumps.Sum(x => x.SelectionCount);
        if(totalSelections > 0) {
            // 3. Determine the percentage of plays a particular song has.
            
            foreach(var candidate in candidates) {
                if(candidate.SelectionCount == 0) {
                    // 3a. If a particular song has *never* been played, add 25 to the weight.
                    candidate.Weight += 25;
                } else {
                    // 3b. Otherwise, reduce the weight by its percentage.
                    candidate.Percentage = (double)candidate.SelectionCount / totalSelections;
                    candidate.Weight -= (int)(candidate.Percentage * 100);
                }
            }
        }
        
        // Now, throw it in the blender and see what we get.
        var weightedRandom = new WeightedRandom<string>();
        foreach(var candidate in candidates) {
            // Before we go any further: 4. Clamp the values to (20, 125).
            candidate.Weight = Math.Clamp(candidate.Weight, 20, 125);

            weightedRandom.Add(candidate.Sha1, candidate.Weight);
        }
        
        // Go through each map lump and select a track. Make sure to pick again if we come across a duplicate.
        var selections = new List<Selection>();
        foreach(var lump in musicLumpsToFill) {
            string? hash;
            do {
                hash = weightedRandom.Select();
            } while(string.IsNullOrWhiteSpace(hash) || selections.Any(x => x.Hash == hash));

            selections.Add(new Selection { Lump = lump, Hash = hash });
        }
        
        // Piece together the new WAD, and update the lump selection counts if applicable.
        var newWadLumps = new List<WadWriterLump>();
        foreach(var selection in selections) {
            var musicLump = musicLumps.First(x => x.Sha1 == selection.Hash);

            if(!_musicIntermission.Contains(selection.Lump) || _config.TrackIntermissionMusicUsage) {
                musicLump.SelectionCount++;
            }
            
            newWadLumps.Add(new WadWriterLump(selection.Lump, musicLump.Filenames.First()));
            
            // If this is an intermission track, make sure to replace the Doom II music as well.
            if(selection.Lump == _musicIntermission[0]) {
                newWadLumps.Add(new WadWriterLump(_musicIntermission[1], musicLump.Filenames.First()));
            }
        }
        
        // Write the WAD and return the updated counts.
        WadWriter.Create(_config.MusicWadFilename, newWadLumps);

        results.MusicLumps = musicLumps;
        return results;
    }

    /// <summary>
    /// Scans the music lumps in the target directory and updates the Wadinator data store.
    /// </summary>
    /// <param name="musicLumps">The list of existing music lumps. This list will be manipulated
    /// and returned by this method.</param>
    /// <returns>An updated list of music lumps.</returns>
    public List<MusicLump> RefreshMusicList(List<MusicLump> musicLumps) {
        // Clear out all existing filenames from the old music lumps.
        foreach(var lump in musicLumps) {
            lump.Filenames.Clear();
        }
        
        // Scan the target directory. Filter out all dot-prefixed files to make sure we
        // don't get any silly Finder metadata.
        var fileList = Directory.GetFiles(_config.SourceLumpPath, "*", SearchOption.AllDirectories)
                                .Where(x => !Path.GetFileName(x).StartsWith("."));

        // Go through each file, figure out its SHA1 hash, and slot it into the final music lump list.
        var sha1 = SHA1.Create();
        foreach(var file in fileList) {
            var fileStream = File.OpenRead(file);
            var fileHash = BitConverter.ToString(sha1.ComputeHash(fileStream))
                                       .Replace("-", "")
                                       .ToLower();

            var lump = musicLumps.FirstOrDefault(x => x.Sha1 == fileHash);
            if(lump is null) {
                // This music is new. Create a new record.
                musicLumps.Add(new MusicLump {
                    Filenames = new List<string> { file },
                    SelectionCount = 0,
                    Sha1 = fileHash
                });
            } else {
                // This music is old. Push this filename into the existing slot.
                lump.Filenames.Add(file);
            }

            fileStream.Close();
        }

        return musicLumps;
    }
}
