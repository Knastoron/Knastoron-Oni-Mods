﻿using HarmonyLib;
using Rockets_TinyYetBig.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilLibs;
using static STRINGS.BUILDING.STATUSITEMS.ACCESS_CONTROL;
using static UtilLibs.RocketryUtils;

namespace Rockets_TinyYetBig
{


    class CategorizedRocketBuildMenuPatches
    {
        /// <summary>
        /// Enables the rocket module menu searchbar input on ctrl+f input 
        /// </summary>
        [HarmonyPatch(typeof(KScreen))]
        [HarmonyPatch(nameof(KScreen.OnKeyDown))]
        public static class ConsumeInputs
        {
            public static void Prefix(KScreen __instance, KButtonEvent e)
            {
                if (__instance is SelectModuleSideScreen)
                {
                    __instance.isEditing = CategoryPatchTest.EditingSearch;
                    if (e.TryConsume(Action.DebugToggleClusterFX))
                    {
                        CategoryPatchTest.SearchBar.Select();
                        CategoryPatchTest.SearchBar.ActivateInputField();
                        KScreenManager.Instance.RefreshStack();
                    }
                }
            }
        }

        /// <summary>
        /// Add consumeMouseScroll so scrolling inside the screen no longer scolls the world outside of it
        /// </summary>
        [HarmonyPatch(typeof(SelectModuleSideScreen))]
        [HarmonyPatch(nameof(SelectModuleSideScreen.OrderBuildSelectedModule))]
        public static class FixScrollingAfterModuleIsBuilt
        {
            public static void Postfix(SelectModuleSideScreen __instance)
            {
                __instance.ConsumeMouseScroll = true;
            }
        }

        /// <summary>
        /// Clear Rocket menu searchbar text on close
        /// </summary>
        [HarmonyPatch(typeof(DetailsScreen))]
        [HarmonyPatch(nameof(DetailsScreen.ClearSecondarySideScreen))]
        public static class ClearSearchBarAfterOnShow
        {
            public static void Prefix()
            {
                CategoryPatchTest.ClearSearchBar();
            }
        }
        /// <summary>
        /// add building categories and searchbar to Rocket module screen
        /// </summary>
        [HarmonyPatch(typeof(SelectModuleSideScreen))]
        [HarmonyPatch(nameof(SelectModuleSideScreen.SpawnButtons))]
        public static class CategoryPatchTest
        {
            public static KInputTextField SearchBar = null;
            static Dictionary<int, MultiToggle> HeaderButtons = new Dictionary<int, MultiToggle>();
            static Dictionary<int, bool> HeaderButtonsActiveStatus = new Dictionary<int, bool>();
            static Dictionary<int, GameObject> Grids = new Dictionary<int, GameObject>();
            public static Dictionary<BuildingDef, GameObject> SearchableButtons = new Dictionary<BuildingDef, GameObject>();

            public static void ToggleCategoriesSearch(bool searchActive)
            {
                var items = Db.Get().TechItems;

                foreach (var item in HeaderButtons.Values)
                {
                    if (item != null && item.gameObject != null && !item.IsNullOrDestroyed())
                        item.gameObject.SetActive(CategoriesActive && !searchActive);
                }
                foreach (var item in Grids)
                {
                    if (!CategoriesActive || searchActive)
                    {
                        if (item.Value != null && !item.Value.IsNullOrDestroyed())
                            item.Value.SetActive(false);

                    }
                    else
                    {
                        if (item.Value != null && !item.Value.IsNullOrDestroyed())
                            item.Value.SetActive(HeaderButtonsActiveStatus[item.Key]);
                    }
                }
                foreach (var item in SearchableButtons)
                {
                    TechItem techItem = items.TryGet(item.Key.PrefabID);
                    bool active = true;
                    if (techItem != null)
                    {
                        active = DebugHandler.InstantBuildMode || Game.Instance.SandboxModeActive || techItem.IsComplete();
                    }
                    if (item.Value != null && !item.Value.IsNullOrDestroyed())
                        item.Value.SetActive(!CategoriesActive && !searchActive && active);
                }
            }

