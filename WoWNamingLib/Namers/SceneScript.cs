using WoWNamingLib.Utils;

namespace WoWNamingLib.Namers
{
    public static class SceneScript
    {
        public static void Name()
        {
            var sceneScriptPackageDB = Namer.LoadDBC("SceneScriptPackage");

            foreach (var sceneScriptPackageRow in sceneScriptPackageDB.Values)
            {
                var sceneScriptPackageID = (uint)sceneScriptPackageRow["ID"];
                var name = (string)sceneScriptPackageRow["Name"];
                var script = SceneScriptParser.CompilePackage(sceneScriptPackageID, name);
            }
        }
    }
}
