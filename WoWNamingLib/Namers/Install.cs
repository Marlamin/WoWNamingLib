using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Install
    {
        public static void Name(Dictionary<int, byte[]> idToHashes, List<(byte[], string)> installCKeyToInstallName)
        {
            var reverseCHashLookup = new Dictionary<string, int>();

            foreach(var entry in idToHashes)
            {
                var cHash = Convert.ToHexStringLower(entry.Value);
                if(!reverseCHashLookup.ContainsKey(cHash))
                    reverseCHashLookup[cHash] = entry.Key;
            }

            foreach (var (installCKey, installName) in installCKeyToInstallName)
            {
                var cHash = Convert.ToHexStringLower(installCKey);
                if (reverseCHashLookup.TryGetValue(cHash, out int fdid))
                    NewFileManager.AddNewFile(fdid, installName.Replace("\\", "/"));
            }
        }
    }
}
