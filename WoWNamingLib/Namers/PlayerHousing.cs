using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class PlayerHousing
    {
        public static void Name()
        {
            // Room component WMOs
            // World/WMO/Expansion11/PlayerHousing/Room/12PH_Race_Room_*.wmo

            var exteriorComponentTypeDB = Namer.LoadDBC("ExteriorComponentType");
            var typeToName = new Dictionary<int, string>();
            foreach (var row in exteriorComponentTypeDB.Values)
            {
                var type = (int)row["ID"];
                var name = row["Name_lang"].ToString()!;
                typeToName[type] = name;
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

                    if (!typeToName.TryGetValue(type, out var typeName))
                        typeName = "UnknownType";

                    var wmoName = wmoRow["Name_lang"].ToString()!;
                    var nameClean = wmoName.Replace("House", "").Replace(" ", "");

                    typeName = typeName.Replace(" ", "");
                    var filename = $"World/WMO/Expansion11/PlayerHousing/Exterior/12PH_Exterior_{nameClean}_{typeName}_{fdid}.wmo";
                    NewFileManager.AddNewFile(fdid, filename, true);
                }
            }

        }
    }
}
