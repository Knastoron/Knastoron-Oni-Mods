﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Util_TwitchIntegrationLib.Scripts
{
    public class LiquidRainSpawner : KMonoBehaviour, ISim200ms
    {
        [SerializeField] public SimHashes elementId;
        [SerializeField] public (float, float) totalAmountRangeKg;
        [SerializeField] public float spawnRadius;
        [SerializeField] public float dropletMassKg;
        [SerializeField] public float durationInSeconds;
        [SerializeField] private float temperature;
        [SerializeField] private bool overrideTemperature;

        public float TIMEOUT = 600;

        public float elapsedTime;

        public int density;

        private float totalMassToBeSpawnedKg;
        private float spawnedMass;
        private Element element;
        private bool raining;
        private int originCell;

        public void SetTemperature(float celsius)
        {
            temperature = GameUtil.GetTemperatureConvertedToKelvin(celsius, GameUtil.TemperatureUnit.Celsius);
            overrideTemperature = true;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            totalMassToBeSpawnedKg = UnityEngine.Random.Range(totalAmountRangeKg.Item1, totalAmountRangeKg.Item2);
            element = ElementLoader.FindElementByHash(elementId);
            var totalDropletCount = totalMassToBeSpawnedKg / dropletMassKg;
            density = (int)(totalDropletCount / durationInSeconds);
        }

        public void StartRaining()
        {
            transform.position = ONITwitchLib.Utils.PosUtil.ClampedMouseCellWorldPos();
            originCell = Grid.PosToCell(this);
            raining = true;
        }
        public int OriginCell => originCell;

        public void Sim200ms(float dt)
        {
            if (!raining)
            {
                return;
            }

            elapsedTime += dt;
            if (elapsedTime > TIMEOUT)
            {
                Util.KDestroyGameObject(gameObject);
                return;
            }

            for (int i = 0; i < density; i++)
            {
                //var cell = ONITwitchLib.Utils.PosUtil.ClampedMousePosWithRange(spawnRadius);
                var pos = UnityEngine.Random.insideUnitCircle * spawnRadius;
                var cell = Grid.OffsetCell(originCell, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));

                if (!Grid.IsValidCellInWorld(cell, this.GetMyWorldId()) || Grid.Solid[cell])
                    continue;

                FallingWater.instance.AddParticle(
                    cell,
                    element.idx,
                    dropletMassKg,
                    overrideTemperature ? temperature : element.defaultValues.temperature,
                    byte.MaxValue,
                    0);

                spawnedMass += dropletMassKg;

                if (spawnedMass > totalMassToBeSpawnedKg)
                {
                    Util.KDestroyGameObject(gameObject);
                    return;
                }
            }
        }
    }
}

