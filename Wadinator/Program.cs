using System.Reflection;
using System.Text;
using System.Text.Json;
using Tomlet;
using Wadinator;
using Wadinator.Configuration;
using Wadinator.Data;

const string playedFilename = "wadinator_played.txt";

static string? GetRandomFile(string path, bool recurse, bool useRngLog, IEnumerable<string> playedFiles) {
    // Scan the directory for WAD files.
    var wadFileList = new List<string>(Directory.GetFiles(path, "*.wad", new EnumerationOptions {
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = recurse
    }));

    // Sanity check: make sure that there are WAD files in that directory.
    if(wadFileList.Count == 0) {
        Print("error: no WAD files found!");
        return null;
    }

    // Read the played WAD list (if that feature is enabled).
    if(useRngLog) {
        // Remove the played WADs from the list.
        for(var index = wadFileList.Count - 1; index >= 0; index--) {
            if(playedFiles.Contains(wadFileList[index], StringComparer.InvariantCultureIgnoreCase)) {
                wadFileList.RemoveAt(index);
            }
        }
    }

    // Sanity check: see if there are any WADs left.
    if(wadFileList.Count == 0) {
        Print("Bro. You've literally played *EVERYTHING*. >:|");
        return null;
    }

    // Pick a random file.
    return wadFileList[Random.Shared.Next(wadFileList.Count)];
}

static string? GetMatchingTextFile(string path, bool dZoneCompat) {
    // If the path isn't a WAD, something's gone wrong.
    if(!path.EndsWith(".wad", StringComparison.InvariantCultureIgnoreCase)) {
        Print("error: this is not a WAD!");
        return null;
    }

    // Replace .wad with .txt for the search pattern.
    var noExt = path.Remove(path.Length - 4);
    path = noExt + ".txt";
    
    // Get the directory name and file name of the potential text file.
    var txtDir = Path.GetDirectoryName(path) ?? "";
    var txtName = Path.GetFileName(path);
    var txtNoExt = Path.GetFileName(noExt);

    // Note that recursive search is only required if searching for things in the context of a
    // D!Zone disk. Since the TXT directory lives in the same directory as all the WADs, the
    // initial search should also recurse and find a text file with a matching filename.
    var searchOpts = new EnumerationOptions {
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = dZoneCompat
    };

    // Attempt to find a matching text file, located in either:
    // - [WADDIR]/[WADNAME].TXT
    // - [WADDIR]/TXT/*/[WADNAME].TXT
    var textFiles = new List<string>(Directory.GetFiles(txtDir, txtName, searchOpts));
    
    // If this also is for D!Zone, look for the text file in the form of [WADDIR]/TXT/[WADNAME]/*.TXT.
    if(dZoneCompat) {
        var dzSpecificDir = Path.Combine(
            Path.GetDirectoryName(path) ?? ".",
            "TXT",
            txtNoExt
        );

        if(Directory.Exists(dzSpecificDir)) {
            var dzResultMine = Directory.GetFiles(
                dzSpecificDir,
                "*.txt",
                searchOpts
            );

            textFiles.AddRange(dzResultMine);
        }
    }
    
    // Return any contender files.
    return textFiles.FirstOrDefault();
}

// Basically just calls Console.WriteLine multiple times. :] This just makes it a little easier
// to format the output text. Please use it across the board for the sake of consistency.
static void Print(params string[] lines) {
    foreach(var line in lines)
        Console.WriteLine(line);
}

// Writes out the Wadinator data file (complete with some sanity checks!).
static void WriteDataFile(string dataFilename, WadinatorData data) {
    var jsonSerializerOptions = new JsonSerializerOptions {
        WriteIndented = true
    };

    var jsonString = JsonSerializer.Serialize(data, typeof(WadinatorData), jsonSerializerOptions);

    if(string.IsNullOrWhiteSpace(jsonString)) {
        Print("Error serializing Wadinator data file!");
        return;
    }
    
    // Make a copy of the last config file if it exists, just in case.
    if(File.Exists(dataFilename)) {
        File.Copy(dataFilename, $"{dataFilename}.last", true);
    }
    
    // Save the file.
    File.WriteAllText(dataFilename, jsonString);
}

// Needed by WadWriter.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Check for a default configuration file, and create one if it doesn't exist.
var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
var configPath = Path.Combine(appDirectory, "Wadinator.toml");

if(!File.Exists(configPath)) {
    var defaultConfig = new WadinatorConfig();
    var defaultConfigToml = TomletMain.DocumentFrom(defaultConfig);
    var defaultConfigFile = File.CreateText(configPath);
    
    defaultConfigFile.WriteLine(defaultConfigToml.SerializedValue);
    defaultConfigFile.Flush();
    defaultConfigFile.Close();
    
    Print(
        "A default configuration has been created called Wadinator.toml.",
        "",
        "Please modify that with your desired settings to use this application."
    );

    return 0;
}

