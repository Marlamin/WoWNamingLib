using DBCD;
using DBCD.IO;
using DBCD.Providers;
using DBDefsLib;
using static WoWNamingLib.Services.WDBManager;

namespace WoWNamingLib.Services
{
    public class DBCManager
    {
        private DBCD.DBCD dbcd;
        private Dictionary<string, DBCD.IDBCDStorage> cache = new Dictionary<string, DBCD.IDBCDStorage>();
        private Dictionary<uint, HotfixReader> hotfixes = [];

        public DBCManager(IDBCProvider dbcProvider, IDBDProvider dbdProvider)
        {
            dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);
        }

        public DBCManager(IDBCProvider dbcProvider, Stream bdbdStream)
        {
            dbcd = new DBCD.DBCD(dbcProvider, bdbdStream);
        }

        public void SetHotfixes(Dictionary<uint, HotfixReader> htfxs)
        {
            hotfixes = htfxs;
        }

        public DBCD.IDBCDStorage Load(string name)
        {
            if (cache.TryGetValue(name, out DBCD.IDBCDStorage? value))
                return value;

            var db = dbcd.Load(name, Namer.build);
            var buildNumber = uint.Parse(Namer.build.Split('.')[3]);

            if (hotfixes.TryGetValue(buildNumber, out HotfixReader? hotfixReaders))
                db.ApplyingHotfixes(hotfixReaders);

            cache.TryAdd(name, db);

            if (db.Count == 0)
                Console.WriteLine("[WARN] No records found in " + name + "!");

            return db;
        }
    }
}
