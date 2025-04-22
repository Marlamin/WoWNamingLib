using DBCD.Providers;
using DBCD.IO;

namespace WoWNamingLib.Services
{
    public class DBCManager
    {
        private DBCD.DBCD dbcd;
        private Dictionary<string, DBCD.IDBCDStorage> cache = new Dictionary<string, DBCD.IDBCDStorage>();
        private HotfixReader? hotfixReader;
        public DBCManager(IDBCProvider dbcProvider, IDBDProvider dbdProvider)
        {
            dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);
            InitHotfixes();
        }

        public DBCManager(IDBCProvider dbcProvider, Stream bdbdStream)
        {
            dbcd = new DBCD.DBCD(dbcProvider, bdbdStream);
            InitHotfixes();
        }

        private void InitHotfixes()
        {
            if (!string.IsNullOrEmpty(Namer.wowDir) && Namer.localProduct == "wow" && File.Exists(Path.Combine(Namer.wowDir, "_retail_", "Cache\\ADB\\enUS\\DBCache.bin")))
            {
                var htfxReader = new HotfixReader(Path.Combine(Namer.wowDir, "_retail_", "Cache\\ADB\\enUS\\DBCache.bin"));

                var buildIDInNamer = Namer.build.Split('.')[3];

                if (htfxReader.BuildId == int.Parse(buildIDInNamer))
                {
                    hotfixReader = htfxReader;
                    Console.WriteLine("Loaded hotfixes from " + hotfixReader.BuildId);
                }
            }
        }

        public DBCD.IDBCDStorage Load(string name)
        {
            if (cache.TryGetValue(name, out DBCD.IDBCDStorage? value))
                return value;

            var db = dbcd.Load(name, Namer.build);
            if (hotfixReader != null)
                db.ApplyingHotfixes(hotfixReader);

            cache.TryAdd(name, db);

            if (db.Count == 0)
                Console.WriteLine("[WARN] No records found in " + name + "!");

            return db;
        }
    }
}
