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

        public static bool TryGetAssigned(Thing thing, out Graphic_UniqueByComp graphic_UniqueByComp) {
            return TryGetAssigned(thing?.Graphic, out graphic_UniqueByComp, thing);
        }
        public static bool TryGetAssigned(Graphic graphic, out Graphic_UniqueByComp graphic_UniqueByComp,Thing thing=null) {
            if (typeof(Graphic_UniqueByComp).IsAssignableFrom(graphic.GetType())) {
                graphic_UniqueByComp = graphic as Graphic_UniqueByComp;
                return true;
            }
            var inner = GraphicUtility.ExtractInnerGraphicFor(graphic, thing);
            if (typeof(Graphic_UniqueByComp).IsAssignableFrom(inner.GetType())) {
                graphic_UniqueByComp = inner as Graphic_UniqueByComp;
                return true;
            }
            graphic_UniqueByComp = null;
            return false;
        }
    }
}
