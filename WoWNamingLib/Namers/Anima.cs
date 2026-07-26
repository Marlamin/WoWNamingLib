using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Anima
    {
        public static void Name()
        {
            var animaCableDB = Namer.LoadDBC("AnimaCable");
            if (!animaCableDB.AvailableColumns.Contains("ParticleModel") || !animaCableDB.AvailableColumns.Contains("Field_9_0_1_33978_006"))
                throw new Exception("AnimaCable is missing required columns");

            foreach (var animaCableRow in animaCableDB.Values)
            {
                var particleModelFDID = int.Parse(animaCableRow["ParticleModel"].ToString()!);
                if (particleModelFDID != 0 && Namer.NeedsName(particleModelFDID))
                    NewFileManager.AddNewFile(particleModelFDID, "world/expansion08/doodads/fx/9fx_animacable_" + particleModelFDID + ".m2");

                var soundKitID = uint.Parse(animaCableRow["Field_9_0_1_33978_006"].ToString()!);
                foreach (var soundFDID in SoundKitHelper.GetRecursiveFileDataIDs(soundKitID))
                {
                    if (Namer.NeedsName((int)soundFDID))
                        NewFileManager.AddNewFile(soundFDID, "sounds/spells/anima_loop_" + soundFDID + ".ogg");
                }
            }

            var animaMaterialDB = Namer.LoadDBC("AnimaMaterial");
            if (!animaMaterialDB.AvailableColumns.Contains("EffectTexture") || !animaMaterialDB.AvailableColumns.Contains("RibbonTexture"))
                throw new Exception("AnimaMaterial is missing required columns");

            foreach (var animaMaterialRow in animaMaterialDB.Values)
            {
                var effectTextures = (int[])animaMaterialRow["EffectTexture"];
                foreach (var effectTexture in effectTextures)
                {
                    if (effectTexture != 0 && Namer.NeedsName(effectTexture))
                        NewFileManager.AddNewFile(effectTexture, "world/expansion08/doodads/fx/9fx_anima_" + effectTexture + ".blp");
                }

                var ribbonTexture = int.Parse(animaMaterialRow["RibbonTexture"].ToString()!);
                if (ribbonTexture != 0 && Namer.NeedsName(ribbonTexture))
                    NewFileManager.AddNewFile(ribbonTexture, "world/expansion08/doodads/fx/9fx_anima_ribbon_" + ribbonTexture + ".blp");
            }
        }
    }
}
