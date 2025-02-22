using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class GunsmithPresetDef : Def {
        [DefaultValue(null)]
        public string customName = null;
        public ThingDef weapon;
        [NoTranslate]
        public List<string> weaponTags;
        public List<PartsAttachHelperClass> requiredParts = new List<PartsAttachHelperClass>();
        public int optionalPartsCount = 1;
        public List<PartsAttachHelperClass> optionalParts = new List<PartsAttachHelperClass>();
    }
    /*
    public class PartsDefIndexPair {
        public ModularPartsDef partsDef = null;
        public int index = 0;
    }
    */
}
