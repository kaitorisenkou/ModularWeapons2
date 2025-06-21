using Verse;
using RimWorld;
using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        static Lazy<bool> isSMYHEnable = new Lazy<bool>(() => AccessTools.AllAssemblies().Any(t => t.FullName.Contains("ShowMeYourHands")));
        public static bool IsShowMeYourHandsEnable => isSMYHEnable.Value;
        static Lazy<bool> isCEEnable = new Lazy<bool>(() => AccessTools.AllAssemblies().Any(t => t.FullName.Contains("CombatExtended")));
        public static bool IsCombatExtendedEnable => isCEEnable.Value;

        public static MW2Settings settings;

        public MW2Mod(ModContentPack content) : base(content) {
            settings = GetSettings<MW2Settings>();
        }

        public override string SettingsCategory() {
            return "ModularWeapons2";
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            Listing_Standard listingStandard1 = new Listing_Standard();
            Rect rect1 = new Rect(inRect);
            rect1.width = inRect.width / 2f - 20;
            listingStandard1.Begin(rect1);
            //--------
            var rectTmp = listingStandard1.GetRect(Text.LineHeightOf(GameFont.Medium));
            var textTmp = "MW2_UseStyledTexture".Translate();
            rectTmp.width = Text.CalcSize(textTmp).x + 36;
            Widgets.CheckboxLabeled(rectTmp, textTmp, ref settings.useStyledTexture);
            //--------
            listingStandard1.End();
            base.DoSettingsWindowContents(inRect);
        }

        public static void InjectStyleDefs() {
            var defs = DefDatabase<StyleCategoryDef>.AllDefsListForReading;
            int injectCount = 0;
            foreach(var catDef in defs) {
                foreach(var i in catDef.thingDefStyles) {
                    if (!i.StyleDef.HasModExtension<ModExtension_ModularStyledWeapon>() &&
                        i.ThingDef.HasComp<CompModularWeapon>() && 
                        typeof(Graphic_UniqueByComp).IsAssignableFrom(i.ThingDef.graphicData.graphicClass)) {
                        var data = i.StyleDef.graphicData;
                        data.graphicClass = typeof(Graphic_UniqueByComp);
                        data.drawSize *= 2;
                        data.CopyFrom(data);
                        injectCount++;
                    }
                }
            }
            GraphicDatabase.Clear();
            Log.Message("[MW2] Injected for "+ injectCount+ " weapon styles");
        }
    }

    public class MW2Settings : ModSettings {
        public bool useStyledTexture = false;

        public override void ExposeData() {
            Scribe_Values.Look(ref useStyledTexture, "useStyledTexture", false);
            base.ExposeData();
        }
    }
}
