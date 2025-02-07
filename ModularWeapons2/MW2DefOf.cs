using Verse;
using RimWorld;

namespace ModularWeapons2 {
    [DefOf]
    public class MW2DefOf {
        static MW2DefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(MW2DefOf));
        }
        public static JobDef ConsumeIngredientsForGunsmith;
        public static JobDef UseGunsmithStation;
    }
}