            public static void ClearSearchBar()
            {
                if (SearchBar != null)
                {
                    SearchBar.text = string.Empty;
                    ToggleCategoriesSearch(false);
                }
            }

            static void SetCategoryVisibility(bool showCategories)
            {
                if (showCategories != CategoriesActive)
                {
                    CategoriesActive = showCategories;
                    RTB_SavegameStoredSettings.Instance.useModuleCategories = showCategories;
                    ToggleCategoriesSearch(false);
                }
            }
            static bool CategoriesActive = false;

            public static void ApplySearchText(string searchText)
            {
                ToggleCategoriesSearch(searchText.Length > 0);
                var items = Db.Get().TechItems;
                foreach (var ModuleButton in SearchableButtons)
                {
                    bool withinParams = searchText.Length > 0 ? ModuleButton.Key.Name.ToLowerInvariant().Contains(searchText.ToLowerInvariant()) || ModuleButton.Key.PrefabID.ToLowerInvariant().Contains(searchText.ToLowerInvariant()) : false;

                    TechItem techItem = items.TryGet(ModuleButton.Key.PrefabID);
                    if (techItem != null)
                    {
                        bool active = DebugHandler.InstantBuildMode || Game.Instance.SandboxModeActive || techItem.IsComplete();
                        if (withinParams)
                            withinParams = active;
                    }

                    ModuleButton.Value.SetActive(withinParams);
                }
            }

            public static void ToggleCategory(int category, SelectModuleSideScreen reference, bool initializing = false)
            {
                string categoryName = category.ToString();

                if (initializing)
                {
                    foreach (var cat in (RocketCategory[])Enum.GetValues(typeof(RocketCategory)))
                    {
                        if (HeaderButtons.ContainsKey((int)cat))
                        {
                            HeaderButtonsActiveStatus[(int)cat] = false;
                            Grids[(int)cat].SetActive(HeaderButtonsActiveStatus[(int)cat]);
                            HeaderButtons[(int)cat].ChangeState(1, true);
                        }
                    }
                    return;
                }

                foreach (var cat in (RocketCategory[])Enum.GetValues(typeof(RocketCategory)))
                {
                    if (HeaderButtons.ContainsKey((int)cat))
                    {
                        bool active = ((int)cat == category);

                        HeaderButtonsActiveStatus[(int)cat] = active ? !HeaderButtonsActiveStatus[(int)cat] : active;
                        HeaderButtons[(int)cat].ChangeState(HeaderButtonsActiveStatus[(int)cat] ? 0 : 1);
                        Grids[(int)cat].SetActive(HeaderButtonsActiveStatus[(int)cat]);
                    }
                }
            }

            private static void ClearButtons(SelectModuleSideScreen _this)
            {
                foreach (var button in ModAssets.CategorizedButtons)
                {
                    if (!button.IsNullOrDestroyed() && !button.Value.IsNullOrDestroyed())
                        Util.KDestroyGameObject(button.Value);
                }
                for (int index = _this.categories.Count - 1; index >= 0; --index)
                {
                    if (!_this.categories[index].IsNullOrDestroyed())
                        Util.KDestroyGameObject(_this.categories[index]);
                }
                foreach (var item in SearchableButtons.Values)
                {
                    if (item != null && !item.IsNullOrDestroyed())
                        item.SetActive(false);
                }
                SearchableButtons.Clear();
                _this.categories.Clear();
                ModAssets.CategorizedButtons.Clear();
            }
            public static bool EditingSearch = false;
            public static void StartEditing(string ac) => EditingSearch = true;
            public static void StopEditing(string ac) => EditingSearch = false;

