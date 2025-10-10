using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class MW2Mod : Mod {
        public static List<StatDef> statDefsShow = new List<StatDef>();
        public static List<StatCategoryDef> statCategoryShow = new List<StatCategoryDef>();
        public static List<string> lessIsBetter = new List<string>();
        public static List<StatDef> statDefsForceNonImmutable = new List<StatDef>();


        static readonly string[] ExternalAssemblyNames = {
            "WeaponRacks",
            "TacticalGroups",
            "ShowMeYourHands",
            "MuzzleFlash",
            "yayoAni",
            "CombatExtended",
        };
        static Lazy<List<Assembly>> ExternalModAssemblies = new Lazy<List<Assembly>>(() => {
            var allAssemblies = AccessTools.AllAssemblies().ToList();
            var result= ExternalAssemblyNames.Select(t => allAssemblies.FirstOrFallback(u => u.GetName().Name.Contains(t))).ToList();
#if DEBUG
            MWDebug.LogMessage(
                "[MW2]ExternalAssemblies: \n  " + 
                string.Join("\n  ", result.Select((t, i) => "[" + i + "] " + (t == null ? "null" : t.FullName))));
#endif
            return result;
        });
        public static bool IsWeaponRacksEnable => ExternalModAssemblies.Value[0] != null;
        public static Assembly Assembly_WeaponRacks => ExternalModAssemblies.Value[0];

        public static bool IsLTOGroupsEnable => ExternalModAssemblies.Value[1] != null;
        public static Assembly Assembly_LTOGroups => ExternalModAssemblies.Value[1];

        public static bool IsShowMeYourHandsEnable => ExternalModAssemblies.Value[2] != null;
        public static Assembly Assembly_ShowMeYourHands => ExternalModAssemblies.Value[2];

        public static bool IsMuzzleFlashEnable => ExternalModAssemblies.Value[3] != null;
        public static Assembly Assembly_MuzzleFlash => ExternalModAssemblies.Value[3];

        public static bool IsYayoAnimationEnable => ExternalModAssemblies.Value[4] != null;
        public static Assembly Assembly_YayoAnimation => ExternalModAssemblies.Value[4];

        public static bool IsCombatExtendedEnable => ExternalModAssemblies.Value[5] != null;
        public static Assembly Assembly_CE => ExternalModAssemblies.Value[5];


        public static void JustQueueLongEvent(Action action, string key) {
#if V15
            LongEventHandler.QueueLongEvent(action, key, false, null, true, null);
#else
            LongEventHandler.QueueLongEvent(action, key, false, null, true, false, null);
#endif
        }


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
            foreach (var catDef in defs) {
                foreach (var i in catDef.thingDefStyles) {
                    if (!i.StyleDef.HasModExtension<ModExtension_ModularStyledWeapon>() &&
                        (i.ThingDef?.HasComp<CompModularWeapon>() ?? false) &&
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
