using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

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
                AccessTools.Method(typeof(StatWorker_MeleeAverageArmorPenetration), nameof(StatWorker_MeleeAverageArmorPenetration.GetExplanationUnfinalized)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_Explanation_MeleeAP), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker_MeleeAverageArmorPenetration), nameof(StatWorker_MeleeAverageArmorPenetration.GetValueUnfinalized)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_Explanation_MeleeAP), null));

            harmony.Patch(
                AccessTools.PropertyGetter(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.AllAbilitiesForReading)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_AbilityTracker), null));


            var innerType_ReloadJob = typeof(JobDriver_Reload).InnerTypes().FirstOrFallback(t => t.Name.Contains("<MakeNewToils>"));
            if (innerType_ReloadJob != null) {
                harmony.Patch(
                    AccessTools.Method(innerType_ReloadJob, "MoveNext"),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_JobReload), null));
            } else {
                Log.Error("[MW2] innerType_ReloadJob not found!");
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
            MWDebug.LogMessage("[MW2]Postfix_DrawEquipmentAiming done");
            harmony.Patch(
                AccessTools.Constructor(typeof(Stance_Busy), new Type[] { typeof(int), typeof(LocalTargetInfo), typeof(Verb) }),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_ConstructerStanceBusy), null));
            MWDebug.LogMessage("[MW2]Postfix_ConstructerStanceBusy done");
            harmony.Patch(
                AccessTools.Method(typeof(VerbTracker), nameof(VerbTracker.ExposeData)),
                prefix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Prefix_VerbTrackerExpose), null));
            MWDebug.LogMessage("[MW2]Prefix_VerbTrackerExpose done");
            harmony.Patch(
                AccessTools.Method(typeof(StatDef), nameof(StatDef.IsImmutable)),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_IsImmutable), null));
            MWDebug.LogMessage("[MW2]Postfix_IsImmutable done");

            harmony.Patch(
                AccessTools.Method(typeof(StatDef), "PopulateMutableStats"),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_PopulateMutableStats), null));
            MWDebug.LogMessage("[MW2]Postfix_PopulateMutableStats done");

#if V15
            harmony.Patch(
                AccessTools.Method(typeof(Widgets), nameof(Widgets.ThingIcon), parameters: new Type[] { typeof(Rect), typeof(ThingDef), typeof(ThingDef), typeof(ThingStyleDef), typeof(float), typeof(Color?), typeof(int?) }),
                    prefix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Prefix_ThingDefIcon), null)
                );
#else
            harmony.Patch(
                AccessTools.Method(typeof(Widgets), nameof(Widgets.ThingIcon), parameters: new Type[] { typeof(Rect), typeof(ThingDef), typeof(ThingDef), typeof(ThingStyleDef), typeof(float), typeof(Color?), typeof(int?), typeof(float) }),
                    prefix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Prefix_ThingDefIcon), null)
                );
#endif
            MWDebug.LogMessage("[MW2]Prefix_ThingDefIcon done");

            harmony.Patch(
                AccessTools.Method(typeof(ThingStyleHelper), nameof(ThingStyleHelper.SetStyleDef)),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_SetStyleDef), null));
            MWDebug.LogMessage("[MW2]Postfix_SetStyleDef done");

            harmony.Patch(
                AccessTools.Method(typeof(Ability), "PreActivate"),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_PreActivate), null));
            MWDebug.LogMessage("[MW2]Postfix_PreActivate done");
#if V16
            harmony.Patch(
                AccessTools.Method(typeof(FloatMenuOptionProvider_Reload), "GetReloadablesUsingAmmo"),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_GetReloadables), null));
            MWDebug.LogMessage("[MW2]Postfix_GetReloadables done");
