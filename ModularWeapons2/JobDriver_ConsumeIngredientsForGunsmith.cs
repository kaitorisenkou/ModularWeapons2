using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace ModularWeapons2 {
    //前作よりコピペ
    public class JobDriver_ConsumeIngredientsForGunsmith : JobDriver {
        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            if (!this.pawn.Reserve(this.job.targetC, this.job, 1, -1, null, errorOnFailed)) return false;
            Thing thing = this.job.GetTarget(StationInd).Thing;
            if (thing != null && thing.def.hasInteractionCell && !this.pawn.ReserveSittableOrSpot(thing.InteractionCell, this.job, errorOnFailed)) {
                return false;
            }
            this.pawn.ReserveAsManyAsPossible(this.job.GetTargetQueue(MaterialInd), this.job, 1, -1, null);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            var comp = job.GetTarget(WeaponInd).Thing.TryGetComp<CompModularWeapon>();
            if (comp == null) Log.Warning("[MW2]comp is null!");
            this.FailOnDespawnedOrNull(StationInd);
            //指定した材料を運搬する
            bool ingredients = job.GetTargetQueue(MaterialInd) != null && job.GetTargetQueue(MaterialInd).Count > 0;
            if (ingredients) {
                foreach (Toil toil in JobDriver_DoBill.CollectIngredientsToils(MaterialInd, StationInd, WeaponInd, true, false, false)) {
                    yield return toil;
                }
            }
            //材料を運び終わった後
            yield return Toils_Goto.GotoThing(StationInd, PathEndMode.InteractionCell);//作業台へ向かう
            Toil waitToil = Toils_General.Wait(WorkTimeTicks, StationInd);
            var thingDef = job.GetTarget(WeaponInd).Thing.def;
            if (thingDef.recipeMaker != null && thingDef.recipeMaker.soundWorking != null)
                waitToil.PlaySustainerOrSound(thingDef.recipeMaker.soundWorking, 1f);
            waitToil.WithProgressBarToilDelay(StationInd, false, -0.5f);
            //待機する
            yield return waitToil;
            //材料を消費する
            //！！！注意！！！  PlaceHauledThingInCellにHarmonyパッチしないとplacedThingsはNullになります
            yield return Toils_General.Do(delegate {
                if (this.job.placedThings == null) {
                    Log.Warning("[MW2] job.placedThings is null!");
                } else if (ingredients) {
                    foreach (var i in job.placedThings) {
                        i.thing.Destroy(DestroyMode.Vanish);
                    }
                    job.placedThings.Clear();
                }
            });
            //武器のカスタマイズを反映
            yield return Toils_General.Do(delegate {
                comp.SetPartsWithBuffer();
            });
            yield break;
        }

        public const TargetIndex MaterialInd = TargetIndex.A;

        public const TargetIndex WeaponInd = TargetIndex.B;

        public const TargetIndex StationInd = TargetIndex.C;

        public const int WorkTimeTicks = 750;
    }
}
