using System.Reflection.Metadata.Ecma335;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class PlayerHousing
    {
        public static void Name()
        {
            var houseThemeDB = Namer.LoadDBC("HouseTheme");
            var houseRoomDB = Namer.LoadDBC("HouseRoom");
            var roomComponentDB = Namer.LoadDBC("RoomComponent");
            var roomComponentOptionDB = Namer.LoadDBC("RoomComponentOption");
            var roomComponentTextureDB = Namer.LoadDBC("RoomComponentTexture");
            var houseDecorDB = Namer.LoadDBC("HouseDecor");

            var themeToName = new Dictionary<int, string>();
            foreach(var houseThemeRec in houseThemeDB.Values)
                themeToName.Add(houseThemeRec.ID, (string)houseThemeRec["Name_lang"]);

            var typeToName = new Dictionary<int, string>();
            typeToName[1] = "Wall";
            typeToName[2] = "Floor";
            typeToName[3] = "Ceiling";
            typeToName[4] = "Stairs";
            typeToName[5] = "Pillar";
            typeToName[6] = "DoorwayWall";
            typeToName[7] = "Doorway";

            var roomComponentDictionary = new Dictionary<int, List<(string cleanName, byte type, byte rcoType, int subType, string themeName)>>();
            foreach (var houseRoomRecord in houseRoomDB.Values)
            {
                var cleanName = houseRoomRecord["Name_lang"].ToString()!.Replace(" ", "_").Replace("(", "").Replace(")", "");
                var roomWMODataID = (int)houseRoomRecord["RoomWmoDataID"];

                foreach(var rcEntry in roomComponentDB.Values)
                {
                    var componentRoomWMODataID = (int)rcEntry["RoomWmoDataID"];
                    if (roomWMODataID != componentRoomWMODataID)
                        continue;

                    var type = (byte)rcEntry["Type"];

                    var meshStyleFilterID = (int)rcEntry["MeshStyleFilterID"];

                    // ModelFileDataID here is also in RoomComponentOption. Do we ignore it here?
                    // RoomComponentOption is selected by MeshStyleFilterID and Type

                    foreach (var rcoEntry in roomComponentOptionDB.Values)
                    {
                        // For RoomComponent::Type 4 (Stairs) there are MeshStyleFilters for 28 and 52. In RoomComponentOption SubType becomes 1-4 for those filters which matches HousingRoomComponentStairType.

                        var rcoModelFDID = (int)rcoEntry["ModelFileDataID"];
                        var rcoMeshStyleFilterID = (int)rcoEntry["MeshStyleFilterID"];
                        if (meshStyleFilterID != rcoMeshStyleFilterID)
                            continue;

                        var rcoType = (byte)rcoEntry["Type"];
                        var themeName = themeToName[(int)rcoEntry["Theme"]];

                        if (!roomComponentDictionary.ContainsKey(rcoModelFDID))
                            roomComponentDictionary[rcoModelFDID] = [];

                        roomComponentDictionary[rcoModelFDID].Add((cleanName, type, rcoType, (int)rcoEntry["SubType"], themeName.Replace("'", "")));
                    }
                }
            }

            foreach(var roomComponent in roomComponentDictionary)
            {
                var fdid = roomComponent.Key;
                if (!Namer.IDToNameLookup.ContainsKey(fdid) || Namer.placeholderNames.Contains(fdid))
                {
                    var modelIndex = 1;
                    var basename = "";
                    if (roomComponent.Value.Select(v => v.cleanName).Distinct().Count() == 1)
                    {
                        var name = roomComponent.Value[0];
                        basename = $"World/WMO/Expansion11/PlayerHousing/Room/12PH_{name.cleanName}_{typeToName[name.type]}_{name.rcoType}_{name.subType}_{name.themeName}";
                    }
                    else
                    {
                        // If all themes are the same, use that in the name
                        var distinctThemes = roomComponent.Value.Select(v => v.themeName).Distinct().ToList();
                        var name = roomComponent.Value[0];

                        if (distinctThemes.Count == 0)
                            basename = $"World/WMO/Expansion11/PlayerHousing/Room/12PH_Generic_{typeToName[name.type]}_{name.rcoType}_{name.subType}_{name.themeName}";
                        else
                            basename = $"World/WMO/Expansion11/PlayerHousing/Room/12PH_Generic_{typeToName[name.type]}_{name.rcoType}_{name.subType}_MultiTheme";
                    }

                    while (Namer.IDToNameLookup.ContainsValue(basename + modelIndex.ToString().PadLeft(2, '0') + ".wmo"))
                        modelIndex++;

                    NewFileManager.AddNewFile(fdid, basename + modelIndex.ToString().PadLeft(2, '0') + ".wmo", true);
                }
            }

            foreach(var rcOptionRow in roomComponentOptionDB.Values)
            {
                var fdid = (int)rcOptionRow["ModelFileDataID"];
                if (!Namer.IDToNameLookup.ContainsKey(fdid) || Namer.placeholderNames.Contains(fdid))
                {
                    if(!themeToName.TryGetValue((int)rcOptionRow["Theme"], out var themeName))
                        themeName = "Unknown";

                    themeName = themeName.Replace("'", "");

                    var rcoType = (byte)rcOptionRow["Type"];
                    var rcoTypeDesc = "";
                    switch (rcoType)
                    {
                        case 0:
                            rcoTypeDesc = "Cosmetic";
                            break;
                        case 1:
                            rcoTypeDesc = "DoorwayWall";
                            break;
                        case 2:
                            rcoTypeDesc = "Doorway"; 
                            break;
                        default:
                            rcoTypeDesc = "Type" + rcoType.ToString();
                            break;
                    }
                    var meshStyleFilterID = (int)rcOptionRow["MeshStyleFilterID"];
                    var subType = (int)rcOptionRow["SubType"];
                    var basename = $"World/WMO/Expansion11/PlayerHousing/Room/12PH_Option_{rcoTypeDesc}_{subType}_{themeName}";
                    var modelIndex = 1;
                    while (Namer.IDToNameLookup.ContainsValue(basename + modelIndex.ToString().PadLeft(2, '0') + ".wmo"))
                        modelIndex++;

                    NewFileManager.AddNewFile(fdid, basename + modelIndex.ToString().PadLeft(2, '0') + ".wmo", true);
                }
            }

            var itemDB = Namer.LoadDBC("Item");

            foreach (var houseDecorRow in houseDecorDB.Values)
            {
                var modelFDID = (int)houseDecorRow["ModelFileDataID"];
                var itemID = (int)houseDecorRow["ItemID"];
                var iconFDID = 0;
                var iconFilename = "";

                if (itemID != 0 && itemDB.TryGetValue(itemID, out var itemRow))
                {
                    iconFDID = (int)itemRow["IconFileDataID"];
                    if (!Namer.IDToNameLookup.TryGetValue(iconFDID, out iconFilename) && Namer.placeholderNames.Contains(iconFDID))
                    {
                        var iconName = BattleNetAPI.GetBaseNameForMediaFDID((uint)iconFDID);
                        if(!string.IsNullOrEmpty(iconName))
                        {
                            iconFilename = "Housing/Icons/" + iconName + ".blp";
                            NewFileManager.AddNewFile(iconFDID, iconFilename, true);
                        }
                        else
                        {
                            Console.WriteLine("No official icon name found for FDID " + iconFDID + ", falling back to model name for now");
                            if(Namer.IDToNameLookup.TryGetValue(modelFDID, out var modelName))
                            {
                                // use capitals for inv_ and embed fdid to signify unofficial name
                                iconFilename = "Housing/Icons/INV_" + Path.GetFileNameWithoutExtension(modelName) + "_" + iconFDID + ".blp";
                                NewFileManager.AddNewFile(iconFDID, iconFilename, true);
                            }
                        }
                    }
                }

                if (iconFDID != 975745 && !string.IsNullOrEmpty(iconFilename))
                {
                    // Hey guess what, we can name the thumbnail based on the icon!
                    var thumbnailFDID = (int)houseDecorRow["ThumbnailFileDataID"];
                    if (!Namer.IDToNameLookup.ContainsKey(thumbnailFDID) || Namer.placeholderNames.Contains(thumbnailFDID))
                    {
                        var thumbnailFilename = iconFilename.Replace("Icons", "Thumbnails").Replace("INV_", "", StringComparison.OrdinalIgnoreCase).Replace(iconFDID.ToString(), thumbnailFDID.ToString());
                        NewFileManager.AddNewFile(thumbnailFDID, thumbnailFilename, true);
                    }
                }

                if (!Namer.IDToNameLookup.ContainsKey(modelFDID) || Namer.placeholderNames.Contains(modelFDID))
                {
                    var modelType = (byte)houseDecorRow["ModelType"];
                    if (modelType == 1)
                    {
                        // letting this part be handled by the model namer, see logic there for more
                    }
                    else if (modelType == 2)
                    {
                        // WMO
                        var folder = "World/WMO/Expansion11/PlayerHousing/Decor/";
                        var baseName = "12PH_Decor_" + modelFDID + ".wmo";
                        var name = houseDecorRow["Name_lang"].ToString();
                        if (name.Contains("12PH") && name.Contains("wmo"))
                        {
                            var startOfName = name.IndexOf("12PH");
                            var endOfName = name.LastIndexOf(".wmo");
                            if (startOfName != -1 && endOfName != -1 && endOfName > startOfName)
                            {
                                var namePart = name.Substring(startOfName, endOfName - startOfName);
                                baseName = namePart;
                            }
                        }

                        NewFileManager.AddNewFile(modelFDID, folder + baseName, true);
                    }
                }
            }

            foreach(var rctRow in roomComponentTextureDB.Values)
            {
                var fdid = (int)rctRow["TextureFileDataID"];
                if (!Namer.IDToNameLookup.ContainsKey(fdid) || Namer.placeholderNames.Contains(fdid))
                {
                    if (!themeToName.TryGetValue((int)rctRow["HouseThemeID"], out var themeName))
                        themeName = "Unknown";

                    themeName = themeName.Replace("'", "");
                    
                    var type = (int)rctRow["RoomComponentType"];
                    var typeDesc = "";
                    switch (type)
                    {
                        case 1:
                            typeDesc = "Wall";
                            break;
                        case 2:
                            typeDesc = "Floor";
                            break;
                        case 3:
                            typeDesc = "Ceiling";
                            break;
                        default:
                            typeDesc = "Type" + type.ToString();
                            break;

                    }
                    var basename = $"World/Texture/Expansion11/PlayerHousing/Room/12PH_ComponentTexture_{typeDesc}_{themeName}_{fdid}.blp";
                    NewFileManager.AddNewFile(fdid, basename, true);
                }
            }

            // Exterior components
            var exteriorComponentTypeDB = Namer.LoadDBC("ExteriorComponentType");
            var extTypeToName = new Dictionary<int, string>();
            foreach (var row in exteriorComponentTypeDB.Values)
            {
                var type = (int)row["ID"];
                var name = row["Name_lang"].ToString()!;
                extTypeToName[type] = name;
            }

            var exteriorComponentDB = Namer.LoadDBC("ExteriorComponent");
            var houseExteriorWmoDataDB = Namer.LoadDBC("HouseExteriorWmoData");

            foreach (var row in exteriorComponentDB.Values)
            {
                var fdid = (int)row["ModelFileDataID"];
                if (!Namer.IDToNameLookup.ContainsKey(fdid) || Namer.placeholderNames.Contains(fdid))
                {
                    var type = (byte)row["Type"];
                    var wmoDataID = (int)row["HouseExteriorWmoDataID"];

                    if (!houseExteriorWmoDataDB.TryGetValue(wmoDataID, out var wmoRow))
                        continue;

                    if (!extTypeToName.TryGetValue(type, out var typeName))
                        typeName = "UnknownType";

                    var wmoName = wmoRow["Name_lang"].ToString()!;
                    var nameClean = wmoName.Replace("House", "").Replace(" ", "");

                    typeName = typeName.Replace(" ", "");
                    var basename = $"World/WMO/Expansion11/PlayerHousing/Exterior/12PH_Exterior_{nameClean}_{typeName}";
                    var modelIndex = 1;
                    while (Namer.IDToNameLookup.ContainsValue(basename + modelIndex.ToString().PadLeft(2, '0') + ".wmo"))
                        modelIndex++;

                    NewFileManager.AddNewFile(fdid, basename + modelIndex.ToString().PadLeft(2, '0') + ".wmo", true);
                }
            }
        }
    }
}
