using System.Text.RegularExpressions;
using Wadinator;

const string playedFileName = "wadinator_played.txt";

static (CompLevel, bool) DetectCompLevel(ref WadReader wad, ref List<WadReader.WadDirectoryEntry> mapList, bool hasDoom2Maps) {
    // Perform a linedef/sector analysis to guess the correct complevel.
    var compLevel = CompLevel.Doom19;
    foreach(var lump in wad.Lumps) {
        // Indiscriminately check all supported lumps.
        var resultComplevel = lump.Name switch {
            "DEHACKED" => Lumpalyzer.AnalyzeDeHackEd(wad.GetLump(lump)),
            "LINEDEFS" => Lumpalyzer.AnalyzeLinedefs(wad.GetLump(lump)),
            "SECTORS" => Lumpalyzer.AnalyzeSectors(wad.GetLump(lump)),
            "THINGS" => Lumpalyzer.AnalyzeThings(wad.GetLump(lump)),
            _ => compLevel
        };

        compLevel = compLevel.Promote(resultComplevel);
    }

    /*
     * Perform any necessary complevel adjustments.
     */
    var hasMismatchedBosses = false;
    if(compLevel < CompLevel.Boom && !hasDoom2Maps) {
        // If Ultimate Doom maps are detected, we already have a good idea which
        // complevel this should be.
        var ultimateDoomMapRegEx = new Regex("^(E4M.)$");
        if(mapList.Any(map => ultimateDoomMapRegEx.IsMatch(map.Name))) {
            compLevel = CompLevel.UltimateDoom;
            return (compLevel, hasMismatchedBosses);
        }

        /*
         * Check for mismatched boss enemies (Cyberdemon/Mastermind on E1M8).
         */
        if(mapList.Any(lump => lump.Name == "E1M8")) {
            var mapLumps = GetMapLumps(wad, "E1M8");
            var sectorLump = mapLumps.FirstOrDefault(x => x.Name == "SECTORS");
            var thingLump = mapLumps.FirstOrDefault(x => x.Name == "THINGS");

            if(sectorLump is not null && thingLump is not null) {
                var sectorStream = wad.GetLump(sectorLump);
                var thingStream = wad.GetLump(thingLump);

                hasMismatchedBosses = Lumpalyzer.HasMismatchedBossEncounter("E1M8", sectorStream, thingStream);
            }
        }

        compLevel = compLevel.Promote(CompLevel.UltimateDoom, !hasMismatchedBosses);
    }

    return (compLevel, hasMismatchedBosses);
}

static List<WadReader.WadDirectoryEntry> GetMapLumps(in WadReader wad, string mapName) {
    // NOTE: If we need to fetch more items, expand this list accordingly.
    var candidates = new List<string> {
        "LINEDEFS",
        "SECTORS",
        "THINGS"
    };

    var mapFound = false;
    var mapLumps = new List<WadReader.WadDirectoryEntry>();
    foreach(var lump in wad.Lumps) {
        if(!mapFound) {
            if(lump.Name != mapName) continue;

            mapFound = true;
            continue;
        }

        // Check for candidates and add them to the list.
        if(candidates.Contains(lump.Name))
            mapLumps.Add(lump);

        // If we run into a marker lump, we've gone too far.
        if(lump.Size == 0) break;
    }

    return mapLumps;
}