#endif

            harmony.Patch(
                AccessTools.Method(typeof(PawnAttackGizmoUtility), "AtLeastTwoSelectedPlayerPawnsHaveDifferentWeapons"),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_AttackGizmoDifferentWeapon), null));
            MWDebug.LogMessage("[MW2]Postfix_AttackGizmoDifferentWeapon done");

            if (MW2Mod.IsWeaponRacksEnable) {
                Log.Message("[MW2] WeaponRacks detected");
                var WeaponRacksType = MW2Mod.Assembly_WeaponRacks.GetType("WeaponRacks.CachedDisplayItem");
                harmony.Patch(
                    AccessTools.PropertyGetter(WeaponRacksType, "Material"),
                    prefix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Prefix_WeaponRackMaterial), null));
                MWDebug.LogMessage("[MW2]Prefix_WeaponRackMaterial done");
            } else {
                MWDebug.LogMessage("[MW2]Prefix_WeaponRackMaterial skiped");
            }
            if (MW2Mod.IsLTOGroupsEnable) {
                Log.Message("[MW2] [LTO] Colony Groups detected");
                //var LTODrawerType = AccessTools.TypeByName("TacticalGroups.TacticalGroups_ColonistBarColonistDrawer");
                var LTODrawerType = MW2Mod.Assembly_LTOGroups.GetType("TacticalGroups.TacticalGroups_ColonistBarColonistDrawer");
                harmony.Patch(
                    AccessTools.Method(LTODrawerType, "DrawColonistsBarWeaponIcon"),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_TLOGWeaponIcon), null));
                /*harmony.Patch(
                    AccessTools.Method(LTODrawerType, "DrawColonistBarWeaponIcon"),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_TLOGWeaponIcon2), null));*/
            } else {
                MWDebug.LogMessage("[MW2]Patch_TLOGWeaponIcon skiped");
            }
            if (MW2Mod.IsShowMeYourHandsEnable) {
                Log.Message("[MW2] ShowMeYourHands detected");
                //var LTODrawerType = AccessTools.TypeByName("ShowMeYourHands.HandDrawer");
                var LTODrawerType = MW2Mod.Assembly_ShowMeYourHands.GetType("ShowMeYourHands.HandDrawer");
                harmony.Patch(
                    AccessTools.Method(LTODrawerType, "DrawHandsOnWeapon", new Type[] { typeof(Thing), typeof(float), typeof(Pawn), typeof(Thing), typeof(bool), typeof(bool) }),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_SMYHHandDrawer), null));
                harmony.Patch(
                    AccessTools.Method(LTODrawerType, "DrawHandsOnWeapon", new Type[] { typeof(Pawn) }),
                    transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_SMYHHandDrawer2), null));
            } else {
                MWDebug.LogMessage("[MW2]Patch for ShowMeYourHands skiped");
            }

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

            MW2Mod.InjectStyleDefs();

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
            MWDebug.LogMessage("[MW2] Patch_StatOffsetFromGear done");
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
            MethodInfo targetMethod_Styled = AccessTools.Method(typeof(ThingStyleDef), nameof(ThingStyleDef.IconForIndex));
            for (int i = 1; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == targetMethod_Styled) {
                    instructionList.RemoveAt(i);
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(ModularWeapons2), nameof(IconForStyle)))
                    });
                    patchCount++;
                }
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == targetMethod) {
                    instructionList.RemoveAt(i - 1);
                    instructionList.Insert(i - 1, new CodeInstruction(OpCodes.Ldarg_0));
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW2]patch failed : Patch_GetIconFor");
            }
            MWDebug.LogMessage("[MW2] Patch_GetIconFor done");
            return instructionList;
        }
        static Texture IconForStyle(ThingStyleDef style, int index, Rot4? rot, Thing thing) {
            if (!thing.def.HasComp<CompModularWeapon>()) 
                return style.IconForIndex(index, rot);
            return thing.Graphic.MatAt(rot.Value, thing).mainTexture;
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
            MWDebug.LogMessage("[MW2] Patch_TryGetTextureAtlasReplacementInfo done");
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
            MWDebug.LogMessage("[MW2] Patch_PlaceHauledThingInCell done");
            return instructionList;
        }



        static void Postfix_CreateVerbTargetCommand(ref Command_VerbTarget __result, Thing ownerThing) {
            var comp = ownerThing.TryGetComp<CompModularWeapon>();
            if (comp == null) {
                return;
            }
            Material mat = ownerThing.Graphic.MatSingleFor(ownerThing);
            __result.icon = mat.mainTexture;
#if V15
            __result.iconDrawScale = ownerThing.Graphic.drawSize.x;
#endif
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
            MWDebug.LogMessage("[MW2] Patch_MeleeVerbs done");
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
            MWDebug.LogMessage("[MW2] Patch_EqTrExposeData done");
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
            MWDebug.LogMessage("[MW2] Patch_Explanation_MeleeDPS done");
            return instructionList;
        }
        static List<Tool> AddMWTools(StatRequest req, List<Tool> tools) {
            var compMW = req.Thing?.TryGetComp<CompModularWeapon>();
            if (compMW == null) {
                return tools;
            }
            return tools.Concat(compMW.Tools).ToList();
        }

        static IEnumerable<CodeInstruction> Patch_Explanation_MeleeAP(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            FieldInfo targetInfo = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.tools));
            MethodInfo addMethodInfo = AccessTools.Method(typeof(ModularWeapons2), nameof(AddMWTools_AP));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Ldfld && instructionList[i].operand is FieldInfo && (FieldInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i + 1, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,addMethodInfo),
                    });
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_Explanation_MeleeDPS");
            }
            MWDebug.LogMessage("[MW2] Patch_Explanation_MeleeDPS done");
            return instructionList;
        }
        static List<Tool> AddMWTools_AP(List<Tool> tools, StatRequest req) {
            return AddMWTools(req, tools);
        }


        static IEnumerable<CodeInstruction> Patch_AbilityTracker(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionList = instructions.ToList();
            MethodInfo targetInfo_Equipment = AccessTools.PropertyGetter(typeof(CompEquippableAbility), nameof(CompEquippableAbility.AbilityForReading));
            bool eqDone = false;
            //↓1.5に対応できてない
            MethodInfo targetInfo_Apparel = AccessTools.PropertyGetter(typeof(Apparel), nameof(Apparel.AllAbilitiesForReading));
            bool apDone= false;
            for (int i = 0; i < instructionList.Count; i++) {
                if (!eqDone && instructionList[i].opcode == OpCodes.Callvirt && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo_Equipment) {
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
                    eqDone = true;
                }
                if(!apDone && instructionList[i].opcode == OpCodes.Callvirt && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo_Apparel) {
                    do { i++; } while (!instructionList[i].labels.Any());
                    var firstInst = new CodeInstruction(OpCodes.Ldarg_0);
                    firstInst.labels = instructionList[i].labels;
                    instructionList[i].labels = new List<Label>();
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        firstInst,
                        new CodeInstruction(OpCodes.Ldloc_S, 4),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld,AccessTools.Field(typeof(Pawn_AbilityTracker),"allAbilitiesCached")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModularWeapons2), nameof(AddMWAbility_Apparel)))
                    });
                    apDone = true;
                }
            }
            if (!eqDone || !apDone) {
                Log.Error("[MW]patch failed : Patch_AbilityTracker (eq=" + eqDone + ", ap=" + apDone + ")");
            }
            MWDebug.LogMessage("[MW2] Patch_AbilityTracker done");
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
        static void AddMWAbility_Apparel(Pawn_AbilityTracker tracker, Apparel apparel, List<Ability> abilityList) {
            var comp = apparel?.TryGetComp<CompModularWeapon>();
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
            MethodInfo targetInfo_apparel = AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), parameters: new Type[] { typeof(Thing) }, generics: new Type[] { typeof(CompApparelReloadable) });
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && 
                    instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo_apparel) {
                    instructionList[i].operand = AccessTools.Method(typeof(ModularWeapons2), nameof(FindIReloadable_Apparel));
                    patchCount++;
                }
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList[i].operand = AccessTools.Method(typeof(ModularWeapons2), nameof(FindIReloadable));
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[MW]patch failed : Patch_JobReload");
            }
            MWDebug.LogMessage("[MW2] Patch_JobReload done");
            return instructionList;
        }

        static IReloadableComp FindIReloadable(Thing gear) {
            var compEq = gear.TryGetComp<CompEquippableAbilityReloadable>();
            if (compEq != null) return compEq;
            return gear.TryGetComp<CompModularWeapon>();
        }
        static IReloadableComp FindIReloadable_Apparel(Thing gear) {
            var compAp = gear.TryGetComp<CompApparelReloadable>();
            if (compAp != null && compAp.NeedsReload(true)) return compAp;
            return gear.TryGetComp<CompModularWeapon>();
        }

        static IEnumerable<CodeInstruction> Patch_ReloadableUtil(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            var targetInfo = AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), parameters: new Type[] { typeof(Thing) }, generics: new Type[] { typeof(CompApparelReloadable) });
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Isinst) {
                    instructionList[i] = new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(ModularWeapons2), nameof(FindIReloadable_ReloadableUtil))
                        );
                    patchCount++;
                } else 
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo &&
                    (MethodInfo)instructionList[i].operand == targetInfo) {
                    var label = generator.DefineLabel();
                    instructionList[i].labels.Add(label);
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(ModularWeapons2), nameof(FindIReloadable_ReloadableUtil_Apparel))),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Brfalse, label),
                        new CodeInstruction(OpCodes.Stloc_3),
                        new CodeInstruction(OpCodes.Pop),
                        new CodeInstruction(OpCodes.Ldloc_3),
                        new CodeInstruction(OpCodes.Ret)
                    });
                    patchCount++;
                }
                if (patchCount >= 2) break;
            }
            if (patchCount < 2) {
                Log.Error("[MW]patch failed : Patch_ReloadableUtil (patchCount:" + patchCount + ")");
            }
            MWDebug.LogMessage("[MW2] Patch_ReloadableUtil done");
            return instructionList;
        }

        static IReloadableComp FindIReloadable_ReloadableUtil(CompEquippable gear) {
            var compEqAb = gear as IReloadableComp;
            if (compEqAb != null) return compEqAb;
            var compMW = gear?.parent?.TryGetComp<CompModularWeapon>() ?? null;
            return compMW;
        }

        static IReloadableComp FindIReloadable_ReloadableUtil_Apparel(Thing thing, bool allowForcedReload) {
            var compMW = thing?.TryGetComp<CompModularWeapon>();
            return (compMW?.NeedsReload(allowForcedReload) ?? false) ? compMW : null;
        }


        static void Postfix_CompEqVerbProperties(ref List<VerbProperties> __result, CompEquippable __instance) {
            var compMW = __instance?.parent?.TryGetComp<CompModularWeapon>();
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
                if (instructionList[i].opcode == OpCodes.Ldfld && instructionList[i].operand is FieldInfo &&(FieldInfo)instructionList[i].operand == targetInfo) {
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
            MWDebug.LogMessage("[MW2] Patch_ThingDefSDS done");
            return instructionList;
        }
        static List<VerbProperties> GetOverriddenVerbs(List<VerbProperties> original, StatRequest req) {
            if (req.HasThing) {
                var comp = req.Thing?.TryGetComp<CompEquippable>();
                if (comp != null)
                    return comp.VerbProperties;
            }

            return original;
        }


        static void Postfix_DrawEquipmentAiming(Thing eq, Vector3 drawLoc) {
            var compMW = eq.TryGetComp<CompModularWeapon>();
            if (compMW == null) return;
            compMW.DrawTacDevice(drawLoc);
        }
        static void Postfix_ConstructerStanceBusy(Verb ___verb) {
            var compMW = ___verb?.EquipmentSource?.TryGetComp<CompModularWeapon>();
            if (compMW == null) return;
            compMW.OnStanceBusy(___verb);
        }


        static bool Prefix_VerbTrackerExpose(VerbTracker __instance) {
            if (__instance.directOwner is CompModularWeapon /*||
                (__instance.directOwner as CompEquippable)?.parent.TryGetComp<CompModularWeapon>() != null*/) {
                if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                    __instance.InitVerbsFromZero();
                }
                return false;
            }
            if (__instance.directOwner is CompEquippable && __instance.AllVerbs.NullOrEmpty()) {
                __instance.InitVerbsFromZero();
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
            MWDebug.LogMessage("[MW2] Postfix_PopulateMutableStats worked");
            foreach (var i in DefDatabase<ModularPartsDef>.AllDefsListForReading) {
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


        static bool Prefix_WeaponRackMaterial(Thing ___thing, ref Material __result) {
            MWDebug.LogMessage("[MW2] Prefix_WeaponRackMaterial working for " + ___thing.GetUniqueLoadID());
            if (___thing == null) {
                return true;
            }
            MWDebug.LogMessage("[MW2] type: " + ___thing.Graphic.GetType());
            if (Graphic_UniqueByComp.TryGetAssigned(___thing.Graphic, out Graphic_UniqueByComp gUBC, ___thing)) {
                __result = gUBC.MatSingleFor(___thing);
                MWDebug.LogMessage("[MW2] __result assigned");
                return false;
            }
            return true;
        }

        static bool Prefix_ThingDefIcon(ThingDef thingDef, ref float scale) {
            if (thingDef.comps.Any(t => t is CompProperties_ModularWeapon) &&
                typeof(Graphic_UniqueByComp).IsAssignableFrom(thingDef.graphicData.graphicClass)) {
                scale /= 2;
            }
            return true;
        }

        static IEnumerable<CodeInstruction> Patch_TLOGWeaponIcon(IEnumerable<CodeInstruction> instructions) {
            var instructionList = instructions.ToList();
            var castIndex = instructionList.FirstIndexOf(t => t.opcode == OpCodes.Castclass && (Type)t.operand == typeof(Texture2D));
            if (castIndex > 0) {
                instructionList[castIndex].operand = typeof(Texture);
            } else {
                Log.Error("[MW2]patch failed : Patch_TLOGWeaponIcon (castIndex not found)");
            }
            var rectIndex = instructionList.FirstIndexOf(t => t.opcode == OpCodes.Ldarg_0);
            if (rectIndex > 0) {
                instructionList.InsertRange(rectIndex + 1, new CodeInstruction[] {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(ModularWeapons2),nameof(GetTLOWeaponIconRect)))
                });
            } else {
                Log.Error("[MW2]patch failed : Patch_TLOGWeaponIcon (rectIndex not found)");
            }
            MWDebug.LogMessage("[MW2] Patch_TLOGWeaponIcon done");
            return instructionList;
        }
        public static Rect GetTLOWeaponIconRect(Rect original, Thing weapon) {
            if (Graphic_UniqueByComp.TryGetAssigned(weapon, out _) &&
                weapon?.TryGetComp<CompModularWeapon>() != null) {
                var center = original.center;
                var result = new Rect(original);
                result.width *= 2;
                result.height *= 2;
                result.center = center;
                return result;
            }
            return original;
        }

        static IEnumerable<CodeInstruction> Patch_TLOGWeaponIcon2(IEnumerable<CodeInstruction> instructions) {
            var instructionList = instructions.ToList();
            var drawTextureIndex = instructionList.FirstIndexOf(t => t.opcode == OpCodes.Call && t.operand is MethodInfo && (MethodInfo)t.operand == AccessTools.Method(typeof(GUI), nameof(GUI.DrawTexture), new Type[] { typeof(Rect), typeof(Texture) }));
            if (drawTextureIndex > 0) {
                instructionList[drawTextureIndex].operand = AccessTools.Method(typeof(Widgets), nameof(Widgets.DrawTextureFitted), new Type[] { typeof(Rect), typeof(Texture), typeof(float) });
                instructionList.Insert(drawTextureIndex, new CodeInstruction(OpCodes.Ldc_R4, 1.0f));
            } else {
                Log.Error("[MW2]patch failed : Patch_TLOGWeaponIcon2 (drawTextureIndex not found)");
            }
            MWDebug.LogMessage("[MW2] Patch_TLOGWeaponIcon done");
            return instructionList;
        }

        static void Postfix_SetStyleDef(Thing thing) {
            var compMW = thing.TryGetComp<CompModularWeapon>();
            if (compMW != null) {
                compMW.RefleshParts();
                //compMW.SetGraphicDirty();
            }
        }

        static IEnumerable<CodeInstruction> Patch_SMYHHandDrawer(IEnumerable<CodeInstruction> instructions) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            FieldInfo targetInfo = AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawSize));
            MethodInfo addMethodInfo = AccessTools.Method(typeof(ModularWeapons2), nameof(GetDivValueForSMYH));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Ldflda && (FieldInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i + 2, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,addMethodInfo),
                        new CodeInstruction(OpCodes.Div)
                    });
                    patchCount++;
                }
            }
            if (patchCount < 4) {
                Log.Error("[MW]patch failed : Patch_SMYHHandDrawer (" + patchCount + ")");
            }
            MWDebug.LogMessage("[MW2] Patch_SMYHHandDrawer done");
            return instructionList;
        }
        public static float GetDivValueForSMYH(Thing weapon) {
            if (weapon.HasComp<CompModularWeapon>() && Graphic_UniqueByComp.TryGetAssigned(weapon, out _)) {
                //return weapon.Graphic.drawSize.x;
                return 2;
            }
            return 1;
        }

        static IEnumerable<CodeInstruction> Patch_SMYHHandDrawer2(IEnumerable<CodeInstruction> instructions) {
            int patchCount = 0;
            var instructionList = instructions.ToList();
            var drawerType = MW2Mod.Assembly_ShowMeYourHands.GetType("ShowMeYourHands.HandDrawer");
            FieldInfo targetInfo1 = AccessTools.Field(drawerType, "MainHand");
            FieldInfo targetInfo2 = AccessTools.Field(drawerType, "OffHand");
            MethodInfo addMethodInfo1 = AccessTools.Method(typeof(ModularWeapons2), nameof(GetHandOffsetForSMYH_Main));
            MethodInfo addMethodInfo2 = AccessTools.Method(typeof(ModularWeapons2), nameof(GetHandOffsetForSMYH_Off));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Stfld && (FieldInfo)instructionList[i].operand == targetInfo1) {
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,addMethodInfo1)
                    });
                    i += 3;
                    patchCount++;
                }
                if (instructionList[i].opcode == OpCodes.Stfld && (FieldInfo)instructionList[i].operand == targetInfo2) {
                    instructionList.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,addMethodInfo2)
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 2) {
                Log.Error("[MW]patch failed : Patch_SMYHHandDrawer2 (" + patchCount + ")");
            }
            MWDebug.LogMessage("[MW2] Patch_SMYHHandDrawer2 done");
            return instructionList;
        }
        public static Vector3 GetHandOffsetForSMYH_Main(Vector3 original, Pawn pawn) {
            var comp = pawn.equipment.Primary.TryGetComp<CompModularWeapon>();
            if (comp == null)
                return original;
            return original + comp.GetMainHandOffset();
        }
        public static Vector3 GetHandOffsetForSMYH_Off(Vector3 original, Pawn pawn) {
            var comp = pawn.equipment.Primary.TryGetComp<CompModularWeapon>();
            if (comp == null)
                return original;
            return original + comp.GetOffHandOffset();
        }


        static void Postfix_PreActivate(Ability __instance) {
            var comp = __instance.pawn?.equipment?.Primary?.TryGetComp<CompModularWeapon>();
            if (comp != null) {
                comp.UpdateRemainingCharges();
            }
        }

        static void Postfix_GetReloadables(Pawn pawn, Thing clickedThing, ref IEnumerable<IReloadableComp> __result) {
            __result= GetReloadablesFix_Inner(pawn, clickedThing, __result);
        }
        static IEnumerable<IReloadableComp> GetReloadablesFix_Inner(Pawn pawn, Thing clickedThing, IEnumerable<IReloadableComp> original) {
            foreach(var i in original) {
                yield return i;
            }
            Pawn_EquipmentTracker equipment = pawn.equipment;
            /*
            if (((equipment != null) ? equipment.PrimaryEq : null) != null) {
                IReloadableComp reloadableComp = pawn.equipment.Primary.TryGetComp<CompModularWeapon>();
                if (reloadableComp != null && clickedThing.def == reloadableComp.AmmoDef) {
                    yield return reloadableComp;
                }
            }
            */
            if (equipment != null) { 
                foreach(var i in equipment.AllEquipmentListForReading) {
                    IReloadableComp reloadableComp = i.TryGetComp<CompModularWeapon>();
                    if (reloadableComp != null && clickedThing.def == reloadableComp.AmmoDef) {
                        yield return reloadableComp;
                    }
                }
            }

            Pawn_ApparelTracker apparel = pawn.apparel;
            if (apparel != null) {
                foreach (var i in apparel.WornApparel) {
                    IReloadableComp reloadableComp = i.TryGetComp<CompModularWeapon>();
                    if (reloadableComp != null && clickedThing.def == reloadableComp.AmmoDef) {
                        yield return reloadableComp;
                    }
                }
            }
        }

        static void Postfix_AttackGizmoDifferentWeapon(ref bool __result) {
            if (__result || Find.Selector.NumSelected <= 1) return;
            foreach(var i in Find.Selector.SelectedObjectsListForReading) {
                if ((i as Pawn)?.equipment?.Primary?.def?.comps?.Any(t => t is CompProperties_ModularWeapon) == true) {
                    __result = true;
                    return;
                }
            }
        }
    }
}
