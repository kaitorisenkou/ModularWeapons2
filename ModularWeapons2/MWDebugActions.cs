using LudeonTK;
using Verse;

namespace ModularWeapons2 {
    public static class MWDebugActions {
        [DebugAction("ModularWeapons2", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GunsmithWeapon() {
            foreach (Thing i in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())) {
                var comp = i.TryGetComp<CompModularWeapon>();
                if (comp == null)
                    continue;
                Find.WindowStack.Add(new Dialog_Gunsmith(comp, null, null));
                break;
            }
        }
    }
}
