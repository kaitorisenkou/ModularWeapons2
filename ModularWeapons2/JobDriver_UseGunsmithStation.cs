using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace ModularWeapons2 {
    //前作よりコピペ
    public class JobDriver_UseGunsmithStation : JobDriver {
        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            Thing thing;
            if (job.GetTarget(TargetIndex.B) == null) {
                thing = this.pawn.equipment.Primary;
            } else {
                thing = job.GetTarget(TargetIndex.B).Thing;
            }
            //var primary = this.pawn.equipment.Primary;
            if (thing == null ) yield break;
            var comp = thing.TryGetComp<CompModularWeapon>();
            if (comp == null) yield break;

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_General.Do(
                delegate {
                    Find.WindowStack.Add(new Dialog_Gunsmith(comp, this.job.GetTarget(TargetIndex.A).Thing, pawn));
                });
            yield break;
        }
    }
}
