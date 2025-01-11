using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class CompProperties_ModularWeapon : CompProperties {
        public CompProperties_ModularWeapon() {
            this.compClass = typeof(CompModularWeapon);
        }

        public GraphicData baseGraphicData;

        public List<MountAdapterClass> partsMounts;
        //public List<ModularPartsDef> defaultParts;
    }
}
