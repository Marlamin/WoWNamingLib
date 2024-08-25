using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Anima
    {
        public static void Name()
        {
            var animaCableDB = Namer.LoadDBC("AnimaCable");
            foreach (var animaCableRow in animaCableDB.Values)
            {
                var particleModelFDID = int.Parse(animaCableRow["ParticleModel"].ToString());
                if (particleModelFDID != 0 && !Namer.IDToNameLookup.ContainsKey(particleModelFDID))
                    NewFileManager.AddNewFile(particleModelFDID, "world/expansion08/doodads/fx/9fx_animacable_" + particleModelFDID + ".m2");

                var soundKitID = uint.Parse(animaCableRow["Field_9_0_1_33978_006"].ToString());
                foreach (var soundFDID in SoundKitHelper.GetRecursiveFileDataIDs(soundKitID))
                {
                    if (!Namer.IDToNameLookup.ContainsKey(soundFDID))
                        NewFileManager.AddNewFile(soundFDID, "sounds/spells/anima_loop_" + soundFDID + ".ogg");
                }
            }

            var animaMaterialDB = Namer.LoadDBC("AnimaMaterial");
            foreach (var animaMaterialRow in animaMaterialDB.Values)
            {
                var effectTextures = (int[])animaMaterialRow["EffectTexture"];
                foreach (var effectTexture in effectTextures)
                {
                    if (effectTexture != 0 && !Namer.IDToNameLookup.ContainsKey(effectTexture))
                        NewFileManager.AddNewFile(effectTexture, "world/expansion08/doodads/fx/9fx_anima_" + effectTexture + ".blp");
                }

                var ribbonTexture = int.Parse(animaMaterialRow["RibbonTexture"].ToString());
                if (ribbonTexture != 0 && !Namer.IDToNameLookup.ContainsKey(ribbonTexture))
                    NewFileManager.AddNewFile(ribbonTexture, "world/expansion08/doodads/fx/9fx_anima_ribbon_" + ribbonTexture + ".blp");
            }
        }
    }
}
