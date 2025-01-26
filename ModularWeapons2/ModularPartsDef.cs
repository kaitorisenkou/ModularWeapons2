using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class ModularPartsDef : Def {
        public string labelShort;
        public ModularPartsMountDef attachedTo;
        public GraphicData graphicData;
        public float GUIScale = 2f;

        public List<StatModifier> StatOffsets = new List<StatModifier>();
        public List<StatModifier> StatFactors = new List<StatModifier>();
        public List<StatModifier> EquippedStatOffsets=new List<StatModifier>();
    }
}
