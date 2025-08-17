using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ModularWeapons2 {
    public class Verb_AbilityUseUBGL : Verb_LaunchProjectileStatic, IAbilityVerb {
        public Ability ability;
        public Ability Ability {
            get => this.ability;
            set => this.ability = value;
        }
        protected override bool TryCastShot() {
            this.ability.Activate(this.currentTarget, this.currentDestination);
            var comp = verbTracker.directOwner as CompModularWeapon;
            if (comp != null) {
                comp.UpdateRemainingCharges();
            }
            return base.TryCastShot();
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref this.ability, "ability", false);
        }

        protected override int ShotsPerBurst {
            get {
#if V15
                return base.verbProps.burstShotCount;
#else
                return base.BurstShotCount;
#endif
            }
        }
    }
}
