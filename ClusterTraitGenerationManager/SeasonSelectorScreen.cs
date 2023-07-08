﻿using ProcGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilLibs;
using UtilLibs.UIcmp;
using static ClusterTraitGenerationManager.CGSMClusterManager;
using UnityEngine.UI;
using Klei.AI;
using static ClusterTraitGenerationManager.STRINGS.UI.CGM.INDIVIDUALSETTINGS;

namespace ClusterTraitGenerationManager
{
    internal class SeasonSelectorScreen : FScreen
    {
        public static SeasonSelectorScreen Instance { get; private set; }

        Dictionary<string, GameObject> Seasons = new Dictionary<string, GameObject>();
        public StarmapItem SelectedPlanet;
        public static System.Action OnCloseAction;

        public bool IsCurrentlyActive = false;
        public string ReplaceOldSeason = string.Empty;

        public static void InitializeView(StarmapItem _planet, System.Action onclose, string seasonToReplace = "")
        {
            if(Instance == null)
            {
                var screen = Util.KInstantiateUI(ModAssets.TraitPopup, FrontEndManager.Instance.gameObject, true);
                Instance = screen.AddOrGet<SeasonSelectorScreen>();
                Instance.Init();
            }
            OnCloseAction = onclose;
            Instance.ReplaceOldSeason = seasonToReplace;
            Instance.Show(true);
            Instance.SelectedPlanet = _planet;
            Instance.ConsumeMouseScroll = true;
            Instance.transform.SetAsLastSibling();


            if (CustomCluster.HasStarmapItem(_planet.id, out var item))
            {
                foreach (var traitContainer in Instance.Seasons.Values)
                {
                    traitContainer.SetActive(true);
                }
                foreach (var activeSeason in item.CurrentMeteorSeasons)
                {
                    Instance.Seasons[activeSeason.Id].SetActive(false);
                }
            }
        }

        private GameObject SeasonPrefab;
        private GameObject PossibleSeasonContainer;
        private bool init=false;
        private void Init()
        {
            if(init) return;
            init=true;
            SeasonPrefab = transform.Find("ScrollArea/Content/ListViewEntryPrefab").gameObject;
            PossibleSeasonContainer = transform.Find("ScrollArea/Content").gameObject;

            var closeButton = transform.Find("CancelButton").FindOrAddComponent<FButton>();
            closeButton.OnClick += () =>
            {
                OnCloseAction.Invoke();
                Show(false);
            };
            UIUtils.TryChangeText(transform, "Text", METEORSEASON.SEASONSELECTOR.TITEL);
            UIUtils.TryChangeText(PossibleSeasonContainer.transform, "NoTraitAvailable/Label", METEORSEASON.SEASONSELECTOR.NOSEASONTYPESAVAILABLE);


            InitializeTraitContainer();
        }
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.ConsumeMouseScroll = true;
            
            Init();
        }

        void InitializeTraitContainer()
        {
            foreach (var gameplaySeason in Db.Get().GameplaySeasons.resources)
            {
                if (!(gameplaySeason is MeteorShowerSeason) || gameplaySeason.Id.Contains("Fullerene") || gameplaySeason.Id.Contains("TemporalTear") || gameplaySeason.dlcId != DlcManager.EXPANSION1_ID)
                    continue;
                var meteorSeason = gameplaySeason as MeteorShowerSeason;

                var seasonInstanceHolder = Util.KInstantiateUI(SeasonPrefab, PossibleSeasonContainer, true);


                string name = meteorSeason.Name.Replace("MeteorShowers", string.Empty);
                string description = meteorSeason.events.Count == 0 ? METEORSEASON.SEASONSELECTOR.SEASONTYPENOMETEORSTOOLTIP : METEORSEASON.SEASONSELECTOR.SEASONTYPETOOLTIP; 
                
                foreach (var meteorShower in meteorSeason.events)
                {
                    description += "\n • ";
                    description += Assets.GetPrefab((meteorShower as MeteorShowerEvent).clusterMapMeteorShowerID).GetProperName();// Assets.GetPrefab((Tag)meteor.prefab).GetProperName();
                }
                UIUtils.AddSimpleTooltipToObject(seasonInstanceHolder.transform, description);

                var icon = seasonInstanceHolder.transform.Find("Label/TraitImage").GetComponent<Image>();
                icon.gameObject.SetActive(false);

                UIUtils.TryChangeText(seasonInstanceHolder.transform, "Label", name);

                var AddTraitButton = seasonInstanceHolder.transform.Find("AddThisTraitButton").gameObject.FindOrAddComponent<FButton>();
                seasonInstanceHolder.transform.Find("AddThisTraitButton/Text").gameObject.FindOrAddComponent<LocText>().text = METEORSEASON.SEASONSELECTOR.ADDSEASONTYPEBUTTONLABEL;

                AddTraitButton.OnClick += () =>
                {
                    if (CustomCluster.HasStarmapItem(SelectedPlanet.id, out var item))
                    {
                        if(ReplaceOldSeason != string.Empty)
                        {
                            item.RemoveMeteorSeason(ReplaceOldSeason);
                        }
                        item.AddMeteorSeason(gameplaySeason.Id);
                    }
                    CloseThis();
                };

                Seasons[gameplaySeason.Id] = seasonInstanceHolder;
            }
        }


        public override void Show(bool show = true)
        {
            base.Show(show);
            this.IsCurrentlyActive = show;
        }
        void CloseThis()
        {
            OnCloseAction.Invoke();
            Show(false);
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(Action.Escape) || e.TryConsume(Action.MouseRight))
            {
                //SgtLogger.l("CONSUMING 3");
                CloseThis();
            }

            base.OnKeyDown(e);
        }
    }
}
