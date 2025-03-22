﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using Verse.Noise;
using Verse.AI;
using RimWorld.Utility;
using System.Net.NetworkInformation;

namespace ModularWeapons2 {
    [StaticConstructorOnStartup]
    public class ModularWeapons2 {
        static ModularWeapons2() {
            Log.Message("[MW2]Now Active");
            var harmony = new Harmony("kaitorisenkou.ModularWeapons2");

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear), new Type[] { typeof(Thing), typeof(StatDef) }),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_StatOffsetFromGear), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker), "GearHasCompsThatAffectStat", new Type[] { typeof(Thing), typeof(StatDef) }),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_GearHasCompsThatAffectStat), null));

            MethodInfo GetIconForMethod =
                typeof(Widgets).GetMethods().First(t =>
                t.Name == "GetIconFor" && t.GetParameters().Any(tt => tt.ParameterType == typeof(Thing))
                );
            harmony.Patch(GetIconForMethod, transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_GetIconFor), null));

            harmony.Patch(
                AccessTools.Method(typeof(Graphic), nameof(Graphic.TryGetTextureAtlasReplacementInfo)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_TryGetTextureAtlasReplacementInfo), null));

            //Type PlaceHauledThingInCell_type = AccessTools.TypeByName("Verse.AI.Toils_Haul+<>c__DisplayClass8_0");
            var types_PlaceHauledThingInCell = typeof(Toils_Haul).GetNestedTypes(AccessTools.all);
            bool isFound_PlaceHauledThingInCell = false;
            foreach (var i in types_PlaceHauledThingInCell) {
                //Log.Message(i.Name);
                MethodInfo method = i.FirstMethod(t => t.Name.Contains("PlaceHauledThingInCell"));
                if (method != null) {
                    harmony.Patch(method, null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(ModularWeapons2), nameof(Patch_PlaceHauledThingInCell), null)));
                    isFound_PlaceHauledThingInCell = true;
                    break;
                }
            }
            if (!isFound_PlaceHauledThingInCell) {
                Log.Error("[MW2] Inner method of PlaceHauledThingInCell not found! (" + types_PlaceHauledThingInCell.Length.ToString() + ")");
            }

            harmony.Patch(
                AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand", new Type[] { typeof(Thing), typeof(Verb) }),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_CreateVerbTargetCommand), null));

            harmony.Patch(
                AccessTools.Method(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_MeleeVerbs), null));

            harmony.Patch(
                AccessTools.PropertyGetter(typeof(Verb), "EquipmentSource"),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_VerbEquipmentSource), null));

            harmony.Patch(
                AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.ExposeData)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_EqTrExposeData), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker_MeleeAverageDPS), nameof(StatWorker_MeleeAverageDPS.GetExplanationUnfinalized)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_Explanation_MeleeDPS), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker_MeleeAverageDPS), nameof(StatWorker_MeleeAverageDPS.GetValueUnfinalized)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_Explanation_MeleeDPS), null));

            harmony.Patch(
                AccessTools.PropertyGetter(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.AllAbilitiesForReading)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_AbilityTracker), null));


            var innerType_ReloadJob = typeof(JobDriver_Reload).InnerTypes().FirstOrFallback(t => t.Name.Contains("<MakeNewToils>"));
            if (innerType_ReloadJob != null) {
                harmony.Patch(
                    AccessTools.Method(innerType_ReloadJob, "MoveNext"),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_JobReload), null));
            }

            var innerType_ReloadUtil = typeof(ReloadableUtility).InnerTypes().FirstOrFallback(t => t.Name.Contains("<FindPotentiallyReloadableGear>"));
            if (innerType_ReloadUtil != null) {
                harmony.Patch(
                    AccessTools.Method(innerType_ReloadUtil, "MoveNext"),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_ReloadableUtil), null));
            }
            harmony.Patch(
                AccessTools.Method(typeof(ReloadableUtility), nameof(ReloadableUtility.FindSomeReloadableComponent)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_ReloadableUtil), null));

            harmony.Patch(
                AccessTools.PropertyGetter(typeof(CompEquippable), nameof(CompEquippable.VerbProperties)),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_CompEqVerbProperties), null));

            innerType_ThingDefSDS = typeof(ThingDef).InnerTypes().FirstOrFallback(t => t.Name.Contains("<SpecialDisplayStats>"));
            if (innerType_ThingDefSDS != null) {
                harmony.Patch(
                    AccessTools.Method(innerType_ThingDefSDS, "MoveNext"),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_ThingDefSDS), null));
            }

            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming)),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_DrawEquipmentAiming), null));
            harmony.Patch(
                AccessTools.Constructor(typeof(Stance_Busy), new Type[] { typeof(int), typeof(LocalTargetInfo), typeof(Verb) }),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_ConstructerStanceBusy), null));

            harmony.Patch(
                AccessTools.Method(typeof(VerbTracker), nameof(VerbTracker.ExposeData)),
                prefix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Prefix_VerbTrackerExpose), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatDef), nameof(StatDef.IsImmutable)),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_IsImmutable), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatDef), "PopulateMutableStats"),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_PopulateMutableStats), null));

            Log.Message("[MW2] Harmony patch complete!");

            StatDef.SetImmutability();

            MW2Mod.statDefsShow.AddRange(new StatDef[] {
                StatDefOf.Mass,
                StatDefOf.MeleeDodgeChance,
                StatDefOf.MeleeHitChance
            });
            MW2Mod.statCategoryShow.AddRange(new StatCategoryDef[]{
                StatCategoryDefOf.Weapon,
                StatCategoryDefOf.Weapon_Ranged,
                StatCategoryDefOf.Weapon_Melee,
                StatCategoryDefOf.EquippedStatOffsets
            });
            MW2Mod.lessIsBetter.AddRange(new string[]{
                StatDefOf.Mass.label.CapitalizeFirst(),
                StatDefOf.RangedWeapon_Cooldown.label.CapitalizeFirst(),
                "Stat_Thing_Weapon_RangedWarmupTime_Desc".Translate(),
                "RangedWarmupTime".Translate(),
                StatDefOf.EquipDelay.label.CapitalizeFirst()
            });
            MW2Mod.statDefsForceNonImmutable.AddRange(new StatDef[]{
                StatDefOf.RangedWeapon_Cooldown
            });
            Log.Message("[MW2] Misc initializations complete!");
        }

        //前作よりコピペ 軽量化の余地あり？
        static IEnumerable<CodeInstruction> Patch_StatOffsetFromGear(IEnumerable<CodeInstruction> instructions) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            for (int i = 1; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Stloc_0) {
                    i++;
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(ModularWeapons2),nameof(GetEquippedOffset))),
                        new CodeInstruction(OpCodes.Add),
                        new CodeInstruction(OpCodes.Stloc_0)
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW2]patch failed : Patch_StatOffsetFromGear");
            }
            return instructionList;
        }
        static float GetEquippedOffset(Thing gear, StatDef stat) {
            var comp = gear.TryGetComp<CompModularWeapon>();
            if (comp != null) {
                return comp.GetEquippedOffset(stat);
            }
            return 0;
        }

        static void Postfix_GearHasCompsThatAffectStat(ref bool __result, Thing gear, StatDef stat) {
            if (__result) {
                return;
            }
            var comp = gear.TryGetComp<CompModularWeapon>();
            if (comp == null) {
                return;
            }
            __result |= Mathf.Approximately(comp.GetEquippedOffset(stat), 0);
        }



        static IEnumerable<CodeInstruction> Patch_GetIconFor(IEnumerable<CodeInstruction> instructions) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetMethod = AccessTools.Method(typeof(Graphic), nameof(Graphic.MatAt));
            for (int i = 1; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == targetMethod) {
                    instructionList.RemoveAt(i - 1);
                    instructionList.Insert(i - 1, new CodeInstruction(OpCodes.Ldarg_0));
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW2]patch failed : Patch_GetIconFor");
            }
            return instructionList;
        }

        static IEnumerable<CodeInstruction> Patch_TryGetTextureAtlasReplacementInfo(IEnumerable<CodeInstruction> instructions) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetMethod = AccessTools.Method(typeof(GlobalTextureAtlasManager), nameof(GlobalTextureAtlasManager.TryGetStaticTile));

            for (int i = 1; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetMethod) {
                    var branch = instructionList[i + 1];
                    while (true) {
                        i--;
                        if (instructionList[i].opcode == OpCodes.Ldarg_1)
                            break;
                    }
                    instructionList[i].opcode = OpCodes.Ldarg_0;
                    instructionList.InsertRange(i + 1, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(ModularWeapons2),nameof(IsTexture2D))),
                        branch,
                        new CodeInstruction(OpCodes.Ldarg_1)
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW2]patch failed : Patch_TryGetTextureAtlasReplacementInfo");
            }
            return instructionList;
        }
        static bool IsTexture2D(Material mat) {
            return typeof(Texture2D).IsAssignableFrom(mat.mainTexture.GetType());
        }


        static IEnumerable<CodeInstruction> Patch_PlaceHauledThingInCell(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            FieldInfo fieldInfo = AccessTools.Field(typeof(JobDefOf), nameof(JobDefOf.DoBill));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Ldsfld && (FieldInfo)instructionList[i].operand == fieldInfo && instructionList[i + 1].opcode == OpCodes.Beq_S) {
                    var copy = instructionList.GetRange(i - 3, 5);
                    copy[3] = copy[3].Clone();
                    copy[3].operand = AccessTools.Field(typeof(MW2DefOf), nameof(MW2DefOf.ConsumeIngredientsForGunsmith));
                    instructionList.InsertRange(i + 2, copy);
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : PlaceHauledThingInCell_Patch");
            }
            return instructionList;
        }



        static void Postfix_CreateVerbTargetCommand(ref Command_VerbTarget __result, Thing ownerThing) {
            var comp = ownerThing.TryGetComp<CompModularWeapon>();
            if (comp == null) {
                return;
            }
            Material mat = ownerThing.Graphic.MatSingleFor(ownerThing);
            __result.icon = mat.mainTexture;
            __result.iconDrawScale = ownerThing.Graphic.drawSize.x;
        }


        static IEnumerable<CodeInstruction> Patch_MeleeVerbs(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetInfo = AccessTools.PropertyGetter(typeof(CompEquippable), "AllVerbs");
            MethodInfo replaceInfo = AccessTools.Method(typeof(ModularWeapons2), nameof(GetAllVerbs_IncludeMW));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList[i].operand = replaceInfo;
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_MeleeVerbs");
            }
            return instructionList;
        }
        static List<Verb> GetAllVerbs_IncludeMW(CompEquippable compEq) {
            if (Scribe.mode != LoadSaveMode.Inactive) {
                return compEq.AllVerbs;
            }
            var compMW = compEq.parent.TryGetComp<CompModularWeapon>();
            if (compMW != null) {
                return compEq.AllVerbs.Concat(compMW.AllVerbs).ToList();
            }
            return compEq.AllVerbs;
        }

        static void Postfix_VerbEquipmentSource(ref ThingWithComps __result, Verb __instance) {
            if (__result != null)
                return;
            if (__instance is Verb_AbilityUseUBGL && __instance.verbProps.ForcedMissRadius > 0.5f) {
                __result = (__instance as Verb_AbilityUseUBGL).Ability.pawn.equipment.Primary;
            }
            var compMW = __instance.DirectOwner as CompModularWeapon;
            if (compMW != null)
                __result = compMW.parent;
        }

        static IEnumerable<CodeInstruction> Patch_EqTrExposeData(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetInfo = AccessTools.PropertyGetter(typeof(CompEquippable), "AllVerbs");
            MethodInfo replaceInfo = AccessTools.Method(typeof(ModularWeapons2), nameof(GetAllVerbs_IncludeMW));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList[i].operand = replaceInfo;
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_EqTrExposeData");
            }
            return instructionList;
        }

        static IEnumerable<CodeInstruction> Patch_Explanation_MeleeDPS(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetInfo = AccessTools.Method(typeof(StatWorker_MeleeAverageDPS), "GetVerbsAndTools");
            MethodInfo addMethodInfo = AccessTools.Method(typeof(ModularWeapons2), nameof(AddMWTools));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i + 1, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloc_2),
                        new CodeInstruction(OpCodes.Call,addMethodInfo),
                        new CodeInstruction(OpCodes.Stloc_2)
                    });
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_Explanation_MeleeDPS");
            }
            return instructionList;
        }
        static List<Tool> AddMWTools(StatRequest req, List<Tool> tools) {
            var compMW = req.Thing?.TryGetComp<CompModularWeapon>();
            if (compMW == null) {
                return tools;
            }
            return tools.Concat(compMW.Tools).ToList();
        }


        static IEnumerable<CodeInstruction> Patch_AbilityTracker(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetInfo = AccessTools.PropertyGetter(typeof(CompEquippableAbility), nameof(CompEquippableAbility.AbilityForReading));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo) {
                    do { i++; } while (!instructionList[i].labels.Any());
                    var firstInst = new CodeInstruction(OpCodes.Ldarg_0);
                    firstInst.labels = instructionList[i].labels;
                    instructionList[i].labels = new List<Label>();
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        firstInst,
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld,AccessTools.Field(typeof(Pawn_AbilityTracker),"allAbilitiesCached")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModularWeapons2), nameof(AddMWAbility)))
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_AbilityTracker");
            }
            return instructionList;
        }
        static void AddMWAbility(Pawn_AbilityTracker tracker, List<Ability> abilityList) {
            var comp = tracker.pawn.equipment?.Primary?.TryGetComp<CompModularWeapon>();
            if (comp != null) {
                var ability = comp.AbilityForReading;
                if (ability != null) {
                    abilityList.Add(ability);
                }
            }
        }


        static IEnumerable<CodeInstruction> Patch_JobReload(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            MethodInfo targetInfo = AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), parameters: new Type[] { typeof(Thing) }, generics: new Type[] { typeof(CompEquippableAbilityReloadable) });
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList[i].operand = AccessTools.Method(typeof(ModularWeapons2), nameof(FindIReloadable));
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_JobReload");
            }
            return instructionList;
        }

        static IReloadableComp FindIReloadable(Thing gear) {
            var compEq = gear.TryGetComp<CompEquippableAbilityReloadable>();
            if (compEq != null) return compEq;
            return gear.TryGetComp<CompModularWeapon>();
        }

        static IEnumerable<CodeInstruction> Patch_ReloadableUtil(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Isinst) {
                    instructionList[i] = new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(ModularWeapons2), nameof(FindIReloadable_ReloadableUtil))
                        );
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_ReloadableUtil");
            }
            return instructionList;
        }

        static IReloadableComp FindIReloadable_ReloadableUtil(CompEquippable gear) {
            var compEqAb = gear as IReloadableComp;
            if (compEqAb != null) return compEqAb;
            return gear.parent.TryGetComp<CompModularWeapon>();
        }


        static void Postfix_CompEqVerbProperties(ref List<VerbProperties> __result, CompEquippable __instance) {
            var compMW = __instance.parent.TryGetComp<CompModularWeapon>();
            if (compMW == null) return;
            __result = compMW.VerbPropertiesForOverride;
        }

        static Type innerType_ThingDefSDS = null;
        static IEnumerable<CodeInstruction> Patch_ThingDefSDS(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            FieldInfo targetInfo = AccessTools.Field(typeof(ThingDef), "verbs");
            FieldInfo reqInfo = AccessTools.Field(innerType_ThingDefSDS, "req");
            MethodInfo replaceMethodInfo = AccessTools.Method(typeof(ModularWeapons2), nameof(GetOverriddenVerbs));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Ldfld && (FieldInfo)instructionList[i].operand == targetInfo) {
                    i++;
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld,reqInfo),
                        new CodeInstruction(OpCodes.Call,replaceMethodInfo)
                    });
                    patchCount++;
                }
            }
            if (patchCount < 2) {
                Log.Error("[MW]patch failed : Patch_ThingDefSDS");
            }
            return instructionList;
        }
        static List<VerbProperties> GetOverriddenVerbs(List<VerbProperties> original, StatRequest req) {
            if (req.HasThing) {
                var comp = req.Thing.TryGetComp<CompEquippable>();
                if (comp != null)
                    return comp.VerbProperties;
            }

            return original;
        }


        static void Postfix_DrawEquipmentAiming(Thing eq) {
            var compMW = eq.TryGetComp<CompModularWeapon>();
            if (compMW == null) return;
            compMW.DrawTacDevice();
        }
        static void Postfix_ConstructerStanceBusy(Verb ___verb) {
            var compMW = ___verb?.EquipmentSource?.TryGetComp<CompModularWeapon>();
            if (compMW == null) return;
            compMW.OnStanceBusy(___verb);
        }


        static bool Prefix_VerbTrackerExpose(VerbTracker __instance) {
            if (__instance.directOwner is CompModularWeapon ||
                (__instance.directOwner as CompEquippable)?.parent.TryGetComp<CompModularWeapon>() != null) {
                if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                    __instance.InitVerbsFromZero();
                }
                return false;
            }
            return true;
        }


        static void Postfix_IsImmutable(ref bool __result, StatDef __instance) {
            __result = false;
            __instance.cacheable = false;
            /*
            if (__result) {
                __result = !MW2Mod.statDefsForceNonImmutable.Contains(__instance);
            }
            */
        }
        static void Postfix_PopulateMutableStats(ref HashSet<StatDef> ___mutableStats) {
            Log.Message("[MW2] Postfix_PopulateMutableStats done");
            foreach(var i in DefDatabase<ModularPartsDef>.AllDefsListForReading) {
                if (i.StatFactors != null)
                    foreach (var j in i.StatFactors) {
                        ___mutableStats.Add(j.stat);
                    }
                if (i.StatOffsets != null)
                    foreach (var j in i.StatOffsets) {
                        ___mutableStats.Add(j.stat);
                    }
            }
        }
    }
}
