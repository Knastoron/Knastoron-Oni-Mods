﻿using Database;
using HarmonyLib;
using Klei.AI;
using KMod;
using ProcGenGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UtilLibs;
using static ClusterTraitGenerationManager.ModAssets;

using ProcGen;
using static STRINGS.BUILDINGS.PREFABS.DOOR.CONTROL_STATE;
using static ClusterTraitGenerationManager.STRINGS.UI;
using STRINGS;
using Klei.CustomSettings;
using static STRINGS.UI.FRONTEND;
using static ClusterTraitGenerationManager.CGSMClusterManager;
using System.Threading;
using static ClusterTraitGenerationManager.STRINGS;
using System.Text.RegularExpressions;
using PeterHan.PLib.Options;
using static Door;
using static STRINGS.DUPLICANTS.THOUGHTS;
using Klei;
using static STRINGS.UI.CLUSTERMAP;
using System.Security.Cryptography;

namespace ClusterTraitGenerationManager
{
    internal class Patches
    {

        /// <summary>
        /// Init. auto translation
        /// </summary>
        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix()
            {
                LocalisationUtil.Translate(typeof(STRINGS), true);
            }
        }

        /// <summary>
        /// adds gear button to cluster view
        /// </summary>
        [HarmonyPatch(typeof(ColonyDestinationSelectScreen))]
        [HarmonyPatch(nameof(ColonyDestinationSelectScreen.OnSpawn))]
        public static class InsertCustomClusterOption
        {
            public static void Prefix(ColonyDestinationSelectScreen __instance)
            {
                InitExtraWorlds.InitWorlds();
                OverrideWorldSizeOnDataGetting.ResetCustomSizes();

                if (SettingsCache.clusterLayouts.clusterCache.ContainsKey(CustomClusterID))
                {
                    SettingsCache.clusterLayouts.clusterCache.Remove(CustomClusterID);
                }
                CGSMClusterManager.selectScreen = __instance;
            }
            public static void Postfix(ColonyDestinationSelectScreen __instance)
            {
                var InsertLocation = __instance.shuffleButton.transform.parent; //__instance.transform.Find("Layout/DestinationInfo/Content/InfoColumn/Horiz/Section - Destination/DestinationDetailsHeader/");
                var copyButton = Util.KInstantiateUI(__instance.shuffleButton.gameObject, InsertLocation.gameObject, true); //UIUtils.GetShellWithoutFunction(InsertLocation, "CoordinateContainer", "cgsm");

                // UIUtils.ListAllChildrenPath(__instance.transform); 

                UIUtils.TryFindComponent<Image>(copyButton.transform, "FG").sprite = Assets.GetSprite("icon_gear");
                UIUtils.TryFindComponent<ToolTip>(copyButton.transform, "").toolTip = STRINGS.UI.CGMBUTTON.DESC;
                UIUtils.TryFindComponent<KButton>(copyButton.transform, "").onClick += () => CGSMClusterManager.InstantiateClusterSelectionView(__instance);

                LoadCustomCluster = false;

                if (CGSMClusterManager.LastGenFailed)
                {
                    CGSMClusterManager.InstantiateClusterSelectionView(__instance);
                    LastWorldGenDidFail(false);
                }
            }
        }

        /// <summary>
        /// Regenerates Custom cluster with newly created traits on seed shuffle
        /// </summary>
        [HarmonyPatch(typeof(CustomGameSettings))]
        [HarmonyPatch(nameof(CustomGameSettings.SetQualitySetting))]
        [HarmonyPatch(new Type[] { typeof(SettingConfig), typeof(string) })]
        public static class TraitShuffler
        {
            public static void Postfix(CustomGameSettings __instance, SettingConfig config, string value)
            {
                if (__instance == null || LoadCustomCluster)
                    return;
                if (config.id != "WorldgenSeed" && config.id != "ClusterLayout")
                    return;
                RegenerateCGM(__instance, config.id);
            }
        }
        public static void RegenerateCGM(CustomGameSettings __instance, string changedConfigID)
        {
            if (CGSMClusterManager.LastGenFailed)
            {
                SgtLogger.l("Skipping regenerating due to failed previous worldgen.");

                return;
            }

            string clusterPath = __instance.GetCurrentQualitySetting(CustomGameSettingConfigs.ClusterLayout).id;
            if (clusterPath == null || clusterPath.Count() == 0)
            {
                ///default is no path selected, this picks either classic Terra on "classic" selection or Terrania on "spaced out" selection
                clusterPath = DestinationSelectPanel.ChosenClusterCategorySetting == 1 ? "expansion1::clusters/VanillaSandstoneCluster" : "expansion1::clusters/SandstoneStartCluster";
            }

            if (CGM_Screen == null || !CGM_Screen.isActiveAndEnabled)
            {
                CGM_MainScreen_UnityScreen.PresetApplied = false;
                CGSMClusterManager.LoadCustomCluster = false;
                CGSMClusterManager.CreateCustomClusterFrom(clusterPath, ForceRegen: true);
                SgtLogger.l("Regenerating Cluster from " + clusterPath + ". Reason: " + changedConfigID + " changed.");
            }
            else
            {
                CGSMClusterManager.RerollTraits();
                SgtLogger.l("Regenerating Traits for " + clusterPath + ". Reason: " + changedConfigID + " changed.");
            }
        }
        [HarmonyPatch(typeof(ColonyDestinationSelectScreen))]
        [HarmonyPatch(nameof(ColonyDestinationSelectScreen.ShuffleClicked))]
        public static class SeedInserted
        {
            public static void Postfix(ColonyDestinationSelectScreen __instance)
            {
                CGSMClusterManager.selectScreen = __instance;
                if (__instance.newGameSettings == null)
                    return;

                RegenerateCGM(__instance.newGameSettings.settings, "Coordinate");
            }
        }

