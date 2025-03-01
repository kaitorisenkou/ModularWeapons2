using RimWorld;
using Verse;

namespace ModularWeapons2 {
    public class MWAbilityProperties {
        public AbilityDef abilityDef;
        public int maxCharges;
        public ThingDef ammoDef;
        public int ammoCountToRefill;
        public int ammoCountPerCharge;
        public int baseReloadTicks = 60;
        public bool replenishAfterCooldown;
        public SoundDef soundReload;
        [MustTranslate]
        public string chargeNoun = "charge";
        [MustTranslate]
        public string cooldownGerund = "on cooldown";


        public NamedArgument CooldownVerbArgument {
            get {
                return this.cooldownGerund.CapitalizeFirst().Named("COOLDOWNGERUND");
            }
        }
        public NamedArgument ChargeNounArgument {
            get {
                return this.chargeNoun.Named("CHARGENOUN");
            }
        }
    }
}
