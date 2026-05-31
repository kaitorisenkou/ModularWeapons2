using LudeonTK;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public static class MWDebug {
        [Conditional("DEBUG")]
        public static void LogMessage(object obj) {
            Log.Message(obj);
        }
        [Conditional("DEBUG")]
        public static void LogWarning(object obj) {
            Log.Warning(obj.ToString());
        }
        [Conditional("DEBUG")]
        public static void LogError(object obj) {
            Log.Error(obj.ToString());
        }

        [DebugAction("ModularWeapons2", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void GunsmithWeapon() {
            foreach (Thing i in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())) {
                var comp = i.TryGetComp<CompModularWeapon>();
                if (comp == null)
                    continue;
                Find.WindowStack.Add(new Dialog_Gunsmith(comp, null, null));
#if DEBUG
                Graphic_UniqueByComp.TryGetAssigned(comp.parent, out var gra);
                if (gra == null) {
                    LogMessage("[MW2]" + (comp.parent?.GetUniqueLoadID() ?? "(null)") + "'s graphicClass is not UniqueByComp");
                    LogMessage("[MW2] It's " + comp.parent.Graphic.GetType().ToString());
                    LogMessage("[MW2] ...and " + comp.parent.Graphic.ExtractInnerGraphicFor(comp.parent).GetType().ToString() + " inside");
                }
#endif
                break;
            }
        }
#if DEBUG
        [DebugAction("ModularWeapons2", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CheckGraphicClass() {
            foreach (Thing i in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())) {
                LogMessage("[MW2]"+i.GetUniqueLoadID()+": "+i.Graphic.GetType().ToString());
            }
        }
#endif
#if DEBUG
        [DebugAction("ModularWeapons2", null, false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CheckAllVerbs(Pawn p) {
            List<Verb> allVerbs = new List<Verb>(p.verbTracker.AllVerbs);
            allVerbs.AddRange(p.equipment.AllEquipmentListForReading.SelectMany(t => t?.GetComp<CompEquippable>()?.AllVerbs?? Array.Empty<Verb>().ToList()));
            allVerbs.AddRange(p.apparel.WornApparel.SelectMany(t => t?.GetComp<CompEquippable>()?.AllVerbs ?? Array.Empty<Verb>().ToList()));

            var getter = new TableDataGetter<Verb>[] {
                new TableDataGetter<Verb>("string",v=>v.ToString()),
                new TableDataGetter<Verb>("equipment",v=>v.EquipmentSource?.Label??"null"),
                new TableDataGetter<Verb>("meleeDabageDef",v=>v.IsMeleeAttack?(v.verbProps.meleeDamageDef.label):"not melee")
            };

            DebugTables.MakeTablesDialog<Verb>(allVerbs.Where(t=>t!=null), getter);
        }
#endif
    }
}
