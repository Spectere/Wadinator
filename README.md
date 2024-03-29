# The Wadinator

## Description

The Wadinator is a cross-platform command line application designed to choose a
random WAD from a given path and to analyze a WAD to determine the most
appropriate complevel for it. The randomization is optional, as giving the
Wadinator the path to a WAD (rather than a directory) will cause it to skip the
random draw and simply analyze the file.

Only the Doom WAD format is supported at this time.

## Usage

The Wadinator requires a single parameter—the path to the WAD file or
directory—and supports an optional recursion (`-r`) parameter. If the program
is used with Heretic WADs, the `-heretic` parameter must be specified.

The recursion function only works if a path is passed. This causes the
Wadinator to scan the path and its subdirectories rather than just the base
path.

## Building from Source

To build the Wadinator, you will need to install .NET 6 or newer on your system.
This can be downloaded from [here](https://dotnet.microsoft.com/en-us/).

After downloading and installing the .NET SDK, go into a command prompt or
some other sort of terminal and do the following:

1. Clone the repository: `git clone https://github.com/Spectere/Wadinator`
2. Change into the directory of the newly-cloned repo: `cd Wadinator`
3. Run `dotnet build`

This will build a binary that can be found in the `Wadinator/bin/Debug/net6.0`
directory, relative to where you cloned the repository.

If you plan to modify the Wadinator, it's highly recommended that you use an
IDE in order to streamline the development process. A few options include:

* [JetBrains Rider](https://www.jetbrains.com/rider/) (commercial, Windows / macOS / Linux)
* [Visual Studio](https://visualstudio.microsoft.com) (free, Windows / macOS)
* [Visual Studio Code](https://code.visualstudio.com) (free, Windows / macOS / Linux)
* [MonoDevelop](https://www.monodevelop.com) (free, Windows / macOS / Linux)

## Randomization!

If a path is given to the Wadinator, a running list of picked WADs will be
created and saved into the `wadinator_data.json` file. When all of the files
in the directory have been picked, the program will throw an error.

To reset the random pool, you can modify the `wadinator_data.json` file and
remove all of the WADs from the `SelectedWads` section (that is, replace it
with an empty array).

## Analysis

The Wadinator analyzes the following lumps within a WAD file:

* DEHACKED
* LINEDEFS
* SECTORS
* THINGS

In addition, it also checks the map names and some map features. Note that
complevel detection will not be performed for Heretic WADs.

### DEHACKED

The DeHackEd parser in the Wadinator is very primitive and currently only
checks for MBF21 features (bits, groups, and code pointers, to name a few).

### LINEDEFS/SECTORS/THINGS

The Wadinator searches these lumps for types and flags specific to a given
complevel. These are used to determine whether Boom, MBF, or MBF21 features
are used.

### Map Names and Features

Note: The checks in this section are only performed if the compatibility level
is *below* Boom.

If Doom II map names are detected, the base complevel will be 2 (Doom v1.9).

If only Doom maps are found, the complevel will typically be 3 (Ultimate Doom
v1.9). The only time this will be downgraded to 2 is if all of the following
conditions are true:

1. The WAD contains a lump for E1M8.
2. E1M8 contains a Cyberdemon or Spider Mastermind.
3. E1M8 contains a sector with tag 666.

While there's no way to be sure of the exact behavior programatically, some
classic WADs (such as UAC_DEAD.WAD) rely on the legacy boss death behavior
in complevel 0-2 in order to function correctly. This detection attempts to
ensure that those WADs will be fully playable.

## Music WAD Generation

The Wadinator has the ability to pick music from a random pool. This is useful
if you just can't bear to hear D_E1M1 and/or D_RUNNIN while sifting through
random WADs.

To use this feature, music lumps must be imported into the Wadinator. To do so,
the `-import-music` parameter must be used, with the path or file name 
corresponding to the music directory or file that should be imported. This
command honors the `RecurseDirectories` configuration option, and can also 
be controlled by the `-recurse` and `-no-recurse` command line parameters.

If you wish to delete the music database, use the `-clear-music` parameter.

### Music Metadata

The Wadinator can also display which song was picked for each map slot. In
order for this to be accurate, track information must be populated. To do 
this, create a `manifest.json` file in the directory that contains your 
music. Here is an example of a `manifest.json` file:

```json
{
  "Entries": {
    "MyMusic.mid": {
      "Title": "My Cool Song",
      "Artist": "CoolDude1986"
    },
    "SMB.mid": {
      "Title": "Shuper Marius Bros Theme",
      "Artist": "ShuperSoft",
      "Sequencer": "SeqKing"
    }
  }
}
```

The `Title` and `Artist` fields are fairly self-explanatory. The `Sequencer` 
should be used in cases where a music file was converted from another source,
such as when someone makes a MIDI conversion of a song from another video 
game. Any of these fields can be omitted.

## Limitations

The Wadinator's complevel detection behaves as expected, both against a
fairly healthy test suite as well after considerable real-world testing and
refinement, though there is still likely some room for improvement.

The Wadinator currently only returns complevels 2, 3, 9, 11, and 21. Most of
the others are difficult to determine with any degree of certainty.

There are a number of quirks with older DOS level editors that may cause
unexpected results. While some of these have been worked around, it's possible
that more may crop up as the sample size increases. If you run into something
like this, please open a GitHub issue and let us know! However, please note
that invalid data produced by these editors (particularly garbage data in unused
bits, likely due to uninitialized memory) can be impossible to distinguish from
legitmate Boom/MBF tags. This sort of error is far more common on very old WAD
files.

The Wadinator makes no attempt to detect Heretic (or other) WAD files. A
command line parameter must be specified in order for it to handle these
gracefully. Autodetection support may be provided at a future date.

## Contributions

Contributions of any kind are welcome! If you have any ideas on how to improve
the Wadinator, please open an issue and I'll take a look. If you know your way
around an IDE, pull requests are welcome.
