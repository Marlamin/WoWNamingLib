using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class Collectable
    {
        struct CollectableEntry
        {
            public bool isModel;
            public uint itemAppearanceID;
            public string color;
            public string description;
            public string filename;
            public string raw;
        }

        public static void Name()
        {
            var fdidMap = new Dictionary<uint, List<CollectableEntry>>();

            // try
            // {
            var tfdMap = new Dictionary<int, List<uint>>();

            var textureFileData = Namer.LoadDBC("TextureFileData");

            foreach (var tfdRow in textureFileData.Values)
            {
                var usageType = uint.Parse(tfdRow["UsageType"].ToString());
                var materialResourcesID = int.Parse(tfdRow["MaterialResourcesID"].ToString());

                if (usageType == 0)
                {
                    if (tfdMap.ContainsKey(materialResourcesID))
                    {
                        tfdMap[materialResourcesID].Add(uint.Parse(tfdRow["FileDataID"].ToString()));
                    }
                    else
                    {
                        tfdMap.TryAdd(materialResourcesID, new List<uint>() { uint.Parse(tfdRow["FileDataID"].ToString()) });
                    }
                }
            }

            var modelFileData = Namer.LoadDBC("ModelFileData");
            var mfdMap = new Dictionary<int, List<uint>>();
            foreach (var mfdRow in modelFileData.Values)
            {
                var fileDataID = uint.Parse(mfdRow["FileDataID"].ToString());
                var modelResourcesID = int.Parse(mfdRow["ModelResourcesID"].ToString());

                if (mfdMap.ContainsKey(modelResourcesID))
                {
                    mfdMap[modelResourcesID].Add(fileDataID);
                }
                else
                {
                    mfdMap.Add(modelResourcesID, new List<uint>() { fileDataID });
                }
            }

            var itemDisplayInfo = Namer.LoadDBC("ItemDisplayInfo");
            var idiMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var idiRow in itemDisplayInfo.Values)
            {
                idiMap.Add(int.Parse(idiRow["ID"].ToString()), idiRow);
            }

            var itemAppearance = Namer.LoadDBC("ItemAppearance");
            var iaMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var iaRow in itemAppearance.Values)
            {
                iaMap.Add(int.Parse(iaRow["ID"].ToString()), iaRow);
            }

            var itemModifiedAppearance = Namer.LoadDBC("ItemModifiedAppearance");
            var imaMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var imaRow in itemModifiedAppearance.Values)
            {
                imaMap.Add(int.Parse(imaRow["ID"].ToString()), imaRow);
            }

            var cfdDB = Namer.LoadDBC("ComponentModelFileData");
            var chrRaces = Namer.LoadDBC("ChrRaces");
            var racePrefix = new Dictionary<uint, string>();
            foreach (dynamic chrRaceEntry in chrRaces.Values)
            {
                racePrefix.Add(uint.Parse(chrRaceEntry.ID.ToString()), chrRaceEntry.ClientPrefix.ToString());
            }

            var itemModelSuffixes = new Dictionary<uint, string>();

            foreach (var idiEntry in itemDisplayInfo.Values)
            {
                for (int i = 0; i < 2; i++)
                {
                    var modelRes = ((uint[])idiEntry["ModelResourcesID"])[i];
                    if (mfdMap.TryGetValue((int)modelRes, out var itemM2FDIDs))
                    {
                        foreach (var itemM2FDID in itemM2FDIDs)
                        {
                            if (itemM2FDID == 0)
                                continue;

                            if (itemModelSuffixes.ContainsKey(itemM2FDID))
                                continue;

                            if (cfdDB.TryGetValue(int.Parse(itemM2FDID.ToString()), out var cfdRow))
                            {
                                if (!racePrefix.TryGetValue(uint.Parse(cfdRow["RaceID"].ToString()), out string miniComponentRace))
                                {
                                    miniComponentRace = "xx";
                                }

                                var miniComponentGender = uint.Parse(cfdRow["GenderIndex"].ToString()) switch
                                {
                                    0 => "m",
                                    1 => "f",
                                    2 or 3 => "u",
                                    _ => throw new Exception("unknown component gender index " + cfdRow["GenderIndex"].ToString()),
                                };

                                if (cfdRow["PositionIndex"].ToString() == "-1")
                                {
                                    itemModelSuffixes.Add(itemM2FDID, "_" + miniComponentRace + "_" + miniComponentGender);
                                }
                                else if (cfdRow["PositionIndex"].ToString() == "0")
                                {
                                    itemModelSuffixes.Add(itemM2FDID, "_l");
                                }
                                else if (cfdRow["PositionIndex"].ToString() == "1")
                                {
                                    itemModelSuffixes.Add(itemM2FDID, "_r");
                                }
                            }
                        }
                    }
                }
            }

            var collectableSourceInfo = Namer.LoadDBC("CollectableSourceInfo");
            foreach (var csiRow in collectableSourceInfo.Values)
            {
                var itemModifiedAppearanceID = int.Parse(csiRow["ItemModifiedAppearanceID"].ToString());
                if (!imaMap.TryGetValue(itemModifiedAppearanceID, out var itemModifiedAppearanceRow))
                    continue;

                var itemAppearanceID = int.Parse(itemModifiedAppearanceRow["ItemAppearanceID"].ToString());
                if (!iaMap.TryGetValue(itemAppearanceID, out var itemAppearanceRow))
                    continue;

                var itemDisplayInfoID = int.Parse(itemAppearanceRow["ItemDisplayInfoID"].ToString());
                if (!idiMap.TryGetValue(itemDisplayInfoID, out var itemDisplayInfoRow))
                    continue;


                var description = csiRow["Description"].ToString().Replace("Monster - ", "");

                // Manual overrides for some lines
                var id = uint.Parse(csiRow["ID"].ToString());

                if (id == 3535)
                    description = "Item Appearance: (9048) - 10.0.0 Weapon Mace2H - MISC_2H_TUSKARFISHINGPOLE_A_01";
                if (id == 49516)
                    description = "Item Appearance: (2493) - 1.0 Weapon Axe1H - STAVE_2H_LONG_C_01 - Gold";
                if (id == 40021)
                    description = "Item Appearance: (76649) - 10.0.0 Weapon - Sword_1H_PrimalistRaid_D_01 - Earth";
                if (id == 40198)
                    description = "Item Appearance: (76650) - 10.0.0 Weapon - Sword_1H_PrimalistRaid_D_01 - Water";
                if (id == 40199)
                    description = "Item Appearance: (76647) - 10.0.0 Weapon - Sword_1H_PrimalistRaid_D_01 - Air";
                if (id == 40200)
                    description = "Item Appearance: (76651) - 10.0.0 Weapon - Sword_1H_PrimalistRaid_D_01 - Teal";
                if (id == 40805)
                    description = "Item Appearance: (76648) - 10.0.0 Weapon - Sword_1H_PrimalistRaid_D_01 - Fire";

                var splitDescription = description.Split(" - ");

                if (splitDescription.Length < 3 || splitDescription.Length > 4)
                {
                    Console.WriteLine("Error parsing: " + description);
                    continue;
                }
                var collectableEntry = new CollectableEntry()
                {
                    itemAppearanceID = uint.Parse(splitDescription[0].Replace("Item Appearance: (", "").Replace(")", "")),
                    description = splitDescription[1].Trim(),
                    filename = splitDescription[2].Trim(),
                    raw = csiRow["Description"].ToString(),
                };

                if (splitDescription.Length == 4)
                    collectableEntry.color = splitDescription[3].Trim();

                for (int i = 0; i < 2; i++)
                {
                    var mrID = ((int[])itemDisplayInfoRow["ModelResourcesID"])[i];
                    collectableEntry.isModel = true;
                    if (mfdMap.TryGetValue(mrID, out List<uint> fileDataIDs))
                    {
                        foreach (var fileDataID in fileDataIDs)
                        {
                            if (fdidMap.ContainsKey(fileDataID))
                            {
                                fdidMap[fileDataID].Add(collectableEntry);
                            }
                            else
                            {
                                fdidMap.Add(fileDataID, new List<CollectableEntry>() { collectableEntry });
                            }
                        }
                    }
                }

                for (int i = 0; i < 2; i++)
                {
                    var mmrID = ((int[])itemDisplayInfoRow["ModelMaterialResourcesID"])[i];
                    collectableEntry.isModel = false;
                    if (tfdMap.TryGetValue(mmrID, out List<uint> fileDataIDs))
                    {
                        foreach (var fileDataID in fileDataIDs)
                        {
                            if (fdidMap.ContainsKey(fileDataID))
                            {
                                fdidMap[fileDataID].Add(collectableEntry);
                            }
                            else
                            {
                                fdidMap.Add(fileDataID, new List<CollectableEntry>() { collectableEntry });
                            }
                        }
                    }
                }
            }
            // }
            // catch (Exception e)
            // {
            //      Console.WriteLine("Error loading DB2s: " + e.Message);
            // }

            var output = new List<string>();
            foreach (var collectableList in fdidMap.OrderBy(x => x.Key))
            {
                var filedataid = collectableList.Key;
                var currentName = "";
                if (Namer.IDToNameLookup.TryGetValue((int)filedataid, out var name))
                {
                    currentName = Path.GetFileNameWithoutExtension(name);
                    output.Add(filedataid + ";" + name);
                }
                else
                {
                    currentName = filedataid.ToString();
                    output.Add(filedataid + ";unnamed/" + filedataid);
                }

                var uniqueList = collectableList.Value.DistinctBy(x => x.raw);

                if (uniqueList.All(x => x.filename.ToLower() == uniqueList.First().filename.ToLower()))
                {
                    var first = uniqueList.First();
                    var filenameMatches = false;

                    if (first.filename.ToLower() == currentName.ToLower())
                        filenameMatches = true;

                    if (!first.isModel && first.color != null && first.filename.ToLower() == currentName.ToLower() + first.color.ToLower())
                        filenameMatches = true;

                    if (first.isModel && itemModelSuffixes.ContainsKey(filedataid) && first.filename.ToLower() == currentName.ToLower() + itemModelSuffixes[filedataid].ToLower())
                        filenameMatches = true;

                    if (filenameMatches)
                        output.Add("\t \u2705 Filename matches");
                    else
                    {
                        output.Add("\t \u274C Filename does not match");
                        var folder = "";

                        switch (first.filename.Split("_")[0].ToLower().Trim())
                        {
                            case "armor":
                            case "mail":
                            case "cloth":
                            case "leather":
                            case "chest":
                            case "pant":
                            case "plate":
                            case "belt":
                            case "hand":
                            case "collections":
                                folder = "item/objectcomponents/collections";
                                break;
                            case "robe":
                            case "cape":
                                folder = "item/objectcomponents/cape";
                                break;
                            case "helm":
                            case "helmet":
                            case "blindfold":
                            case "circlet":
                            case "glasses":
                            case "sunglasses":
                            case "goggles":
                                folder = "item/objectcomponents/head";
                                break;
                            case "lshoulder":
                            case "rshoulder":
                            case "shoulder":
                            case "shoulders":
                                folder = "item/objectcomponents/shoulder";
                                break;
                            case "buckler":
                            case "shield":
                                folder = "item/objectcomponents/shield";
                                break;
                            case "buckle":
                                folder = "item/objectcomponents/waist";
                                break;
                            case "axe":
                            case "bow":
                            case "crossbow":
                            case "glaive":
                            case "firearm":
                            case "knife":
                            case "mace":
                            case "polearm":
                            case "offhand":
                            case "stave":
                            case "staff":
                            case "sword":
                            case "thrown":
                            case "hammer":
                            case "spear":
                            case "wand":
                            case "misc":
                            case "enchanting":
                                folder = "item/objectcomponents/weapon";
                                break;
                            case "arrow":
                            case "bullet":
                                folder = "item/objectcomponents/ammo";
                                break;
                            default:
                                Console.WriteLine("Unknown prefix: " + first.filename.Split("_")[0].ToLower());
                                break;
                        }


                        if (folder == "")
                            continue;

                        if (!first.isModel)
                        {
                            if (currentName.Contains(filedataid.ToString()))
                            {
                                if (first.color != null)
                                {
                                    if (Namer.IDToNameLookup.ContainsKey((int)filedataid) && Namer.placeholderNames.Contains((int)filedataid))
                                    {
                                        //NewFileManager.AddNewFile(filedataid, folder + "/" + currentName.Replace(filedataid.ToString(), first.color.Replace(" ", "").Replace("(", "").Replace(")", "") + ".blp"), true, true);
                                    }
                                    else if(Namer.IDToNameLookup.ContainsKey((int)filedataid))
                                    {
                                        NewFileManager.AddNewFile(filedataid, Namer.IDToNameLookup[(int)filedataid].Replace(filedataid.ToString(), first.color.Replace(" ", "").Replace("(", "").Replace(")", "")), true, true);
                                    }
                                }

                                //else
                                //    NewFileManager.AddNewFile(filedataid, folder + "/" + first.filename.Replace(" ", "") + ".blp", true);
                            }
                        }
                        else
                        {
                            if (Namer.placeholderNames.Contains((int)filedataid))
                            {
                                if (itemModelSuffixes.ContainsKey(filedataid))
                                    NewFileManager.AddNewFile(filedataid, folder + "/" + first.filename.Replace(" ", "") + itemModelSuffixes[filedataid] + ".m2", true, true);
                                else
                                    NewFileManager.AddNewFile(filedataid, folder + "/" + first.filename.Replace(" ", "") + ".m2", true, true);
                            }
                        }
                    }
                }

                //foreach (var collectable in uniqueList)
                //{
                //    var outputLine = "";

                //    if (collectable.isModel)
                //    {
                //        outputLine += "\t ItemAppearanceID: " + collectable.itemAppearanceID + ", Description: " + collectable.description + ", Filename: " + collectable.filename;
                //    }
                //    else
                //    {
                //        outputLine += "\t ItemAppearanceID: " + collectable.itemAppearanceID + ", Description: " + collectable.description + ", Filename: " + collectable.filename;
                //        if (collectable.color != null)
                //            outputLine += ", Color: " + collectable.color;
                //    }

                //    output.Add(outputLine);
                //}
            }
            //File.WriteAllLines("collectables_dump.txt", output, System.Text.Encoding.UTF8);
        }
    }
}
