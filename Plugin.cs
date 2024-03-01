// Copyright (C) 2024 Rémy Cases
// See LICENSE file for extended copyright information.
// This file is part of the Speedshard repository from https://github.com/remyCases/Shardpunk-MoreSkillLevels.

using BepInEx;
using HarmonyLib;
using Assets.Scripts.Logic;
using Assets.Scripts.Logic.Skills;
using Assets.Scripts.Logic.Tactical;
using System.Collections.Generic;
using Assets.Scripts.GameUI.GameActions;
using UnityEngine;
using Assets.Scripts.GameUI;
using Assets.Scripts.GameUI.Tactical;
using TMPro;
using Assets.Scripts.Logic.GameActions;
using System;
using Assets.Scripts.UI.AnimationRoutines;
using System.Reflection.Emit;
using System.Reflection;
using Assets.Scripts;
using HarmonyLib.Tools;
using Assets.Scripts.Localisation;

namespace MoreSkillLevels;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        HarmonyFileLog.Enabled = true;
        Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}

// add a fourth possible level
[HarmonyPatch]
public static class General 
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WeaponPartUpgradeProgressDisplayBehaviour))]
    [HarmonyPatch("Awake")]
    static void WeaponPartUpgradeProgressDisplayBehaviourAwake(WeaponPartUpgradeProgressDisplayBehaviour __instance)
    {
        FieldInfo levelInfo = AccessTools.Field(typeof(WeaponPartUpgradeProgressDisplayBehaviour), "_levelIndicators");
        UpgradableLevelIndicatorDisplayBehaviour[] levelArray = levelInfo.GetValue(__instance) as UpgradableLevelIndicatorDisplayBehaviour[];

        if (levelArray.Length >= 4) return;

        GameObject pickerPrefab = GameObject.Find("ProgressElementBorderImage");
        for (int i = 0; i < 4 - levelArray.Length; i++)
        {
            GameObject copied = UnityEngine.Object.Instantiate(pickerPrefab, __instance.transform);
            copied.GetComponent<UnityEngine.UI.Image>().enabled = false;
        }
        
        levelInfo.SetValue(__instance, __instance.GetComponentsInChildren<UpgradableLevelIndicatorDisplayBehaviour>());
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SkillLevelsPreviewDisplayBehaviour))]
    [HarmonyPatch("Awake")]
    static void Awake()
    {
        GameObject pickerPrefab = GameObject.Find("LevelPreview1");
        GameObject copied = UnityEngine.Object.Instantiate(pickerPrefab, pickerPrefab.transform.parent);
        copied.transform.SetSiblingIndex(3);
        copied.name = "LevelPreview4";
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SkillLevelsPreviewDisplayBehaviour))]
    [HarmonyPatch("UpdateDisplay")]
    static void UpdateDisplay(SkillLevelsPreviewDisplayBehaviour __instance)
    {
        MethodInfo getHighlightedSkill = AccessTools.PropertyGetter(typeof(SkillLevelsPreviewDisplayBehaviour), "HighlightedSkill");
        Skill highlightedSkill = (Skill)getHighlightedSkill.Invoke(__instance, null);
        MethodInfo resolve = AccessTools.Method(typeof(SkillLevelsPreviewDisplayBehaviour), "ResolveSelectedCharacter");
        Character selectedCharacter = (Character)resolve.Invoke(__instance, null);

        MethodInfo setleveldescription = AccessTools.Method(typeof(SkillLevelsPreviewDisplayBehaviour), "SetLevelDescription");
        GameObject level4 = GameObject.Find("LevelPreview4");
        setleveldescription.Invoke(__instance, new object[] {4, highlightedSkill, selectedCharacter, level4.GetComponentInChildren<TextMeshProUGUI>(), level4.GetComponent<CanvasGroup>()});
    }
}

