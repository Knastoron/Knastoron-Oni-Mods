﻿using ONITwitchLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util_TwitchIntegrationLib;
using UtilLibs;

namespace Imalas_TwitchChaosEvents.Events
{
    internal class FogEvent : ITwitchEventBase
    {
        public string ID => "ChaosTwitch_Fog";

        public string EventGroupID => null;

        public string EventName => STRINGS.CHAOSEVENTS.FOG.NAME;

        public string EventDescription => STRINGS.CHAOSEVENTS.FOG.TOASTTEXT;

        public EventWeight EventWeight => EventWeight.WEIGHT_COMMON;

        public Action<object> EventAction => (object data) =>
        {
            GameScheduler.Instance.Schedule("fog start", 15f, _ =>
            {
                ToastManager.InstantiateToast(
                STRINGS.CHAOSEVENTS.FOG.TOAST,
                 STRINGS.CHAOSEVENTS.FOG.TOASTTEXT
                 );
                SoundUtils.PlaySound(ModAssets.SOUNDS.THUNDERSTRIKE, SoundUtils.GetSFXVolume() * 1.0f, true);
            });

            var pos = CameraController.Instance.baseCamera.transform.GetPosition();
            pos.z = 40;
            ModAssets.CurrentFogGO = Util.KInstantiate(ModAssets.FogSpawner, pos);

            ModAssets.CurrentFogGO.SetActive(true);

            SgtLogger.l(CameraController.Instance.baseCamera.transform.position.ToString(), "CAMERA");


            GameScheduler.Instance.Schedule("fog removal", 600f, _ =>
            {
                ToastManager.InstantiateToast(
                STRINGS.CHAOSEVENTS.FOG.TOAST,
                 STRINGS.CHAOSEVENTS.FOG.TOASTTEXTENDING
                 );
                UnityEngine.Object.Destroy(ModAssets.CurrentFogGO);
                ModAssets.CurrentFogGO = null;
            });
        };

        public Func<object, bool> Condition =>
            (data) =>
            {
                return ModAssets.CurrentFogGO == null;
            };

        public Danger EventDanger => Danger.None;

    }
}
