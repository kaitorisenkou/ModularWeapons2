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
            return base.TryCastShot();
        }
    }
}