            public static bool Prefix(SelectModuleSideScreen __instance)
            {
                ClearButtons(__instance);

                GameObject gameObject = Util.KInstantiateUI(__instance.categoryPrefab, __instance.categoryContent, force_active: true);
                Transform searchButtonsContainer = gameObject.GetComponent<HierarchyReferences>().GetReference<Transform>("content");

                var searchbarPreset = PlanScreen.Instance.recipeInfoScreenParent.transform.Find("BuildingGroups/Searchbar").gameObject;
                PlanScreen.Instance.recipeInfoScreenParent.transform.Find("BuildingGroups").TryGetComponent<BuildingGroupScreen>(out var screen);

                GameObject searchbar = Util.KInstantiateUI(searchbarPreset, __instance.gameObject, true);
                searchbar.transform.SetAsFirstSibling();
                SearchBar = searchbar.transform.Find("FilterInputField").GetComponent<KInputTextField>();
                SearchBar.text = string.Empty;
                SearchBar.onValueChanged.AddListener(ApplySearchText);
                SearchBar.onSelect.AddListener(StartEditing);
                SearchBar.onEndEdit.AddListener(StopEditing);
                SearchBar.placeholder.TryGetComponent<TMPro.TextMeshProUGUI>(out var textMesh);
                textMesh.text = STRINGS.ROCKETBUILDMENUCATEGORIES.SEARCHBARFILLER;

                ///TODO: PLACEHOLDER TEXT

                UIUtils.AddActionToButton(searchbar.transform, "ClearSearchButton", () => ClearSearchBar());
                UIUtils.AddActionToButton(searchbar.transform, "ListViewButton", () => SetCategoryVisibility(true));
                UIUtils.AddActionToButton(searchbar.transform, "GridViewButton", () => SetCategoryVisibility(false));
                //UIUtils.FindAndDestroy(searchbar.transform, "GridViewButton");
                //UIUtils.FindAndDestroy(searchbar.transform, "ListViewButton");



                foreach (var category in RocketModuleList.GetRocketModuleList())
                {
                    if (category.Value.Count > 0)
                    {
                        GameObject categoryGO = Util.KInstantiateUI(__instance.categoryPrefab, __instance.categoryContent, true);
                        categoryGO.name = category.Key.ToString();
                        HierarchyReferences component = categoryGO.GetComponent<HierarchyReferences>();
                        //categoryGO.name = ((RocketCategory)category.Key).ToString().ToUpperInvariant();

                        __instance.categories.Add(categoryGO);

                        var headergo = categoryGO.transform.Find("Header").gameObject;
                        UnityEngine.Object.Destroy(headergo);

                        string CategoryName = ((RocketCategory)category.Key).ToString().ToUpperInvariant();

                        categoryGO.TryGetComponent<VerticalLayoutGroup>(out var L);
                        L.padding = new RectOffset(2, 17, 0, 0);

                        var foldButtonGO = Util.KInstantiateUI(PlanScreen.Instance.subgroupPrefab.transform.Find("Header").gameObject, categoryGO.gameObject, true);
                        foldButtonGO.transform.SetAsFirstSibling();
                        if (foldButtonGO.TryGetComponent<PlanSubCategoryToggle>(out var cmp))
                        {
                            UnityEngine.Object.Destroy(cmp);
                        }

                        var ToggleBtn = foldButtonGO.GetComponent<MultiToggle>();
                        ToggleBtn.onClick += () =>
                        {
                            ToggleCategory(category.Key, __instance);
                        };

                        HeaderButtons[category.Key] = ToggleBtn;


                        //var buttonText = references.GetReference<LocText>("HeaderLabel");
                        var buttonText = foldButtonGO.transform.Find("Label").GetComponent<LocText>();
                        buttonText.text = Strings.Get("STRINGS.ROCKETBUILDMENUCATEGORIES.CATEGORYTITLES." + CategoryName);
                        var tooltip = UIUtils.AddSimpleTooltipToObject(foldButtonGO.transform, Mod.Tooltips[category.Key], onBottom: true);

                        Transform CategoryGrid = component.transform.Find("Grid");
                        Grids[category.Key] = CategoryGrid.gameObject;

                        List<GameObject> prefabsWithComponent = Assets.GetPrefabsWithComponent<RocketModuleCluster>();
                        foreach (string RocketModuleID in category.Value)
                        {
                            GameObject part = prefabsWithComponent.Find((Predicate<GameObject>)(p => p.PrefabID().Name == RocketModuleID));
                            if ((UnityEngine.Object)part == (UnityEngine.Object)null)
                            {
                                SgtLogger.warning(("Found an id [" + RocketModuleID + "] in moduleButtonSortOrder in SelectModuleSideScreen.cs that doesn't have a corresponding rocket part!"));
                            }
                            else
                            {
                                GameObject ModuleButton = Util.KInstantiateUI(__instance.moduleButtonPrefab, CategoryGrid.gameObject, true);
                                ModuleButton.GetComponentsInChildren<Image>()[1].sprite = Def.GetUISprite((object)part).first;
                                LocText componentInChildren = ModuleButton.GetComponentInChildren<LocText>();
                                componentInChildren.text = part.GetProperName();
                                componentInChildren.alignment = TextAlignmentOptions.Bottom;
                                componentInChildren.enableWordWrapping = true;
                                ModuleButton.GetComponent<MultiToggle>().onClick += (() => __instance.SelectModule(part.GetComponent<Building>().Def));
                                ModAssets.CategorizedButtons.Add(new Tuple<BuildingDef, int>(part.GetComponent<Building>().Def, category.Key), ModuleButton);

                                __instance.SetupBuildingTooltip(ModuleButton.GetComponent<ToolTip>(), part.GetComponent<Building>().Def);

                                if (__instance.selectedModuleDef != (UnityEngine.Object)null)
                                    __instance.SelectModule(__instance.selectedModuleDef);

                                if (!SearchableButtons.ContainsKey(part.GetComponent<Building>().Def))
                                {
                                    var Copy = Util.KInstantiateUI(ModuleButton, searchButtonsContainer.gameObject);
                                    Copy.GetComponent<MultiToggle>().onClick += (() => __instance.SelectModule(part.GetComponent<Building>().Def));
                                    __instance.SetupBuildingTooltip(Copy.GetComponent<ToolTip>(), part.GetComponent<Building>().Def);
                                    Copy.SetActive(false);
                                    SearchableButtons.Add(part.GetComponent<Building>().Def, Copy);
                                }
                            }
                        }
                    }
                }
                __instance.UpdateBuildableStates();
                ToggleCategory(0, __instance, true);
                ClearSearchBar();
                SetCategoryVisibility(RTB_SavegameStoredSettings.Instance.useModuleCategories);
                return false;
            }
        }

