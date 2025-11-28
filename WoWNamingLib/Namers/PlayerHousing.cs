using System.Reflection.Metadata.Ecma335;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class PlayerHousing
    {
        public static void Name()
        {
            // TODO: Room component WMOs
            // World/WMO/Expansion11/PlayerHousing/Room/12PH_Race_Room_*.wmo
            var houseThemeDB = Namer.LoadDBC("HouseTheme");
            var houseRoomDB = Namer.LoadDBC("HouseRoom");
            var roomComponentDB = Namer.LoadDBC("RoomComponent");
            var roomComponentOptionDB = Namer.LoadDBC("RoomComponentOption");

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

            // TODO: Room Component Option

            // TODO: House Decor thumbnails and icons

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
