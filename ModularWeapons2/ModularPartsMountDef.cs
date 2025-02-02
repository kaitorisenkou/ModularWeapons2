using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class ModularPartsMountDef : Def {
        public List<MountAdapterClass> canAdaptAs = new List<MountAdapterClass>();
        [DefaultValue(true)]
        public bool allowEmpty = true;
        public string emptyLabel = "empty";

        public string labelShort = "";
        public string LabelShort {
            get => string.IsNullOrEmpty(labelShort) ? label : labelShort;
        }
    }
}
