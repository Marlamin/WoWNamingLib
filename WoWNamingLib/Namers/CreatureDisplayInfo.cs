using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class CreatureDisplayInfo
    {
        public static void Name()
        {
            var cmdDB = Namer.LoadDBC("CreatureModelData");
            var cmdIDToFDIDMap = new Dictionary<uint, int>();

            foreach (var cmdEntry in cmdDB.Values)
            {
                var mFDID = int.Parse(cmdEntry["FileDataID"].ToString());
                cmdIDToFDIDMap.Add(uint.Parse(cmdEntry["ID"].ToString()), mFDID);
            }

            var creatureDisplayInfoDB = Namer.LoadDBC("CreatureDisplayInfo");
            foreach (var cdiRow in creatureDisplayInfoDB.Values)
            {
                if (!cmdIDToFDIDMap.TryGetValue(uint.Parse(cdiRow["ModelID"].ToString()), out var modelFileDataID))
                {
                    Console.WriteLine("!!! Nonexisting FDID for CDI " + cdiRow["ID"].ToString() + " CMD " + cdiRow["ModelID"].ToString());
                    continue;
                }

                if (!Namer.IDToNameLookup.TryGetValue(modelFileDataID, out var modelFileName))
                {
                    Console.WriteLine("!!! Unnamed FDID " + modelFileDataID + " for CDI " + cdiRow["ID"].ToString() + " CMD " + cdiRow["ModelID"].ToString());
                    continue;
                }

                var textureVariationFDIDs = (int[])cdiRow["TextureVariationFileDataID"];
                foreach (var textureVariationFDID in textureVariationFDIDs)
                {
                    if (
                        textureVariationFDID != 0 &&
                            (
                                !Namer.IDToNameLookup.ContainsKey(textureVariationFDID) ||
                                Namer.IDToNameLookup[textureVariationFDID] == "creature/" + Path.GetFileNameWithoutExtension(modelFileName) + "/" + Path.GetFileNameWithoutExtension(modelFileName) + "_" + textureVariationFDID + ".blp"
                            )
                        )
                        NewFileManager.AddNewFile(textureVariationFDID, Path.GetDirectoryName(modelFileName) + "/" + Path.GetFileNameWithoutExtension(modelFileName) + "_" + textureVariationFDID + ".blp", true);
                }
            }
        }
    }
}
