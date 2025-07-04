﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Noise;
using static HarmonyLib.Code;

namespace ModularWeapons2 {
    public class ModularPartsDef : Def {
        [NoTranslate]
        public string labelShort;
        public ModularPartsMountDef attachedTo;
        public GraphicData graphicData;
        public float GUIScale = 2f;

        public List<ResearchProjectDef> researchPrerequisites;


        public List<ThingDefCountClass> costList = new List<ThingDefCountClass>();
        public int stuffCost = 0;

        public ModularPartEffects effects;

        public List<StatModifier> StatOffsets {
            get {
                if (effects.statOffsets == null) effects.statOffsets = new List<StatModifier>();
                return effects.statOffsets;
            }
        }
        public List<StatModifier> StatFactors {
            get {
                if (effects.statFactors == null) effects.statFactors = new List<StatModifier>();
                return effects.statFactors;
            }
        }
        public List<StatModifier> EquippedStatOffsets {
            get {
                if (effects.equippedStatOffsets == null) effects.equippedStatOffsets = new List<StatModifier>();
                return effects.equippedStatOffsets;
            }
        }
        public List<MountAdapterClass> AdditionalAdapters {
            get {
                if (effects.additionalAdapters == null) effects.additionalAdapters = new List<MountAdapterClass>();
                return effects.additionalAdapters;
            }
        }
        public List<Tool> Tools {
            get {
                if (effects.tools == null) effects.tools = new List<Tool>();
                return effects.tools;
            }
        }
        public MWAbilityProperties Ability {
            get {
                return effects.ability;
            }
        }

        protected Texture2D Texture {
            get {
                return graphicData.Graphic.MatSingle.mainTexture as Texture2D;
            }
        }

        protected readonly Color colorFactor = new Color(1f, 1f, 1f, 0.5f);
        public virtual void DrawDescription(Rect rect, CompModularWeapon weapon = null) {
            if(!IsResearchFinished(out IEnumerable<string> unfinishedLabels)) {
                Widgets.Label(rect.ContractedBy(4f), "MW2_researchPrerequisites".Translate() + string.Join(", ", unfinishedLabels));
                return;
            }
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

        Vector2 scrollPos_statChanges = Vector2.zero;
        Rect viewRect_statChanges = new Rect();
        protected virtual void DrawStatChanges(Rect rect, CompModularWeapon weapon = null) {
            var tmpColor = GUI.color;
            var lineHeight = Text.LineHeightOf(GameFont.Small);
            Rect textRect = new Rect(0, 0, rect.width - 16f, lineHeight);
            Widgets.BeginScrollView(rect, ref scrollPos_statChanges, viewRect_statChanges, true);
            foreach (var i in effects.GetStatChangeTexts(weapon).OrderByDescending(t=>t.Item2)) {
                textRect.height = Text.CalcHeight(i.Item1, textRect.width);
                //GUI.color = i.Item2;
                Widgets.Label(textRect, i.Item1);
                textRect.y += textRect.height;
            }
            Widgets.EndScrollView();
            viewRect_statChanges = new Rect(0, 0, rect.width - 16f, textRect.y);
            GUI.color = tmpColor;
        }


        public bool IsResearchFinished(out IEnumerable<string> unfinishedLabels) {
            if (researchPrerequisites.NullOrEmpty()) {
                unfinishedLabels = Array.Empty<string>();
                return true;
            }
            unfinishedLabels = researchPrerequisites.Where(t => !t.IsFinished).Select(t => t.label);
            return !unfinishedLabels.Any();
        }
    }
}