        //public static class ReplaceDefaultName_3
        //{
        //    public static void Postfix(ref string __result)
        //    {
        //        if (LoadCustomCluster)
        //        {
        //            var regex = new Regex(Regex.Escape("-"));
        //            __result = regex.Replace(__result, @"^[^\s-]+", CustomClusterIDCoordinate);
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(NewGameSettingsPanel))]
        [HarmonyPatch(nameof(NewGameSettingsPanel.SetSetting))]
        public static class ReplaceDefaultName
        {
            public static bool Prefix(SettingConfig setting, string level)
            {
                if (LoadCustomCluster) return false;
                return true;
            }
        }
        /// <summary>
        /// custom meteor example code
        /// </summary>
        //[HarmonyPatch(typeof(Db), "Initialize")]
        //public static class Db_addSeason
        //{
        //    public static void Postfix(Db __instance)
        //    {
        //        __instance.GameplayEvents.Add(
        //            new MeteorShowerSeason(
        //                "AllShowersInOnceID",
        //                GameplaySeason.Type.World,
        //                "EXPANSION1_ID",
        //                20f,
        //                false,
        //                startActive: true,
        //                clusterTravelDuration: 6000f)
        //            .AddEvent(Db.Get().GameplayEvents.MeteorShowerDustEvent)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterCopperShower)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterGoldShower)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterIronShower)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterIceShower)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterBiologicalShower)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterBleachStoneShower)
        //            .AddEvent(Db.Get().GameplayEvents.ClusterUraniumShower));
        //        ///obv. not all events
        //    }
        //}


        /// <summary>
        /// CoreTraitFix_SolarSystemWorlds
        /// </summary>
        [HarmonyPatch(typeof(SettingsCache))]
        [HarmonyPatch(nameof(SettingsCache.LoadWorldTraits))]
        public static class TraitInitPostfix_ExclusionFix
        {
            public static void Postfix()
            {
                string coreKey = string.Empty;
                string cryoVolcano = string.Empty;
                string holesKey = string.Empty;


                foreach (var trait in SettingsCache.GetCachedWorldTraitNames())
                {
                    if (trait.Contains(SpritePatch.missingMoltenCoreTexture))
                        coreKey = trait;

                    if (trait.Contains(SpritePatch.missingHoleTexture))
                        holesKey = trait;
                    if (trait.Contains(SpritePatch.missingTexture_CryoVolcanoes))
                        cryoVolcano = trait;
                }

                var IronCoreTrait = SettingsCache.GetCachedWorldTrait(coreKey, false);
                if (IronCoreTrait != null)
                {
                    IronCoreTrait.colorHex = "B7410E"; /// BA5C3F  or B7410E
                    if (IronCoreTrait.exclusiveWithTags == null)
                        IronCoreTrait.exclusiveWithTags = new List<string>();
                    if (!IronCoreTrait.exclusiveWithTags.Contains("CoreTrait"))
                        IronCoreTrait.exclusiveWithTags.Add("CoreTrait");
                }
                var HolesTrait = SettingsCache.GetCachedWorldTrait(holesKey, false);
                if (HolesTrait != null)
                {
                    ///Light purple
                    HolesTrait.colorHex = "9696e2";
                    ///black
                    //HolesTrait.colorHex = "000000"; 
                }
                var cryoVolcanoTrait = SettingsCache.GetCachedWorldTrait(cryoVolcano, false);
                if (cryoVolcanoTrait != null)
                {
                    ///Light blue
                    cryoVolcanoTrait.colorHex = "91D8F0";
                }
            }
        }


        /// <summary>
        /// Prevents the normal cluster menu from closing when the custom cluster menu is open
        /// </summary>
        [HarmonyPatch(typeof(NewGameFlowScreen))]
        [HarmonyPatch(nameof(NewGameFlowScreen.OnKeyDown))]
        public static class CatchGoingBack
        {
            public static bool Prefix(KButtonEvent e)
            {
                if (CGSMClusterManager.Screen != null && CGSMClusterManager.Screen.activeSelf)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Allow planets to load into the world cache if 
        /// - they are in a cluster (default condition)
        /// - have "CGM"/"CGSM" in their Name (added)
        /// </summary>
        [HarmonyPatch(typeof(Worlds))]
        [HarmonyPatch(nameof(Worlds.UpdateWorldCache))]
        public static class AllowUnusedWorldTemplatesToLoadIntoCache
        {
            public static bool ContainsOrIsPredefined(ISet<string> referencedWorlds, string toContain)
            {
                return referencedWorlds.Contains(toContain) || toContain.Contains("CGSM") || toContain.Contains("CGM");
            }

            private static readonly MethodInfo AllowTemplates = AccessTools.Method(
               typeof(AllowUnusedWorldTemplatesToLoadIntoCache),
               nameof(ContainsOrIsPredefined)
            );

            private static readonly MethodInfo TargetMethod = AccessTools.Method(
                    typeof(System.Collections.Generic.ICollection<string>),
                    nameof(System.Collections.Generic.ICollection<string>.Contains));

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = instructions.ToList();

                var insertionIndex = code.FindIndex(ci => ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo f && f == TargetMethod);

                if (insertionIndex != -1)
                {
                    code[insertionIndex] = new CodeInstruction(OpCodes.Call, AllowTemplates);
                }

                return code;
            }

        }


        //[HarmonyPatch(typeof(WorldPlacement))]
        //[HarmonyPatch(nameof(WorldPlacement.CompareLocationType))]
        //public static class help
        //{
        //    public static void Prefix(ProcGen.WorldPlacement a, ProcGen.WorldPlacement b)
        //    {
        //        SgtLogger.l(a.ToString(), "a");
        //        SgtLogger.l(b.ToString(), "b");

        //        UtilMethods.ListAllPropertyValues(a);
        //        UtilMethods.ListAllPropertyValues(b);
        //    }
        //}



        /// <summary>
        /// yes this code is very necessary
        /// </summary>
        struct Child { public bool isGood, isNaughty, areParentsPoor; public List<string> gifts; }
        static List<Child> GetAllChildren() { return new List<Child>(); }
        static void SelectGoodGifts(Child child) { }
        static void SelectCoal(Child child) { }
        static class SantaClaus { public static async Task ComingToTown() { await new Task(() => { }); } }

        /// <summary>
        /// faith hill: santa claus is coming to town
        /// </summary>
        public static async Task SantaExe()
        {
            List<Child> allKids = GetAllChildren();

            foreach (var kid in allKids)
                if (kid.isNaughty) SelectCoal(kid);
            foreach (var kid in allKids)
                if (kid.isGood) SelectGoodGifts(kid);
            await SantaClaus.ComingToTown();
        }


        //[HarmonyPatch(typeof(ProcGenGame.WorldGen), (nameof(ProcGenGame.WorldGen.LoadSettings)))]
        //public class ReplaceForDebug
        //{
        //    public static void Prefix(bool in_async_thread)
        //    {
        //        SgtLogger.l($"LoadSettings, {in_async_thread}");
        //    }
        //}
        //[HarmonyPatch(typeof(ProcGenGame.WorldGen), (nameof(ProcGenGame.WorldGen.LoadSettings_Internal)))]
        //public class ReplaceForDebug2
        //{
        //    public static void Prefix(bool is_playing, bool preloadTemplates)
        //    {
        //        SgtLogger.l($"LoadSettings_Internal, {is_playing}, {preloadTemplates}");
        //    }
        //}





        //[HarmonyPatch(typeof(Db),(nameof(Db.Initialize)))]
        public static class InitExtraWorlds
        {
            static bool initialized = false;
            public static void InitWorlds()
            {

                if (initialized)
                    return;
                initialized = true;

                ProcGen.Worlds __instance = SettingsCache.worlds;

                SgtLogger.l("Initializing generation of additional planetoids, current count: " + __instance.worldCache.Count());
                List<KeyValuePair<string, ProcGen.World>> toAdd = new List<KeyValuePair<string, ProcGen.World>>();
                foreach (var sourceWorld in __instance.worldCache)
                {
                    ///Moonlets already exist in all 3 configurations

                    string BaseName = sourceWorld.Key.Replace("Start", "").Replace("Outer", "").Replace("Warp", "");

                    SgtLogger.l(sourceWorld.Key, "current planet");


                    if (sourceWorld.Key.Contains("NiobiumMoonlet")
                        || sourceWorld.Key.Contains("RegolithMoonlet")
                        || sourceWorld.Key.Contains("MooMoonlet")

                        )
                    {
                        SgtLogger.l("skipping to avoid unlivable planets");
                        continue;
                    }


                    var TypeToIgnore = DeterminePlanetType(sourceWorld.Value);
                    if (TypeToIgnore == StarmapItemCategory.Starter)
                    {
                        if (
                        __instance.worldCache.ContainsKey(sourceWorld.Key.Replace("Start", "")) && sourceWorld.Key.Contains("Start")
                        || __instance.worldCache.ContainsKey(sourceWorld.Key.Replace("Start", "").Replace("Outer", "") + "Warp")
                        )
                        {
                            SgtLogger.l("skipping bc there is already a warp and normal asteroid");
                            continue;
                        }
                    }
                    else if (TypeToIgnore == StarmapItemCategory.Warp)
                    {
                        if (
                        __instance.worldCache.ContainsKey(sourceWorld.Key.Replace("Warp", "")) && sourceWorld.Key.Contains("Warp")
                        || __instance.worldCache.ContainsKey(sourceWorld.Key.Replace("Warp", "").Replace("Outer", "") + "Start")
                        )
                        {
                            SgtLogger.l("skipping bc there is already a start and outer asteroid");
                            continue;
                        }
                    }
                    else if (TypeToIgnore == StarmapItemCategory.Outer)
                    {
                        if (
                           __instance.worldCache.ContainsKey(sourceWorld.Key + "Warp")
                        || __instance.worldCache.ContainsKey(sourceWorld.Key + "Start")
                        )
                        {
                            SgtLogger.l("skipping bc there is already a warp and Start asteroid");
                            continue;
                        }
                    }

                    List<string> additionalTemplates = new List<string>();

                    if (sourceWorld.Value.startingBaseTemplate != null && sourceWorld.Value.startingBaseTemplate.Count()>0 &&
                        (sourceWorld.Value.startingBaseTemplate.Contains("sap_tree_room")|| sourceWorld.Value.startingBaseTemplate.Contains("poi_satellite_3_a")))
                    {
                        additionalTemplates.Add(sourceWorld.Value.startingBaseTemplate);
                    }

                    //StartWorld

                    if (TypeToIgnore != StarmapItemCategory.Starter)
                    {
                        string newStartWorldPath = BaseName + "Start";

                        var StartWorld = new ProcGen.World();

                        CopyValues(StartWorld, sourceWorld.Value);


                        if(StartWorld.worldsize.X<100 && StartWorld.worldsize.Y < 160)
                        {
                            float planetSizeRatio = StartWorld.worldsize.Y / StartWorld.worldsize.X;
                            float newX = 100f;
                            float newY = 100f * planetSizeRatio;
                            StartWorld.worldsize = new Vector2I(Mathf.RoundToInt(newX), Mathf.RoundToInt(newY));
                        }
                        else if (StartWorld.worldsize.Y < 100 && StartWorld.worldsize.X < 160)
                        {
                            float planetSizeRatio = StartWorld.worldsize.X / StartWorld.worldsize.Y;
                            float newY = 100f;
                            float newX = 100f * planetSizeRatio;
                            StartWorld.worldsize = new Vector2I(Mathf.RoundToInt(newX), Mathf.RoundToInt(newY));
                        }



                        StartWorld.unknownCellsAllowedSubworlds = new List<ProcGen.World.AllowedCellsFilter>(sourceWorld.Value.unknownCellsAllowedSubworlds);
                        StartWorld.subworldFiles = new List<WeightedSubworldName>(sourceWorld.Value.subworldFiles);

                        if (StartWorld.subworldFiles != null && StartWorld.subworldFiles.Count > 0)
                        {
                            for (int i = StartWorld.subworldFiles.Count - 1; i >= 0; --i)
                            {
                                if (StartWorld.subworldFiles[i].name.Contains("Start"))
                                    StartWorld.subworldFiles.RemoveAt(i);
                            }
                        }
                        if (StartWorld.unknownCellsAllowedSubworlds != null && StartWorld.unknownCellsAllowedSubworlds.Count > 0)
                        {
                            for (int i = StartWorld.unknownCellsAllowedSubworlds.Count - 1; i >= 0; --i)
                            {
                                if (StartWorld.unknownCellsAllowedSubworlds[i].subworldNames.Any(world => world.ToLowerInvariant().Contains("start")))
                                    StartWorld.unknownCellsAllowedSubworlds.RemoveAt(i);
                            }
                        }



                        StartWorld.worldTemplateRules = new List<ProcGen.World.TemplateSpawnRules>();

                        if (sourceWorld.Value.worldTemplateRules != null && sourceWorld.Value.worldTemplateRules.Count > 0)
                        {
                            foreach (var rule in sourceWorld.Value.worldTemplateRules)
                            {
                                var ruleNew = new ProcGen.World.TemplateSpawnRules();
                                CopyValues(ruleNew, rule);

                                //if (ruleNew.listRule == ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeAll)
                                //    ruleNew.listRule = ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeSomeTryMore;
                                StartWorld.worldTemplateRules.Add(ruleNew);
                            }

                        }
                        StartWorld.worldTraitRules = new List<ProcGen.World.TraitRule>();

                        if (sourceWorld.Value.worldTraitRules != null && StartWorld.worldTraitRules.Count > 0)
                        {
                            foreach (var rule in sourceWorld.Value.worldTraitRules)
                            {
                                var newRule = new ProcGen.World.TraitRule(rule.min, rule.max);
                                newRule.requiredTags = new List<string>(rule.requiredTags);
                                newRule.specificTraits = new List<string>(rule.specificTraits);
                                newRule.forbiddenTags = new List<string>(rule.forbiddenTags);
                                newRule.forbiddenTraits = new List<string>(rule.forbiddenTraits);

                                if (newRule.forbiddenTags.Contains("StartChange"))
                                    newRule.forbiddenTags.Remove("StartChange");
                                if (!newRule.forbiddenTags.Contains("StartWorldOnly"))
                                    newRule.forbiddenTags.Add("StartWorldOnly");

                                StartWorld.worldTraitRules.Add(newRule);
                            }

                        }

                        StartWorld.filePath = newStartWorldPath;
                        StartWorld.startSubworldName = ModAPI.GetStartAreaSubworld(StartWorld, false);
                        StartWorld.startingBaseTemplate = ModAPI.GetStarterBaseTemplate(StartWorld, false);

                        //Starter Biome subworld files
                        var startBiome = new WeightedSubworldName(ModAPI.GetStartAreaSubworld(StartWorld, false), 1);
                        startBiome.overridePower = 3;

                        var startBiomeWater = new WeightedSubworldName(ModAPI.GetStartAreaWaterSubworld(StartWorld), 1);
                        startBiomeWater.overridePower = 0.7f;
                        startBiomeWater.minCount = 1;
                        startBiomeWater.maxCount = 4;

                        StartWorld.subworldFiles.Insert(0, startBiomeWater);
                        StartWorld.subworldFiles.Insert(0, startBiome);

                        //Starter biome placement rules

                        ProcGen.World.AllowedCellsFilter CoreSandstone = new ProcGen.World.AllowedCellsFilter();
                        CoreSandstone.tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.Default;
                        CoreSandstone.command = ProcGen.World.AllowedCellsFilter.Command.Replace;
                        CoreSandstone.subworldNames = new List<string>() { ModAPI.GetStartAreaSubworld(StartWorld, false) };

                        ProcGen.World.AllowedCellsFilter MiniWater = new ProcGen.World.AllowedCellsFilter();
                        MiniWater.tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.DistanceFromTag;
                        MiniWater.tag = "AtStart";
                        MiniWater.minDistance = 1;
                        MiniWater.maxDistance = 1;
                        MiniWater.command = ProcGen.World.AllowedCellsFilter.Command.Replace;
                        MiniWater.subworldNames = new List<string>() { ModAPI.GetStartAreaWaterSubworld(StartWorld) };

                        StartWorld.unknownCellsAllowedSubworlds.Insert(0,MiniWater);
                        StartWorld.unknownCellsAllowedSubworlds.Insert(0,CoreSandstone);

                        //Teleporter PlacementRules

                        ProcGen.World.TemplateSpawnRules TeleporterSpawn = new ProcGen.World.TemplateSpawnRules();

                        //Deleting any of the existing teleporter templates
                        StartWorld.worldTemplateRules.RemoveAll((template) => ModAPI.IsATeleporterTemplate(StartWorld, template));

                        TeleporterSpawn.names = new List<string>() 
                        {
                            "expansion1::poi/warp/sender_mini",///MaterialTeleporter sender
                            "expansion1::poi/warp/receiver_mini",///MaterialTeleporter reciever
                            "expansion1::poi/warp/teleporter_mini" ///Big Dupe Teleporter Building
                        
                        };
                        if(additionalTemplates.Count > 0)
                            TeleporterSpawn.names.AddRange(additionalTemplates);

                        TeleporterSpawn.listRule = ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeAll;
                        TeleporterSpawn.priority = 90;
                        TeleporterSpawn.allowedCellsFilter = new List<ProcGen.World.AllowedCellsFilter>()
                        {
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.Replace,
                                tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.DistanceFromTag,
                                tag = "AtStart",
                                minDistance = 1,
                                maxDistance = 2,
                            },
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.ExceptWith,
                                tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.DistanceFromTag,
                                tag = "AtDepths",
                                minDistance = 0,
                                maxDistance = 0,
                            },
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.ExceptWith,
                                zoneTypes  = new List<SubWorld.ZoneType>() { SubWorld.ZoneType.Space
                                , SubWorld.ZoneType.MagmaCore
                                }
                            },
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.ExceptWith,
                                tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.AtTag,
                                tag = "NoGravitasFeatures"
                            }
                        };
                        StartWorld.worldTemplateRules.Insert(0, TeleporterSpawn);

                        toAdd.Add(new(newStartWorldPath, StartWorld));

                        SgtLogger.l(newStartWorldPath, "Created Starter Planet Variant");

                    }
                    ///Warp planet variant
                    if (TypeToIgnore != StarmapItemCategory.Warp)
                    {
                        string newStartWorldPath = BaseName + "Warp";

                        var StartWorld = new ProcGen.World();

                        CopyValues(StartWorld, sourceWorld.Value);

                        if (StartWorld.worldsize.X < 100 && StartWorld.worldsize.Y < 160)
                        {
                            float planetSizeRatio = StartWorld.worldsize.Y / StartWorld.worldsize.X;
                            float newX = 100f;
                            float newY = 100f * planetSizeRatio;
                            StartWorld.worldsize = new Vector2I(Mathf.RoundToInt(newX), Mathf.RoundToInt(newY));
                        }
                        else if (StartWorld.worldsize.Y < 100 && StartWorld.worldsize.X < 160)
                        {
                            float planetSizeRatio = StartWorld.worldsize.X / StartWorld.worldsize.Y;
                            float newY = 100f;
                            float newX = 100f * planetSizeRatio;
                            StartWorld.worldsize = new Vector2I(Mathf.RoundToInt(newX), Mathf.RoundToInt(newY));
                        }


                        StartWorld.unknownCellsAllowedSubworlds = new List<ProcGen.World.AllowedCellsFilter>(sourceWorld.Value.unknownCellsAllowedSubworlds);
                        StartWorld.subworldFiles = new List<WeightedSubworldName>(sourceWorld.Value.subworldFiles);
                        StartWorld.worldTemplateRules = new List<ProcGen.World.TemplateSpawnRules>();

                        if (sourceWorld.Value.worldTemplateRules != null && sourceWorld.Value.worldTemplateRules.Count > 0)
                        {
                            foreach (var rule in sourceWorld.Value.worldTemplateRules)
                            {
                                var ruleNew = new ProcGen.World.TemplateSpawnRules();
                                CopyValues(ruleNew, rule);

                                StartWorld.worldTemplateRules.Add(ruleNew);
                            }
                        }
                        if(StartWorld.subworldFiles !=null && StartWorld.subworldFiles.Count > 0)
                        {
                            for(int i = StartWorld.subworldFiles.Count-1; i >= 0; --i)
                            {
                                SgtLogger.l(StartWorld.subworldFiles[i].name);
                                if (StartWorld.subworldFiles[i].name.Contains("Start"))
                                {
                                    StartWorld.subworldFiles.RemoveAt(i);
                                }
                            }
                        }
                        if (StartWorld.unknownCellsAllowedSubworlds != null && StartWorld.unknownCellsAllowedSubworlds.Count > 0)
                        {
                            for (int i = StartWorld.unknownCellsAllowedSubworlds.Count - 1; i >= 0; --i)
                            {
                                if (StartWorld.unknownCellsAllowedSubworlds[i].subworldNames.Any(world => world.ToLowerInvariant().Contains("start")) )
                                    StartWorld.unknownCellsAllowedSubworlds.RemoveAt(i);
                            }
                        }



                        StartWorld.worldTraitRules = new List<ProcGen.World.TraitRule>();

                        if (StartWorld.worldTraitRules != null && StartWorld.worldTraitRules.Count > 0)
                        {
                            foreach (var rule in sourceWorld.Value.worldTraitRules)
                            {
                                var newRule = new ProcGen.World.TraitRule(rule.min, rule.max);
                                newRule.requiredTags = new List<string>(rule.requiredTags);
                                newRule.specificTraits = new List<string>(rule.specificTraits);
                                newRule.forbiddenTags = new List<string>(rule.forbiddenTags);
                                newRule.forbiddenTraits = new List<string>(rule.forbiddenTraits);

                                if (newRule.forbiddenTags.Contains("StartChange"))
                                    newRule.forbiddenTags.Remove("StartChange");
                                if (newRule.forbiddenTags.Contains("StartWorldOnly"))
                                    newRule.forbiddenTags.Remove("StartWorldOnly");

                                StartWorld.worldTraitRules.Add(newRule);
                            }

                        }

                        StartWorld.filePath = newStartWorldPath;
                        StartWorld.startingBaseTemplate = ModAPI.GetStarterBaseTemplate(StartWorld, true);
                        StartWorld.startSubworldName = ModAPI.GetStartAreaSubworld(StartWorld, true);

                        //Starter Biome subworld files
                        var startBiome = new WeightedSubworldName(ModAPI.GetStartAreaSubworld(StartWorld, true), 1);
                        startBiome.overridePower = 3;

                        //var startBiomeWater = new WeightedSubworldName("expansion1::subworlds/sandstone/SandstoneMiniWater", 1);
                        //startBiomeWater.overridePower = 0.7f;
                        //startBiomeWater.maxCount = 2;

                        //StartWorld.subworldFiles.Insert(0, startBiomeWater);
                        StartWorld.subworldFiles.Insert(0, startBiome);
                        //Starter biome placement rules
                        ProcGen.World.AllowedCellsFilter CoreBiome = new ProcGen.World.AllowedCellsFilter();
                        CoreBiome.tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.Default;
                        CoreBiome.command = ProcGen.World.AllowedCellsFilter.Command.Replace;
                        CoreBiome.subworldNames = new List<string>() { ModAPI.GetStartAreaSubworld(StartWorld, true) };
                        
                        StartWorld.unknownCellsAllowedSubworlds.Insert(0, CoreBiome);


                        //Teleporter PlacementRules

                        //Deleting any of the existing teleporter templates

                        StartWorld.worldTemplateRules.RemoveAll((template) => ModAPI.IsATeleporterTemplate(StartWorld,template));


                        ProcGen.World.TemplateSpawnRules TeleporterSpawn = new ProcGen.World.TemplateSpawnRules();
                        TeleporterSpawn.names = new List<string>() 
                        { 
                            "expansion1::poi/warp/sender_mini", ///MaterialTeleporter sender
                            "expansion1::poi/warp/receiver_mini" ///MaterialTeleporter reciever 
                        };

                        if (additionalTemplates.Count > 0)
                            TeleporterSpawn.names.AddRange(additionalTemplates);

                        TeleporterSpawn.listRule = ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeAll;
                        TeleporterSpawn.priority = 90;
                        TeleporterSpawn.allowedCellsFilter = new List<ProcGen.World.AllowedCellsFilter>()
                        {
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.Replace,
                                tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.DistanceFromTag,
                                tag = "AtStart",
                                minDistance = 1,
                                maxDistance = 2,
                            },
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.ExceptWith,
                                tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.DistanceFromTag,
                                tag = "AtDepths",
                                minDistance = 0,
                                maxDistance = 0,
                            },
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.ExceptWith,
                                zoneTypes  = new List<SubWorld.ZoneType>() { SubWorld.ZoneType.Space
                                , SubWorld.ZoneType.MagmaCore
                                }
                            },
                            new ProcGen.World.AllowedCellsFilter()
                            {
                                command = ProcGen.World.AllowedCellsFilter.Command.ExceptWith,
                                tagcommand = ProcGen.World.AllowedCellsFilter.TagCommand.AtTag,
                                tag = "NoGravitasFeatures"
                            }
                        };
                        StartWorld.worldTemplateRules.Insert(0, TeleporterSpawn);

                        //foreach (var subworld in StartWorld.subworldFiles)
                        //    SgtLogger.l(subworld.name, "SUBWORLD");
                        //foreach (var unknownCell in StartWorld.unknownCellsAllowedSubworlds)
                        //    foreach (var name in unknownCell.subworldNames)
                        //        SgtLogger.l(name, "UNKNOWNCELL");
                        //SgtLogger.l(StartWorld.startSubworldName, "STARTSUB");



                        toAdd.Add(new(newStartWorldPath, StartWorld));

                        SgtLogger.l(newStartWorldPath, "Created Warp Planet Variant");

                    }

                    if (TypeToIgnore != StarmapItemCategory.Outer)
                    {
                        string newStartWorldPath = BaseName + "Outer";

                        var StartWorld = new ProcGen.World();

                        CopyValues(StartWorld, sourceWorld.Value);
                        StartWorld.filePath = newStartWorldPath;
                        StartWorld.startingBaseTemplate = null;
                        //StartWorld.startSubworldName = string.Empty;

                        StartWorld.unknownCellsAllowedSubworlds = new List<ProcGen.World.AllowedCellsFilter>(sourceWorld.Value.unknownCellsAllowedSubworlds);
                        StartWorld.subworldFiles = new List<WeightedSubworldName>(sourceWorld.Value.subworldFiles);

                        //if (StartWorld.subworldFiles != null && StartWorld.subworldFiles.Count > 0)
                        //{
                        //    for (int i = StartWorld.subworldFiles.Count - 1; i >= 0; --i)
                        //    {
                        //        if (StartWorld.subworldFiles[i].name.Contains("Start"))
                        //            StartWorld.subworldFiles.RemoveAt(i);
                        //    }
                        //}
                        //if (StartWorld.unknownCellsAllowedSubworlds != null && StartWorld.unknownCellsAllowedSubworlds.Count > 0)
                        //{
                        //    for (int i = StartWorld.unknownCellsAllowedSubworlds.Count - 1; i >= 0; --i)
                        //    {
                        //        if (StartWorld.unknownCellsAllowedSubworlds[i].subworldNames.Any(world => world.ToLowerInvariant().Contains("start")))
                        //            StartWorld.unknownCellsAllowedSubworlds.RemoveAt(i);
                        //    }
                        //}

                        StartWorld.worldTemplateRules = new List<ProcGen.World.TemplateSpawnRules>();
                        


                        if (sourceWorld.Value.worldTemplateRules != null && sourceWorld.Value.worldTemplateRules.Count > 0)
                        {
                            foreach (var rule in sourceWorld.Value.worldTemplateRules)
                            {
                                var ruleNew = new ProcGen.World.TemplateSpawnRules();
                                CopyValues(ruleNew, rule);

                                //if (ruleNew.listRule == ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeAll)
                                //    ruleNew.listRule = ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeSomeTryMore;
                                StartWorld.worldTemplateRules.Add(ruleNew);
                            }
                        }

                        StartWorld.worldTraitRules = new List<ProcGen.World.TraitRule>();

                        if (StartWorld.worldTraitRules.Count > 0)
                        {
                            foreach (var rule in sourceWorld.Value.worldTraitRules)
                            {
                                var newRule = new ProcGen.World.TraitRule(rule.min, rule.max);
                                newRule.requiredTags = new List<string>(rule.requiredTags);
                                newRule.specificTraits = new List<string>(rule.specificTraits);
                                newRule.forbiddenTags = new List<string>(rule.forbiddenTags);
                                newRule.forbiddenTraits = new List<string>(rule.forbiddenTraits);

                                if (newRule.forbiddenTags.Contains("StartChange"))
                                    newRule.forbiddenTags.Remove("StartChange");
                                if (newRule.forbiddenTags.Contains("StartWorldOnly"))
                                    newRule.forbiddenTags.Remove("StartWorldOnly");

                                StartWorld.worldTraitRules.Add(newRule);
                            }

                        }

                        //StartWorld.unknownCellsAllowedSubworlds.RemoveAll(cellsfilter => cellsfilter.tag == "AtStart");
                        //StartWorld.subworldFiles.RemoveAll(cellsfilter => cellsfilter.name.Contains("Start"));
                        StartWorld.worldTemplateRules.RemoveAll((template) => ModAPI.IsATeleporterTemplate(StartWorld, template));
                        //StartWorld.worldTemplateRules.ForEach(TemplateRule =>
                        //{
                        //    if (TemplateRule.listRule == ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeAll)
                        //        TemplateRule.listRule = ProcGen.World.TemplateSpawnRules.ListRule.GuaranteeSomeTryMore;
                        //}
                        //);

                        //StartWorld.worldTemplateRules.RemoveAll(cellsfilter => cellsfilter.allowedCellsFilter.Any(item=> item.tag=="AtStart"));

                        toAdd.Add(new(newStartWorldPath, StartWorld));
                        SgtLogger.l(newStartWorldPath, "Created Outer Planet Variant");
                    }


                }
                foreach (var item in toAdd)
                {
                    if (!__instance.worldCache.ContainsKey(item.Key))
                        __instance.worldCache.Add(item.Key, item.Value);
                }
            }
            static void CopyValues<T>(T targetObject, T sourceObject)
            {
                foreach (PropertyInfo property in typeof(T).GetProperties().Where(p => p.CanWrite))
                {
                    property.SetValue(targetObject, property.GetValue(sourceObject, null), null);
                }
            }
        }


        /// <summary>
        /// Makes error msg display the actual error instead of "couldn't germinate"
        /// </summary>
        [HarmonyPatch(typeof(WorldGen))]
        [HarmonyPatch(nameof(WorldGen.ReportWorldGenError))]
        public static class betterError
        {
            public static void Prefix(Exception e, ref string errorMessage)
            {
                if(CGSMClusterManager.LoadCustomCluster)
                    CGSMClusterManager.LastWorldGenDidFail();

                if (e.Message.Contains("Could not find a spot in the cluster for"))
                {
                    string planetName = e.Message.Replace("Could not find a spot in the cluster for ", string.Empty).Split()[0];
                    SgtLogger.l(planetName);
                    var planetData = SettingsCache.worlds.GetWorldData(planetName);
                    OverrideWorldSizeOnDataGetting.ResetCustomSizes();
                    if (planetData != null)
                    {
                        if (Strings.TryGet(planetData.name, out var name))
                        {
                            SgtLogger.error(name + " could not be placed.");
                            errorMessage = string.Format(STRINGS.ERRORMESSAGES.PLANETPLACEMENTERROR, name);
                            return;
                        }

                    }
                }
                if (e is TemplateSpawningException)
                {
                    errorMessage = e.Message;
                }
                OverrideWorldSizeOnDataGetting.ResetCustomSizes();
            }
        }

        /// <summary>
        /// During Cluster generation, load traits from custom cluster instead of randomized
        /// </summary>
        [HarmonyPatch(typeof(SettingsCache))]
        [HarmonyPatch(nameof(SettingsCache.GetRandomTraits))]
        public static class OverrideWorldTraits
        {
            /// <summary>
            /// Inserting Custom Traits
            /// </summary>
            public static bool Prefix(int seed, ProcGen.World world, ref List<string> __result)
            {
                if (CGSMClusterManager.LoadCustomCluster && CGSMClusterManager.CustomCluster != null)
                {
                    var traitIDs = CGSMClusterManager.CustomCluster.GiveWorldTraitsForWorldGen(world);

                    if (traitIDs.Count > 0)
                    {
                        List<WorldTrait> list = new List<WorldTrait>(SettingsCache.worldTraits.Values);

                        __result = new List<string>();
                        foreach (var trait in traitIDs)
                        {
                            //WorldTrait gatheredTrait = SettingsCache.GetCachedWorldTrait(trait, true);
                            __result.Add(trait);
                        }
                        return false;
                    }

                }
                return true;
            }
            public static void Postfix(ProcGen.World world, ref List<string> __result)
            {
                if (__result.Count > 0)
                {
                    var list = new List<string>();
                    int replaceCount = 0;
                    foreach (var trait in __result)
                    {
                        if (!trait.Contains("CGMRandomTraits"))
                        {
                            list.Add(trait);
                        }
                        else
                            ++replaceCount;
                    }
                    if (replaceCount > 0)
                    {
                        __result = CGSMClusterManager.CustomClusterData.AddRandomTraitsForWorld(list, world, replaceCount);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FileNameDialog))]
        [HarmonyPatch(nameof(FileNameDialog.OnActivate))]
        public static class FixCrashOnActivate
        {
            private static bool Prefix(FileNameDialog __instance)
            {
                if (CameraController.Instance == null)
                {
                    __instance.OnShow(show: true);
                    __instance.inputField.Select();
                    __instance.inputField.ActivateInputField();
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(FileNameDialog))]
        [HarmonyPatch(nameof(FileNameDialog.OnDeactivate))]
        public static class FixCrashOnDeactivate
        {
            private static bool Prefix(FileNameDialog __instance)
            {
                if (CameraController.Instance == null)
                {
                    __instance.OnShow(show: false);
                    return false;
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(WorldGenSettings))]
        [HarmonyPatch(nameof(WorldGenSettings.GetFloatSetting))]
        public static class WorldGenSettings_GetFloatSetting_Patch
        {
            private static void Postfix(WorldGenSettings __instance, string target, ref float __result)
            {
                if (!CGSMClusterManager.LoadCustomCluster)
                    return;
                if (!(target == "OverworldDensityMin") && !(target == "OverworldDensityMax") && !(target == "OverworldAvoidRadius") 
                    //&& !(target == "OverworldMinNodes") && !(target == "OverworldMaxNodes")
                    )
                    return;
                //SgtLogger.l("Target float:");
                __result = GetMultipliedSizeFloat(__result, __instance);
            }
        }
        [HarmonyPatch(typeof(WorldGenSettings))]
        [HarmonyPatch(nameof(WorldGenSettings.GetIntSetting))]
        public static class WorldGenSettings_GetIntSetting_Patch
        {
            private static void Postfix(WorldGenSettings __instance, string target, ref int __result)
            {
                if (!CGSMClusterManager.LoadCustomCluster || !(target == "OverworldDensityMin") && !(target == "OverworldDensityMax") && !(target == "OverworldAvoidRadius") && !(target == "OverworldMinNodes") && !(target == "OverworldMaxNodes"))
                    return;

                //SgtLogger.l("Target int:");
                __result = GetMultipliedSizeInt(__result, __instance);
            }
        }


        [HarmonyPatch(typeof(Border))]
        [HarmonyPatch(nameof(Border.ConvertToMap))]
        public static class Border_ConvertToMap_Patch
        {
            private static void Prefix(Border __instance)
            {
                if(CGSMClusterManager.LoadCustomCluster)
                    __instance.width = Mathf.Max(0.25f, __instance.width * borderSizeMultiplier);
            }
        }
        // static float OriginalBorder = -1f;

        [HarmonyPatch(typeof(WorldGenSettings))]
        [HarmonyPatch(nameof(WorldGenSettings.GetSubworldsForWorld))]
        public static class Getw
        {
            private static void Postfix(WorldGenSettings __instance, ref List<WeightedSubWorld> __result)
            {
                if (!CGSMClusterManager.LoadCustomCluster)
                    return;
                
                foreach (var weightedSubworld in __result)
                {
                    weightedSubworld.minCount = Mathf.Max(1, GetMultipliedSizeInt( weightedSubworld.minCount, __instance));
                }
            }
        }
        [HarmonyPatch(typeof(WorldLayout))]
        [HarmonyPatch(nameof(WorldLayout.ConvertUnknownCells))]
        public static class ApplyMultipliersToWeights
        {
            private static void Prefix(ref bool isRunningDebugGen)
            {
                if (CGSMClusterManager.LoadCustomCluster)
                    isRunningDebugGen = true;
            }
        }


        [HarmonyPatch(typeof(WorldGen))]
        [HarmonyPatch(nameof(WorldGen.GenerateOffline))]
        public static class GrabPlanetGenerating
        {
            private static void Prefix(WorldGen __instance)
            {
                if (!CGSMClusterManager.LoadCustomCluster)
                    return;
                if (__instance != null && __instance.Settings != null)
                {
                    borderSizeMultiplier =  Mathf.Min(1, GetMultipliedSizeFloat(1f, __instance.Settings));
                    SgtLogger.l(borderSizeMultiplier.ToString(), "BorderSizeMultiplier");
                }

            }
        }
        static float borderSizeMultiplier = 1f;

        public static float GetMultipliedSizeFloat(float inputNumber, WorldGenSettings worldgen)
        {

            if (worldgen != null && worldgen.world != null && worldgen.world.filePath != null &&
                CGSMClusterManager.CustomCluster.HasStarmapItem(worldgen.world.filePath, out var item)
                && (item.CurrentSizeMultiplier < 1)
                )
            {
                SgtLogger.l($"changed input float: {inputNumber}, multiplied: {item.ApplySizeMultiplierToValue((float)inputNumber)}","CGM WorldgenModifier");

                    return item.ApplySizeMultiplierToValue((float)inputNumber);
            }
            return inputNumber;
        }
        public static int GetMultipliedSizeInt(int inputNumber, WorldGenSettings worldgen)
        {

            if (worldgen!=null && worldgen.world!=null && worldgen.world.filePath != null &&
                CGSMClusterManager.CustomCluster.HasStarmapItem(worldgen.world.filePath, out var item)
                && (item.CurrentSizeMultiplier < 1))
            {

                SgtLogger.l($"changed input int: {inputNumber}, multiplied: {item.ApplySizeMultiplierToValue((float)inputNumber)}", "CGM WorldgenModifier");
                
                
                return Mathf.RoundToInt(item.ApplySizeMultiplierToValue((float)inputNumber));
            }
            return inputNumber;
        }




        [HarmonyPatch(typeof(Worlds))]
        [HarmonyPatch(nameof(Worlds.GetWorldData))]
        public static class OverrideWorldSizeOnDataGetting
        {
            public static Dictionary<string, Vector2I> OriginalPlanetSizes = new Dictionary<string, Vector2I>();
            /// <summary>
            /// Inserting Custom Traits
            /// </summary>
            /// 


            public static void ResetCustomSizes()
            {
                foreach (var world in SettingsCache.worlds.worldCache)
                {
                    if (OriginalPlanetSizes.ContainsKey(world.Key))
                    {
                        SgtLogger.l("Resetting custom planet size to " + world.Key + ", new size: " + OriginalPlanetSizes[world.Key].X + "x" + OriginalPlanetSizes[world.Key].Y, "CGM WorldgenModifier");
                        world.Value.worldsize = OriginalPlanetSizes[world.Key];
                    }
                }
                OriginalPlanetSizes.Clear();
            }

            public static bool Prefix(Worlds __instance, string name, ref ProcGen.World __result)
            {
                if (CGSMClusterManager.LoadCustomCluster && CGSMClusterManager.CustomCluster != null && ApplyCustomGen.IsGenerating)
                {
                    if (!name.IsNullOrWhiteSpace() && __instance.worldCache.TryGetValue(name, out var value))
                    {
                        if (CGSMClusterManager.CustomCluster.HasStarmapItem(name, out var item) && value.worldsize != item.CustomPlanetDimensions)
                        {

                            if (!OriginalPlanetSizes.ContainsKey(name))
                            {
                                Vector2I newDimensions = item.CustomPlanetDimensions;
                                SgtLogger.l(value.worldsize.ToString(), "Original World Size");
                                OriginalPlanetSizes[name] = value.worldsize;
                                value.worldsize = newDimensions;
                                //UtilMethods.ListAllPropertyValues(value.defaultsOverrides);
                                //foreach(var d in value.defaultsOverrides.data)
                                //{
                                //    SgtLogger.l(d.Value.ToString() + d.Value.GetType(), d.Key);
                                //}

                                // value.defaultsOverrides.data["OverworldDensityMin"] = (int)item.ApplySizeMultiplierToValue(int.Parse(value.defaultsOverrides.data["OverworldDensityMin"].ToString()));
                                // value.defaultsOverrides.data["OverworldDensityMax"] = (int)item.ApplySizeMultiplierToValue(int.Parse(value.defaultsOverrides.data["OverworldDensityMax"].ToString()));

                                SgtLogger.l("Applied custom planet size to " + item.DisplayName + ", new size: " + newDimensions.X + "x" + newDimensions.Y, "CGM WorldgenModifier");
                            }
                        }
                        else if (OriginalPlanetSizes.ContainsKey(name))
                        {
                            value.worldsize = OriginalPlanetSizes[name];
                            OriginalPlanetSizes.Remove(name);
                        }
                        //value.worldsize = new((int)(value.worldsize.x*0.8f), (int)(value.worldsize.y*0.8));
                        //SgtLogger.l("Applied custom planet size to " + name);

                        __result = value;
                    }
                    return false;
                }
                else if (OriginalPlanetSizes.ContainsKey(name) && __instance.worldCache.TryGetValue(name, out var value))
                {
                    value.worldsize = OriginalPlanetSizes[name];
                    OriginalPlanetSizes.Remove(name);
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(TemplateSpawning))]
        [HarmonyPatch(nameof(TemplateSpawning.SpawnTemplatesFromTemplateRules))]
        public static class AddSomeGeysers
        {
            static Dictionary<string, Dictionary<List<string>, int>> OriginalGeyserAmounts = new Dictionary<string, Dictionary<List<string>, int>>();
            /// <summary>
            /// Inserting Custom Traits
            /// </summary>
            public static void Prefix(WorldGenSettings settings)
            {
                const string geyserKey = "GEYSER";
                if (CGSMClusterManager.LoadCustomCluster && CGSMClusterManager.CustomCluster != null)
                {
                    if (!OriginalGeyserAmounts.ContainsKey(settings.world.filePath))
                        OriginalGeyserAmounts[settings.world.filePath] = new Dictionary<List<string>, int>();

                    if (CGSMClusterManager.CustomCluster.HasStarmapItem(settings.world.filePath, out var item) && item.CurrentSizePreset != WorldSizePresets.Normal)
                    {
                        foreach (var WorldTemplateRule in settings.world.worldTemplateRules)
                        {
                            if (WorldTemplateRule.names.Any(name => name.ToUpperInvariant().Contains(geyserKey)))
                            {
                                if (!OriginalGeyserAmounts[settings.world.filePath].ContainsKey(WorldTemplateRule.names))
                                {
                                    OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names] = WorldTemplateRule.times;
                                }
                                float newGeyserAmount = (((float)OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names]) *(float)item.CurrentSizePreset / 100f);
                                SgtLogger.l(string.Format("Adjusting geyser roll amount to worldsize for {0}; {1} -> {2}", WorldTemplateRule.names.FirstOrDefault(), OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names], newGeyserAmount), item.id);

                                if(newGeyserAmount > 1)
                                {
                                    WorldTemplateRule.times = Mathf.RoundToInt(newGeyserAmount);
                                    SgtLogger.l("new Geyser amount above/equal to 1, rounding to " + Mathf.RoundToInt(newGeyserAmount), "CGM WorldgenModifier");
                                }
                                else
                                {

                                    SgtLogger.l("new Geyser amount below 1, rolling for the geyser to appear at all..." );
                                    float chance =  ((float)new System.Random(CGSMClusterManager.CurrentSeed + BitConverter.ToInt32(MD5.Create().ComputeHash(Encoding.Default.GetBytes(WorldTemplateRule.names.First())), 0)).Next(100))/100f;
                                    SgtLogger.l("rolled: " + chance);
                                    if (chance <= newGeyserAmount)
                                    {
                                        SgtLogger.l("roll succeeded: " + chance * 100f, "POI Chance: " + newGeyserAmount.ToString("P"));
                                        WorldTemplateRule.times = 1;
                                    }
                                    else
                                    {
                                        SgtLogger.l("roll failed: " + chance * 100f, "POI Chance: " + newGeyserAmount.ToString("P"));
                                        WorldTemplateRule.times = 0;
                                    }
                                }

                                //WorldTemplateRule.times = Math.Max(1, Mathf.RoundToInt(((float)OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names]) * (float)item.CurrentSizePreset / 100f));
                                //SgtLogger.l(string.Format("Adjusting geyser roll amount to worldsize for {0}; {1} -> {2}", WorldTemplateRule.names.FirstOrDefault(), OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names], WorldTemplateRule.times), item.id);
                            }
                        }
                    }

                }
            }
            public static void Postfix(WorldGenSettings settings)
            {
                if (OriginalGeyserAmounts.ContainsKey(settings.world.filePath))
                {
                    foreach (var WorldTemplateRule in settings.world.worldTemplateRules)
                    {
                        if (OriginalGeyserAmounts[settings.world.filePath].ContainsKey(WorldTemplateRule.names))
                        {
                            SgtLogger.l(string.Format("Resetting Geyser rules back for {0}; {1} -> {2}", WorldTemplateRule.names.FirstOrDefault(), WorldTemplateRule.times, OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names]), WorldTemplateRule.ruleId);
                            WorldTemplateRule.times = OriginalGeyserAmounts[settings.world.filePath][WorldTemplateRule.names];
                        }
                    }
                    //OriginalGeyserAmounts.Remove(settings.world.filePath);
                }
            }
        }

        [HarmonyPatch(typeof(Cluster))]
        [HarmonyPatch(typeof(Cluster), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(string), typeof(int), typeof(List<string>), typeof(bool), typeof(bool) })]
        public static class ApplyCustomGen
        {
            public static bool IsGenerating = false;
            /// <summary>
            /// Setting ClusterID to custom cluster if it should load
            /// 
            /// </summary>
            public static void Prefix(ref string name)
            {
                //CustomLayout
                if (CGSMClusterManager.LoadCustomCluster)
                {
                    //if (CGSMClusterManager.CustomCluster == null)
                    //{
                    //    ///Generating custom cluster if null
                    //    CGSMClusterManager.AddCustomClusterAndInitializeClusterGen();
                    //}
                    name = CGSMClusterManager.CustomClusterID;
                    IsGenerating = true;
                }
            }
            public static void Postfix()
            {
                IsGenerating = false;
            }
        }
    }
}
