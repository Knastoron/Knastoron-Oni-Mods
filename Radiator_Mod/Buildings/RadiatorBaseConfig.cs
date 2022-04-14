﻿using RadiatorMod.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUNING;
using UnityEngine;

namespace RoboRockets.Buildings
{
    class RadiatorBaseConfig : IBuildingConfig
    {
        public const string ID = "RadiatorBase";
        public const string NAME = "Space Radiator";

        private static readonly List<Storage.StoredItemModifier> StoredItemModifiers = new List<Storage.StoredItemModifier>()
        {
            Storage.StoredItemModifier.Hide,
            Storage.StoredItemModifier.Insulate,
            Storage.StoredItemModifier.Seal
        };

        public override BuildingDef CreateBuildingDef()
        {
            float[] matCosts = { 1200f
                    //, 400f 
            };

            string[] construction_materials = new string[]
                {
                    "RefinedMetal"
                   // ,"KATAIRITE"
                };
            EffectorValues tieR2 = NOISE_POLLUTION.NONE;
            EffectorValues none2 = BUILDINGS.DECOR.NONE;
            EffectorValues noise = tieR2;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, 2, 6, "heat_radiator_kanim", 100, 120f, matCosts, construction_materials, 1600f, BuildLocationRule.Anywhere, none2, noise);
            BuildingTemplates.CreateFoundationTileDef(buildingDef);

            buildingDef.InputConduitType = ConduitType.Liquid;
            buildingDef.OutputConduitType = ConduitType.Liquid;
            buildingDef.Floodable = false;

            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(1, 0);

            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.TileLayer = ObjectLayer.FoundationTile;

            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.ViewMode = OverlayModes.LiquidConduits.ID;

            buildingDef.OverheatTemperature = 398.15f;
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.LiquidVentIDs, ID);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Liquid;
            conduitConsumer.consumptionRate = 10f;
            conduitConsumer.alwaysConsume = true;
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<RadiatorBase>();
            UnityEngine.Object.DestroyImmediate(go.GetComponent<RequireInputs>());
            UnityEngine.Object.DestroyImmediate(go.GetComponent<ConduitConsumer>());
            UnityEngine.Object.DestroyImmediate(go.GetComponent<ConduitDispenser>());
           // AddVisualPreview(go, false);

            MakeBaseSolid.Def solidBase = go.AddOrGetDef<MakeBaseSolid.Def>();
            solidBase.occupyFoundationLayer = true;
            solidBase.solidOffsets = new CellOffset[]
            {
                new CellOffset(0, 0),
                new CellOffset(1, 0)
            };

        }
        //public override void DoPostConfigurePreview(BuildingDef def, GameObject go) => RadiatorBaseConfig.AddVisualPreview(go, true);

        //public override void DoPostConfigureUnderConstruction(GameObject go) => RadiatorBaseConfig.AddVisualPreview(go, false);

        private static void AddVisualPreview(GameObject go, bool movable) { 
        
            var vis = go.AddOrGet<StationaryChoreRangeVisualizer>();
            vis.y = 1;
            vis.width = 2;
            vis.height = 5;
            vis.vision_offset = new CellOffset(0, 1);
            vis.blocking_tile_visible = false;
            vis.movable = movable;
        }
    }
}