[HarmonyPatch]
public static class MoreShelterAPs
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillMoreShelterAPs))]
    [HarmonyPatch("Create")]
    static void Create(ref Skill __result)
    {
        FieldInfo levelsInfo = AccessTools.Field(typeof(Skill), "_levels");
        List<SkillLevel> levels = levelsInfo.GetValue(__result) as List<SkillLevel>;

        SkillLevel newSkillLevel = new(2, (Character c) => TeamSkillMoreShelterAPs.GetLevelDescription(2));
        levels.Add(newSkillLevel);
        levelsInfo.SetValue(__result, levels);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillMoreShelterAPs))]
    [HarmonyPatch("GetLevelDescription", new Type[] { typeof(int) })]
    static void GetLevelDescription(ref string __result, int skillLevel)
    {
        if (skillLevel >= 2)
        {
            __result = __result + "\n" + Loc.GetFormat("teamSkill_moreShelterAPsFood_desc");
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FoodDistributionContinueButtonBehaviour))]
    [HarmonyPatch("FeedCharactersAndProceedToNextScreen", MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> FeedCharactersAndProceedToNextScreen(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        int to_insert_label = 0;
        Label label = il.DefineLabel();
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(AccessTools.Method(typeof(Stat), "Increase")))
            {
                yield return instruction;

                // if branch
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Game), "get_Instance"));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Game), "get_TeamSkills"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TeamSkillMoreShelterAPs), "TypeName"));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(SkillsContainer), "GetLevel"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_2, null);
                to_insert_label = 1;
                yield return new CodeInstruction(OpCodes.Blt_Un_S, label);

                // main code
                yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(TraitDodgeBonus)));
                yield return new CodeInstruction(OpCodes.Stloc_S, 9);
                yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Character), "get_Traits"));
                yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1, null);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CharacterTraits), "Add"));
                yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
                yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
                yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CharacterTrait), "GetName"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameColors), "EventTextIconGreenColor"));
                yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(TemporaryTextDefinition), new Type[] {typeof(string), typeof(Color)}));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(TemporaryTextDefinition)), nameof(List<TemporaryTextDefinition>.Add)));
            } 
            else 
            {
                if (to_insert_label == 1) {
                    instruction.labels.Add(label);
                    to_insert_label = 2;
                }
                yield return instruction;
            }
        }
    }
}

[HarmonyPatch]
public static class QuickerBunkerOpening
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillQuickerBunkerOpening))]
    [HarmonyPatch("Create")]
    static void Create(ref Skill __result)
    {
        FieldInfo levelsInfo = AccessTools.Field(typeof(Skill), "_levels");
        List<SkillLevel> levels = levelsInfo.GetValue(__result) as List<SkillLevel>;

        SkillLevel newSkillLevel = new(3, (Character c) => TeamSkillQuickerBunkerOpening.GetLevelDescription(3));
        levels.Add(newSkillLevel);
        levelsInfo.SetValue(__result, levels);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillQuickerBunkerOpening))]
    [HarmonyPatch("GetBunkerOpeningTime", new Type[] { typeof(int) })]
    static void GetBunkerOpeningTime(ref int __result, int skillLevel)
    {
        if (skillLevel >= 3) {
            __result = 3; 
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillQuickerBunkerOpening))]
    [HarmonyPatch("GetLevelDescription", new Type[] { typeof(int) })]
    static void GetLevelDescription(ref string __result, int skillLevel)
    {
        if (skillLevel >= 3) 
        {
            object[] locParams = new object[] 
            {
                TeamSkillQuickerBunkerOpening.GetBunkerOpeningTime(skillLevel),
                5
            };
            __result = Loc.GetFormat("teamSkill_quickerBunkerOpeningNoAP_desc", locParams);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CombatActionBreachBunkerDoor))]
    [HarmonyPatch("Create")]
    static void CombatActionBreachBunkerDoorCreate(ref GameAction __result, Character character)
    {
        Type descriptionProviderType = AccessTools.Inner(typeof(CombatActionBreachBunkerDoor), "DescriptionProvider");

        if (Game.Instance.TeamSkills.GetLevel(TeamSkillQuickerBunkerOpening.TypeName) >= 3)
        {
            IGameActionCondition[] gameActionConditions = new IGameActionCondition[]
            {
                new CurrentMapLocationNotOfTypeCondition(MapLevelType.Cave),
                new CurrentMapLocationNotOfTypeCondition(MapLevelType.ChapterOneBridge),
                new CharacterIsStandingOnExitAreaCondition(),
                new MapShelterPoweredUpCondition(false)
            };

            GameAction gameAction = new(
                new GameActionPlainTextProvider(Loc.Get("skill_breachBunkerDoor_name")),
                "BreachBunkerDoor",
                (IGameActionTextProvider)AccessTools.CreateInstance(descriptionProviderType),
                new FlatActionPointCost(0),
                false,
                GameActionInputType.None)
            {
                AvailabilityCondition = new LogicalAndCondition(gameActionConditions),
                Effect = new BreachExitPointEffect(),
                ResolvePriority = (CombatCharacter _) => GameActionOrderPriority.Special,
                ShouldBeBlinking = true
            };
            __result = gameAction;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CombatActionUnlockBunkerDoor))]
    [HarmonyPatch("Create")]
    static void CombatActionUnlockBunkerDoorCreate(ref GameAction __result, Character character)
    {
        if (Game.Instance.TeamSkills.GetLevel(TeamSkillQuickerBunkerOpening.TypeName) >= 3) 
        {
            IGameActionCondition[] gameActionConditions = new IGameActionCondition[]
            {
                new CurrentMapLocationNotOfTypeCondition(MapLevelType.Cave),
                new CurrentMapLocationNotOfTypeCondition(MapLevelType.ChapterOneBridge),
                new CharacterIsStandingOnExitAreaCondition(),
                new MapShelterNotOpenedCondition()
            };

            GameAction gameAction = new(
                Loc.Get("skill_unlockBunkerDoor_name"),
                "ActivateExitPoint",
                Loc.Get("skill_unlockBunkerDoor_desc"),
                new FlatActionPointCost(0),
                false,
                GameActionInputType.None
            )
            {
                AvailabilityCondition = new LogicalAndCondition(gameActionConditions),
                ExecutionCondition = new CharacterInventoryHasAtLeastOneItemCondition(ItemTypes.FusionCore),
                Effect = new UnlockExitPointEffect(),
                ShouldBeBlinking = true,
                ResolvePriority = (CombatCharacter _) => GameActionOrderPriority.Special
            };
            __result = gameAction;
        }
    }
}

