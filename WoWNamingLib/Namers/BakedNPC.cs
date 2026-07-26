using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class BakedNPC
    {
        public static void Name()
        {
            // Baked NPC skins
            var tfdMap = new Dictionary<int, List<int>>();
            var tfdMapS = new Dictionary<int, int>();
            var tfdMapE = new Dictionary<int, int>();

            var textureFileData = Namer.LoadDBC("TextureFileData");
            if (!textureFileData.AvailableColumns.Contains("UsageType") || !textureFileData.AvailableColumns.Contains("MaterialResourcesID") || !textureFileData.AvailableColumns.Contains("FileDataID"))
                throw new Exception("TextureFiledata is missing required columns");

            foreach (var tfdRow in textureFileData.Values)
            {
                var usageType = uint.Parse(tfdRow["UsageType"].ToString()!);
                var materialResourcesID = int.Parse(tfdRow["MaterialResourcesID"].ToString()!);

                if (usageType == 0)
                {
                    if (tfdMap.TryGetValue(materialResourcesID, out List<int>? fileDataIDs))
                        fileDataIDs.Add(int.Parse(tfdRow["FileDataID"].ToString()!));
                    else
                        tfdMap.TryAdd(materialResourcesID, new List<int>() { int.Parse(tfdRow["FileDataID"].ToString()!) });
                }
                else if (usageType == 1)
                {
                    tfdMapS.TryAdd(int.Parse(tfdRow["MaterialResourcesID"].ToString()!), int.Parse(tfdRow["FileDataID"].ToString()!));
                }
                else if (usageType == 2)
                {
                    tfdMapE.TryAdd(int.Parse(tfdRow["MaterialResourcesID"].ToString()!), int.Parse(tfdRow["FileDataID"].ToString()!));
                }
            }

            var cdiExtra = Namer.LoadDBC("CreatureDisplayInfoExtra");
            if (!cdiExtra.AvailableColumns.Contains("BakeMaterialResourcesID") || !cdiExtra.AvailableColumns.Contains("HDBakeMaterialResourcesID"))
                throw new Exception("CreatureDisplayInfoExtra is missing required columns");

            foreach (var row in cdiExtra.Values)
            {
                var SDMat = int.Parse(row["BakeMaterialResourcesID"].ToString()!);
                if (SDMat != 0 && tfdMap.TryGetValue(SDMat, out var SDFDIDs))
                {
                    if (Namer.NeedsName(SDFDIDs[0]))
                    {
                        NewFileManager.AddNewFile(SDFDIDs[0], "textures/bakednpctextures/creaturedisplayextra-" + row["ID"].ToString() + ".blp");
                    }
                }

                if (SDMat != 0 && tfdMapS.TryGetValue(SDMat, out var SDFDID_s))
                {
                    if (Namer.NeedsName(SDFDID_s))
                    {
                        NewFileManager.AddNewFile(SDFDID_s, "textures/bakednpctextures/creaturedisplayextra-" + row["ID"].ToString() + "_s.blp");
                    }
                }

                if (SDMat != 0 && tfdMapE.TryGetValue(SDMat, out var SDFDID_e))
                {
                    if (Namer.NeedsName(SDFDID_e))
                    {
                        NewFileManager.AddNewFile(SDFDID_e, "textures/bakednpctextures/creaturedisplayextra-" + row["ID"].ToString() + "_e.blp");
                    }
                }

                var HDMat = int.Parse(row["HDBakeMaterialResourcesID"].ToString()!);
                if (HDMat != 0 && tfdMap.TryGetValue(HDMat, out var HDFDIDs))
                {
                    if (Namer.NeedsName(HDFDIDs[0]))
                    {
                        NewFileManager.AddNewFile(HDFDIDs[0], "textures/bakednpctextures/creaturedisplayextra-" + row["ID"].ToString() + "_hd.blp");
                    }
                }

                if (HDMat != 0 && tfdMapS.TryGetValue(HDMat, out var HDFDID_s))
                {
                    if (Namer.NeedsName(HDFDID_s))
                    {
                        NewFileManager.AddNewFile(HDFDID_s, "textures/bakednpctextures/creaturedisplayextra-" + row["ID"].ToString() + "_hd_s.blp");
                    }
                }

                if (HDMat != 0 && tfdMapE.TryGetValue(HDMat, out var HDFDID_e))
                {
                    if (Namer.NeedsName(HDFDID_e))
                    {
                        NewFileManager.AddNewFile(HDFDID_e, "textures/bakednpctextures/creaturedisplayextra-" + row["ID"].ToString() + "_hd_e.blp");
                    }
                }
            }
        }
    }
}
