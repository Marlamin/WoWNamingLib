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
            {"8eea9a50f3308eb004fc03f5397237c6", "8fx_water_scroll5" },
            {"597d97551e7a7f5efef278b0df2b3a67", "Dirt_Scroll_Wispy_Holy" },
            {"4057bfcefe7b622f40c0ee66fc883495", "ArmorReflect_Hard_Orange" },
            {"a081c993e1171f2a569b4381aece5762", "Fire_FlameLicks_DissolveFBs_Blend" },
            {"404287bae656f0dccef312988b725e5d", "oldglass3" },
            {"1b0c8e7d6bc9b48131f8013d0431ef6a", "ArmorReflect7" },
            {"e593865b187c40b40252a893f038dc28", "ember_chunky_blankmorerotation_A" },
            {"d59164c89070cedd331859937cbef2fa", "fb_shadoweataway_warlock_small_ba" },
            {"2ed9baf0d006c3de327656333e7838e7", "wispymagic_hori_warlockshadow_purple_128" },
            {"2fc676e1df6363b5dcb9046c574835a3", "wispymagic_vert_azeritemist_blue_128_greyrotate" },
            {"1f5a9d16a8766a4807cb5ac02a0858e4", "Smoke_Puffy_Mask_BlendAdd" },
            {"0803c366af5b8dff13aa0a3bc4d2ead4", "7FX_EnergyScroll_Fire_A" },
            {"9df17b9977a5c6118d3d4405ebd51f41", "10XP_Waterfall_Mod2x_Main" },
            {"2468d6e678e544c04dceede4d57fad79", "10XP_Waterfall_Base01" },
            {"effc7fb16b8f9bff8bd7dfa9a6969609", "10XP_Waterfall_Mod2x_Bottom" },
            {"dcea0c9b8bfd633d40310d7ae8ca4e1d", "10XP_Waterfall_Bottom01" },
            {"11cc3b1885eced2033e60a7a82276ba5", "10XP_Waterfall_Mod2x_Top" },
            {"a753e75f641de6682545d7137b0d4684", "10XP_Waterfall_Top01" },
            {"0dbf9e9c1662a215c09f8166e85852cf", "10GL_Gnoll_Campfire_Campfire_Fire01_Emissive" },
            {"76e099e896e9f54f3e27deed3dc07a41", "10GL_Gnoll_Campfire_Campfire_Fire01" },
            {"79f1ebf3198fb27e320a6ac8393e0ce9", "ArmorReflect6" },
            {"7dabc14b04800cf9d91369178d8d7c4d", "10DG_Dragon_Brazier02" },
            {"c3dbfc86eb44d3886262015bef9bc36c", "10DG_Dragon_Colors" },
            {"c95dda18d341d2d3560a193d79575076", "Smoke_Generic_PaintyMask01_Blend" },
            {"3d96d448c6eec6489fcf176bee85a870", "UI_Mechagnome_PlayerShadow" },
            {"da55665164753c82ce8308967ea5ae24", "10FX_Wind_Streams" },
            {"420a1d80dd8ee738ae1f09a0dce7961b", "10FX_General_WindTile_06" },
            {"d5bb9b52b7769227ae3ee45c845478df", "9FX_HeroAura_RibbonMask_12_Gradual" },
            {"d1b5a5706593185732c4364983ced220", "dragonarmorspec" },
            {"ae4a6b1b8f2f0142c06186bc4b373a9e", "MawReflect" },
            {"589380b440483602efdbb459122e35f7", "Smoke_Soft_01" },
            {"1ca924d64026d5d3d37d64f28985dcf2", "9FX_Flare_B" },
            {"347bdcfd3b8095e1e1bc8e7bce57b497", "Crystalsong_SparkleLeaf02" },
            {"8aebd963e171ae2ed002ee14c69ef8f7", "10TI_Titan_Tyrhold_Device_Glow01" },
            {"de96935405fc279ed5f0cbca1c81d1a3", "Fire_Blurred_Mod2x_fixed" },
            {"b55d374701d2de568060667ec5cb32a6", "7FX_AlphaMask_Glow_DirtyYellow512" },
            {"de380afdf17e77816756e6f2806a79c9", "7FX_AlphaMask_Glow_Donut2" },
            {"d899b19d022e77f320ac38236ee932bb", "8hu_KulTiras_Crate01" },
            {"90e3b062c6f84233eba445d2fef89c63", "Bird_Condor_01" },
            {"be0fa3ce84879fdcb0d58c397ff33ae0", "8FX_Painted_CloudScroll_02" },
            {"bbe0941af7a3840fca805d3562a57d10", "9VL_Aspirants_Bird01" },
            {"2415a422c177aa9232ab2f95fb14bed2", "9VL_Aspirants_Bird02" },
            {"f9625772d9ce232275df29b8926e83f9", "10xp_fog02" },
            {"8b17d34dc3804872b50c9b937775428b", "10xp_fogScroll02" },
            {"8a3ea1137568e851bf4c014e5e43ac3d", "PORTALFRONT" },
            {"ce9d1cda4856f26956ad4399e41f8467", "Cloud_Soft_Painterly_Alpha" },
            {"130092560186e1ef2229f7e664327e12", "Gray_64" },
            {"40fc9b8d820b6ca4574a42fccc7c070a", "11EA_Earthen_Special_AwakeningMachine02" },
            {"e064f62cc18f7634f468dffc6f39e5e1", "Metal_Reflect" },
            {"6099f1af1945cc5813015cceae6d80ca", "11EA_Earthen_Special_AwakeningMachine01" },
            {"95c23d48aaa71ae1c076843c083d248b", "11EA_Earthen_Special_AwakeningMachine01_FX" },
            {"21f7ea405c1d9324bf2d435fd24f3b00", "AlphaMask_VerticalGradient" },
            {"ca2ae1e773a4129d4513b75c30a01a1f", "WispyMagic_Vert_Grey" },
            {"f8e1c412b43b44489e561fae34ddecba", "8FX_Basic_Spark_Sheet" },
            {"3f3e97fee08fb086cbcb189511e79a6a", "t_vfx_hero_aura_glob" },
            {"c29cfef66f8922136f303d52380ac43b", "T_VFX_FLARE09" },
            {"a61d438b3478430741d1467a09e88240", "T_VFX_HERO_AURA_01_DESAT" },
            {"03f3f90ad33608a4e62049b8532fd9f8", "Flare1_smooth" },
            {"710d764834c67cdb658ea2cd6416cc5a", "11EA_Earthen_GearBase01_Small" },
            {"fb62f2d1aea342a728399ecd670c2365", "11EA_Earthen_Structure_Gear01" },
            {"dfcdd97a62cfb86eab61bd869f78b110", "10TI_Titan_Raid_StormDoor_Glow" },
            {"960892f853cc9e661c4c77789608c150", "delete_me" }
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
