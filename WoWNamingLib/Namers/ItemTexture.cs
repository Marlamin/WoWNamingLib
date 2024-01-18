using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class ItemTexture
    {
        public static void Name()
        {
            try
            {
                // TODO: Item texture naming from ItemDisplayInfoModelMatRes 

                var tfdMap = new Dictionary<int, List<int>>();

                var textureFileData = Namer.LoadDBC("TextureFileData");

                foreach (var tfdRow in textureFileData.Values)
                {
                    var usageType = uint.Parse(tfdRow["UsageType"].ToString());
                    var materialResourcesID = int.Parse(tfdRow["MaterialResourcesID"].ToString());

                    if (usageType == 0)
                    {
                        if (tfdMap.ContainsKey(materialResourcesID))
                        {
                            tfdMap[materialResourcesID].Add(int.Parse(tfdRow["FileDataID"].ToString()));
                        }
                        else
                        {
                            tfdMap.TryAdd(materialResourcesID, new List<int>() { int.Parse(tfdRow["FileDataID"].ToString()) });
                        }
                    }
                }

                var modelFileData = Namer.LoadDBC("ModelFileData");
                var mfdMap = new Dictionary<uint, List<int>>();
                foreach (var mfdRow in modelFileData.Values)
                {
                    var fileDataID = int.Parse(mfdRow["FileDataID"].ToString());
                    var modelResourcesID = uint.Parse(mfdRow["ModelResourcesID"].ToString());

                    if (mfdMap.ContainsKey(modelResourcesID))
                    {
                        mfdMap[modelResourcesID].Add(fileDataID);
                    }
                    else
                    {
                        mfdMap.Add(modelResourcesID, new List<int>() { fileDataID });
                    }
                }

                var itemDisplayInfo = Namer.LoadDBC("ItemDisplayInfo");
                var idiMap = new Dictionary<int, DBCD.DBCDRow>();
                foreach (var idiRow in itemDisplayInfo.Values)
                {
                    idiMap.Add(int.Parse(idiRow["ID"].ToString()), idiRow);
                }

                var itemAppearance = Namer.LoadDBC("ItemAppearance");
                var chrRaces = Namer.LoadDBC("ChrRaces");

                var stupidItemExtensions = new List<string>();
                foreach (var raceRow in chrRaces.Values)
                {
                    var clientPrefix = raceRow["ClientPrefix"].ToString().ToLower();
                    stupidItemExtensions.Add(clientPrefix + "m.m2");
                    stupidItemExtensions.Add(clientPrefix + "_m.m2");
                    stupidItemExtensions.Add(clientPrefix + "f.m2");
                    stupidItemExtensions.Add(clientPrefix + "_f.m2");
                }

                foreach (dynamic idiRow in itemDisplayInfo.Values)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var modelResourcesID = uint.Parse(idiRow.ModelResourcesID[i].ToString());
                        var modelMaterialResourcesID = int.Parse(idiRow.ModelMaterialResourcesID[i].ToString());

                        if (modelMaterialResourcesID == 0)
                            continue;

                        if (tfdMap.TryGetValue(modelMaterialResourcesID, out List<int> fileDataIDs))
                        {
                            foreach (var fileDataID in fileDataIDs)
                            {
                                if (Namer.IDToNameLookup.ContainsKey(fileDataID))
                                    continue;

                                Console.WriteLine(fileDataID);

                                if (mfdMap.TryGetValue(modelResourcesID, out List<int> M2FileDataIDs))
                                {
                                    if (Namer.IDToNameLookup.TryGetValue(M2FileDataIDs[0], out var filename))
                                    {
                                        var textureFilename = filename;
                                        foreach (var stupidExtension in stupidItemExtensions)
                                        {
                                            textureFilename = textureFilename.Replace(stupidExtension, fileDataID + ".blp");
                                        }

                                        if (textureFilename.Contains(".m2"))
                                        {
                                            textureFilename = textureFilename.Replace(".m2", "_" + fileDataID + ".blp");
                                        }

                                        NewFileManager.AddNewFile(fileDataID, textureFilename);
                                    }
                                    else
                                    {
                                        uint iconFDID = 0;

                                        foreach (dynamic iaRow in itemAppearance.Values)
                                        {
                                            if (uint.Parse(iaRow["ItemDisplayInfoID"].ToString()) == uint.Parse(idiRow.ID.ToString()))
                                            {
                                                iconFDID = uint.Parse(iaRow.DefaultIconFileDataID.ToString());
                                            }
                                        }

                                        if (iconFDID != 0 && Namer.IDToNameLookup.TryGetValue((int)iconFDID, out string iconFileName))
                                        {
                                            var cleanedName = iconFileName.ToLower().Replace("\\", "/").Replace("interface/icons/inv_", "").Replace(".blp", "");
                                            Console.WriteLine("!!! Item texture " + fileDataID + " belongs to unnamed M2 " + M2FileDataIDs[0] + ", but has an icon with name " + cleanedName + ", skipping naming..");
                                        }
                                        else
                                        {
                                            Console.WriteLine("!!! Item texture " + fileDataID + " belongs to unnamed M2 " + M2FileDataIDs[0] + ", skipping naming..");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var itemDisplayInfoMaterialRes = Namer.LoadDBC("ItemDisplayInfoMaterialRes");
                var itemDisplayInfoModelMatRes = Namer.LoadDBC("ItemDisplayInfoModelMatRes");
                var componentTextureFileData = Namer.LoadDBC("ComponentTextureFileData");
                var ctfdMap = new Dictionary<int, int>();
                foreach (var ctfdRow in componentTextureFileData.Values)
                {
                    ctfdMap.Add(int.Parse(ctfdRow["ID"].ToString()), int.Parse(ctfdRow["GenderIndex"].ToString()));
                }

                foreach (var idimrRow in itemDisplayInfoMaterialRes.Values)
                {
                    var textureFolder = "";
                    var miniComponent = "";
                    var miniComponentGender = "u";

                    switch (int.Parse(idimrRow["ComponentSection"].ToString()))
                    {
                        case 0: // ArmUpper
                            textureFolder = "item/texturecomponents/armuppertexture";
                            miniComponent = "au_";
                            break;
                        case 1: // ArmLower
                            textureFolder = "item/texturecomponents/armlowertexture";
                            miniComponent = "al_";
                            break;
                        case 2: // Hand
                            textureFolder = "item/texturecomponents/handtexture";
                            miniComponent = "ha_";
                            break;
                        case 3: // TorsoUpper
                            textureFolder = "item/texturecomponents/torsouppertexture";
                            miniComponent = "tu_";
                            break;
                        case 4: // TorsoLower
                            textureFolder = "item/texturecomponents/torsolowertexture";
                            miniComponent = "tl_";
                            break;
                        case 5: // LegUpper
                            textureFolder = "item/texturecomponents/leguppertexture";
                            miniComponent = "lu_";
                            break;
                        case 6: // LegLower
                            textureFolder = "item/texturecomponents/leglowertexture";
                            miniComponent = "ll_";
                            break;
                        case 7: // Foot
                            textureFolder = "item/texturecomponents/foottexture";
                            miniComponent = "fo_";
                            break;
                        case 8: // Accessory
                            textureFolder = "item/texturecomponents/accessorytexture";
                            miniComponent = "pr_";
                            break;
                        default:
                            throw new Exception("Unhandled component type " + idimrRow["ComponentSection"].ToString());
                    }

                    if (tfdMap.TryGetValue(int.Parse(idimrRow["MaterialResourcesID"].ToString()), out List<int> fileDataIDs))
                    {
                        foreach (var fileDataID in fileDataIDs)
                        {
                            if (!Namer.IDToNameLookup.ContainsKey(fileDataID))
                            {
                                if (ctfdMap.TryGetValue((int)fileDataID, out var genderIndex))
                                {
                                    switch (genderIndex)
                                    {
                                        case 0:
                                            miniComponentGender = "m";
                                            break;
                                        case 1:
                                            miniComponentGender = "f";
                                            break;
                                        case 2:
                                        case 3:
                                            miniComponentGender = "u";
                                            break;
                                        default:
                                            throw new Exception("unknown component gender index " + genderIndex);
                                    }
                                }
                                else
                                {
                                    miniComponentGender = "u";
                                }

                                var named = false;

                                if (idiMap.TryGetValue(int.Parse(idimrRow["ItemDisplayInfoID"].ToString()), out var idiRow))
                                {
                                    for (int i = 0; i < 2; i++)
                                    {
                                        var modelResourcesID = ((uint[])idiRow["ModelResourcesID"])[i];

                                        if (modelResourcesID == 0)
                                            continue;

                                        if (mfdMap.TryGetValue(modelResourcesID, out List<int> M2FileDataIDs))
                                        {
                                            for (int j = 0; j < M2FileDataIDs.Count; j++)
                                            {
                                                if (Namer.IDToNameLookup.TryGetValue(M2FileDataIDs[j], out var filename))
                                                {
                                                    var textureFilename = Path.GetFileName(filename);
                                                    foreach (var stupidExtension in stupidItemExtensions)
                                                    {
                                                        textureFilename = textureFilename.Replace(stupidExtension, "");
                                                    }

                                                    if (textureFilename.Contains(".m2"))
                                                    {
                                                        textureFilename = textureFilename.Replace(".m2", "");
                                                    }

                                                    var actualFilename = textureFolder + "/" + textureFilename + "_" + miniComponent + miniComponentGender + "_" + fileDataID + ".blp";

                                                    actualFilename = actualFilename.Replace("__", "_");

                                                    NewFileManager.AddNewFile(fileDataID, actualFilename);
                                                    named = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!named)
                                {
                                    foreach (var iaRow in itemAppearance.Values)
                                    {
                                        if (int.Parse(iaRow["ItemDisplayInfoID"].ToString()) != int.Parse(idimrRow["ItemDisplayInfoID"].ToString()))
                                            continue;

                                        if (Namer.IDToNameLookup.TryGetValue(int.Parse(iaRow["DefaultIconFileDataID"].ToString()), out var iconFilename))
                                        {
                                            if (iconFilename.ToLower().Contains("questionmark") || iconFilename.ToLower() == "interface/icons/temp.blp")
                                                continue;

                                            iconFilename = iconFilename.ToLower().Replace("\\", "/").Replace("interface/icons/inv_", "").Replace(".blp", "");
                                            iconFilename = textureFolder + "/" + iconFilename + "_" + miniComponent + miniComponentGender + "_" + fileDataID + ".blp";
                                            NewFileManager.AddNewFile(fileDataID, iconFilename);
                                            named = true;
                                        }
                                    }
                                }

                                if (!named)
                                {
                                    Console.WriteLine("Didn't find a name for item texture FDID " + fileDataID);
                                }
                            }
                        }
                    }
                }

                foreach (var idimmRow in itemDisplayInfoModelMatRes.Values)
                {
                    var miniComponent = "";
                    var miniComponentGender = "u";
                    var materialResourcesID = (int)idimmRow["MaterialResourcesID"];
                    if (tfdMap.TryGetValue(materialResourcesID, out List<int> fileDataIDs))
                    {
                        foreach (var fileDataID in fileDataIDs)
                        {
                            if (!Namer.IDToNameLookup.ContainsKey(fileDataID))
                            {
                                if (ctfdMap.TryGetValue((int)fileDataID, out var genderIndex))
                                {
                                    switch (genderIndex)
                                    {
                                        case 0:
                                            miniComponentGender = "m";
                                            break;
                                        case 1:
                                            miniComponentGender = "f";
                                            break;
                                        case 2:
                                        case 3:
                                            miniComponentGender = "u";
                                            break;
                                        default:
                                            throw new Exception("unknown component gender index " + genderIndex);
                                    }
                                }
                                else
                                {
                                    miniComponentGender = "u";
                                }

                                var named = false;

                                if (idiMap.TryGetValue(int.Parse(idimmRow["ItemDisplayInfoID"].ToString()), out var idiRow))
                                {
                                    for (int i = 0; i < 2; i++)
                                    {
                                        var modelResourcesID = ((uint[])idiRow["ModelResourcesID"])[i];

                                        if (modelResourcesID == 0)
                                            continue;

                                        if (mfdMap.TryGetValue(modelResourcesID, out List<int> M2FileDataIDs))
                                        {
                                            for (int j = 0; j < M2FileDataIDs.Count; j++)
                                            {
                                                if (Namer.IDToNameLookup.TryGetValue(M2FileDataIDs[j], out var filename))
                                                {
                                                    var textureFolder = Path.GetDirectoryName(filename);
                                                    var textureFilename = Path.GetFileName(filename);
                                                    foreach (var stupidExtension in stupidItemExtensions)
                                                    {
                                                        textureFilename = textureFilename.Replace(stupidExtension, "");
                                                    }

                                                    if (textureFilename.Contains(".m2"))
                                                    {
                                                        textureFilename = textureFilename.Replace(".m2", "");
                                                    }

                                                    var actualFilename = textureFolder.Replace("\\", "/") + "/" + textureFilename + "_" + fileDataID + ".blp";

                                                    actualFilename = actualFilename.Replace("__", "_");

                                                    NewFileManager.AddNewFile(fileDataID, actualFilename);
                                                    named = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                //if (!named)
                                //{
                                //    foreach (var iaRow in itemAppearance.Values)
                                //    {
                                //        if (int.Parse(iaRow["ItemDisplayInfoID"].ToString()) != int.Parse(idimmRow["ItemDisplayInfoID"].ToString()))
                                //            continue;

                                //        if (Namer.IDToNameLookup.TryGetValue(uint.Parse(iaRow["DefaultIconFileDataID"].ToString()), out var iconFilename))
                                //        {
                                //            iconFilename = iconFilename.ToLower().Replace("\\", "/").Replace("interface/icons/inv_", "").Replace(".blp", "");
                                //            iconFilename = textureFolder + "/" + iconFilename + "_" + miniComponent + miniComponentGender + "_" + fileDataID + ".blp";
                                //            NewFileManager.AddNewFile(fileDataID, iconFilename);
                                //            named = true;
                                //        }
                                //    }
                                //}

                                if (!named)
                                {
                                    Console.WriteLine("Didn't find a name for item texture FDID " + fileDataID);
                                }
                            }
                        }
                    }
                }

                foreach (var idiRow in itemDisplayInfo.Values)
                {
                    var itemDisplayInfoID = int.Parse(idiRow["ID"].ToString());
                    for (int i = 0; i < 2; i++)
                    {
                        var mrID = ((int[])idiRow["ModelMaterialResourcesID"])[i];
                        if (tfdMap.TryGetValue(mrID, out List<int> fileDataIDs))
                        {
                            foreach (var fileDataID in fileDataIDs)
                            {
                                if (!Namer.IDToNameLookup.ContainsKey(fileDataID))
                                {
                                    foreach (var iaRow in itemAppearance.Values)
                                    {
                                        if (int.Parse(iaRow["ItemDisplayInfoID"].ToString()) != itemDisplayInfoID)
                                            continue;

                                        if (Namer.IDToNameLookup.TryGetValue(int.Parse(iaRow["DefaultIconFileDataID"].ToString()), out var iconFilename))
                                        {
                                            iconFilename = iconFilename.ToLower().Replace("\\", "/").Replace("interface/icons/inv_", "").Replace(".blp", "");
                                            if (iconFilename.Substring(0, 4) == "cape")
                                            {
                                                iconFilename = "item/objectcomponents/cape/" + iconFilename + "_" + fileDataID + ".blp";
                                                NewFileManager.AddNewFile(fileDataID, iconFilename);
                                            }
                                            else if (iconFilename.EndsWith("_cape"))
                                            {
                                                iconFilename = "item/objectcomponents/cape/cape_" + iconFilename.Substring(0, iconFilename.Length - 5) + "_" + fileDataID + ".blp";
                                                NewFileManager.AddNewFile(fileDataID, iconFilename);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during item texture naming: " + e.Message);
            }
        }
    }
}
