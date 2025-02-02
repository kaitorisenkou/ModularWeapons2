using System.Collections.Generic;
using Verse;

namespace ModularWeapons2 {
    public class GunsmithPresetDef : Def {
        [DefaultValue(null)]
        public string customName = null;
        public ThingDef weapon;
        [NoTranslate]
        public List<string> weaponTags;
        public List<PartsDefIndexPair> requiredParts = new List<PartsDefIndexPair>();
        public int optionalPartsCount = 1;
        public List<PartsDefIndexPair> optionalParts = new List<PartsDefIndexPair>();
    }
    public class PartsDefIndexPair {
        public ModularPartsDef partsDef = null;
        public int index = 0;
    }
}
