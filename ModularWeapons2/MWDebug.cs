using LudeonTK;
using System.Diagnostics;
using System;
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
    }
}
