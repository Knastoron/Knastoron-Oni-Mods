﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilLibs;

namespace UL_UniversalLyzer
{
    public class MultiConverterElectrolyzer : StateMachineComponent<MultiConverterElectrolyzer.StatesInstance>
    {
        [SerializeField]
        public float maxMass = 2.5f;
        [SerializeField]
        public bool hasMeter = true;
        [SerializeField]
        public CellOffset emissionOffset = CellOffset.none;
        [MyCmpAdd]
        private Storage storage;
        public List<ElementConverter> converterList = new List<ElementConverter>();
        [MyCmpReq]
        private Operational operational;
        private MeterController meter;

        [MyCmpGet]
        KBatchedAnimController controller;

        public override void OnSpawn()
        {
            converterList = this.GetComponents<ElementConverter>().ToList();

            if (this.hasMeter)
                this.meter = new MeterController(controller, "U2H_meter_target", "meter", Meter.Offset.Behind, Grid.SceneLayer.NoLayer, new Vector3(-0.4f, 0.5f, -0.1f), new string[4]
                {
                    "U2H_meter_target",
                    "U2H_meter_tank",
                    "U2H_meter_waterbody",
                    "U2H_meter_level"
                });
            this.smi.StartSM();
            this.UpdateMeter();
            Tutorial.Instance.oxygenGenerators.Add(this.gameObject);
        }

        public override void OnCleanUp()
        {
            Tutorial.Instance.oxygenGenerators.Remove(this.gameObject);
            base.OnCleanUp();
        }

        public void UpdateMeter()
        {
            if (!this.hasMeter)
                return;
            this.meter.SetPositionPercent(Mathf.Clamp01(this.storage.MassStored() / this.storage.capacityKg));
            UpdateColor();
        }
        public void UpdateColor()
        {
            var liquid = storage.FindFirstWithMass(GameTags.AnyWater,0.2f);
            if (liquid != null && liquid.TryGetComponent<PrimaryElement>(out var element))
            {
                var color = element.Element.substance.conduitColour;
                meter.SetSymbolTint("u2h_meter_waterlevel", color);
                meter.SetSymbolTint("u2h_meter_waterbody", color);
                controller.SetSymbolTint("u1h_fxbubbles", color);
                controller.SetSymbolTint("filterwater", color);
                controller.SetSymbolTint("bub", color);
            }
        }


        private bool RoomForPressure => Config.Instance.IsPiped ? true : !GameUtil.FloodFillCheck(new Func<int, MultiConverterElectrolyzer, bool>(MultiConverterElectrolyzer.OverPressure), this, Grid.OffsetCell(Grid.PosToCell(this.transform.GetPosition()), this.emissionOffset), 3, true, true);

        private static bool OverPressure(int cell, MultiConverterElectrolyzer MultiConverterElectrolyzer) => (double)Grid.Mass[cell] > MultiConverterElectrolyzer.maxMass;

        public class StatesInstance :
          GameStateMachine<States, StatesInstance, MultiConverterElectrolyzer, object>.GameInstance
        {
            public StatesInstance(MultiConverterElectrolyzer smi)
              : base(smi)
            {
            }
        }

        public class States :
          GameStateMachine<States, StatesInstance, MultiConverterElectrolyzer>
        {
            public State disabled;
            public State waiting;
            public State converting;
            public State overpressure;

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = disabled;
                this.root
                    .EventTransition(GameHashes.OperationalChanged, this.disabled, smi => !smi.master.operational.IsOperational)
                    .EventHandler(GameHashes.OnStorageChange, smi => smi.master.UpdateMeter());
                this.disabled
                    .EventTransition(GameHashes.OperationalChanged, this.waiting, smi => smi.master.operational.IsOperational);
                this.waiting
                    .Enter("Waiting", smi => smi.master.operational.SetActive(false))
                    .EventTransition(GameHashes.OnStorageChange, this.converting, smi => smi.master.HasEnoughMassToStartConverting());
                this.converting.Enter("Ready", smi => smi.master.operational.SetActive(true))
                          .Transition(this.waiting, smi => !smi.master.CanConvertAtAll())
                          .Transition(this.overpressure, smi => !smi.master.RoomForPressure)
                        .Update((smi, dt) =>
                        {
                            smi.master.UpdateStatusItems();
                            smi.master.UpdateColor();
                        });
                this.overpressure
                    .Enter("OverPressure", smi => smi.master.operational.SetActive(false))
                    .ToggleStatusItem(Db.Get().BuildingStatusItems.PressureOk)
                    .Transition(this.converting, smi => smi.master.RoomForPressure);
            }
        }
        private void UpdateStatusItems()
        {
            foreach (ElementConverter converter in converterList)
            {
                if (converter.CanConvertAtAll())
                {
                    if (converter.consumedElementStatusHandles.Count == 0)
                        converter.smi.AddStatusItems();
                }
                else
                {
                    converter.smi.RemoveStatusItems();

                }
            }

        }
        private bool CanConvertAtAll()
        {
            foreach (var converter in converterList)
            {
                if (converter.CanConvertAtAll()) return true;
            }
            return false;
        }
        private bool HasEnoughMassToStartConverting()
        {
            foreach (var converter in converterList)
            {
                if (converter.HasEnoughMassToStartConverting()) return true;
            }
            return false;
        }
        private Substance lastElement = default;
        private bool ElementChanged()
        {
            if (storage.items.Count == 0) return false;

            if (storage.items.First().TryGetComponent<PrimaryElement>(out var element))
            {
                if (lastElement != element.Element.substance)
                {
                    lastElement = element.Element.substance;
                    return true;
                }
            }
            return false;
        }
    }

}
