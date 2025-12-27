using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Noise;

namespace ModularWeapons2 {
    public class GunsmithPresetDef : Def {
        [DefaultValue(null)]
        public string customName = null;
        public ThingDef weapon;
        [NoTranslate]
        public List<string> weaponTags;
        public List<WeaponClassDef> weaponClasses;
        public List<PartsAttachHelperClass> requiredParts = new List<PartsAttachHelperClass>();
        public int optionalPartsCount = 1;
        public List<PartsAttachHelperClass> optionalParts = new List<PartsAttachHelperClass>();


        public virtual bool IsAllowed(Pawn pawn) {
            return IsAllowedWeaponTag(pawn) && IsAllowedWeaponClass(pawn);
        }

        public virtual bool IsAllowedWeaponTag(Pawn pawn) {
            List<string> pawnWeaponTags = null;
            if (pawn.kindDef != null && pawn.kindDef.weaponTags != null) {
                pawnWeaponTags = pawn.kindDef.weaponTags;
            }
            if (pawnWeaponTags.NullOrEmpty()) {
                return true;
            }
            return pawnWeaponTags.Any(t => weaponTags.Contains(t));
        }
        public virtual bool IsAllowedWeaponClass(Pawn pawn) {
            var forbiddenByXenotype = pawn.genes?.Xenotype?.forbiddenWeaponClasses;
            if (!forbiddenByXenotype.NullOrEmpty()) {
                foreach (var i in weaponClasses) {
                    if (forbiddenByXenotype.Contains(i)) 
                        return false;
                }
            }
            var precepts = pawn.ideo?.Ideo?.GetAllPreceptsOfType<Precept_Weapon>();
            if (precepts!=null && precepts.Any()) {
                foreach (var i in precepts) {
                    if (weaponClasses.Contains(i.despised)) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
    /*
    public class PartsDefIndexPair {
        public ModularPartsDef partsDef = null;
        public int index = 0;
    }
    */
}
