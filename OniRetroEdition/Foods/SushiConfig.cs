﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OniRetroEdition.Foods
{
    internal class SushiConfig : IEntityConfig
    {
        public const string ID = "Sushi";
        public static ComplexRecipe recipe;

        public GameObject CreatePrefab()
        {
            EdiblesManager.FoodInfo SUSHI = new EdiblesManager.FoodInfo(ID, "", 1600000f, 3, 255.15f, 277.15f, 9600f, can_rot: true);

            return EntityTemplates.ExtendEntityToFood(EntityTemplates.CreateLooseEntity("Sushi", (string)global::STRINGS.ITEMS.FOOD.SUSHI.NAME, (string)global::STRINGS.ITEMS.FOOD.SUSHI.DESC, 1f, false, Assets.GetAnim((HashedString)"zestysalsa_kanim"), "object", Grid.SceneLayer.Front, EntityTemplates.CollisionShape.RECTANGLE, 0.8f, 0.5f, true), SUSHI);
        }

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }
}
