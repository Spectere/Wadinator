using System.Security.Cryptography;
using System.Text.Json;
using Wadinator.Configuration;
using Wadinator.Data;

namespace Wadinator; 

/// <summary>
/// Randomly picks music for each map.
/// </summary>
public class MusicRandomizer {
    private readonly MusicRandomizerConfig _config;

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
    public MusicRandomizer(MusicRandomizerConfig configuration) {
        _config = configuration;
    }

    /// <summary>
    /// Prints a message only if the <paramref name="condition"/> variable is <c>true</c>.
    /// </summary>
    /// <param name="condition"><paramref name="message"/> will only be printed if this field is <c>true</c>.</param>
    /// <param name="message">The message that might be printed.</param>
    /// <param name="newLine">If <c>true</c>, a newline character will be appended to this message. Defaults to <c>true</c>.</param>
    private static void ConditionalPrint(bool condition, string message, bool newLine = true) {
        if(!condition) {
            return;
        }

        if(newLine) {
            Console.WriteLine(message);
        } else {
            Console.Write(message);
        }
    }

    /// <summary>
    /// Generates a music WAD.
    /// </summary>
    /// <param name="musicLumps">A list of eligible music lumps.</param>
    /// <param name="mapList">All of the maps contained in the WAD file.</param>
    /// <param name="existingMusic">All of the music (D_*) lumps in the WAD file. This will be ignored if "ReplaceExistingMusic"
    /// is enabled.</param>
    /// <param name="allowCopyrightedSongs">If this is set to <c>true</c>, songs marked with the copyright flag will
    /// be included from the list of potential candidates.</param> 
    /// <returns>The results of the generation process.</returns>
    public MusicWadGenerationResults GenerateWad(List<MusicLump> musicLumps, List<string> mapList, List<string> existingMusic, bool allowCopyrightedSongs) {
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
        var filteredLumps = musicLumps;
        if (!allowCopyrightedSongs) {
            filteredLumps = filteredLumps.Where(x => x.Copyright != true).ToList();
        }
        
        var candidates = filteredLumps.Where(x => x.Exists)
                                   .Select(x => new MusicStats {
                                       Sha1 = x.Sha1,
                                       SelectionCount = x.SelectionCount,
                                       Weight = 100
                                   })
                                   .ToList();
        
        // 2. If no selections have been made, leave it at that.
        var totalSelections = filteredLumps.Sum(x => x.SelectionCount);
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
        
        // Go through each map lump and select a track. Make sure to pick again if we come across a duplicate, or if the
        // file was deleted from the music store.
        var selections = new List<Selection>();
        foreach(var lump in musicLumpsToFill) {
            string? hash;
            do {
                hash = weightedRandom.Select();
                
                // Ensure that the file exists on disk.
                if(hash is not null) {
                    var hashIndex = hash[..2];
                    if(!File.Exists(Path.Combine(_config.SourceLumpPath, hashIndex, hash))) {
                        // File does *not* exist. Reset the hash variable and force another loop.
                        results.MusicLumps.First(x => x.Sha1 == hash).Exists = false;
                        hash = "";
                    }
                }
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

            var hashIndex = selection.Hash[..2];
            var filename = Path.Combine(_config.SourceLumpPath, hashIndex, selection.Hash);
            
            newWadLumps.Add(new WadWriterLump(selection.Lump, filename));
            
            if(selection.Lump == _musicIntermission[0]) {
                // If this is an intermission track, make sure to replace the Doom II music as well.
                newWadLumps.Add(new WadWriterLump(_musicIntermission[1], filename));
                results.SelectedLumps.Add("Intermission", musicLump);
            } else {
                // If not, just report it as a replaced lump.
                
                // Do a reverse lookup to get the map name. Yeah, it's silly. :)
                var mapName = _musicMap.First(x => x.Value == selection.Lump).Key;
                results.SelectedLumps.Add(mapName, musicLump);
            }
        }
        
        // Write the WAD and return the updated counts.
        WadWriter.Create(_config.MusicWadFilename, newWadLumps);

        results.MusicLumps = musicLumps;
        return results;
    }

    /// <summary>
    /// Imports music files into the Wadinator music collection.
    /// </summary>
    /// <param name="config">A valid <see cref="WadinatorConfig"/> object.</param>
    /// <param name="path">A directory or file to import.</param>
    /// <param name="verbose">If <c>true</c>, additional output will be printed.</param>
    /// <returns>A list of <see cref="MusicLump"/> objects to be merged into the data file, or <c>null</c> if an error
    /// occurred.</returns>
    public static IEnumerable<MusicLump>? ImportMusic(WadinatorConfig config, string path, bool verbose) {
        var newMusicLumps = new List<MusicLump>();
        var pathIsDirectory = Directory.Exists(path);
        
        // Quick sanity check to make sure that the path is actually valid.
        if(!pathIsDirectory && !File.Exists(path)) {
            Console.WriteLine($"[!!] error: {path} does not exist!");
            return null;
        }

        if(pathIsDirectory) {
            newMusicLumps.AddRange(ProcessDirectory(path, config.MusicRandomizerConfig.SourceLumpPath, config.RecurseDirectories, verbose));
        } else {
            ConditionalPrint(verbose, "Searching directory for manifest...", false);
            var manifest = ReadManifest(Path.GetDirectoryName(path) ?? "");
            ConditionalPrint(verbose, manifest is null
                ? "NOT FOUND"
                : "FOUND!"
            );

            ConditionalPrint(verbose, $"Reading music lump...\n    |- {path}: ", false);
            var musicLump = ProcessFile(path, config.MusicRandomizerConfig.SourceLumpPath, manifest, verbose);
            
            if(musicLump is not null) {
                newMusicLumps.Add(musicLump);
            }
        }
        
        return newMusicLumps;
    }

    /// <summary>
    /// Processes music lumps within a given directory.
    /// </summary>
    /// <param name="path">The directory to process.</param>
    /// <param name="storePath">The location on disk where music lumps are stored.</param>
    /// <param name="recurse">If <c>true</c>, all subdirectories within this path will also be processed.</param>
    /// <param name="verbose">If <c>true</c>, additional output will be printed.</param>
    /// <returns>A list of <see cref="MusicLump"/> objects.</returns>
    private static IEnumerable<MusicLump> ProcessDirectory(string path, string storePath, bool recurse, bool verbose) {
        var newMusicLumps = new List<MusicLump>();
        var manifestCollection = new Dictionary<string, MusicManifest>();
        var fileList = Directory.GetFiles(path, "*", new EnumerationOptions { RecurseSubdirectories = recurse });
        
        // Read all of the manifest files in the file list.
        ConditionalPrint(verbose, "Reading manifest files...");
        var manifestFiles = fileList.Where(x => x.ToLower().EndsWith(".json"));
        foreach(var manifestFile in manifestFiles) {
            ConditionalPrint(verbose, $"    |- {manifestFile}: ", false);
            var manifest = ReadManifest(manifestFile);
            if(manifest is null) {
                ConditionalPrint(verbose, "not a manifest");
                continue;
            }
            
            // Use the base path as a dictionary key to give it some commonality with the music lumps.
            ConditionalPrint(verbose, "OK!");
            manifestCollection.Add(
                Path.GetDirectoryName(manifestFile) ?? "", 
                manifest
            );
        }
        
        // Process the remaining files that were found.
        ConditionalPrint(verbose, "\nReading music lumps...");
        var musicLumpFiles = fileList.Where(x => !x.ToLower().EndsWith(".json"));
        foreach(var musicLumpFile in musicLumpFiles) {
            ConditionalPrint(verbose, $"    |- {musicLumpFile}: ", false);
            
            // Look for a matching manifest and process the file.
            manifestCollection.TryGetValue(Path.GetDirectoryName(musicLumpFile) ?? "", out var musicManifest);
            var musicLump = ProcessFile(musicLumpFile, storePath, musicManifest, verbose);
            if(musicLump is null) {
                continue;
            }

            newMusicLumps.Add(musicLump);
        }

        return newMusicLumps;
    }

    /// <summary>
    /// Processes a single music lump.
    /// </summary>
    /// <param name="path">The file to process.</param>
    /// <param name="storePath">The location on disk where music lumps are stored.</param>
    /// <param name="manifest">A <see cref="MusicManifest"/> containing metadata for this (and other) files. This
    /// can be <c>null</c>.</param>
    /// <param name="verbose">If <c>true</c>, additional output will be printed.</param>
    /// <returns>A <see cref="MusicLump"/> representing the file, or <c>null</c> if the file is invalid or missing.</returns>
    private static MusicLump? ProcessFile(string path, string storePath, MusicManifest? manifest, bool verbose) {
        // Quick sanity check.
        if(!File.Exists(path)) {
            ConditionalPrint(verbose, "NOT FOUND");
            return null;
        }
        
        // Calculate the SHA1 sum of the file.
        using var sha1 = SHA1.Create();
        using var fileStream = File.OpenRead(path);
        var fileHash = BitConverter.ToString(sha1.ComputeHash(fileStream))
                                   .Replace("-", "")
                                   .ToLower();
        
        var musicLump = new MusicLump {
            Sha1 = fileHash,
            SelectionCount = 0,
            Exists = true
        };

        // Look up the music info from the manifest file, if it exists.
        if(manifest?.Entries is not null) {
            manifest.Entries.TryGetValue(Path.GetFileName(path), out var manifestEntry);

            if(manifestEntry is null) {
                // Try it without the file extension.
                manifest.Entries.TryGetValue(Path.GetFileNameWithoutExtension(path), out manifestEntry);
            }

            if(manifestEntry is not null) {
                musicLump.Title = manifestEntry.Title ?? musicLump.Title;
                musicLump.Artist = manifestEntry.Artist ?? musicLump.Artist;
                musicLump.Sequencer = manifestEntry.Sequencer ?? musicLump.Sequencer;
                musicLump.Copyright = manifestEntry.Copyright ?? musicLump.Copyright ?? false;
            }
        }
        
        // If no title could be found, use the filename.
        musicLump.Title ??= Path.GetFileName(path);
        
        // Create a storage directory and copy the lump into it.
        var hashIndex = fileHash[..2];
        var finalPath = Path.Combine(storePath, hashIndex);
        Directory.CreateDirectory(finalPath);
        File.Copy(path, Path.Combine(finalPath, fileHash), true);

        // OK! :D
        ConditionalPrint(verbose, musicLump.Sha1);
        return musicLump;
    }

    /// <summary>
    /// Checks a given directory for a manifest file.
    /// </summary>
    /// <param name="path">The directory to check</param>
    /// <returns>A populated <see cref="MusicManifest"/> object.</returns>
    private static MusicManifest? ReadManifest(string path) {
        const string manifestFilename = "manifest.json";
        
        // Make sure the file actually exists.
        if(!Directory.Exists(path) && !File.Exists(path)) {
            return null;
        }

        // If it's a directory, attempt to read the default filename. If it's a file, just use the passed name. 
        if(Directory.Exists(path)) {
            path = string.IsNullOrWhiteSpace(path)
                ? manifestFilename
                : Path.Combine(path, manifestFilename);
        }

        var dataJson = File.ReadAllText(path);
        MusicManifest? musicManifest;
        try {
            musicManifest = JsonSerializer.Deserialize<MusicManifest>(dataJson);
        } catch(JsonException ex) {
            Console.WriteLine($"[!!] error deserializing {path}: {ex.Message}");
            return null;
        }

        return musicManifest;
    }
}
