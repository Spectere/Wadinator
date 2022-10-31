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

## Randomization!

If a path is given to the Wadinator, a running list of picked WADs will be
created and saved into the `wadinator_played.txt` file. When all of the files
in the directory have been picked, the program will throw an error.

To reset the random pool, simply delete the `wadinator_played.txt` file. This
file is just a simple list of strings, so you also have the option of manually
adding files to it (if you don't want them to be selected) or deleting
individual records.

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

## Limitations

The Wadinator's complevel detection behaved as expected on the author's system,
using an admittedly narrow test suite (~20 WADs). More testing will be needed
to fully determine its reliability.

The Wadinator currently only returns complevels 2, 3, 9, 11, and 21. Most of
the others are difficult to determine with any degree of certainty.

There are a number of quirks with older DOS level editors that may cause
unexpected results. While some of these have been worked around, it's possible
that more may crop up as the sample size increases. If you run into something
like this, please open a GitHub issue and let us know!

The Wadinator makes no attempt to detect Heretic (or other) WAD files. A
command line parameter must be specified in order for it to handle these
gracefully. Autodetection support may be provided at a future date.

## Contributions

Contributions of any kind are welcome! If you have any ideas on how to improve
the Wadinator, please open an issue and I'll take a look. If you know your way
around an IDE, pull requests are welcome.
