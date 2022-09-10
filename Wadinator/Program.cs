using System.Reflection;
using Tomlet;
using Wadinator;
using Wadinator.Configuration;

const string playedFileName = "wadinator_played.txt";

static string? GetRandomFile(string path, bool recurse, bool useRngLog) {
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

    // Read the "played" file (if it exists).
    if(useRngLog && File.Exists(playedFileName)) {
        var playedFiles = new List<string>(File.ReadAllText(playedFileName).Split('\n'));

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

// Basically just calls Console.WriteLine multiple times. :]
// Please use it across the board for the sake of consistency.
static void Print(params string[] lines) {
    foreach(var line in lines)
        Console.WriteLine(line);
}

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

// Check arguments.
if(args.Length == 0 && string.IsNullOrWhiteSpace(config.DefaultPath)) {
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
        "    -doom          Specifies that the WADs in the directory were designed for",
        "                   Doom/Doom II.",
        "    -(no-)recurse  Scan directory recursively (only valid if a directory is",
        "                   specified). Use 'no-recurse' to explicitly disable this",
        "                   behavior.",
        "    -(no)-log      Enables or disables reading/writing from/to the played file.",
        "    -heretic       Specifies that the WADs in the directory were designed for",
        "                   Heretic.",
        ""
    );
    return 1;
}

// Handle command line parameters as lazily as possible.
var recurse = config.DefaultRecurse;
var game = Game.Unknown;
var logRngResults = config.LogRandomWadResults;
var path = config.DefaultPath;
foreach(var arg in args) {
    switch(arg) {
        case "-doom":
            game = Game.Doom;
            break;
        
        case "-log":
            logRngResults = true;
            break;
        
        case "-no-log":
            logRngResults = false;
            break;
        
        case "-no-r":
        case "-no-recurse":
            recurse = false;
            break;
        
        case "-r":
        case "-recurse":
            recurse = true;
            break;

        case "-heretic":
            game = Game.Heretic;
            break;

        default:
            if(arg.StartsWith("-")) {
                Print("error: unknown parameter");
                return 1;
            }

            if(!string.IsNullOrWhiteSpace(path) && path != config.DefaultPath) {
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

// Determine if the passed path is a file or directory (or if it doesn't exist at all).
var pathIsDirectory = false;

if(Directory.Exists(path)) {
    pathIsDirectory = true;
    path = GetRandomFile(path, recurse, logRngResults);

    if(path is null) {
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
var wad = new WadReader(path);

// Try to figure out if this is Doom 1 or Doom 2 based on the map list.
var analysisResults = new Analyzer(wad).AnalyzeWad();

// Add the WAD to the played file.
if(logRngResults && pathIsDirectory) {
    var playedFileStream = File.AppendText(playedFileName);
    playedFileStream.WriteLine(path);
    playedFileStream.Flush();
    playedFileStream.Close();
}

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
       $"    {exePrefix}{config.Games.Doom.ExecutableName} -iwad {(analysisResults.ContainsMapXxMaps ? "doom2.wad" : "doom.wad")} -file {path} -skill 4{compLevelText}{warpString}",
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

    if(config.Games.Doom.UsesComplevels) {
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
                "  When in doubt, check the WAD's readme!"
            );
        }
    }
}

Print("");

return 0;