[HarmonyPatch]
public static class Grenade
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillGrenadeMastery))]
    [HarmonyPatch("Create")]
    static void Create(ref Skill __result)
    {
        FieldInfo levelsInfo = AccessTools.Field(typeof(Skill), "_levels");
        List<SkillLevel> levels = levelsInfo.GetValue(__result) as List<SkillLevel>;

        SkillLevel newSkillLevel = new(4, (Character c) => TeamSkillQuickerBunkerOpening.GetLevelDescription(4));
        levels.Add(newSkillLevel);
        levelsInfo.SetValue(__result, levels);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillGrenadeMastery))]
    [HarmonyPatch("GetDamageGain", new Type[] { typeof(int) })]
    static void GetDamageGain(ref Range __result, int skillLevel)
    {
        if (skillLevel >= 3) {
            __result = new Range(1, 3); 
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillGrenadeMastery))]
    [HarmonyPatch("GetLevelDescription", new Type[] { typeof(int) })]
    static void GetLevelDescription(ref string __result, int skillLevel)
    {
        Range range = TeamSkillGrenadeMastery.GetDamageGain(skillLevel).Add(CombatActionThrowGrenade.BaseGrenadeDamage);
        if (skillLevel > 3) 
        {
            object[] locParams = new object[]
            {
                range,
                CombatActionThrowGrenade.BaseGrenadeDamage,
                2,
                1
            };

            string text = Loc.GetFormat("teamSkill_grenadeMasteryAndRange_desc", locParams); 
            text = text + "\n" + Loc.Get("teamSkill_grenadeMasteryStunGrenades_desc");
            __result = text;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ThrowGrenadeRoutine))]
    [HarmonyPatch("GetDamagedCharacters")]
    static IEnumerable<CodeInstruction> GetDamagedCharacters(IEnumerable<CodeInstruction> instructions) {

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(AccessTools.Method(typeof(RangeCalculator), "GetGrenadeAOETiles", new Type[] { typeof(Point2D) })))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_2, null);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrenadeUtils), "GetGrenadeAOETiles", new Type[] { typeof(Point2D), typeof(bool) }));
            } 
            else 
            {
                yield return instruction;
            }
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ThrowGrenadeRoutine))]
    [HarmonyPatch("Routine")]
    static IEnumerable<CodeInstruction> ThrowGrenadeRoutine(IEnumerable<CodeInstruction> instructions) {

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(AccessTools.Method(typeof(RangeCalculator), "GetGrenadeAOETiles", new Type[] { typeof(Point2D) })))
            {
                yield return new CodeInstruction(OpCodes.Ldloc_1, null);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThrowGrenadeRoutine), "_stunMode"));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrenadeUtils), "GetGrenadeAOETiles", new Type[] { typeof(Point2D), typeof(bool) }));
            } 
            else 
            {
                yield return instruction;
            }
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ExplodeRoutine))]
    [HarmonyPatch("DisplayAOE")]
    static IEnumerable<CodeInstruction> ExplodeRoutineDisplayAOE(IEnumerable<CodeInstruction> instructions) {

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(AccessTools.Method(typeof(RangeCalculator), "GetGrenadeAOETiles", new Type[] { typeof(Point2D) })))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_1, null);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrenadeUtils), "GetGrenadeAOETiles", new Type[] { typeof(Point2D), typeof(bool) }));
            } 
            else 
            {
                yield return instruction;
            }
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ExplodeRoutine))]
    [HarmonyPatch("GetDamagedCharacters")]
    static IEnumerable<CodeInstruction> ExplodeRoutineGetDamagedCharacters(IEnumerable<CodeInstruction> instructions) {

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(AccessTools.Method(typeof(RangeCalculator), "GetGrenadeAOETiles", new Type[] { typeof(Point2D) })))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_1, null);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrenadeUtils), "GetGrenadeAOETiles", new Type[] { typeof(Point2D), typeof(bool) }));
            } 
            else 
            {
                yield return instruction;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ThrowingActionBehaviour))]
    [HarmonyPatch("Update")]
    static IEnumerable<CodeInstruction> ThrowingActionBehaviourUpdate(IEnumerable<CodeInstruction> instructions) {

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(AccessTools.Method(typeof(RangeCalculator), "GetGrenadeAOETiles", new Type[] { typeof(Point2D) })))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameActionBehaviour), "get_Action"));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameAction), "get_Type"));
                yield return new CodeInstruction(OpCodes.Ldstr, "ThrowStunGrenade");
                yield return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo((string a, string b) => string.Equals(a, b)));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrenadeUtils), "GetGrenadeAOETiles", new Type[] { typeof(Point2D), typeof(bool) }));
            } 
            else 
            {
                yield return instruction;
            }
        }
    }
}

