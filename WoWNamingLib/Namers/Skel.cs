using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public static class Skel
    {
        private static bool overrideCheck(bool overrideName, uint fdid)
        {
            return fdid != 0 && (overrideName || !Namer.IDToNameLookup.ContainsKey((int)fdid) || (Namer.IDToNameLookup.ContainsKey((int)fdid) && (Namer.IDToNameLookup[(int)fdid].Contains("unk_exp09") || int.TryParse(Namer.IDToNameLookup[(int)fdid], out _))));
        }

        public static void Name(uint filedataid, string modelName, string folder, bool overrideName)
        {
            using (var ms = new MemoryStream())
            {
                try
                {
                    var file = CASCManager.GetFileByID(filedataid).Result;
                    file.CopyTo(ms);
                    ms.Position = 0;

                    var bin = new BinaryReader(ms);
                    if (bin.ReadUInt64() == 0)
                        throw new Exception("Skel is encrypted");

                    ms.Position = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to open skel: " + e.Message);
                }

                Model.M2Model m2 = new Model.M2Model();

                using (var bin = new BinaryReader(ms))
                {
                    while (bin.BaseStream.Position < bin.BaseStream.Length)
                    {
                        var chunkName = bin.ReadUInt32();
                        var chunkSize = bin.ReadUInt32();

                        if (chunkName == 0)
                            throw new Exception("Skel is encrypted");

                        var prevPos = bin.BaseStream.Position;
                        switch (chunkName)
                        {
                            case 'A' << 0 | 'F' << 8 | 'I' << 16 | 'D' << 24: // Animation file IDs
                                var afids = new Model.AFID[chunkSize / 8];
                                for (var a = 0; a < chunkSize / 8; a++)
                                {
                                    afids[a].animID = bin.ReadInt16();
                                    afids[a].subAnimID = bin.ReadInt16();
                                    afids[a].fileDataID = bin.ReadUInt32();
                                }
                                m2.animFileDataIDs = afids;
                                break;
                            case 'B' << 0 | 'F' << 8 | 'I' << 16 | 'D' << 24: // Animation file IDs
                                for (var i = 0; i < chunkSize / 4; i++)
                                {
                                    var bfid = bin.ReadUInt32();
                                    if (bfid == 0)
                                        continue;

                                    if (overrideCheck(overrideName, bfid))
                                        NewFileManager.AddNewFile(bfid, folder + "/" + modelName.ToLower() + "_" + bfid + ".bone", overrideName);
                                }
                                break;
                            default:
                                bin.BaseStream.Position += chunkSize;
                                break;
                        }
                    }
                }

                if (m2.animFileDataIDs != null)
                {
                    for (var i = 0; i < m2.animFileDataIDs.Length; i++)
                    {
                        var anim = m2.animFileDataIDs[i];
                        if (anim.fileDataID == 0)
                            continue;

                        if (overrideCheck(overrideName, anim.fileDataID))
                            NewFileManager.AddNewFile(anim.fileDataID, folder + "/" + modelName.ToLower() + anim.animID.ToString().PadLeft(4, '0') + "-" + anim.subAnimID.ToString().PadLeft(2, '0') + ".anim", overrideName);
                    }
                }
            }
        }
    }
}
