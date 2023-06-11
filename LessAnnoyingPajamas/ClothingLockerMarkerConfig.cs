﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUNING;
using UnityEngine;

namespace ClothingLockerMod
{
    internal class ClothingLockerMarkerConfig : IBuildingConfig
    {
        public const string ID = "CLM_ClothingLockerMarker";

        public override BuildingDef CreateBuildingDef()
        {
            string[] refinedMetals = MATERIALS.REFINED_METALS;
            float[] construction_mass = new float[1]
            {
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0]
            };
            string[] construction_materials = refinedMetals;
            EffectorValues none = NOISE_POLLUTION.NONE;
            EffectorValues tieR1 = BUILDINGS.DECOR.BONUS.TIER1;
            EffectorValues noise = none;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, 1, 2, "setpiece_locker_kanim", 30, 30f, construction_mass, construction_materials, 1600f, BuildLocationRule.OnFloor, tieR1, noise);
            buildingDef.PreventIdleTraversalPastBuilding = true;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SuitIDs, ID);
            return buildingDef;
        }


        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<SuitLocker>().OutfitTags = new Tag[1]
            {
               SleepClinicPajamas.ID
            };
            Prioritizable.AddRef(go); 
            
            SuitMarker suitMarker = go.AddOrGet<SuitMarker>();
            suitMarker.LockerTags = new Tag[1]
            {
                new Tag(ClothingLockerConfig.ID)
            };
            suitMarker.PathFlag = ModAssets.PajamasFlag;

            go.AddTag(GameTags.JetSuitBlocker);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
        }

        public override void DoPostConfigureComplete(GameObject go) => SymbolOverrideControllerUtil.AddToPrefab(go);
    }

}