var configToml = TomlParser.ParseFile(configPath);
var config = TomletMain.To<WadinatorConfig>(configToml);

if(!ConfigSanity.Check(config)) {
    return 1;
}

// If the data file cannot be found, migrate the wadinator_played.txt file (if it exists).
if(!File.Exists(config.DataFile)) {
    var migrationData = new WadinatorData();

    if(File.Exists(playedFilename)) {
        Console.WriteLine("Migrating legacy played data...\n");
        migrationData.SelectedWads = File.ReadAllText(playedFilename)
                                         .Split(Environment.NewLine)
                                         .Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Select(x => new SelectedWad { Filename = x, Skipped = false })
                                         .ToList();
    }

    WriteDataFile(config.DataFile, migrationData);
}

// Load the Wadinator data file.
var dataJson = File.ReadAllText(config.DataFile);
WadinatorData? data;

try {
    data = JsonSerializer.Deserialize<WadinatorData>(dataJson);
} catch(JsonException ex) {
    Console.WriteLine("An error occurred while deserializing the Wadinator data file:\n");
    Console.WriteLine(ex.Message);
    return 1;
}

if(data is null) {
    Console.WriteLine("Unable to deserialize the Wadinator data file. Aborting.");
    return 1;
}

// Check arguments.
var helpArg = args.Contains("-help") || args.Contains("-h");
if((args.Length == 0 && string.IsNullOrWhiteSpace(config.DefaultPath)) || helpArg) {
    var startCommand = Environment.CommandLine.Split(' ')[0];
    Print(
        $"usage: {startCommand} [options] <path/file>",
        "",
        "  If a directory is specified, a WAD will be randomly selected and logged to",
        "  ensure that the same file is not picked twice. If a file is selected, the WAD",
        "  file analysis will be performed but no data will be logged. This can be useful",
        "  if you only want the program to estimate the appropriate complevel for a single",
        "  file.",
        "",
        "    -doom           Specifies that the WADs in the directory were designed for",
        "                    Doom/Doom II.",
        "    -(no-)recurse   Scan directory recursively (only valid if a directory is",
        "                    specified). Use 'no-recurse' to explicitly disable this",
        "                    behavior.",
        "    -(no)-log       Enables or disables reading/writing from/to the played file.",
        "    -heretic        Specifies that the WADs in the directory were designed for",
        "                    Heretic.",
        "    -(no-)find-txt  Enables or disables the finding of a WAD's specified text",
        "                    file.",
        "    -(no-)dzone     Enables or disables D!Zone compatibility when searching for",
        "                    text files. This means it will search for either",
        "                    [WADDIR]/TXT/*/[WADNAME].TXT or any such text file located",
        "                    in [WADDIR]/TXT/[WADNAME]/, as well as the directory of the",
        "                    WAD.",
        "    -(no-)music     Enables or disables music WAD generation.",
        ""
    );
    return 0;
}

// Handle command line parameters as lazily as possible.
// TODO: Improve command line parsing. I mean, seriously...just look at this crap. XD
var game = config.UseHeretic ? Game.Heretic : Game.Doom;
var path = config.DefaultPath;
foreach(var arg in args) {
    switch(arg) {
        case "-doom":
            game = Game.Doom;
            break;
        
        case "-log":
            config.LogRandomWadResults = true;
            break;
        
        case "-no-log":
            config.LogRandomWadResults = false;
            break;
        
        case "-no-r":
        case "-no-recurse":
            config.RecurseDirectories = false;
            break;
        
        case "-r":
        case "-recurse":
            config.RecurseDirectories = true;
            break;

        case "-heretic":
            game = Game.Heretic;
            break;
        
        case "-no-ft":
        case "-no-find-txt":
            config.ReadmeTexts.SearchForText = false;
            break;
            
        case "-ft":
        case "-find-txt":
            config.ReadmeTexts.SearchForText = true;
            break;
        
        case "-no-dz":
        case "-no-dzone":
            config.ReadmeTexts.DZoneCompat = false;
            break;
        
        case "-dz":
        case "-dzone":
            config.ReadmeTexts.DZoneCompat = true;
            break;
        
        case "-m":
        case "-music":
            config.MusicRandomizerConfig.GenerateMusicWad = true;
            break;
            
        case "-no-m":
        case "-no-music":
            config.MusicRandomizerConfig.GenerateMusicWad = false;
            break;

        default:
            if(arg.StartsWith("-")) {
                Print("error: unknown parameter");
                return 1;
            }

            if(!string.IsNullOrWhiteSpace(config.DefaultPath) && path != config.DefaultPath) {
                Print(
                    "error: only one path/filename can be specified at a time!",
                    "",
                    "If the path has spaces in it, be sure to surround it in quotes."
                );

                return 1;
            }

            path = arg;
            break;
    }
}