        /// <summary>
        /// Add button color setter to categorized buttons
        /// </summary>
        [HarmonyPatch(typeof(SelectModuleSideScreen))]
        [HarmonyPatch(nameof(SelectModuleSideScreen.SetButtonColors))]
        public static class ButtonColorPatch
        {
            public static bool Prefix(SelectModuleSideScreen __instance, ref Dictionary<BuildingDef, bool> ___moduleBuildableState, BuildingDef ___selectedModuleDef)
            {
                foreach (var button in ModAssets.CategorizedButtons)
                {
                    if (!button.IsNullOrDestroyed() && !button.Value.IsNullOrDestroyed())
                    {
                        if (button.Value.TryGetComponent<MultiToggle>(out var component1) && button.Value.TryGetComponent<HierarchyReferences>(out var component2))
                        {
                            if (!___moduleBuildableState[button.Key.first])
                            {
                                component2.GetReference<Image>("FG").material = PlanScreen.Instance.desaturatedUIMaterial;
                                if (button.Key.first == ___selectedModuleDef)
                                    component1.ChangeState(1);
                                else
                                    component1.ChangeState(0);
                            }
                            else
                            {
                                component2.GetReference<Image>("FG").material = PlanScreen.Instance.defaultUIMaterial;
                                if (button.Key.first == ___selectedModuleDef)
                                    component1.ChangeState(3);
                                else
                                    component1.ChangeState(2);
                            }
                        }

                    }
                }
                foreach (var button in CategoryPatchTest.SearchableButtons)
                {
                    if (!button.IsNullOrDestroyed() && !button.Value.IsNullOrDestroyed())
                    {
                        if (button.Value.TryGetComponent<MultiToggle>(out var component1) && button.Value.TryGetComponent<HierarchyReferences>(out var component2))
                        {
                            if (!___moduleBuildableState[button.Key])
                            {
                                component2.GetReference<Image>("FG").material = PlanScreen.Instance.desaturatedUIMaterial;
                                if (button.Key == ___selectedModuleDef)
                                    component1.ChangeState(1);
                                else
                                    component1.ChangeState(0);
                            }
                            else
                            {
                                component2.GetReference<Image>("FG").material = PlanScreen.Instance.defaultUIMaterial;
                                if (button.Key == ___selectedModuleDef)
                                    component1.ChangeState(3);
                                else
                                    component1.ChangeState(2);
                            }
                        }

                    }
                }
                __instance.UpdateBuildButton();
                return false;

            }
        }

