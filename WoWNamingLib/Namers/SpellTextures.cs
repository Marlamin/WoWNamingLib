using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class SpellTextures
    {
        public static void Name()
        {
            var spellChainEffectsDB = Namer.LoadDBC("SpellChainEffects");
            foreach(var sceRow in spellChainEffectsDB.Values)
            {
                var textureFileDataIDs = (int[])sceRow["TextureFileDataID"];
                foreach(var tFDID in textureFileDataIDs)
                {
                    if (tFDID != 0 && !Namer.IDToNameLookup.ContainsKey(tFDID))
                        NewFileManager.AddNewFile(tFDID, "spells/textures/spellchaineffect_" + sceRow["ID"].ToString() + "_" + tFDID + ".blp");
                }
            }

            var textureBlendSetDB = Namer.LoadDBC("TextureBlendSet");
            foreach (var tbsRow in textureBlendSetDB.Values)
            {
                var textureFileDataIDs = (int[])tbsRow["TextureFileDataID"];
                foreach (var tFDID in textureFileDataIDs)
                {
                    if (tFDID != 0 && !Namer.IDToNameLookup.ContainsKey(tFDID))
                        NewFileManager.AddNewFile(tFDID, "spells/textures/textureblendset_" + tbsRow["ID"].ToString() + "_" + tFDID + ".blp");
                }
            }
        }
    }
}