static string? GetRandomFile(string path, bool recurse) {
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
    if(File.Exists(playedFileName)) {
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

if(args.Length < 1 || args.Length > 2) {
    var startCommand = Environment.CommandLine.Split(' ')[0];
    Print($"usage: {startCommand} [-r] [-heretic] <path/file>",
        "",
        "  If a directory is specified, a WAD will be randomly selected and logged to",
        "  ensure that the same file is not picked twice. If a file is selected, the WAD",
        "  file analysis will be performed but no data will be logged. This can be useful",
        "  if you only want the program to estimate the appropriate complevel for a single",
        "  file.",
        "",
        "    -r        Scan directory recursively (only valid if a directory is specified).",
        "    -heretic  Specifies that the WADs in the directory were designed for Heretic.",
        ""
    );
    return 1;
}

// Handle command line parameters as lazily as possible.
var basePathIndex = 0;
var recurse = false;
var heretic = false;
var path = "";
foreach(var arg in args) {
    switch(arg) {
        case "-r":
            recurse = true;
            break;

        case "-heretic":
            heretic = true;
            break;

        default:
            if(arg.StartsWith("-")) {
                Print("error: unknown parameter");
                return 1;
            }

            if(!string.IsNullOrWhiteSpace(path)) {
                Print(
                    "error: only one path/filename can be specified at a time!",
                    "",
                    "If the path has spaces in it, be sure to surround it in quotes.");

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
    path = GetRandomFile(path, recurse);

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
var allMapsRegEx = new Regex("^(E.M.|MAP..)$");
var doom1MapRegEx = new Regex("^(E.M.)$");

var mapList = wad.Lumps.Where(x => allMapsRegEx.IsMatch(x.Name)).ToList();
var hasDoom1Maps = mapList.Any(x => doom1MapRegEx.IsMatch(x.Name));
var hasDoom2Maps = mapList.Any(x => x.Name.StartsWith("MAP"));

var compLevel = CompLevel.Doom19;
var hasMismatchedBosses = false;

if(!heretic) {
    (compLevel, hasMismatchedBosses) = DetectCompLevel(ref wad, ref mapList, hasDoom2Maps);
}

// Add the WAD to the played file.
if(pathIsDirectory) {
    var playedFileStream = File.AppendText(playedFileName);
    playedFileStream.WriteLine(path);
    playedFileStream.Flush();
    playedFileStream.Close();
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
         "");
}

Print(
    "",
    "  This WAD contains the following maps:",
    "",
   $"    {string.Join(", ", mapList.OrderBy(lump => lump.Name).Select(lump => lump.Name))}",
    "",
    "",
    "  Here, have a convenient command line:",
    ""
);

if(heretic) {
    // Heretic
    Print(
        $"    {exePrefix}crispy-heretic -file {path} -skill 4"
    );
} else {
    // Doom or Doom II
    Print(
       $"    {exePrefix}dsda-doom -iwad {(hasDoom2Maps ? "doom2.wad" : "doom.wad")} -file {path} -complevel {(int)compLevel} -skill 4",
        ""
    );

    if(hasMismatchedBosses) {
        Print(
            "",
            "  NOTE: This WAD has a cyberdemon and/or spider mastermind on E1M8, as well as sectors",
            "        with the 666 tag value. If this WAD was designed around Ultimate Doom 1.9's",
            "        behavior, this will likely lead to unexpected results. In this case, use complevel",
            "        3. If this is an older WAD that relies on the legacy behavior (such as UAC_DEAD.WAD),",
            "        complevel 2 (as specified above) must be used.",
            ""
        );
    }

    if(compLevel < CompLevel.Mbf21) {
        Print(
            "", 
           $"  The complevel detection isn't perfect. If you run into trouble, try {(compLevel == CompLevel.Mbf ? "this" : "one of these")} instead:",
            ""
        );
    }

    if(compLevel < CompLevel.Boom && hasDoom1Maps) {
        Print(
            "     0 - Doom v1.2",
            "     1 - Doom v1.666");

        if(compLevel == CompLevel.UltimateDoom) {
            Print(
                "     2 - Doom v1.9"
            );
        }

        if(compLevel == CompLevel.Doom19) {
            Print(
                "     3 - Ultimate Doom v1.9"
            );
        }
    }

    if(compLevel < CompLevel.Boom && hasDoom2Maps) {
        Print(
            "     1 - Doom v1.666",
            "     4 - Final Doom"
        );
    }

    if(compLevel < CompLevel.Boom)
        Print("     9 - Boom");
    if(compLevel < CompLevel.Mbf)
        Print("    11 - MBF");
    if(compLevel < CompLevel.Mbf21) {
        Print(
            "    21 - MBF21",
            "",
            "  When in doubt, check the WAD's readme!"
        );
    }
}

Print("");

return 0;
