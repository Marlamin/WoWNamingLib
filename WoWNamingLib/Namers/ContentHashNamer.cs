using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class ContentHashNamer
    {
        private static Dictionary<string, string> knownHashes = new Dictionary<string, string>()
        {
            {"3ae255c5ce0b3c8216237f09bf45ec56", "smoke_loose_02_256_blend_contrast"},
            {"4f00cef6127f05c82305fd21adc815fe", "7fx_energyscroll_holy_dark_highalpha"},
            {"2c9e16d0370b1378c459c345b9b72f7b", "dirt_02"},
            {"2a666d2d44fb99a58361d4e2896f9c45", "Gray8x8"},
            {"c7af911b8cf113ea49abeb359d3f0ff0", "7fx_explosionsmoke"},
            {"657bbb1903372ae90b8d0cc905255716", "8fx_water_scroll_alpha2"},
            {"34d616b5caffa9c3c51e2afcc8288b1a", "7fx_arcane_starfield_noa_grey2"},
            {"92ec80116e279c7ed0fc3b7470f0b321", "Smoke_Loose_01"},
            {"dd5b9fe582d0a833a6f9c582d5a45dd4", "star5b"},
            {"2341b633909f4fe7005e993805136f21", "7fx_alphamask_shockwavesoft_256"},
            {"4dd747e20f33d167ae7416d1c316634c", "8fx_nightelf_moonwell_rocks_pillar"},
            {"bde59a31b01629f605c35215de43e963", "t_star2_purple"},
            {"9232c9b603df2c3d3e8652b930f8e26a", "7fx_alphamask_glow_ghost"},
            {"386d50449780dcd6cc3b0af27c4775f3", "grey50percentsquare16x16"},
            {"7cc149e674ea5c638b3c889d05dd12bf", "7FX_AlphaMask_Glow" },
            {"abafdf49a88245f37ba062f37175611d", "ArmorReflect4" },
            {"d3ad3bffaabb7f5a6916e42a57ba38d5", "GenericGlow_Alpha_128" },
            {"a234e55096efa26b66c61bc1ba6734a1", "Smoke_Puffy_Mask_VerySoft" },
            {"fb352f8353c02ff73c938806c3aa8f4d", "Cloud_PuffySofter_Gorgrond" },
            {"df26e19da0626eb1c135afad673b5bf5", "Cloud_Puffy_Mod" },
            {"7e889943a74ca478411bd3ea620a33c8", "Ember_Offset_Streak" },
            {"8dd1db810f643aad8a06212f6984300a", "Ember_Offset" },
            {"ecc52caeb8255235d198ab3913250836", "Ember_Offset_Grey" },
            {"7aa965cef2fc60e84f950c0ea05c091b", "8FX_ExplosionSmoke_A" },
            {"0d15229cf2298aa9e1fd6df14d1ec5e2", "White8x8" },
            {"639c2f30f55976c2017794a9801a6385", "ArmorReflect_Hard_Round" },
            {"f6e2981e669e0bf93e06811d8bb6e37a", "8fx_precast_smoke" },
            {"dd3e84e29c0f2bda8bf9e479f70ef230", "8fx_energygeo_gray_a" },
            {"7bc471b5847a9e735ba364d9aa0f1d82", "7FX_EnergyScroll_LightBlue" },
            {"f1927972d0f63c227f0d3844827cc7b8", "7FX_SnowChunks02_Blur"},
            {"ac86a62878a75cb5f297fcec45458198", "grey50percentsquare16x16_a" },
            {"7559bc5177dfc80a67a1f284de803744", "Snowflake_Flipping_01" },
            {"07d049db40cdc4a8d1570ffadfc2e1c2", "8fx_mask_glowgeneric_a_128_brighter" },
            {"3b5fa261b058cb8ef4ddea571cc8f149", "8fx_ribbonswirlfull_a" },
            {"32f29527d99daafb7b03e8d233b21482", "shockwave_spikey_bright_ba_ice" },
            {"47139531b51d7af36a6244184f52c372", "wispymagic_vert_azeritemist_blue_128_lessalpha2_stripe" },
            {"f1688ce58d7c5193bfe95e51ae9d9b58", "7fx_stripeblurr" },
            {"717c3117a2231fba38cf22a8810e9922", "alpha_streak_mid" },
            {"19d228b37baa2d6c93f69f6a24347e21", "7fx_swirl_zandalarigold_desat" },
            {"8b50846b93d2cee5642b1e167be6fb5d", "Ember_Offset_Streak_Desat2_A" },
            {"fa52138a4ca3e97022db37ae4dd3915b", "FairyDust2_2x2_A" },
            {"6744fcff387c421797e542cf36cfec46", "Ribbon_Magic_02_BA22" },
            {"479cf392d863e119bb4adfd1f74a7bf4", "7fx_crystalized01_gold" },
            {"ad8c4f1a5043d4563c3486bd0e03944f", "7fx_energyscroll_holy_dark" },
            {"cf3b51970c1d3b513a1aa4c050882d23", "ArmorReflect_Hard" },
            {"1210b5be89a4b66610325f752dea0d06", "slime03_s2" },
            {"f57d9ef9463df63ddc517ba4a72976c3", "arcane_wisps_oil" },
            {"ea7727416548fb31d02e687ff03f7e46", "smoothreflect_round" },
            {"7c648002c36d66d03bf6825a7bbd6931", "ArmorReflect4Blue" },
            {"6552b96878dba9907d1d85558731f9a5", "7fx_lightraysup" },
            {"69497bf2ad7eb2a0e701155ace86dd6a", "ArmorReflect_Rainbow" },
            {"434b6879b2a1d98cf42f4348ec53f7c2", "8fx_water_scroll_alphadissolve" },
            {"daadd7b17f038733074e74840fd6ddee", "water_caustic_blendadd_radial" },
            {"8eea9a50f3308eb004fc03f5397237c6", "8fx_water_scroll5" }
        };

        public static void Name(Dictionary<int, string> idToHashes)
        {
            foreach (var idToHash in idToHashes)
            {
                if (!knownHashes.TryGetValue(idToHash.Value.ToLower(), out var knownName))
                    continue;

                if (Namer.IDToNameLookup.TryGetValue(idToHash.Key, out var currentName))
                {
                    if (!string.IsNullOrEmpty(currentName) && !currentName.ToLower().StartsWith("models/unktextures"))
                    {
                        var currentDir = Path.GetDirectoryName(currentName);
                        var newFilename = currentDir.Replace("\\", "/") + "/" + knownName + ".blp";
                        NewFileManager.AddNewFile(idToHash.Key, newFilename, true, true);
                        continue;
                    }
                }

                NewFileManager.AddNewFile(idToHash.Key, "models/unktextures/" + knownName + "_" + idToHash.Key + ".blp", true, true);
            }
        }
    }
}
