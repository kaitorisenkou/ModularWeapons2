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
using Verse.AI;

namespace ModularWeapons2 {
    //前作よりコピペ
    public class Building_GunsmithStation : Building_WorkTable {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn)) {
                yield return floatMenuOption;
            }
            if (!selPawn.CanReach(this, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn)) {
                yield return new FloatMenuOption("CannotUseReason".Translate("NoPath".Translate().CapitalizeFirst()), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            var powerTrader = this.TryGetComp<CompPowerTrader>();
            if (powerTrader != null && !powerTrader.PowerOn) {
                yield return new FloatMenuOption("CannotUseReason".Translate("NoPower".Translate().CapitalizeFirst()), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            var refuelable = this.TryGetComp<CompRefuelable>();
            if (refuelable != null && !refuelable.HasFuel) {
                yield return new FloatMenuOption("CannotUseReason".Translate("NoFuel".Translate().CapitalizeFirst()), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            var eq = selPawn.equipment.AllEquipmentListForReading.Where(t => t.TryGetComp<CompModularWeapon>() != null);
            var ap = selPawn.apparel.WornApparel.Where(t => t.TryGetComp<CompModularWeapon>() != null);
            int count = 0;
            foreach (var i in eq) {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("MW2_CustomizeWeapon".Translate(i.Label).CapitalizeFirst(), delegate () {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(MW2DefOf.UseGunsmithStation, this, i), new JobTag?(JobTag.Misc), false);
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0), selPawn, this, "ReservedBy", null);
                count++;
            }
            foreach (var i in ap) {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("MW2_CustomizeWeapon".Translate(i.Label).CapitalizeFirst(), delegate () {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(MW2DefOf.UseGunsmithStation, this, i), new JobTag?(JobTag.Misc), false);
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0), selPawn, this, "ReservedBy", null);
                count++;
            }
            if (count < 1) {
                yield return new FloatMenuOption("CannotUseReason".Translate("MW2_CustomizeUnavailable".Translate().CapitalizeFirst()), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            }
            yield break;
        }
    }
}
