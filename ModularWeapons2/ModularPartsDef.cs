using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace ModularWeapons2 {
    public class ModularPartsDef : Def {
        public string labelShort;
        public ModularPartsMountDef attachedTo;
        public GraphicData graphicData;
        public float GUIScale = 2f;

        public List<StatModifier> StatOffsets = new List<StatModifier>();
        public List<StatModifier> StatFactors = new List<StatModifier>();
        public List<StatModifier> EquippedStatOffsets=new List<StatModifier>();

        public List<ThingDefCountClass> costList = new List<ThingDefCountClass>();
        public int stuffCost = 0;

        protected Texture2D Texture {
            get {
                return graphicData.Graphic.MatSingle.mainTexture as Texture2D;
            }
        }

        protected readonly Color colorFactor = new Color(1f, 1f, 1f, 0.5f);
        public virtual void DrawDescription(Rect rect, CompModularWeapon weapon = null) {
            var rectLeft = rect.LeftPart(0.333f);
            var rectCenter = new Rect(rectLeft) { x = rectLeft.xMax };
            var rectRight = new Rect(rectCenter) { x = rectCenter.xMax };
            Widgets.DrawWindowBackground(rectLeft, colorFactor);
            float labelHeight = Text.LineHeightOf(GameFont.Small) * 2;
            Widgets.Label(rectLeft.TopPartPixels(labelHeight).ContractedBy(4f), label.CapitalizeFirst());
            rectLeft.y += labelHeight;
            rectLeft.height -= labelHeight;
            Widgets.Label(rectLeft.ContractedBy(4f), description);
            Widgets.DrawWindowBackground(rectCenter, colorFactor);
            //Widgets.Label(rectCenter.ContractedBy(4f), GetStatChangesString(weapon).ToString());
            DrawStatChanges(rectCenter.ContractedBy(4f), weapon);
            Widgets.DrawWindowBackground(rectRight, colorFactor);
            //Widgets.Label(rectRight.ContractedBy(4f), "required materials will be here");
            DrawCostList(rectRight.ContractedBy(4f), weapon);
        }

        protected virtual void DrawCostList(Rect rect, CompModularWeapon weapon = null) {
            Listing_Standard listingStandard = new Listing_Standard();
            float lineHeight = Text.LineHeightOf(GameFont.Medium);
            listingStandard.Begin(rect);
            if (stuffCost > 0) {
                var rectLine = listingStandard.GetRect(lineHeight);
                var stuff= weapon?.parent.Stuff ?? ThingDefOf.Cloth;
                Widgets.DrawTextureFitted(rectLine.LeftPartPixels(lineHeight), stuff.graphic.MatSingle.mainTexture, 1f);
                rectLine.x += lineHeight;
                Widgets.Label(rectLine, " x" + stuffCost);
            }
            foreach(var i in costList) {
                var rectLine = listingStandard.GetRect(lineHeight);
                var stuff = i.thingDef;
                Widgets.DrawTextureFitted(rectLine.LeftPartPixels(lineHeight), stuff.graphic.MatSingle.mainTexture, 1f);
                rectLine.x += lineHeight;
                Widgets.Label(rectLine, " x" + i.count);
            }
            listingStandard.End();
        }
        protected ThingDef GetWeaponStuff(CompModularWeapon weapon) {
            return weapon?.parent.Stuff ?? ThingDefOf.Cloth;
        }

        protected virtual void DrawStatChanges(Rect rect, CompModularWeapon weapon = null) {
            var tmpColor = GUI.color;
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.verticalSpacing = -6f;
            listingStandard.Begin(rect);
            foreach (var i in GetStatChanges(weapon)) {
                GUI.color = i.Item2;
                listingStandard.Label(i.Item1);
            }
            listingStandard.End();
            GUI.color = tmpColor;
        }

        protected virtual IEnumerable<(string,Color)> GetStatChanges(CompModularWeapon weapon = null) {
            var builder = new StringBuilder();
            Color color = Color.white;
            foreach(var i in StatOffsets) {
                builder.Clear();
                float statValue = (i.stat.ToStringStyleUnfinalized.ToString().Contains("Percent") ? i.value * 100 : i.value);
                if (statValue > 0) { builder.Append("+"); }
                builder.Append(statValue.ToString());
                builder.Append(" ");
                builder.Append(i.stat.label);
                if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                    color = Color.red;
                } else {
                    color = Color.green;
                }
                yield return (builder.ToString(), color);
            }
            foreach (var i in StatFactors) {
                builder.Clear();
                if (i.value > 0) builder.Append("+");
                builder.Append(i.value.ToStringPercent());
                builder.Append(" ");
                builder.Append(i.stat.label);
                if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                    color = Color.red;
                } else {
                    color = Color.green;
                }
                yield return (builder.ToString(), color);
            }
            foreach (var i in EquippedStatOffsets) {
                builder.Clear();
                builder.Append(i.value.ToStringByStyle(i.stat.ToStringStyleUnfinalized, ToStringNumberSense.Offset));
                builder.Append(" ");
                builder.Append(i.stat.label);
                if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                    color = Color.red;
                } else {
                    color = Color.green;
                }
                yield return (builder.ToString(), color);
            }
        }
    }
}
