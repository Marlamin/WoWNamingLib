using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class TerrainMaterial
    {
        public static void Name(bool fullrun = false)
        {
            var terrainMaterialDB = Namer.LoadDBC("TerrainMaterial");
            foreach (var tmRow in terrainMaterialDB.Values)
            {
                var envMapD = int.Parse(tmRow["EnvMapDiffuseFileID"].ToString());
                if (envMapD != 0 && !Namer.IDToNameLookup.ContainsKey(envMapD))
                    NewFileManager.AddNewFile(envMapD, "tileset/terrain cube maps/tcm_" + tmRow["ID"] + "_" + envMapD + ".blp");

                var envMapS = int.Parse(tmRow["EnvMapSpecularFileID"].ToString());
                if (envMapS != 0 && !Namer.IDToNameLookup.ContainsKey(envMapS))
                    NewFileManager.AddNewFile(envMapS, "tileset/terrain cube maps/tcm_" + tmRow["ID"] + "_s_" + envMapS + ".blp");
            }

            //if (fullrun)
            //{
            //    foreach (var file in Namer.IDToNameLookup.Where(x => x.Value.StartsWith("tileset") && x.Value.Contains(x.Key.ToString()) && !x.Value.Contains("terrain cube maps")))
            //    {
            //        using (var texMS = new MemoryStream())
            //        {
            //            try
            //            {
            //                var texFile = CASCManager.GetFileByID((uint)file.Key).Result;
            //                texFile.CopyTo(texMS);
            //                texMS.Position = 0;
            //            }
            //            catch (Exception e)
            //            {
            //                continue;
            //            }

            //            using (var bin = new BinaryReader(texMS))
            //            {
            //                bin.BaseStream.Position = 8;

            //                var comp = bin.ReadByte();
            //                var alpha = bin.ReadByte();
            //                var unkAlpha = bin.ReadByte();
            //                var unkComp = bin.ReadByte();
            //                var resX = bin.ReadInt32();
            //                var resY = bin.ReadInt32();
            //                if (resX != resY)
            //                {
            //                    Console.WriteLine("Non-square texture: " + file.Key + ";" + file.Value + " (" + resX + "x" + resY + ")");
            //                    NewFileManager.AddNewFile(file.Key, "", true, true);
            //                }

            //                if (resX != 512 && resX != 1024 && resX != 2048)
            //                {
            //                    Console.WriteLine("Non-power-of-two texture: " + file.Key + ";" + file.Value + " (" + resX + "x" + resY + ")");
            //                    NewFileManager.AddNewFile(file.Key, "", true, true);
            //                }

            //                if (comp != 2)
            //                {
            //                    Console.WriteLine("Non-DXT texture: " + file.Key + ";" + file.Value + " (" + resX + "x" + resY + ")");
            //                    NewFileManager.AddNewFile(file.Key, "", true, true);
            //                }

            //                if (file.Value.EndsWith("_h.blp") || file.Value.EndsWith("_s.blp"))
            //                {
            //                    Console.WriteLine(file.Key + " Comp: " + comp + "; Alpha: " + alpha + "; UnkAlpha: " + unkAlpha + "; UnkComp: " + unkComp + "; Res: " + resX + "x" + resY);
            //                }
            //            }
            //        }
            //    }
            //}
        }
    }
}
