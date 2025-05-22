using System;
using System.ComponentModel.DataAnnotations;
using WoWNamingLib.Services;
using WoWNamingLib.Utils;

namespace WoWNamingLib.Namers
{
    public static class Map
    {
        private static void NameMap(string mapDirectory, uint wdtFileDataID = 0)
        {
            var isClassic = wdtFileDataID == 0;

            if (wdtFileDataID == 0)
                wdtFileDataID = (uint)CASCManager.GetFileDataIDByName("world/maps/" + mapDirectory + "/" + mapDirectory + ".wdt").Result;

            if (wdtFileDataID == 0)
                return;

            if (!Namer.IDToNameLookup.ContainsKey((int)wdtFileDataID))
                NewFileManager.AddNewFile(wdtFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + ".wdt", true);

            using (var ms = new MemoryStream())
            {
                try
                {
                    var file = CASCManager.GetFileByID(wdtFileDataID).Result;
                    file.CopyTo(ms);
                    ms.Position = 0;
                }
                catch (Exception e)
                {
                    //  Console.WriteLine("Unable to open WDT " + mapDirectory + " (" + wdtFileDataID + "): " + e.Message);
                    return;
                }

                var mapFiles = ProcessWDT(ms);

                if (mapFiles.lgtFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.lgtFileDataID))
                    NewFileManager.AddNewFile(mapFiles.lgtFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + "_lgt.wdt", true);

                if (mapFiles.occFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.occFileDataID))
                    NewFileManager.AddNewFile(mapFiles.occFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + "_occ.wdt", true);

                if (mapFiles.mpvFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.mpvFileDataID))
                    NewFileManager.AddNewFile(mapFiles.mpvFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + "_mpv.wdt", true);

