﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class MountAdapterClass {
        //public string tagString = null;
        public ModularPartsMountDef mountDef;
        public Vector2 offset = Vector2.zero;
        public int layerOrder = 0;
        [DefaultValue(null)]
        public GraphicData adapterGraphic = null;
        public bool allowMoreAdapter = true;
        public ModularPartEffects effectsWhenEmpty;
        /*
        [DefaultValue(null)]
        public ModularPartsDef defaultPart = null;
        */

        Vector2? normalizedOffset_cache;
        public Vector2 NormalizedOffsetForUI {
            get {
                if (!normalizedOffset_cache.HasValue) {
                    SetParentAdapter(null);
                }
                return normalizedOffset_cache.Value;
            }
        }
        public void SetParentAdapter(MountAdapterClass parent) {
            var result = offset;
            if (parent != null) {
                result += parent.offset;
            }
            if (Mathf.Abs(result.y) < 1E-45f) {
                result.y = (mountDef.label[0] % 2) > 0 ? 0.75f : -0.75f;
            } else {
                result.y = result.y > 0 ? 0.9375f : -0.9375f;
            }
            result.x = Mathf.Min(result.x * 2.5f, 0.9375f);
            normalizedOffset_cache = result;
        }
        public static void ResetAdaptersParent(IEnumerable<MountAdapterClass> children, MountAdapterClass parent = null) {
            foreach(var i in children) {
                i.SetParentAdapter(parent);
            }
        }

        IEnumerable<ModularPartsDef> attatchableParts_cache;
        public IEnumerable<ModularPartsDef> GetAttatchableParts() {
            if (attatchableParts_cache == null) {
                attatchableParts_cache = GetAttatchableParts_Internal(true);
            }
            return attatchableParts_cache;
        }
        IEnumerable<ModularPartsDef> GetAttatchableParts_Internal(bool containAllowEmpty) {
            if (containAllowEmpty && mountDef.allowEmpty) {
                yield return null;
            }
            if (allowMoreAdapter && mountDef.canAdaptAs != null) {
                foreach (var ac in mountDef.canAdaptAs) {
                    foreach (var i in ac.GetAttatchableParts_Internal(false)) {
                        yield return i;
                    }
                }
            }
            foreach (var i in DefDatabase<ModularPartsDef>.AllDefsListForReading.Where(t => t.attachedTo == this.mountDef)) {
                yield return i;
            }
        }

        public PartsAttachHelper GenerateHelper(ModularPartsDef partsDef) {
            /*if (tagString.NullOrEmpty()) {
                return new PartsAttachHelper(partsDef, tagString);
            }*/
            return new PartsAttachHelper(partsDef, mountDef);
        }


        protected readonly Color colorFactor = new Color(1f, 1f, 1f, 0.5f);
        public virtual void DrawEmptyDescription(Rect rect, CompModularWeapon weapon = null) {
            var rectLeft = rect.LeftPart(0.333f);
            var rectCenter = new Rect(rectLeft) { x = rectLeft.xMax };
            Widgets.DrawWindowBackground(rectLeft, colorFactor);
            float labelHeight = Text.LineHeightOf(GameFont.Small) * 2;
            Widgets.Label(rectLeft.TopPartPixels(labelHeight).ContractedBy(4f), mountDef.emptyLabel.CapitalizeFirst());
            rectLeft.y += labelHeight;
            rectLeft.height -= labelHeight;
            if (mountDef.emptyDescription.NullOrEmpty()) {
                mountDef.emptyDescription = "MW2_emptyDescription".Translate();
            }
            Widgets.Label(rectLeft.ContractedBy(4f), mountDef.emptyDescription);
            Widgets.DrawWindowBackground(rectCenter, colorFactor);
            var tmpColor = GUI.color;
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.verticalSpacing = -6f;
            listingStandard.Begin(rectCenter);
            foreach (var i in effectsWhenEmpty.GetStatChangeTexts(weapon)) {
                GUI.color = i.Item2;
                listingStandard.Label(i.Item1);
            }
            listingStandard.End();
            GUI.color = tmpColor;
        }
    }
}
