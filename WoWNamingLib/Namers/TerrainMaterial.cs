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

            var liquidTypeDB = Namer.LoadDBC("LiquidType");
            var liquidTypeXTextureDB = Namer.LoadDBC("LiquidTypeXTexture");

            var liquidTypeXTextureLookup = new Dictionary<int, List<(int FileDataID, int OrderIndex)>>();
            foreach (var ltxRow in liquidTypeXTextureDB.Values)
            {
                if (!liquidTypeXTextureLookup.ContainsKey(int.Parse(ltxRow["LiquidTypeID"].ToString())))
                    liquidTypeXTextureLookup.Add(int.Parse(ltxRow["LiquidTypeID"].ToString()), new List<(int FileDataID, int OrderIndex)>());

                liquidTypeXTextureLookup[int.Parse(ltxRow["LiquidTypeID"].ToString())].Add(
                    (int.Parse(ltxRow["FileDataID"].ToString()), int.Parse(ltxRow["OrderIndex"].ToString()))
                );
            }

            foreach (var liquidTypeRow in liquidTypeDB.Values)
            {
                Console.WriteLine("Naming " + liquidTypeRow["Name"].ToString());

                var liquidTypeID = int.Parse(liquidTypeRow["ID"].ToString());
                var liquidType = liquidTypeRow["Name"].ToString();
                var liquidTextureArray = (string[])liquidTypeRow["Texture"];
                var frameCountArray = (byte[])liquidTypeRow["FrameCountTexture"];

                if (!liquidTypeXTextureLookup.TryGetValue(liquidTypeID, out var liquidTypeXTextures))
                    continue;

                var orderIndex = 0;
                for (var i = 0; i < 6; i++)
                {
                    // walk through the 6 textures
                    var liquidTexture = liquidTextureArray[i];

                    if (liquidTexture == "" || !liquidTexture.EndsWith(".blp"))
                        continue;

                    var frameCount = frameCountArray[i];
                    if(frameCount > 1)
                    {
                        if (!liquidTexture.Contains("%d"))
                        {
                            Console.WriteLine("!!! FrameCount > 1 but no %d in texture name: " + liquidTexture);
                            continue;
                        }

                        for(var j = 1; j < frameCount + 1; j++)
                        {
                            foreach(var liquidTypeXTexture in liquidTypeXTextures)
                            {
                                if (liquidTypeXTexture.OrderIndex == orderIndex && liquidTypeXTexture.FileDataID != 0)
                                {
                                    NewFileManager.AddNewFile(liquidTypeXTexture.FileDataID, liquidTexture.Replace("%d", j.ToString()));
                                    Console.WriteLine(liquidTypeXTexture.OrderIndex + ": " + liquidTypeXTexture.FileDataID + ";" + liquidTexture.Replace("%d", j.ToString()));

                                    break;
                                }
                            }
                            orderIndex++;
                        }
                    }
                    else
                    {
                        foreach (var liquidTypeXTexture in liquidTypeXTextures)
                        {
                            if (liquidTypeXTexture.OrderIndex == orderIndex && liquidTypeXTexture.FileDataID != 0)
                            {
                                NewFileManager.AddNewFile(liquidTypeXTexture.FileDataID, liquidTexture);
                                Console.WriteLine(liquidTypeXTexture.OrderIndex + ": " + liquidTypeXTexture.FileDataID + ";" + liquidTexture);

                                break;
                            }
                        }

                        orderIndex++;
                    }
                }
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
