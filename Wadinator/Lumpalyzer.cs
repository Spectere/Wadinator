using System.Text;

namespace Wadinator;

/// <summary>
/// Focuses on reading and analyzes WAD lumps.
/// </summary>
public static class Lumpalyzer {
    // ReSharper disable NotAccessedPositionalProperty.Local
    /// <summary>
    /// Represents a Doom or Heretic linedef.
    /// </summary>
    /// <param name="StartVertex">The starting vertex for this linedef.</param>
    /// <param name="EndVertex">The ending vertex for this linedef.</param>
    /// <param name="Flags">Special flags for this linedef.</param>
    /// <param name="Type">The action type for this linedef.</param>
    /// <param name="Tag">This linedef's tag.</param>
    /// <param name="FrontSidedef">This linedef's front sidedef.</param>
    /// <param name="BackSidedef">This linedef's back sidedef.</param>
    private record Linedef(
        ushort StartVertex,
        ushort EndVertex,
        ushort Flags,
        ushort Type,
        ushort Tag,
        ushort FrontSidedef,
        ushort BackSidedef
    );

    /// <summary>
    /// Represents a Doom or Heretic sector.
    /// </summary>
    /// <param name="FloorHeight">The height of the floor.</param>
    /// <param name="CeilingHeight">The height of the ceiling.</param>
    /// <param name="FloorTexture">The texture of the floor.</param>
    /// <param name="CeilingTexture">The texture of the ceiling.</param>
    /// <param name="LightLevel">The light level.</param>
    /// <param name="Type">This sector's special type.</param>
    /// <param name="Tag">This sector's tag.</param>
    private record Sector(
        ushort FloorHeight,
        ushort CeilingHeight,
        string FloorTexture,
        string CeilingTexture,
        ushort LightLevel,
        ushort Type,
        ushort Tag
    );

    /// <summary>
    /// Represents a Doom or Heretic thing.
    /// </summary>
    /// <param name="XPosition">The X position.</param>
    /// <param name="YPosition">The Y position.</param>
    /// <param name="Angle">The angle of this thing.</param>
    /// <param name="Type">The type of this thing.</param>
    /// <param name="Flags">This thing's flags.</param>
    private record Thing(
        ushort XPosition,
        ushort YPosition,
        ushort Angle,
        ushort Type,
        ushort Flags
    );
    // ReSharper restore NotAccessedPositionalProperty.Local

    /// <summary>
    /// Guesses which complevel is required to make use of a WAD's DeHackEd lump.
    /// </summary>
    /// <param name="dehackedStream">A <see cref="Stream"/> containing the WAD's DeHackEd lump.</param>
    /// <returns>The estimated complevel.</returns>
    public static CompLevel AnalyzeDeHackEd(Stream dehackedStream) {
        // TODO: Consider splitting this off into a more robust DEH parser.
        var mbf21CodePointers = new List<string> {
            "A_AddFlags",
            "A_CheckAmmo",
            "A_ClearTracer",
            "A_ConsumeAmmo",
            "A_FindTracer",
            "A_GunFlashTo",
            "A_HealChase",
            "A_JumpIfFlagsSet",
            "A_JumpIfHealthBelow",
            "A_JumpIfTargetCloser",
            "A_JumpIfTargetInSight",
            "A_JumpIfTracerCloser",
            "A_JumpIfTracerInSight",
            "A_MonsterBulletAttack",
            "A_MonsterMeleeAttack",
            "A_MonsterProjectile",
            "A_NoiseAlert",
            "A_RadiusDamage",
            "A_RefireTo",
            "A_RemoveFlags",
            "A_SeekTracer",
            "A_SpawnObject",
            "A_WeaponAlert",
            "A_WeaponBulletAttack",
            "A_WeaponJump",
            "A_WeaponMeleeAttack",
            "A_WeaponProjectile",
            "A_WeaponSound"
        };

        KeyValuePair<string, string> SplitLine(string dehLine) {
            var parts = dehLine.Split('=');

            return parts.Length > 1
                ? new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim())
                : new KeyValuePair<string, string>(parts[0].Trim(), "");
        }

        var result = CompLevel.Doom19;

        using var streamReader = new StreamReader(dehackedStream);

        // Read all the lines!
        var dehLines = streamReader.ReadToEnd().Split("\n").ToList();

