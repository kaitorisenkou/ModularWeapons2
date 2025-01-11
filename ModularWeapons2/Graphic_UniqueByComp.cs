using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class Graphic_UniqueByComp : Graphic_Single {
        public override Material MatSingleFor(Thing thing) {
            if (thing == null) {
                return MatSingle;
            }
            CompProperties compProps = thing.def.comps.FirstOrFallback(t => typeof(ICompUniqueGraphic).IsAssignableFrom(t.compClass));
            if (compProps == null) {
                return base.MatSingleFor(thing);
            }
            ThingComp comp = thing.TryGetComp(compProps);
            return ((ICompUniqueGraphic)comp).GetMaterial();
        }
        public override Material MatAt(Rot4 rot, Thing thing = null) {
            if (thing != null) {
                return MatSingleFor(thing);
            }
            return base.MatAt(rot, thing);
        }
    }
}