// If we're on Windows, transform the path to ensure that we're using backslashes.
if(OperatingSystem.IsWindows()) {
    path = path.Replace("/", "\\");
}

// Determine if the passed path is a file or directory (or if it doesn't exist at all).
var pathIsDirectory = false;
var originalPath = path;

if(Directory.Exists(path)) {
    pathIsDirectory = true;

    if((path = GetRandomFile(originalPath, config.RecurseDirectories, config.LogRandomWadResults, data.SelectedWads.Select(x => x.Filename))) is null) {
        return 0;
    }
} else if(!File.Exists(path)) {
    // Naughty user!
    Print("error: the specified path does not exist!");
    return 1;
}


/**************************
 * Perform file analysis. *
 **************************/
AnalysisResults analysisResults;

do {
    var wad = new WadReader(path);

    // Configure the analyzer.
    var analysisSettings = new AnalysisSettings {
        DetectDeathmatchWads = config.Analysis.SkipDeathmatchMaps,
        DeathmatchMapEnemyThreshold = config.Analysis.SkipDeathmatchThreshold,
        IsHeretic = game == Game.Heretic
    };

    // Analyze the WAD to fetch the map list, determine the complevel, etc.
    analysisResults = new Analyzer(wad, analysisSettings).AnalyzeWad();

    // Determine whether or not the WAD should be logged.
    var logWad = config.LogRandomWadResults && pathIsDirectory;

    if(analysisResults.IsDeathmatchWad && pathIsDirectory) {
        // Change the behavior somewhat if the WAD is detected as a deathmatch WAD.
        logWad = config.Analysis.RecordSkippedMaps;
        
        // Alert the user that a map was skipped.
        Print($">> Skipped deathmatch map: {path}...");
    }
    
    // Add the WAD to the played file.
    if(logWad) {
        data.SelectedWads.Add(new SelectedWad { Filename = path, Skipped = analysisResults.IsDeathmatchWad });
    }

    if(analysisResults.IsDeathmatchWad && pathIsDirectory) {
        // Pick a new WAD.
        if((path = GetRandomFile(originalPath, config.RecurseDirectories, config.LogRandomWadResults, data.SelectedWads.Select(x => x.Filename))) is null) {
            return 0;
        }
    }
} while(analysisResults.IsDeathmatchWad && pathIsDirectory);


/*****************************
 * Perform text file search. *
 *****************************/

// Attempt to find the .txt file for the WAD if requested, with D!Zone compatibility if requested.
string? wadTxt = null;
if(config.ReadmeTexts.SearchForText) {
    wadTxt = GetMatchingTextFile(path, config.ReadmeTexts.DZoneCompat);
}


/*******************************
 * Perform music WAD creation. *
 *******************************/
var printMusicWadFilename = false;
if(config.MusicRandomizerConfig.GenerateMusicWad) {
    var musicRandomizer = new MusicRandomizer(config.MusicRandomizerConfig);
    data.MusicLumps = musicRandomizer.RefreshMusicList(data.MusicLumps);
    var musicWadGenerationResults = musicRandomizer.GenerateWad(
        musicLumps: data.MusicLumps,
        mapList: analysisResults.MapList.Select(x => x.Name).ToList(),
        existingMusic: analysisResults.MusicList.Select(x => x.Name).ToList()
    );

    printMusicWadFilename = musicWadGenerationResults.Success;
    if(musicWadGenerationResults.Success) {
        data.MusicLumps = musicWadGenerationResults.MusicLumps;
    }
}


/*********************************
 * Save the Wadinator data file. *
 *********************************/
WriteDataFile(config.DataFile, data);


/*******************
 * Report results. *
 *******************/

// Attempt to detect the first map in the WAD and put together a warp string for it.
var warpString = " -warp ";
if(analysisResults.MapList.Any()) {
    var firstMap = analysisResults.MapList.MinBy(x => x.Name)!;

    if(firstMap.Name.StartsWith("MAP")) {
        warpString += firstMap.Name[3..].TrimStart('0');
    } else {
        warpString += $"{firstMap.Name[1]} {firstMap.Name[3]}";
    }
}

// Assemble the WAD list.
var wadList = $"\"{path}\"";

if(printMusicWadFilename) {
    wadList += $" \"{config.MusicRandomizerConfig.MusicWadFilename}\"";
}

// Announce the results.
var exePrefix = OperatingSystem.IsWindows() ? @".\" : "./";

