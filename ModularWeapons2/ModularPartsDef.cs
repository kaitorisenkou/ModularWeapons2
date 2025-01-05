using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class ModularPartsDef : Def {
        public ModularPartsMountDef attachedTo;

        public List<StatModifier> StatOffsets;
        public List<StatModifier> StatFactors;
        public List<StatModifier> EquippedStatOffsets;
    }
}
