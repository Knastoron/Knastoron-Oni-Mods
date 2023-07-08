﻿using KSerialization;
using Rockets_TinyYetBig.Docking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilLibs;

namespace Rockets_TinyYetBig.Behaviours
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class DockingDoor : KMonoBehaviour, ISidescreenButtonControl
    {
        /// <summary>
        /// Transfer Storages
        /// </summary>



        [MyCmpGet]
        public Assignable assignable;


        [MyCmpGet]
        public NavTeleporter Teleporter;
        [MyCmpGet]
        public MoveToDocked MoveTo; 

        [Serialize]
        Ref<DockingDoor> connected = null;

        public CellOffset porterOffset = new CellOffset(0,0);

        public DockingManager dManager;

        public CraftModuleInterface GetCraftModuleInterface()
        {
            return GetWorldObject().GetComponent<CraftModuleInterface>();
        }
        public CraftModuleInterface GetDockedCraftModuleInterface()
        {
            if (connected == null)
                return null;
            else
            {
                if (connected.Get() != null && connected.Get().GetWorldObject() != null && connected.Get().GetWorldObject().TryGetComponent<CraftModuleInterface>(out var interfac))
                    return interfac;
                return null;
            }
        }

        public void ConnecDoor(DockingDoor d)
        {
            // SgtLogger.debuglog("Door: " + d);
            this.Trigger((int)GameHashes.RocketLanded);
            d.Trigger((int)GameHashes.RocketLanded);
            this.Trigger((int)GameHashes.ChainedNetworkChanged);
            connected = new Ref<DockingDoor>(d);
            Teleporter.SetTarget(d.Teleporter);
            if (!this.gameObject.IsNullOrDestroyed() && gameObject.TryGetComponent<KBatchedAnimController>(out var kanim))
            {
                kanim.Play("extending");
                kanim.Queue("extended");
            }
            assignable.canBeAssigned = true;
            //DetailsScreen.Instance.Refresh(gameObject);
        }

        public bool IsConnected => GetConnec() != null;

        public DockingDoor GetConnec()
        {
            if(connected!=null)
                return connected.Get();
            return null;
        }

        public int GetConnectedTargetWorldId()
        {
            if (connected != null)
                return connected.Get().GetMyWorldId();
            return -1;
        }

        public void DisconnecDoor(bool skipanim = false)
        {
#if DEBUG
            SgtLogger.debuglog(dManager.GetWorldId() + " disconneccted from " + connected.Get().dManager.GetWorldId());
#endif

            this.Trigger((int)GameHashes.RocketLaunched);
            this.Trigger((int)GameHashes.ChainedNetworkChanged);
            if (this.gameObject.IsNullOrDestroyed())
            {
                SgtLogger.l("wasdestroyed");
                return;
            }

            if (gameObject == null || assignable == null || Teleporter == null) return;

            connected = null;
            assignable.Unassign();
            assignable.canBeAssigned = false;
            Teleporter.SetTarget(null);

            if (skipanim)
                return;

            if (gameObject.TryGetComponent<KBatchedAnimController>(out var kanim))
            {
                SgtLogger.l("cleanup: "+skipanim);
                if (!skipanim)
                {
                    kanim.Play("retracting");
                    kanim.Queue("retracted");
                }
                else
                {
                    kanim.Play("retracted");
                }
            }
            //DetailsScreen.Instance.Refresh(gameObject);
        }

        public CellOffset GetRotatedOffset()
        {
            var offset = porterOffset;
            if (TryGetComponent<Rotatable>(out var rotatable))
            {
                offset = rotatable.GetRotatedCellOffset(porterOffset);
            }
            return offset;
        }
        public int GetPorterCell()
        {
            return Grid.OffsetCell(Grid.PosToCell(this), GetRotatedOffset());
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            //dManager = ModAssets.Dockables.Items.Find(item => item.GetWorldId() == worldId);//GetRocket().GetComponent<DockingdManager>();
            dManager = ClusterUtil.GetMyWorld(this.gameObject).gameObject.AddOrGet<DockingManager>();

            Teleporter.offset = GetRotatedOffset();
            Teleporter.OnCellChanged();

            dManager.AddDoor(this);
            dManager.SetManagerType();
            string startKanim = string.Empty;
            if (connected != null && connected.Get() != null && connected.Get().Teleporter != null)
            {
                Teleporter.SetTarget(connected.Get().Teleporter);
                startKanim = ("extended");
                assignable.canBeAssigned = true;
            }
            else
            {
                startKanim = ("retracted");
                assignable.Unassign();
                assignable.canBeAssigned = false;
            }
            if(gameObject.TryGetComponent<KBatchedAnimController>(out var kanim))
            {
                kanim.Play(startKanim);
            }
        }

        protected override void OnCleanUp()
        {
            dManager.RemoveDoor(this);
            base.OnCleanUp();
        }

        private GameObject GetWorldObject()
        {
            WorldContainer world = ClusterManager.Instance.GetWorld(dManager.WorldId);
            return world == null ? null : world.gameObject;
        }

        #region Sidescreen

        public string SidescreenButtonText => STRINGS.UI_MOD.UISIDESCREENS.DOCKINGSIDESCREEN.BUTTON;

        public string SidescreenButtonTooltip => STRINGS.UI_MOD.UISIDESCREENS.DOCKINGSIDESCREEN.BUTTONINFO;

        public int ButtonSideScreenSortOrder()
        {
            return 20;
        }

        public void OnSidescreenButtonPressed()
        {
            if(connected!=null && connected.Get()!=null)
            {
                ClusterManager.Instance.SetActiveWorld(connected.Get().GetMyWorld().id);
                SelectTool.Instance.Activate();
            }
        }

        public void SetButtonTextOverride(ButtonMenuTextOverride text) => throw new NotImplementedException();
        public bool SidescreenButtonInteractable() => assignable.canBeAssigned;

        public bool SidescreenEnabled()
        {
            return true;
        }

        public int HorizontalGroupID()
        {
            return -1;
        }
        #endregion
    }
}
