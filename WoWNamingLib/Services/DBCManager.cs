using DBCD.Providers;
using DBFileReaderLib;

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

            if (Namer.localProduct == "wow" && File.Exists(Path.Combine(Namer.wowDir, "_retail_", "Cache\\ADB\\enUS\\DBCache.bin")))
            {
                hotfixReader = new HotfixReader(Path.Combine(Namer.wowDir, "_retail_", "Cache\\ADB\\enUS\\DBCache.bin"));
                Console.WriteLine("Loaded hotfixes from " + hotfixReader.BuildId);
            }
        }

        public DBCD.IDBCDStorage Load(string name)
        {
            if (cache.ContainsKey(name))
                return cache[name];

            var db = dbcd.Load(name, Namer.build);
            if (hotfixReader != null)
                db.ApplyingHotfixes(hotfixReader);

            cache.Add(name, db);

            if (db.Count == 0)
                Console.WriteLine("[WARN] No records found in " + name + "!");

            return db;
        }
    }
}
