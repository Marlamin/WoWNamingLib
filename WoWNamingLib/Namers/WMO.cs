using System.Text;
using System.Text.RegularExpressions;
using WoWNamingLib.Services;
using WoWNamingLib.Utils;

namespace WoWNamingLib.Namers
{
    class WMO
    {
        public static void Name(bool fullRun = false, string overrideIDs = "")
        {
            var wmoList = new List<int>();

            if (string.IsNullOrWhiteSpace(overrideIDs))
            {
                if (fullRun)
                {
                    Console.WriteLine("Listing all WMOs from listfile");
                    var groupRegex = new Regex("/(_\\d\\d\\d_)|(_\\d\\d\\d.wmo$)|(lod\\d.wmo$)");
                    wmoList.AddRange(Namer.IDToNameLookup.Where(x => x.Value.EndsWith(".wmo")).Where(x => !groupRegex.IsMatch(x.Value)).Select(x => x.Key).ToList());
                    Console.WriteLine("Found " + wmoList.Count + " WMOs");
                }
                else
                {
                    Console.WriteLine("Listing all unknown WMOs from listfile");
                    wmoList.AddRange(Namer.IDToNameLookup.Where(x => x.Value.ToLower().StartsWith("world/wmo/autogen-names/unknown-fdid/map-")).Select(x => x.Key).ToList());
                    Console.WriteLine("Found " + wmoList.Count + " WMOs that need to be named");
                }
            }
            else
            {
                var splitString = overrideIDs.Split(',');
                foreach (var splitID in splitString)
                {
                    wmoList.Add(int.Parse(splitID));
                }
            }

            // Newest first so oldest get done last -- BLP names need to be of the oldest WMO.
            wmoList = wmoList.OrderByDescending(x => x).ToList();

            var wmoMinimapTextureDB = Namer.LoadDBC("WMOMinimapTexture");
            var wmoMinimapTextureMap = new Dictionary<uint, List<DBCD.DBCDRow>>();
            foreach (var wmtRow in wmoMinimapTextureDB.Values)
            {
                var wmoID = uint.Parse(wmtRow["WMOID"].ToString());

                if (!wmoMinimapTextureMap.ContainsKey(wmoID))
                {
                    wmoMinimapTextureMap.Add(wmoID, new List<DBCD.DBCDRow>() { wmtRow });
                }
                else
                {
                    wmoMinimapTextureMap[wmoID].Add(wmtRow);
                }
            }

            var wmoAreaTableDB = Namer.LoadDBC("WMOAreaTable");
            var wmoAreaTableMap = new Dictionary<uint, List<uint>>();
            foreach (var wmatRow in wmoAreaTableDB.Values)
            {
                var wmoID = uint.Parse(wmatRow["WMOID"].ToString());

                if (!wmoAreaTableMap.ContainsKey(wmoID))
                {
                    wmoAreaTableMap.Add(wmoID, new List<uint>() { uint.Parse(wmatRow["AreaTableID"].ToString()) });
                }
                else
                {
                    wmoAreaTableMap[wmoID].Add(uint.Parse(wmatRow["AreaTableID"].ToString()));
                }
            }

            var areaTableDB = Namer.LoadDBC("AreaTable");
            var areaTableMap = new Dictionary<uint, string>();
            foreach (var atRow in areaTableDB.Values)
            {
                areaTableMap.Add(uint.Parse(atRow["ID"].ToString()), atRow["ZoneName"].ToString());
            }

            var mapDB = Namer.LoadDBC("Map");

            var counter = 0;
            foreach (var fdid in wmoList)
            {
                if (counter % 1000 == 0)
                    Console.WriteLine("Processed " + counter + "/" + wmoList.Count + " WMOs");

                counter++;

                if (!CASCManager.FileExists(fdid))
                {
                    continue;
                }

                using (var ms = new MemoryStream())
                {
                    try
                    {
                        var file = CASCManager.GetFileByID((uint)fdid).Result;
                        file.CopyTo(ms);
                        ms.Position = 0;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }

                    WorldModel wmo;

                    try
                    {
                        wmo = ParseWMO(ms);
                    }
                    catch (Exception e)
                    {
                        if (e.Message == "Trying to parse group WMO as root WMO!")
                        {
                            Console.WriteLine("WMO " + fdid + " is a group WMO, removing current name");
                            NewFileManager.AddNewFile(fdid, "", true, true);
                            continue;
                        }

                        Console.WriteLine("Error reading WMO:" + e.Message);
                        continue;
                    }

                    var premapRenameName = "";
                    if (!Namer.IDToNameLookup.TryGetValue(fdid, out var wmoFilename) || wmoFilename.ToLower().StartsWith("world/wmo/autogen-names/unknown-fdid/map-"))
                    {
                        premapRenameName = wmoFilename;
                        Console.WriteLine("WMO " + fdid + " is unnamed");
                        wmoFilename = "World/WMO/autogen-names/unknown/" + wmo.header.wmoID + ".wmo";
                        NewFileManager.AddNewFile(fdid, wmoFilename);
                    }

                    var resetName = false;
                    if (wmoFilename.ToLower().StartsWith("world/wmo/autogen-names/unknown"))
                    {
                        if (wmoAreaTableMap.TryGetValue(wmo.header.wmoID, out var areaTableIDs))
                        {
                            var mostCommonIDs = areaTableIDs.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key);
                            foreach (var mostCommonID in mostCommonIDs)
                            {
                                if (areaTableMap.TryGetValue(mostCommonID, out var zoneName))
                                {
                                    wmoFilename = "World/WMO/autogen-names/" + zoneName.ToLower() + "/" + wmo.header.wmoID + ".wmo";
                                    NewFileManager.AddNewFile(fdid, wmoFilename, true);
                                    resetName = true;

                                    break;
                                }
                            }
                        }
                    }

                    if (wmoFilename.ToLower().StartsWith("world/wmo/autogen-names/unknown") || resetName)
                    {
                        if (wmo.groupNames.Length != 0)
                        {
                            Console.WriteLine(fdid + " has group names: " + string.Join(", ", wmo.groupNames.Select(x => x.name)));
                        }

                        if (!string.IsNullOrEmpty(premapRenameName) && premapRenameName.ToLower().StartsWith("world/wmo/autogen-names/unknown-fdid/map-"))
                        {
                            Console.WriteLine("WMO " + premapRenameName + " has parent map " + premapRenameName.Split('-')[3].Split('/')[0]);
                            var parentMap = uint.Parse(premapRenameName.Split('-')[3].Split('/')[0]);

                            foreach (var areaEntry in areaTableDB.Values)
                            {
                                if (int.Parse(areaEntry["ParentAreaID"].ToString()) != 0)
                                    continue;

                                if (int.Parse(areaEntry["ContinentID"].ToString()) != parentMap)
                                    continue;

                                wmoFilename = "World/WMO/autogen-names/" + areaEntry["ZoneName"].ToString().ToLower() + "/" + wmo.header.wmoID + ".wmo";
                                NewFileManager.AddNewFile(fdid, wmoFilename, true);
                                resetName = true;

                                break;
                            }
                        }
                    }

                    if (overrideIDs != "")
                    {
                        resetName = true;
                    }

                    var lodCount = wmo.groupFileDataIDs.Length / wmo.header.nGroups;
                    var i = 0;
                    var lodMats = new Dictionary<short, List<(short, short)>>();
                    var normalMats = new List<short>();

                    for (short lodIndex = 0; lodIndex < lodCount; lodIndex++)
                    {
                        for (short groupIndex = 0; groupIndex < wmo.header.nGroups; groupIndex++)
                        {
                            var groupFileDataID = wmo.groupFileDataIDs[i++];
                            if (groupFileDataID == 0)
                                continue;

                            using (var groupMS = new MemoryStream())
                            {
                                try
                                {
                                    var groupFile = CASCManager.GetFileByID(groupFileDataID).Result;
                                    groupFile.CopyTo(groupMS);
                                    groupMS.Position = 0;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to download group WMO: " + e.Message);
                                    continue;
                                }

                                WorldModelGroup groupwmo;

                                try
                                {
                                    groupwmo = ParseGroupWMO(groupMS);
                                    if (groupwmo.batches != null && groupwmo.batches.Length > 0)
                                    {
                                        foreach (var batch in groupwmo.batches)
                                        {
                                            var matID = (short)batch.materialID;

                                            if ((batch.flags & 2) == 2)
                                            {
                                                matID = batch.materialIDLarge;
                                            }

                                            if (lodIndex > 0)
                                            {
                                                if (lodMats.ContainsKey(matID))
                                                {
                                                    lodMats[matID].Add((groupIndex, lodIndex));
                                                }
                                                else
                                                {
                                                    lodMats.Add(matID, new List<(short, short)>() { (groupIndex, lodIndex) });
                                                }
                                            }
                                            else
                                            {
                                                normalMats.Add(matID);
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to parse group WMO: " + e.Message);
                                }
                            }

                            if ((Namer.IDToNameLookup.ContainsKey((int)groupFileDataID) && !resetName && !Namer.IDToNameLookup[(int)groupFileDataID].Contains("/autogen-names")))
                                continue;

                            if (lodIndex > 0)
                            {
                                NewFileManager.AddNewFile(groupFileDataID, wmoFilename.Replace(".wmo", "_" + groupIndex.ToString().PadLeft(3, '0') + "_lod" + lodIndex + ".wmo"), true);
                            }
                            else
                            {
                                NewFileManager.AddNewFile(groupFileDataID, wmoFilename.Replace(".wmo", "_" + groupIndex.ToString().PadLeft(3, '0') + ".wmo"), true);
                            }
                        }
                    }

                    short matIndex = 0;

                    foreach (var wmoMat in wmo.materials)
                    {
                        var overrideTex1 = resetName;
                        var overrideTex2 = resetName;
                        var overrideTex3 = resetName;

                        var texture1Filename = wmoFilename.Replace(".wmo", "_" + wmoMat.texture1 + ".blp");
                        var texture2Filename = wmoFilename.Replace(".wmo", "_" + wmoMat.texture2 + ".blp");
                        var texture3Filename = wmoFilename.Replace(".wmo", "_" + wmoMat.texture3 + ".blp");

                        if (wmoMat.shader == 21 || (lodMats.ContainsKey(matIndex) && !normalMats.Contains(matIndex)))
                        {
                            if (!lodMats.ContainsKey(matIndex))
                            {
                                texture1Filename = wmoFilename.Replace(".wmo", "_lod_" + wmoMat.texture1 + ".blp");
                                texture2Filename = wmoFilename.Replace(".wmo", "_lod_l_" + wmoMat.texture2 + ".blp");
                                texture3Filename = wmoFilename.Replace(".wmo", "_lod_e_" + wmoMat.texture3 + ".blp");

                                if (Namer.IDToNameLookup.ContainsKey((int)wmoMat.texture1) && Namer.IDToNameLookup[(int)wmoMat.texture1].EndsWith(wmoFilename.Replace(".wmo", "_" + wmoMat.texture1 + ".blp")))
                                    overrideTex1 = true;

                                if (Namer.IDToNameLookup.ContainsKey((int)wmoMat.texture2) && Namer.IDToNameLookup[(int)wmoMat.texture2].EndsWith(wmoFilename.Replace(".wmo", "_" + wmoMat.texture2 + ".blp")))
                                    overrideTex2 = true;

                                if (Namer.IDToNameLookup.ContainsKey((int)wmoMat.texture3) && Namer.IDToNameLookup[(int)wmoMat.texture3].EndsWith(wmoFilename.Replace(".wmo", "_" + wmoMat.texture3 + ".blp")))
                                    overrideTex3 = true;
                            }
                            else
                            {
                                var usedInGroup = lodMats[matIndex][0].Item1.ToString().PadLeft(3, '0');
                                var usedLODLevel = lodMats[matIndex][0].Item2;

                                texture1Filename = wmoFilename.Replace(".wmo", "_" + usedInGroup + "_lod" + usedLODLevel + ".blp");
                                texture2Filename = wmoFilename.Replace(".wmo", "_" + usedInGroup + "_lod" + usedLODLevel + "_l" + ".blp");
                                texture3Filename = wmoFilename.Replace(".wmo", "_" + usedInGroup + "_lod" + usedLODLevel + "_e" + ".blp");

                                overrideTex1 = true;
                                overrideTex2 = true;
                                overrideTex3 = true;
                            }

                            if (
                                wmoMat.texture1 != 0 &&
                                (!Namer.IDToNameLookup.ContainsKey((int)wmoMat.texture1) || overrideTex1 || Namer.placeholderNames.Contains((int)wmoMat.texture1) || resetName)
                                )
                                NewFileManager.AddNewFile(wmoMat.texture1, texture1Filename, true, resetName);

                            if (
                                wmoMat.texture2 != 0 &&
                                (!Namer.IDToNameLookup.ContainsKey((int)wmoMat.texture2) || overrideTex2 || Namer.placeholderNames.Contains((int)wmoMat.texture2) || resetName)
                                )
                                NewFileManager.AddNewFile(wmoMat.texture2, texture2Filename, true, resetName);

                            if (
                                wmoMat.texture3 != 0 &&
                                (!Namer.IDToNameLookup.ContainsKey((int)wmoMat.texture3) || overrideTex3 || Namer.placeholderNames.Contains((int)wmoMat.texture3) || resetName)
                                )
                                NewFileManager.AddNewFile(wmoMat.texture3, texture3Filename, true, resetName);
                        }
                        else if (wmoMat.shader == 23)
                        {
                            var textureFDIDs = new List<uint>() { wmoMat.texture1, wmoMat.texture2, wmoMat.texture3, wmoMat.color3, wmoMat.flags3, wmoMat.runtimeData0, wmoMat.runtimeData1, wmoMat.runtimeData2, wmoMat.runtimeData3 };

                            foreach (var textureFDID in textureFDIDs)
                            {
                                if (textureFDID == 0 || textureFDID == 3609875)
                                    continue;

                                var placeholderFilename = wmoFilename.Replace(".wmo", "_" + textureFDID + ".blp");
                                if (
                                    !Namer.IDToNameLookup.ContainsKey((int)textureFDID) || resetName
                                  )
                                {
                                    NewFileManager.AddNewFile(textureFDID, placeholderFilename, true, true);
                                }
                            }
                        }
                        else
                        {
                            var textureFDIDs = new List<uint>() { wmoMat.texture1, wmoMat.texture2, wmoMat.texture3 };
                            foreach (var textureFDID in textureFDIDs)
                            {
                                if (textureFDID == 0 || textureFDID == 3609875)
                                    continue;

                                var placeholderFilename = wmoFilename.Replace(".wmo", "_" + textureFDID + ".blp");
                                if (
                                    !Namer.IDToNameLookup.ContainsKey((int)textureFDID) || resetName
                                  )
                                {
                                    NewFileManager.AddNewFile(textureFDID, placeholderFilename, true, true);
                                }
                            }
                        }

                        matIndex++;
                    }

                    if (wmo.doodadIds != null)
                    {
                        foreach (var wmoDoodad in wmo.doodadIds)
                        {
                            if (wmoDoodad != 0 && !Namer.IDToNameLookup.ContainsKey((int)wmoDoodad))
                                Console.WriteLine("TODO WMO " + fdid + " (" + wmoFilename + ") refers to unnamed doodad " + wmoDoodad);
                        }
                    }

                    if (wmoMinimapTextureMap.TryGetValue(wmo.header.wmoID, out var wmtRows))
                    {
                        foreach (var wmtRow in wmtRows)
                        {
                            var minimapFDID = uint.Parse(wmtRow["FileDataID"].ToString());

                            if (
                                Namer.IDToNameLookup.ContainsKey((int)minimapFDID) &&
                                !resetName &&
                                !Namer.IDToNameLookup[(int)minimapFDID].Contains("/autogen-names")
                                )
                                continue;

                            var groupNum = uint.Parse(wmtRow["GroupNum"].ToString());
                            var blockX = uint.Parse(wmtRow["BlockX"].ToString());
                            var blockY = uint.Parse(wmtRow["BlockY"].ToString());


                            if (wmoFilename.ToLower().StartsWith("world/wmo/"))
                            {
                                var minimapFilename = "World/Minimaps/WMO/" + wmoFilename.Substring(10).Replace(".wmo", "_" + groupNum.ToString().PadLeft(3, '0') + "_" + blockX.ToString().PadLeft(2, '0') + "_" + blockY.ToString().PadLeft(2, '0') + ".blp");

                                NewFileManager.AddNewFile(minimapFDID, minimapFilename, true, true);
                            }
                            else
                            {
                                throw new Exception("Unknown WMO start path: " + wmoFilename);
                            }
                        }
                    }

                    if (wmo.newLights != null && wmo.newLights.Length > 0)
                    {
                        foreach (var newLight in wmo.newLights)
                        {
                            if (newLight.lightCookieFileID == 0 || Namer.IDToNameLookup.ContainsKey((int)newLight.lightCookieFileID))
                                continue;

                            NewFileManager.AddNewFile(newLight.lightCookieFileID, wmoFilename.Replace(".wmo", "_lightcookie_" + newLight.lightCookieFileID + ".blp"), true);
                        }
                    }
                }
            }
        }

        public struct WorldModel
        {
            public MOHD header;
            public uint version;
            public MOMT[] materials;
            public uint[] doodadIds;
            public MOGN[] groupNames;
            public uint[] groupFileDataIDs;
            public MNLD[] newLights;
            public string skybox;
            public uint skyboxFileDataID;
        }

        public struct MOHD
        {
            public uint nMaterials;
            public uint nGroups;
            public uint nPortals;
            public uint nLights;
            public uint nModels;
            public uint nDoodads;
            public uint nSets;
            public uint ambientColor;
            public uint wmoID;
            public float boundingBox1_x;
            public float boundingBox1_y;
            public float boundingBox1_z;
            public float boundingBox2_x;
            public float boundingBox2_y;
            public float boundingBox2_z;
            public short flags;
            public short nLod;
        }

        public struct MOMT
        {
            public uint flags;
            public uint shader;
            public uint blendMode;
            public uint texture1;
            public uint color1;
            public uint color1b;
            public uint texture2;
            public uint color2;
            public uint groundType;
            public uint texture3;
            public uint color3;
            public uint flags3;
            public uint runtimeData0;
            public uint runtimeData1;
            public uint runtimeData2;
            public uint runtimeData3;
        }
        public struct MOGN
        {
            public string name;
            public int offset;
        }

        public struct MNLD
        {
            public int lightIndex { get; set; }
            public uint lightCookieFileID { get; set; }
        }

        public struct WorldModelGroup
        {
            public MOBA[] batches;
        }

        public struct MOBA
        {
            public short materialIDLarge;
            public byte flags;
            public byte materialID;
        }

        private static WorldModel ParseWMO(MemoryStream stream)
        {
            var wmofile = new WorldModel();

            using (var bin = new BinaryReader(stream))
            {
                long position = 0;
                while (position < stream.Length)
                {
                    stream.Position = position;

                    var chunkName = bin.ReadUInt32();
                    var chunkSize = bin.ReadUInt32();

                    position = stream.Position + chunkSize;

                    switch (chunkName)
                    {
                        case 'M' << 24 | 'V' << 16 | 'E' << 8 | 'R' << 0:
                            wmofile.version = bin.ReadUInt32();
                            if (wmofile.version != 17)
                            {
                                throw new Exception("Unsupported WMO version! (" + wmofile.version + ")");
                            }
                            break;
                        case 'M' << 24 | 'O' << 16 | 'G' << 8 | 'P' << 0:
                            throw new NotSupportedException("Trying to parse group WMO as root WMO!");
                        case 'M' << 24 | 'O' << 16 | 'H' << 8 | 'D' << 0:
                            wmofile.header = bin.Read<MOHD>();
                            break;
                        case 'M' << 24 | 'O' << 16 | 'M' << 8 | 'T' << 0:
                            wmofile.materials = ReadMOMTChunk(chunkSize, bin);
                            break;
                        case 'M' << 24 | 'O' << 16 | 'G' << 8 | 'N' << 0:
                            wmofile.groupNames = ReadMOGNChunk(chunkSize, bin);
                            break;
                        case 'M' << 24 | 'O' << 16 | 'D' << 8 | 'I' << 0:
                            wmofile.doodadIds = ReadMODIChunk(chunkSize, bin);
                            break;
                        case 'M' << 24 | 'N' << 16 | 'L' << 8 | 'D' << 0:
                            wmofile.newLights = ReadMNLDChunk(chunkSize, bin);
                            break;
                        case 'M' << 24 | 'O' << 16 | 'S' << 8 | 'I' << 0:
                            wmofile.skyboxFileDataID = bin.ReadUInt32();
                            break;
                        case 'G' << 24 | 'F' << 16 | 'I' << 8 | 'D' << 0:
                            wmofile.groupFileDataIDs = ReadGFIDChunk(chunkSize, bin);
                            break;
                        default:
                            break;
                    }
                }
            }

            var lodLevel = 0;
            var start = wmofile.header.nGroups * lodLevel;

            for (var i = 0; i < wmofile.header.nGroups; i++)
            {
                if (wmofile.groupFileDataIDs == null)
                    continue;

                var groupFileDataID = wmofile.groupFileDataIDs[start + i];

                if (lodLevel == 3 && groupFileDataID == 0) // if lod is 3 and there's no lod3 available, fall back to lod1
                {
                    groupFileDataID = wmofile.groupFileDataIDs[i + (wmofile.header.nGroups * 2)];
                }

                if (lodLevel >= 2 && groupFileDataID == 0) // if lod is 2 or higher and there's no lod2 available, fall back to lod1
                {
                    groupFileDataID = wmofile.groupFileDataIDs[i + (wmofile.header.nGroups * 1)];
                }

                if (lodLevel >= 1 && groupFileDataID == 0) // if lod is 1 or higher check if lod1 available, fall back to lod0
                {
                    groupFileDataID = wmofile.groupFileDataIDs[i];
                }

                //if (!Namer.IDToNameLookup.ContainsKey(groupFileDataID))
                //{
                //    //Console.WriteLine("Missing WMO group name for " + groupFileDataID);
                //}
            }

            return wmofile;
        }

        private static uint[] ReadGFIDChunk(uint size, BinaryReader bin)
        {
            var count = size / 4;
            var gfids = new uint[count];
            for (var i = 0; i < count; i++)
            {
                gfids[i] = bin.ReadUInt32();
            }
            return gfids;
        }

        private static uint[] ReadMODIChunk(uint size, BinaryReader bin)
        {
            var numIds = size / 4;
            var fileDataIDs = new uint[numIds];
            for (var i = 0; i < numIds; i++)
            {
                fileDataIDs[i] = bin.ReadUInt32();
            }
            return fileDataIDs;
        }

        private static MOMT[] ReadMOMTChunk(uint size, BinaryReader bin)
        {
            var count = size / 64;
            var materials = new MOMT[count];
            for (var i = 0; i < count; i++)
            {
                materials[i] = bin.Read<MOMT>();
            }
            return materials;
        }

        private static MOGN[] ReadMOGNChunk(uint size, BinaryReader bin)
        {
            var wmoGroupsChunk = bin.ReadBytes((int)size);
            var str = new StringBuilder();
            var nameList = new List<string>();
            var nameOffset = new List<int>();
            for (var i = 0; i < wmoGroupsChunk.Length; i++)
            {
                if (wmoGroupsChunk[i] == '\0')
                {
                    if (str.Length > 1)
                    {
                        nameOffset.Add(i - str.ToString().Length);
                        nameList.Add(str.ToString());
                    }
                    str = new StringBuilder();
                }
                else
                {
                    str.Append((char)wmoGroupsChunk[i]);
                }
            }

            var groupNames = new MOGN[nameList.Count];
            for (var i = 0; i < nameList.Count; i++)
            {
                groupNames[i].name = nameList[i];
                groupNames[i].offset = nameOffset[i];
            }
            return groupNames;
        }
        private static MNLD[] ReadMNLDChunk(uint size, BinaryReader bin)
        {
            var count = size / 184;
            var newLights = new MNLD[count];
            for (var i = 0; i < count; i++)
            {
                bin.BaseStream.Position += 4;
                newLights[i].lightIndex = bin.ReadInt32();
                bin.BaseStream.Position += 92;
                newLights[i].lightCookieFileID = bin.ReadUInt32();
                bin.BaseStream.Position += 80;
            }
            return newLights;
        }

        private static WorldModelGroup ParseGroupWMO(MemoryStream stream)
        {
            var groupfile = new WorldModelGroup();

            using (var bin = new BinaryReader(stream))
            {
                long position = 0;
                while (position < stream.Length)
                {
                    stream.Position = position;

                    var chunkName = bin.ReadUInt32();
                    var chunkSize = bin.ReadUInt32();

                    position = stream.Position + chunkSize;

                    switch (chunkName)
                    {
                        case 'M' << 24 | 'O' << 16 | 'G' << 8 | 'P' << 0:
                            bin.BaseStream.Position += 68;
                            groupfile = ReadMOGPChunk(chunkSize, bin);
                            break;
                        default:
                            break;
                    }
                }
            }

            return groupfile;
        }

        private static WorldModelGroup ReadMOGPChunk(uint size, BinaryReader bin)
        {
            var groupfile = new WorldModelGroup();

            using (var stream = new MemoryStream(bin.ReadBytes((int)size)))
            using (var subbin = new BinaryReader(stream))
            {
                long position = 0;
                while (position < stream.Length)
                {
                    stream.Position = position;

                    var chunkName = subbin.ReadUInt32();
                    var chunkSize = subbin.ReadUInt32();

                    position = stream.Position + chunkSize;

                    switch (chunkName)
                    {
                        case 'M' << 24 | 'O' << 16 | 'B' << 8 | 'A' << 0:
                            groupfile.batches = ReadMOBAChunk(chunkSize, subbin);
                            break;
                        default:
                            break;
                    }
                }
            }

            return groupfile;
        }

        private static MOBA[] ReadMOBAChunk(uint size, BinaryReader bin)
        {
            var count = size / 24;
            var batches = new MOBA[count];
            for (var i = 0; i < count; i++)
            {
                bin.BaseStream.Position += 10;
                batches[i].materialIDLarge = bin.ReadInt16();
                bin.BaseStream.Position += 10;
                batches[i].flags = bin.ReadByte();
                batches[i].materialID = bin.ReadByte();
            }
            return batches;
        }
    }
}
