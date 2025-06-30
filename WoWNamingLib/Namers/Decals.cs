using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Decals
    {
        public static void Name()
        {
            var decalPropertiesDB = Namer.LoadDBC("DecalProperties");
            if (!decalPropertiesDB.AvailableColumns.Contains("Field_11_2_0_61476_024"))
            {
                Console.WriteLine("DecalProperties DB2 does not contain Field_11_2_0_61476_024 column, skipping naming.");
                return;
            }

            foreach (var decalPropertiesRow in decalPropertiesDB.Values)
            {
                var decalFileDataID = int.Parse(decalPropertiesRow["Field_11_2_0_61476_024"].ToString()!);
                if (decalFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey(decalFileDataID))
                {
                    NewFileManager.AddNewFile(decalFileDataID, "spells/textures/decal_" + decalPropertiesRow.ID + "_" + decalFileDataID + ".blp");
                }
            }
        }
    }
}