        // Check for DSDehacked (MBF21 extension).
        var doomVersionLine = dehLines.FirstOrDefault(x => x.Trim().StartsWith("Doom version"));
        if(doomVersionLine is not null) {
            var doomVersion = SplitLine(doomVersionLine);
            result = result.Promote(CompLevel.Mbf21, doomVersion.Value == "2021");
        }

        foreach(var kvp in dehLines.Select(SplitLine)) {
            // Check for MBF21 bits.
            result.Promote(CompLevel.Mbf21, kvp.Key == "MBF21 Bits");

            // Check for thing groups.
            result.Promote(CompLevel.Mbf21, kvp.Key == "Infighting group");
            result.Promote(CompLevel.Mbf21, kvp.Key == "Projectile group");
            result.Promote(CompLevel.Mbf21, kvp.Key == "Splash group");

            // Check for fast speed definition (MBF21).
            result.Promote(CompLevel.Mbf21, kvp.Key == "Fast speed");

            // Check for melee range definition (MBF21).
            result.Promote(CompLevel.Mbf21, kvp.Key == "Melee range");

            // Check for ammo per shot definition (MBF21).
            result.Promote(CompLevel.Mbf21, kvp.Key == "Ammo per shot");

            // Check for MBF21 code pointers.
            foreach(var codePointer in mbf21CodePointers)
                result.Promote(CompLevel.Mbf21, kvp.Value.StartsWith(codePointer));
        }