if(pathIsDirectory) {
    Print(
         "",
         "    The RNG gods have made their decision!",
         "",
        $"    ===>  {path}  <===",
         ""
    );
} else {
    Print(
        "",
        $"    ===>  {path}  <===",
        ""
    );
}

Print(
    "",
    "  This WAD contains the following maps:",
    "",
   $"    {string.Join(", ", analysisResults.MapList.OrderBy(lump => lump.Name).Select(lump => lump.Name))}",
    "",
    "",
    "  Here, have a convenient command line:",
    ""
);



if(game == Game.Heretic) {
    // Heretic
    Print(
        $"    {exePrefix}{config.Games.Heretic.ExecutableName} -file {path} -skill 4{warpString}"
    );
} else {
    // Doom or Doom II
    var compLevelText = config.Games.Doom.UsesComplevels ? $" -complevel {(int)analysisResults.CompLevel}" : "";
    Print(
       $"    {exePrefix}{config.Games.Doom.ExecutableName} -iwad {(analysisResults.ContainsMapXxMaps ? "doom2.wad" : "doom.wad")} -file {wadList} -skill 4{compLevelText}{warpString}",
        ""
    );

    if(analysisResults.HasMismatchedBosses && analysisResults.CompLevel <= CompLevel.UltimateDoom) {
        if(config.Games.Doom.UsesComplevels) {
            Print(
                "",
                "  NOTE: This WAD has a cyberdemon and/or spider mastermind on E1M8, as well as sectors",
                "        with the 666 tag value. If this WAD was designed around Ultimate Doom 1.9's",
                "        behavior, this will likely lead to unexpected results. In this case, use complevel",
                "        3. If this is an older WAD that relies on the legacy behavior (such as UAC_DEAD.WAD),",
                "        complevel 2 (as specified above) must be used.",
                ""
            );
        } else {
            Print(
                "",
                "  NOTE: This WAD has a cyberdemon and/or spider mastermind on E1M8, as well as sectors",
                "        with the 666 tag value. If this WAD was designed around Ultimate Doom 1.9's",
                "        behavior, this will likely lead to unexpected results. Please ensure that your",
                "        source port's compatibility settings are configured if this WAD relies on legacy",
                "        behavior.",
                ""
            );
        }
    }

    if(config.Games.Doom.UsesComplevels && config.DisplayComplevelReference) {
        if(analysisResults.CompLevel < CompLevel.Mbf21) {
            Print(
                "",
                $"  The complevel detection isn't perfect. If you run into trouble, try {(analysisResults.CompLevel == CompLevel.Mbf ? "this" : "one of these")} instead:",
                ""
            );
        }

        if(analysisResults.CompLevel < CompLevel.Boom && analysisResults.ContainsExMxMaps) {
            Print(
                "     0 - Doom v1.2",
                "     1 - Doom v1.666");

            if(analysisResults.CompLevel == CompLevel.UltimateDoom) {
                Print(
                    "     2 - Doom v1.9"
                );
            }
            
            if(analysisResults.CompLevel == CompLevel.Doom19) {
                Print(
                    "     3 - Ultimate Doom v1.9"
                );
            }
        }

        if(analysisResults.CompLevel < CompLevel.Boom && analysisResults.ContainsMapXxMaps) {
            Print(
                "     1 - Doom v1.666",
                "     4 - Final Doom"
            );
        }

        if(analysisResults.CompLevel < CompLevel.Boom)
            Print("     9 - Boom");
        if(analysisResults.CompLevel < CompLevel.Mbf)
            Print("    11 - MBF");
        if(analysisResults.CompLevel < CompLevel.Mbf21) {
            Print(
                "    21 - MBF21",
                "",
                "  When in doubt, check the WAD's readme or text file!"
            );
        }
    }
}

// Print if the WAD has a text file.
if(config.ReadmeTexts.SearchForText) {
    Print("");

    if(wadTxt != null) {
        if(config.ReadmeTexts.PrintContents) {
            Print(
                "  The WAD has an associated text file, here is its content:",
                "",
                File.ReadAllText(wadTxt)
            );
        } else {
            Print(
                "  The WAD has an associated text file, here is a command line to view it:",
                "",
               $"    {config.Editor.ExecutableName} {config.Editor.ReadOnlyArg} {wadTxt}"
            );
        }
    } else {
        Print("  The WAD has no associated text file.");
    }
}

// If the path is not a directory and is detected a deathmatch WAD, print a message.
if(!pathIsDirectory && analysisResults.IsDeathmatchWad) {
    Print(
        "",
        "",
        $"  NOTE: This WAD contains fewer enemies than the configured threshold ({config.Analysis.SkipDeathmatchThreshold})."
    );
}

Print("");

return 0;