        /// <summary>
        /// Update buildable states for categorized buttons
        /// </summary>
        [HarmonyPatch(typeof(SelectModuleSideScreen))]
        [HarmonyPatch(nameof(SelectModuleSideScreen.UpdateBuildableStates))]
        public static class BuildableStatesCategoryPatch
        {
            public static bool Prefix(SelectModuleSideScreen __instance, ref Dictionary<BuildingDef, bool> ___moduleBuildableState, BuildingDef ___selectedModuleDef)
            {
                foreach (var button in ModAssets.CategorizedButtons)
                {
                    if (!button.IsNullOrDestroyed() && !button.Value.IsNullOrDestroyed())
                    {
                        if (!___moduleBuildableState.ContainsKey(button.Key.first))
                        {
                            ___moduleBuildableState.Add(button.Key.first, false);
                        }
                        TechItem techItem = Db.Get().TechItems.TryGet(button.Key.first.PrefabID);
                        if (techItem != null)
                        {
                            bool flag = DebugHandler.InstantBuildMode || Game.Instance.SandboxModeActive || techItem.IsComplete();

                            if (!button.Value.IsNullOrDestroyed())
                                button.Value.SetActive(flag);
                        }
                        else
                        {
                            if (!button.Value.IsNullOrDestroyed())
                                button.Value.SetActive(true);
                        }
                        ___moduleBuildableState[button.Key.first] = __instance.TestBuildable(button.Key.first);
                    }
                }
                CategoryPatchTest.ToggleCategoriesSearch(false);

                if (___selectedModuleDef != null)
                {
                    __instance.ConfigureMaterialSelector();
                }
                __instance.SetButtonColors();

                foreach (var category in RocketModuleList.GetRocketModuleList())
                {
                    bool keepCategory = false;
                    foreach (var item in category.Value)
                    {
                        TechItem techItem = Db.Get().TechItems.TryGet(item);

                        if (techItem != null)
                        {
                            if (DebugHandler.InstantBuildMode || Game.Instance.SandboxModeActive || techItem.IsComplete())
                            {
                                keepCategory = true;
                                break;
                            }
                        }
                        else
                        {
                            keepCategory = true;
                            break;
                        }
                    }
                    var categoryToDisable = __instance.categories.Find(categ => categ.name == category.Key.ToString());
                    if (!categoryToDisable.IsNullOrDestroyed())
                    {
                        categoryToDisable.SetActive(keepCategory);
                    }
                }
                return false;

            }
        }

    }
}