[HarmonyPatch]
public static class HpGain
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillHPGain))]
    [HarmonyPatch("Create")]
    static void Create(ref Skill __result)
    {
        FieldInfo levelsInfo = AccessTools.Field(typeof(Skill), "_levels");
        List<SkillLevel> levels = levelsInfo.GetValue(__result) as List<SkillLevel>;
        // remove function for level 3
        levels[2].AfterPurchaseFunction = null;

        SkillLevel newSkillLevel = new(4, (Character c) => TeamSkillHPGain.GetLevelDescription(4))
        {
            AfterPurchaseFunction = delegate (Character _)
            {
                AccessTools.Method(typeof(TeamSkillHPGain), "IncreaseHPOfEveryHumanCharacter").Invoke(null, null);
            }
        };
        levels.Add(newSkillLevel);
        levelsInfo.SetValue(__result, levels);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillHPGain))]
    [HarmonyPatch("GetHPGain", new Type[] { typeof(int) })]
    static void GetHPGain(ref int __result, int skillLevel)
    {
        if (skillLevel >= 3) 
        {
            __result -= 1;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeamSkillHPGain))]
    [HarmonyPatch("GetLevelDescription", new Type[] { typeof(int) })]
    static void GetLevelDescription(ref string __result, int skillLevel)
    {
        if (skillLevel >= 3) 
        {
            object[] locParams = new object[]  { 1 };
            __result = __result + "\n" + Loc.GetFormat("teamSkill_hpGainLessPoison_desc", locParams);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TraitPoisoned))]
    [HarmonyPatch("DurationInTurns")]
    static void DurationInTurns(ref int __result, Character owner)
    {
        if (Game.Instance.TeamSkills.GetLevel(TeamSkillHPGain.TypeName) >= 3) 
        {
            __result -= 1;
        }
    }
}

public static class GrenadeUtils
{
    public static MapTile[] GetGrenadeAOETiles(Point2D centerPoint, bool isStun) 
    {
        if (Game.Instance.TeamSkills.GetLevel(TeamSkillGrenadeMastery.TypeName) >= 4 && !isStun) 
        {
                return RangeCalculator.GetAOETiles(centerPoint, 2);
        }
        return RangeCalculator.GetAOETiles(centerPoint, 1);
    }
}