                if (mapFiles.texFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.texFileDataID))
                    NewFileManager.AddNewFile(mapFiles.texFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + ".tex", true);

                if (mapFiles.fogsFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.fogsFileDataID))
                    NewFileManager.AddNewFile(mapFiles.fogsFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + "_fogs.wdt", true);

                if (mapFiles.wdlFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.wdlFileDataID))
                    NewFileManager.AddNewFile(mapFiles.wdlFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + ".wdl", true);

                if (mapFiles.pd4FileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)mapFiles.pd4FileDataID))
                    Console.WriteLine("Found PD4!!!!!!!!!!!!!!! WDT FDID: " + wdtFileDataID + " pd4 fdid: " + mapFiles.pd4FileDataID);

                if (mapFiles.tileFileDataIDs == null && wdtFileDataID != 0)
                {
                    // Console.WriteLine("Map " + mapDirectory + " has no tiles, skipping..");
                    return;
                }

                foreach (var tile in mapFiles.tileFileDataIDs)
                {
                    var adt = tile.Key;
                    var files = tile.Value;

                    if (files.rootADT != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.rootADT))
                        NewFileManager.AddNewFile(files.rootADT, "world/maps/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + ".adt", true);

                    if (files.obj0ADT != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.obj0ADT))
                        NewFileManager.AddNewFile(files.obj0ADT, "world/maps/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + "_obj0.adt", true);

                    if (files.obj0ADT != 0 && int.TryParse(mapDirectory, out int wmapID) && wmapID > 2221)
                    {
                        using (var objMS = new MemoryStream())
                        {
                            try
                            {
                                var file = CASCManager.GetFileByID(files.obj0ADT).Result;
                                file.CopyTo(objMS);
                                objMS.Position = 0;
                            }
                            catch (Exception e)
                            {
                                continue;
                            }

                            var adtObj = ReadObjADTFile(objMS);

                            if (adtObj.wmoFileDataIDs != null)
                            {
                                foreach (var wmoFileDataID in adtObj.wmoFileDataIDs)
                                {
                                    if (wmoFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey((int)wmoFileDataID))
                                        NewFileManager.AddNewFile(wmoFileDataID, "world/wmo/autogen-names/unknown-fdid/map-" + wmapID + "/" + wmoFileDataID + ".wmo");
                                }
                            }

                        }
                    }

                    if (files.obj1ADT != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.obj1ADT))
                        NewFileManager.AddNewFile(files.obj1ADT, "world/maps/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + "_obj1.adt", true);

                    if (files.tex0ADT != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.tex0ADT))
                        NewFileManager.AddNewFile(files.tex0ADT, "world/maps/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + "_tex0.adt", true);

                    // Tileset is pretty slow, only enable once per major patch
                    if (files.tex0ADT != 0 && int.TryParse(mapDirectory, out int mapID) && mapID > 2221)
                    {
                        using (var texMS = new MemoryStream())
                        {
                            try
                            {
                                var file = CASCManager.GetFileByID(files.tex0ADT).Result;
                                file.CopyTo(texMS);
                                texMS.Position = 0;
                            }
                            catch (Exception e)
                            {
                                continue;
                            }

                            var adtTex = ReadTexADTFile(texMS);

                            if (adtTex.diffuseTextureFileDataIDs != null)
                            {
                                foreach (var diffuseFDID in adtTex.diffuseTextureFileDataIDs)
                                {
                                    if (diffuseFDID != 0 && (!Namer.IDToNameLookup.ContainsKey((int)diffuseFDID) || Namer.IDToNameLookup[(int)diffuseFDID].Contains(diffuseFDID.ToString())))
                                    {
                                        var overrideDID = false;

                                        if (Namer.IDToNameLookup.ContainsKey((int)diffuseFDID) && Namer.IDToNameLookup[(int)diffuseFDID].EndsWith(diffuseFDID.ToString() + ".blp"))
                                            overrideDID = true;

                                        NewFileManager.AddNewFile(diffuseFDID, "tileset/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + "_" + diffuseFDID + "_s.blp", overrideDID, overrideDID);
                                    }
                                }
                            }

                            if (adtTex.heightTextureFileDataIDs != null)
                            {
                                foreach (var heightFDID in adtTex.heightTextureFileDataIDs)
                                {
                                    if (heightFDID != 0 && (!Namer.IDToNameLookup.ContainsKey((int)heightFDID) || Namer.IDToNameLookup[(int)heightFDID].Contains(heightFDID.ToString())))
                                    {
                                        var overrideDID = false;

                                        if (Namer.IDToNameLookup.ContainsKey((int)heightFDID) && Namer.IDToNameLookup[(int)heightFDID].EndsWith(heightFDID.ToString() + ".blp"))
                                            overrideDID = true;

                                        NewFileManager.AddNewFile(heightFDID, "tileset/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + "_" + heightFDID + "_h.blp", overrideDID, overrideDID);
                                    }
                                }
                            }

                            //if(adtTex.colorGradings != null)
                            //{
                            //    for(var ci = 0; ci < adtTex.colorGradings.Length; ci++)
                            //    {
                            //        var colorGrading = adtTex.colorGradings[ci];
                            //        if(colorGrading.colorGradingRampFileDataID == 0 && colorGrading.colorGradingFileDataID == 0)
                            //            continue;

                            //        Console.WriteLine(mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + " color grading FDID: " + colorGrading.colorGradingFileDataID + " ramp FDID: " + colorGrading.colorGradingRampFileDataID + ", diffuse FDID: " + adtTex.diffuseTextureFileDataIDs[ci] + ", unk: " + colorGrading.unk0 + " " + colorGrading.unk1);
                            //    }
                            //}
                        }
                    }

                    if (files.lodADT != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.lodADT))
                        NewFileManager.AddNewFile(files.lodADT, "world/maps/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1 + "_" + adt.Item2 + "_lod.adt", true);

                    if (files.minimapTexture != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.minimapTexture))
                        NewFileManager.AddNewFile(files.minimapTexture, "world/minimaps/" + mapDirectory + "/map" + adt.Item1.ToString().PadLeft(2, '0') + "_" + adt.Item2.ToString().PadLeft(2, '0') + ".blp", true);

                    if (files.mapTexture != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.mapTexture))
                        NewFileManager.AddNewFile(files.mapTexture, "world/maptextures/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1.ToString().PadLeft(2, '0') + "_" + adt.Item2.ToString().PadLeft(2, '0') + ".blp", true);

                    if (files.mapTextureN != 0 && !Namer.IDToNameLookup.ContainsKey((int)files.mapTextureN))
                        NewFileManager.AddNewFile(files.mapTextureN, "world/maptextures/" + mapDirectory + "/" + mapDirectory + "_" + adt.Item1.ToString().PadLeft(2, '0') + "_" + adt.Item2.ToString().PadLeft(2, '0') + "_n.blp", true);
                }

                //if (mapFiles.texFileDataID != 0)
                //{
                //    using (var texMS = new MemoryStream())
                //    {
                //        try
                //        {
                //            var texFile = CASCManager.GetFileByID(mapFiles.texFileDataID).Result;
                //            texFile.CopyTo(texMS);
                //            texMS.Position = 0;
                //        }
                //        catch (Exception e)
                //        {
                //            return;
                //        }

                //        var wdtTextures = ProcessTexWDT(texMS);
                //        foreach (var wdtTexture in wdtTextures)
                //        {
                //            if (wdtTexture != 0 && !Namer.IDToNameLookup.ContainsKey(wdtTexture))
                //                NewFileManager.AddNewFile(wdtTexture, "tileset/" + mapDirectory + "/" + mapDirectory + "_" + wdtTexture + ".blp");
                //        }
                //    }
                //}
            }
        }

        public static void Name()
        {
            var mapDB = Namer.LoadDBC("Map");
            if (!mapDB.AvailableColumns.Contains("Directory") && !mapDB.AvailableColumns.Contains("WdtFileDataID"))
            {
                throw new Exception("One of the required columns (Directory or WdtFileDataID) is missing, can't continue map naming.");
            }

            var namedMaps = new List<string>();

            foreach (var entry in mapDB.Values)
            {
                var mapDirectory = entry["Directory"].ToString();

                if (mapDB.AvailableColumns.Contains("WdtFileDataID"))
                {
                    var wdtFileDataID = uint.Parse(entry["WdtFileDataID"].ToString());
                    NameMap(mapDirectory!, wdtFileDataID);
                }
                else
                {
                    NameMap(mapDirectory!);
                }

                namedMaps.Add(mapDirectory!.ToLower());
            }

            Console.WriteLine("Scanning listfile for named WDTs not in map DB2..");
            var bannedExts = new List<string>() { "_fogs.wdt", "_lgt.wdt", "_occ.wdt", "_mpv.wdt", "_preload.wdt" };
            foreach(var entry in Namer.IDToNameLookup)
            {
                if (!entry.Value.EndsWith(".wdt"))
                    continue;

                var banned = false;

                foreach (var bannedExt in bannedExts)
                    if (entry.Value.EndsWith(bannedExt))
                        banned = true;

                if (banned)
                    continue;

                var mapDirectory = Path.GetFileNameWithoutExtension(entry.Value).ToLower();
                if (namedMaps.Contains(mapDirectory))
                    continue;

                Console.WriteLine("Naming listfile-only WDT " + entry.Key + " (" + entry.Value + ")");
                NameMap(mapDirectory, (uint)entry.Key);

                namedMaps.Add(mapDirectory);
            }

            if (mapDB.AvailableColumns.Contains("PreloadFileDataID"))
            {
                foreach (var entry in mapDB.Values)
                {
                    var mapDirectory = entry["Directory"].ToString();
                    var preloadFileDataID = uint.Parse(entry["PreloadFileDataID"].ToString());

                    if(preloadFileDataID == 0)
                        continue;

                    NewFileManager.AddNewFile(preloadFileDataID, "world/maps/" + mapDirectory + "/" + mapDirectory + "_preload.wdt", true);
                }
            }
        }

        public struct MapFileDataIDs
        {
            public uint lgtFileDataID;
            public uint occFileDataID;
            public uint fogsFileDataID;
            public uint mpvFileDataID;
            public uint texFileDataID;
            public uint wdlFileDataID;
            public uint pd4FileDataID;
            public Dictionary<(byte, byte), TileFileDataIDs> tileFileDataIDs;
        }

        public struct TileFileDataIDs
        {
            public uint rootADT;
            public uint obj0ADT;
            public uint obj1ADT;
            public uint tex0ADT;
            public uint lodADT;
            public uint mapTexture;
            public uint mapTextureN;
            public uint minimapTexture;
        }

        private static MapFileDataIDs ProcessWDT(MemoryStream file)
        {
            var mFDIDs = new MapFileDataIDs();

            var bin = new BinaryReader(file);
            while (bin.BaseStream.Position < bin.BaseStream.Length)
            {
                var chunkName = bin.ReadUInt32();
                var chunkSize = bin.ReadUInt32();
                switch (chunkName)
                {
                    case 'M' << 24 | 'P' << 16 | 'H' << 8 | 'D' << 0:
                        bin.ReadUInt32();
                        mFDIDs.lgtFileDataID = bin.ReadUInt32();
                        mFDIDs.occFileDataID = bin.ReadUInt32();
                        mFDIDs.fogsFileDataID = bin.ReadUInt32();
                        mFDIDs.mpvFileDataID = bin.ReadUInt32();
                        mFDIDs.texFileDataID = bin.ReadUInt32();
                        mFDIDs.wdlFileDataID = bin.ReadUInt32();
                        mFDIDs.pd4FileDataID = bin.ReadUInt32();
                        break;
                    case 'M' << 24 | 'A' << 16 | 'I' << 8 | 'D' << 0:
                        mFDIDs.tileFileDataIDs = ReadMAIDChunk(bin);
                        break;
                    default:
                        bin.BaseStream.Position += chunkSize;
                        break;
                }
            }

            return mFDIDs;
        }

        private static Dictionary<(byte, byte), TileFileDataIDs> ReadMAIDChunk(BinaryReader bin)
        {
            var tileFiles = new Dictionary<(byte, byte), TileFileDataIDs>();
            for (byte x = 0; x < 64; x++)
            {
                for (byte y = 0; y < 64; y++)
                {
                    tileFiles.Add((y, x), bin.Read<TileFileDataIDs>());
                }
            }
            return tileFiles;
        }

        private static uint[] ProcessTexWDT(MemoryStream file)
        {
            var bin = new BinaryReader(file);
            while (bin.BaseStream.Position < bin.BaseStream.Length)
            {
                var chunkName = bin.ReadUInt32();
                var chunkSize = bin.ReadUInt32();
                switch (chunkName)
                {
                    case 'T' << 24 | 'X' << 16 | 'V' << 8 | 'R' << 0:
                        var version = bin.ReadUInt32();
                        if (version != 1)
                        {
                            Console.WriteLine("Unsupported .TEX version: " + version);
                            return [];
                        }
                        break;
                    case 'T' << 24 | 'X' << 16 | 'B' << 8 | 'T' << 0:
                        var count = chunkSize / 12;
                        var filedataids = new uint[count];
                        for (var i = 0; i < count; i++)
                        {
                            filedataids[i] = bin.ReadUInt32();
                            bin.ReadBytes(8);
                        }
                        return filedataids;
                    default:
                        bin.BaseStream.Position += chunkSize;
                        break;
                }
            }

            return [];
        }

        public struct TexADT
        {
            public uint[] diffuseTextureFileDataIDs;
            public uint[] heightTextureFileDataIDs;
            public DiffuseColorGrading[] colorGradings;
        }

        public struct DiffuseColorGrading
        {
            public uint unk0;
            public uint unk1;
            public uint colorGradingFileDataID;
            public uint colorGradingRampFileDataID;
        }
        public struct ObjADT
        {
            public uint[] wmoFileDataIDs;
        }

        private static ObjADT ReadObjADTFile(Stream adtTexStream)
        {
            var adt = new ObjADT();

            using (var bin = new BinaryReader(adtTexStream))
            {
                long position = 0;

                while (position < adtTexStream.Length)
                {
                    adtTexStream.Position = position;
                    var chunkName = bin.ReadUInt32();
                    var chunkSize = bin.ReadUInt32();

                    position = adtTexStream.Position + chunkSize;
                    switch (chunkName)
                    {
                        case 'M' << 24 | 'O' << 16 | 'D' << 8 | 'F' << 0:
                            adt.wmoFileDataIDs = ReadMODFChunk(chunkSize, bin);
                            break;
                        default:
                            break;
                    }
                }
            }

            return adt;
        }

        private static TexADT ReadTexADTFile(Stream adtTexStream)
        {
            var adt = new TexADT();

            using (var bin = new BinaryReader(adtTexStream))
            {
                long position = 0;

                while (position < adtTexStream.Length)
                {
                    adtTexStream.Position = position;
                    var chunkName = bin.ReadUInt32();
                    var chunkSize = bin.ReadUInt32();

                    position = adtTexStream.Position + chunkSize;
                    switch (chunkName)
                    {
                        case 'M' << 24 | 'H' << 16 | 'I' << 8 | 'D' << 0: // Height texture fileDataIDs
                            adt.heightTextureFileDataIDs = ReadFileDataIDChunk(chunkSize, bin);
                            break;
                        case 'M' << 24 | 'D' << 16 | 'I' << 8 | 'D' << 0: // Diffuse texture fileDataIDs
                            adt.diffuseTextureFileDataIDs = ReadFileDataIDChunk(chunkSize, bin);
                            break;
                        case 'M' << 24 | 'T' << 16 | 'C' << 8 | 'G' << 0: // Color grading
                            adt.colorGradings = ReadMTCGChunk(chunkSize, bin);
                            break;
                        default:
                            break;
                    }
                }
            }

            return adt;
        }
        private static uint[] ReadFileDataIDChunk(uint size, BinaryReader bin)
        {
            var count = size / 4;
            var filedataids = new uint[count];
            for (var i = 0; i < count; i++)
            {
                filedataids[i] = bin.ReadUInt32();
            }
            return filedataids;
        }

        private static DiffuseColorGrading[] ReadMTCGChunk(uint size, BinaryReader bin)
        {
            var count = size / 16;
            var colorGradings = new DiffuseColorGrading[count];
            for (var i = 0; i < count; i++)
            {
                colorGradings[i] = bin.Read<DiffuseColorGrading>();
            }
            return colorGradings;
        }

        private static uint[] ReadMODFChunk(uint size, BinaryReader bin)
        {
            var count = size / 64;
            var filedataids = new uint[count];
            for (var i = 0; i < count; i++)
            {
                filedataids[i] = bin.ReadUInt32();
                bin.ReadBytes(60);
            }
            return filedataids;
        }
    }
}
