using System;
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

namespace ModularWeapons2 {
    [StaticConstructorOnStartup]
    public class ModularWeapons2 {
        static ModularWeapons2() {
            Log.Message("[MW2]Now Active");
            var harmony = new Harmony("kaitorisenkou.ModularWeapons2"); 

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear), new Type[] { typeof(Thing), typeof(StatDef) }), 
                transpiler:new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_StatOffsetFromGear), null));

            harmony.Patch(
                AccessTools.Method(typeof(StatWorker), "GearHasCompsThatAffectStat", new Type[] { typeof(Thing), typeof(StatDef) }),
                postfix: new HarmonyMethod(typeof(ModularWeapons2), nameof(Postfix_GearHasCompsThatAffectStat), null));

            MethodInfo GetIconForMethod = 
                typeof(Widgets).GetMethods().First(t => 
                t.Name == "GetIconFor" && t.GetParameters().Any(tt=>tt.ParameterType == typeof(Thing))
                );
            harmony.Patch(GetIconForMethod, transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_GetIconFor), null));

            harmony.Patch(
                AccessTools.Method(typeof(Graphic), nameof(Graphic.TryGetTextureAtlasReplacementInfo)),
                transpiler: new HarmonyMethod(typeof(ModularWeapons2), nameof(Patch_TryGetTextureAtlasReplacementInfo), null));

            Log.Message("[MW2] Harmony patch complete!");

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

        static void Postfix_GearHasCompsThatAffectStat(ref bool __result,Thing gear, StatDef stat) {
            if (__result) {
                return;
            }
            var comp = gear.TryGetComp<CompModularWeapon>();
            if (comp == null) {
                return;
            }
            __result |= Math.Abs(comp.GetEquippedOffset(stat)) > 1E-45f;
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
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo&& (MethodInfo)instructionList[i].operand == targetMethod) {
                    var branch = instructionList[i + 1];
                    while (true) {
                        i--;
                        if (instructionList[i].opcode == OpCodes.Ldarg_1)
                            break;
                    }
                    instructionList[i].opcode = OpCodes.Ldarg_0;
                    instructionList.InsertRange(i+1, new CodeInstruction[] {
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
    }
}
