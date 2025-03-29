using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class MountAdapterClass {
        //public string tagString = null;
        public ModularPartsMountDef mountDef;
        public Vector2 offset = Vector2.zero;
        public Vector2 scale = Vector2.one;
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
            if (Mathf.Approximately(result.y, 0)) {
                result.y = (mountDef.label[0] % 3) > 0 ? 0.75f : -0.75f;
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
            Widgets.Label(rectLeft.TopPartPixels(labelHeight).ContractedBy(4f), mountDef.EmptyLabel.CapitalizeFirst());
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

        public Vector2 GetOffsetFor(ModularPartsDef partsDef) {
            Vector2 result = Vector2.zero;
            if(TryGetOffsetFor(partsDef.attachedTo, ref result)) {
                return result;
            }
            return this.offset;
        }
        bool TryGetOffsetFor(ModularPartsMountDef mountDef, ref Vector2 parentOffset) {
            if (this.mountDef == mountDef) {
                parentOffset += this.offset;
                return true;
            }
            if (!allowMoreAdapter || this.mountDef.canAdaptAs == null) {
                return false;
            }
            foreach(var i in this.mountDef.canAdaptAs) {
                if (i.TryGetOffsetFor(mountDef, ref parentOffset)) {
                    parentOffset += this.offset;
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<MWCameraRenderer.MWCameraRequest> GetAdapterCRFor(ModularPartsDef partsDef, Vector2 adapterTextureOffset, int layerOrder) {
            List<MWCameraRenderer.MWCameraRequest> result = new List<MWCameraRenderer.MWCameraRequest>();
            TryGetAdapterCRFor(partsDef.attachedTo, layerOrder, adapterTextureOffset, Vector2.one, ref result);
            return result;
        }
        bool TryGetAdapterCRFor(ModularPartsMountDef mountDef, int layerOrder, Vector2 offset, Vector2 scale, ref List<MWCameraRenderer.MWCameraRequest> result) {
            var newOffset = offset+this.offset;
            var newScale = scale*this.scale;
            if (this.mountDef == mountDef) {
                if (this.adapterGraphic != null) {
                    result.Add(new MWCameraRenderer.MWCameraRequest(
                        this.adapterGraphic.Graphic.MatSingle, newOffset, layerOrder, newScale)
                    );
                }
                return true;
            }
            if (!allowMoreAdapter || this.mountDef.canAdaptAs == null) {
                return false;
            }
            foreach (var i in this.mountDef.canAdaptAs) {
                if (i.TryGetAdapterCRFor(mountDef, layerOrder, newOffset, newScale, ref result)) {
                    if (this.adapterGraphic != null) {
                        result.Add(new MWCameraRenderer.MWCameraRequest(
                            this.adapterGraphic.Graphic.MatSingle, newOffset, layerOrder, newScale)
                            );
                    }
                    return true;
                }
            }
            return false;
        }
        /*
        public GraphicData GetAdapterGraphicFor(ModularPartsDef partsDef) {
            if (TryAdapterGraphicFor(partsDef.attachedTo, out GraphicData result)) {
                return result;
            }
            return this.adapterGraphic;
        }
        bool TryAdapterGraphicFor(ModularPartsMountDef mountDef, out GraphicData result) {
            if (this.mountDef == mountDef) {
                result = this.adapterGraphic;
                return true;
            }
            result = null;
            if (!allowMoreAdapter || this.mountDef.canAdaptAs == null) {
                return false;
            }
            foreach (var i in this.mountDef.canAdaptAs) {
                if (i.TryAdapterGraphicFor(mountDef, out GraphicData newResult)) {
                    result = newResult ?? this.adapterGraphic;
                    return true;
                }
            }
            return false;
        }
        */
        public Vector2 GetScaleFor(ModularPartsDef partsDef) {
            Vector2 result = Vector2.one;
            if (TryGetScaleFor(partsDef.attachedTo, ref result)) {
                return result;
            }
            return this.offset;
        }
        bool TryGetScaleFor(ModularPartsMountDef mountDef, ref Vector2 parentOffset) {
            parentOffset *= this.scale;
            if (this.mountDef == mountDef)
                return true;
            if (!allowMoreAdapter || this.mountDef.canAdaptAs == null) {
                return false;
            }
            foreach (var i in this.mountDef.canAdaptAs) {
                if (i.TryGetScaleFor(mountDef, ref parentOffset)) {
                    return true;
                }
            }
            return false;
        }
    }
}
