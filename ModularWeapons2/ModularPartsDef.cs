using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class ModularPartsDef : Def {
        public string labelShort;
        public ModularPartsMountDef attachedTo;
        public GraphicData graphicData;

        public List<StatModifier> StatOffsets;
        public List<StatModifier> StatFactors;
        public List<StatModifier> EquippedStatOffsets;
    }
}
