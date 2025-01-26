using System.Collections.Generic;

using Verse;
using RimWorld;

namespace ModularWeapons2 {
    public class MW2Mod : Mod {
        
        public static List<StatDef> statDefsShow = new List<StatDef>();
        public static List<StatCategoryDef> statCategoryShow = new List<StatCategoryDef>();
        public static List<string> lessIsBetter = new List<string>();

        public MW2Mod(ModContentPack content) : base(content) {
            
        }
    }
}
