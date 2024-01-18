using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class FullScreenEffect
    {
        public static void Name()
        {
            var textureBlendSetDB = Namer.LoadDBC("TextureBlendSet");
            var textureBlendSetMap = new Dictionary<uint, List<uint>>();
            foreach (var tbsRow in textureBlendSetDB.Values)
            {
                var ID = uint.Parse(tbsRow["ID"].ToString());
                var tFDIDs = (uint[])tbsRow["TextureFileDataID"];
                foreach (var tFDID in tFDIDs)
                {
                    if (tFDID == 0)
                        continue;

                    if (!textureBlendSetMap.ContainsKey(ID))
                    {
                        textureBlendSetMap.Add(ID, new List<uint>() { tFDID });
                    }
                    else
                    {
                        textureBlendSetMap[ID].Add(tFDID);
                    }
                }
            }

            var fullScreenEffectDB = Namer.LoadDBC("FullScreenEffect");

            foreach (var fseRow in fullScreenEffectDB.Values)
            {
                var overlayFDID = uint.Parse(fseRow["OverlayTextureFileDataID"].ToString());
                if (overlayFDID != 0 && !Namer.IDToNameLookup.ContainsKey((int)overlayFDID))
                    NewFileManager.AddNewFile(overlayFDID, "spells/textures/fullscreeneffect_" + overlayFDID + ".blp");

                if (textureBlendSetMap.TryGetValue(uint.Parse(fseRow["TextureBlendSetID"].ToString()), out var tbsFDIDs))
                {
                    foreach (var tbsFDID in tbsFDIDs)
                    {
                        if (tbsFDID != 0 && !Namer.IDToNameLookup.ContainsKey((int)tbsFDID))
                            NewFileManager.AddNewFile(tbsFDID, "spells/textures/fullscreeneffect_blend_" + tbsFDID + ".blp");
                    }
                }
            }
        }
    }
}
