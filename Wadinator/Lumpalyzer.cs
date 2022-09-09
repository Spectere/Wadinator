using System.Text;

namespace Wadinator;

public static class Lumpalyzer {
    // ReSharper disable NotAccessedPositionalProperty.Local
    private record Linedef(
        ushort StartVertex,
        ushort EndVertex,
        ushort Flags,
        ushort Type,
        ushort Tag,
        ushort FrontSidedef,
        ushort BackSidedef
    );

    private record Sector(
        ushort FloorHeight,
        ushort CeilingHeight,
        string FloorTexture,
        string CeilingTexture,
        ushort LightLevel,
        ushort Type,
        ushort Tag
    );

    private record Thing(
        ushort XPosition,
        ushort YPosition,
        ushort Angle,
        ushort Type,
        ushort Flags
    );
    // ReSharper restore NotAccessedPositionalProperty.Local

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
