using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class ContentHashNamer
    {
        public static Dictionary<string, string> knownHashes = [];

        public static void Name(Dictionary<int, byte[]> idToHashes, Dictionary<string, string> additionalHashes, List<int>? filter = null)
        {
            knownHashes = knownHashes.Concat(additionalHashes).ToDictionary(x => x.Key, x => x.Value);

            foreach (var idToHash in idToHashes)
            {
                if (filter != null && !filter.Contains(idToHash.Key))
                    continue;

                var contenthash = Convert.ToHexStringLower(idToHash.Value);

                if (Namer.NeedsName(idToHash.Key))
                {
                    var isRangeMapped = false;
                    if(Namer.IDToNameLookup.TryGetValue(idToHash.Key, out var currentMapName) && currentMapName.Contains("exp"))
                        isRangeMapped = true;

                    // maptexture_n
                    if (contenthash.Equals("93eb33c44532ea7e4f62666417beaa6a", StringComparison.Ordinal) && !isRangeMapped)
                        NewFileManager.AddNewFile(idToHash.Key, "unkmaps/maptextures/" + idToHash.Key + "_n.blp", true, true);

                    // maptexture
                    if (contenthash.Equals("77beda3cb2c5709fc953c9d21e1d2414", StringComparison.Ordinal) && !isRangeMapped)
                        NewFileManager.AddNewFile(idToHash.Key, "unkmaps/maptextures/" + idToHash.Key + ".blp", true, true);

                    // minimaps
                    if (contenthash.Equals("ef3ae8b80605064fadc0515b10c82ef2", StringComparison.Ordinal) && !isRangeMapped)
                        NewFileManager.AddNewFile(idToHash.Key, "unkmaps/minimaps/" + idToHash.Key + ".blp", true, true);
                }

                // Black/empty textures
                if (contenthash == "8660736128e3cd4e244cfd1f32f205ef" || contenthash == "6168c9a0f30f7e811493dc8c6bc24c9f" || contenthash == "d3f5f62a715c7fa4d9ac22bac27a530e" || contenthash == "8f2b25f293846f617401acc66771b0f5")
                    continue;

                if (!knownHashes.TryGetValue(contenthash, out var knownName))
                    continue;

                if (!Namer.NeedsName(idToHash.Key))
                    continue;

                if (Namer.IDToNameLookup.TryGetValue(idToHash.Key, out var currentName))
                {
                    if (!string.IsNullOrEmpty(currentName) && !currentName.StartsWith("models/unktextures", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var currentDir = Path.GetDirectoryName(currentName);
                        var newFilename = currentDir!.Replace("\\", "/") + "/" + knownName + ".blp";
                        NewFileManager.AddNewFile(idToHash.Key, newFilename, true, true);
                        continue;
                    }
                }

                NewFileManager.AddNewFile(idToHash.Key, "models/unktextures/" + knownName + "_" + idToHash.Key + ".blp", true, true);
            }
        }

        private static bool overrideCheck(bool overrideName, uint fdid, bool forceOverride)
        {
            return fdid != 0 && (forceOverride || overrideName || Namer.NeedsName((int)fdid));
        }
    }
}
