using RimWorld;
using Verse;

namespace ModularWeapons2 {
    public class CompModularWeapon : ThingComp {
        public CompProperties_ModularWeapon Props {
            get {
                return (CompProperties_ModularWeapon)this.props;
            }
        }

        public float GetEquippedOffset(StatDef stat) {
            return 0;
        }
        public override float GetStatFactor(StatDef stat) {
            return base.GetStatFactor(stat);
        }
        public override float GetStatOffset(StatDef stat) {
            return base.GetStatOffset(stat);
        }
    }
}
