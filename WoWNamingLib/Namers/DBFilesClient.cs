using System.Text.Json;
using System.Text.Json.Serialization;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class DBFilesClient
    {
        private struct ManifestEntry
        {
            public string tableName { get; set; }
            public string tableHash { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int dbcFileDataID { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int db2FileDataID { get; set; }
        }


        public static void Name(string definitionDir)
        {
            if (!File.Exists(definitionDir + "\\..\\manifest.json"))
            {
                Console.WriteLine("DBD manifest not found, cannot name DB2s.");
                return;
            }

            var scnEntries = Namer.IDToNameLookup.Where(x => x.Value.EndsWith(".scn")).ToDictionary(x => Path.GetFileNameWithoutExtension(x.Value).ToLower(), x => x.Key);

            var baseEntries = JsonSerializer.Deserialize<ManifestEntry[]>(File.ReadAllText(definitionDir + "\\..\\manifest.json"));

            foreach (var baseEntry in baseEntries)
            {
                // TODO: SCN naming based on type detection

                if (scnEntries.TryGetValue(baseEntry.tableName.ToLower(), out var scnFileDataID))
                {
                    var scnFilename = "DBFilesClient/" + baseEntry.tableName + ".scn";
                    if (Namer.IDToNameLookup[scnFileDataID] != scnFilename)
                        NewFileManager.AddNewFile(scnFileDataID, scnFilename, true, true);
                }

                if (baseEntry.db2FileDataID == 0)
                    continue;

                var fileName = "DBFilesClient/" + baseEntry.tableName + ".db2";

                if (!Namer.IDToNameLookup.ContainsKey(baseEntry.db2FileDataID) || Namer.IDToNameLookup[baseEntry.db2FileDataID] != fileName)
                    NewFileManager.AddNewFile(baseEntry.db2FileDataID, fileName, true);

            }
        }
    }
}
