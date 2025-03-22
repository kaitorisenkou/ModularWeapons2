using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class ModularPartsMountDef : Def {
        public List<MountAdapterClass> canAdaptAs = new List<MountAdapterClass>();
        [DefaultValue(true)]
        public bool allowEmpty = true;
        public string emptyLabel = "";
        public string EmptyLabel {
            get => string.IsNullOrEmpty(emptyLabel) ? "MW2_Empty".Translate().ToString() : emptyLabel;
        }
        public string emptyDescription = "";

        [NoTranslate]
        public string labelShort = "";
        public string LabelShort {
            get => string.IsNullOrEmpty(labelShort) ? label : labelShort;
        }
    }
}
