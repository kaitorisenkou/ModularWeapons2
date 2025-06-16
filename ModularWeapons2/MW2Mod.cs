using Verse;
using RimWorld;
using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ModularWeapons2 {
    public class MW2Mod : Mod {
        
        public static List<StatDef> statDefsShow = new List<StatDef>();
        public static List<StatCategoryDef> statCategoryShow = new List<StatCategoryDef>();
        public static List<string> lessIsBetter = new List<string>();
        public static List<StatDef> statDefsForceNonImmutable = new List<StatDef>();

        static Lazy<bool> isWeaponRacksEnable = new Lazy<bool>(() => AccessTools.AllAssemblies().Any(t => t.FullName.Contains("WeaponRacks")));
        public static bool IsWeaponRacksEnable => isWeaponRacksEnable.Value;
        static Lazy<bool> isLTOGroupsEnable = new Lazy<bool>(() => AccessTools.AllAssemblies().Any(t => t.FullName.Contains("TacticalGroups")));
        public static bool IsLTOGroupsEnable => isLTOGroupsEnable.Value;
        static Lazy<bool> isCEEnable = new Lazy<bool>(() => AccessTools.AllAssemblies().Any(t => t.FullName.Contains("CombatExtended")));
        public static bool IsCombatExtendedEnable => isCEEnable.Value;

        public MW2Mod(ModContentPack content) : base(content) {
            
        }
    }
}