        return result;
    }

    /// <summary>
    /// Analyzes a map's LINEDEF lump and guesses the required complevel.
    /// </summary>
    /// <param name="linedefStream">A <see cref="Stream"/> containing the map's LINEDEF lump.</param>
    /// <returns>The estimated complevel.</returns>
    public static CompLevel AnalyzeLinedefs(Stream linedefStream) {
        var result = CompLevel.Doom19;

        // Read all the linedefs!
        var linedefs = ReadLinedefs(linedefStream);

        // Analyze all the things.
        foreach(var linedef in linedefs) {
            /*
             * Flags
             */
            // Check the upper bits. If they're indiscriminately set, odds are the map
            // was created with a wonky editor.
            if(!((linedef.Flags & 0x4000) > 0 || (linedef.Flags & 0x8000) > 0)) {
                // Flag 0x0200 were introduced in Boom.
                result = result.Promote(CompLevel.Boom, (linedef.Flags & 0x0100) > 0);

                // Flag 0x1000 and 0x2000 were introduced in MBF21.
                result = result.Promote(CompLevel.Mbf21, (linedef.Flags & 0x1000) > 0);
                result = result.Promote(CompLevel.Mbf21, (linedef.Flags & 0x2000) > 0);
            }

            /*
             * Types
             */
            // Types 271 and 272 were introduced in MBF.
            result = result.Promote(CompLevel.Mbf, linedef.Type is 271 or 272);

            // Types >= 142 were introduced in Boom. Note: For some reason there's an errant type
            // 65535 in doom.wad (Ultimate Doom v1.9), so we need to add an exception for that.
            result = result.Promote(CompLevel.Boom, linedef.Type is >= 142 and < 65535);
        }

        return result;
    }

    /// <summary>
    /// Analyzes a map's SECTORS lump and guesses the required complevel.
    /// </summary>
    /// <param name="sectorStream">A <see cref="Stream"/> containing the map's SECTORS lump.</param>
    /// <returns>The estimated complevel.</returns>
    public static CompLevel AnalyzeSectors(Stream sectorStream) {
        var result = CompLevel.Doom19;

        // Read all the sectors!
        var sectors = ReadSectors(sectorStream);

        // Analyze until you die.
        foreach(var sector in sectors) {
            /*
             * Types
             */
            // Generalized sector types (introduced in Boom).
            result = result.Promote(CompLevel.Boom, sector.Type > 0x0020);

            // MBF21 generalized sector types.
            result = result.Promote(CompLevel.Mbf21, sector.Type >= 0x1000);
        }

        return result;
    }

    /// <summary>
    /// Analyzes a map's THINGS lump and guesses the required complevel.
    /// </summary>
    /// <param name="thingStream">A <see cref="Stream"/> containing the map's THINGS lump.</param>
    /// <returns>The estimated complevel.</returns>
    public static CompLevel AnalyzeThings(Stream thingStream) {
        var result = CompLevel.Doom19;

        // Read all the things!
        var things = ReadThings(thingStream);

        // Analyze until the demons die.
        foreach(var thing in things) {
            /*
             * Types
             */
            // Boom introduced thing types 5001 (MT_PUSH) and 5002 (MT_PULL).
            result = result.Promote(CompLevel.Boom, thing.Type is 5001 or 5002);

            // MBF adds the dog thing type (140).
            result = result.Promote(CompLevel.Mbf, thing.Type == 140);

            // MT_MUSICSOURCE (14100-14164). I don't know if this was introduced in
            // MBF, but complevel 11 is generally required for it to work.
            result = result.Promote(CompLevel.Mbf, thing.Type is >= 14100 and <= 14164);

            /*
             * Flags
             */
            // Huge exception: if bit 8 (0x0100) is set, ignore the flags below. Some
            // old Doom editors (such as HellMaker) would set the unused thing flag
            // bits to 1.
            if((thing.Flags & 0x0100) == 0) {
                // Boom introduced the 0x0020 (not in DM) and 0x0040 (not in coop) flags.
                result = result.Promote(CompLevel.Boom, thing.Flags > 0x0020);

                // MBF introduced the 0x0080 (friendly monster) flag.
                result = result.Promote(CompLevel.Mbf, thing.Flags > 0x0080);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether this WAD's E1M8 contains sector tag 666, along with unexpected Cyberdemon(s) and/or Spider Mastermind(s).
    /// </summary>
    /// <param name="mapName">The map name.</param>
    /// <param name="sectorStream">A <see cref="Stream"/> containing this map's SECTORS lump.</param>
    /// <param name="thingStream">A <see cref="Stream"/> containing this map's THINGS lump.</param>
    /// <returns><c>true</c> if this is E1M8, has a sector with tag 666, and one or more Cyberdemons and/or Spider Masterminds, otherwise
    /// <c>false</c>.</returns>
    public static bool HasMismatchedBossEncounter(string mapName, Stream sectorStream, Stream thingStream) {
        if(mapName != "E1M8") return false;  // da fok u doin ere, m8??

        var sectors = ReadSectors(sectorStream);
        var things = ReadThings(thingStream);

        // Check for the presence of a cyberdemon (16) and/or a spider mastermind (7), as well as
        // sector tag 666.
        var bossFound = things.Any(thing => thing.Type is 7 or 16);
        var tag666Found = sectors.Any(sector => sector.Tag == 666);

        return bossFound && tag666Found;
    }

    /// <summary>
    /// Reads a LINEDEFS lump into a list of <see cref="Linedef"/> records.
    /// </summary>
    /// <param name="linedefStream">A <see cref="Stream"/> pointing to the level's LINEDEFS lump.</param>
    /// <returns>A list of <see cref="Linedef"/> objects.</returns>
    private static List<Linedef> ReadLinedefs(Stream linedefStream) {
        using var binaryReader = new BinaryReader(linedefStream);

        var linedefs = new List<Linedef>();
        while(linedefStream.Position < linedefStream.Length) {
            linedefs.Add(new Linedef(
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16()
            ));
        }

        return linedefs;
    }

    /// <summary>
    /// Reads a SECTORS lump into a list of <see cref="Sector"/> records.
    /// </summary>
    /// <param name="sectorStream">A <see cref="Stream"/> pointing to the level's SECTORS lump.</param>
    /// <returns>A list of <see cref="Sector"/> objects.</returns>
    private static List<Sector> ReadSectors(Stream sectorStream) {
        using var binaryReader = new BinaryReader(sectorStream);

        var sectors = new List<Sector>();
        while(sectorStream.Position < sectorStream.Length) {
            sectors.Add(new Sector(
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                Encoding.ASCII.GetString(binaryReader.ReadBytes(8)),
                Encoding.ASCII.GetString(binaryReader.ReadBytes(8)),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16()
            ));
        }

        return sectors;
    }

    /// <summary>
    /// Reads a THINGS lump into a list of <see cref="Thing"/> records.
    /// </summary>
    /// <param name="thingStream">A <see cref="Stream"/> pointing to the level's THINGS lump.</param>
    /// <returns>A list of <see cref="Thing"/> objects.</returns>
    private static List<Thing> ReadThings(Stream thingStream) {
        using var binaryReader = new BinaryReader(thingStream);

        var things = new List<Thing>();
        while(thingStream.Position < thingStream.Length) {
            things.Add(new Thing(
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16(),
                binaryReader.ReadUInt16()
            ));
        }

        return things;
    }
}
