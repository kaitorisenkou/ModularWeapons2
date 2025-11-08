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
        public float rotation = 0;
        public int layerOrder = 0;
        [DefaultValue(null)]
        public GraphicData adapterGraphic = null;
        public bool allowMoreAdapter = true;
        public ModularPartEffects effectsWhenEmpty;
        /*
        [DefaultValue(null)]
        public ModularPartsDef defaultPart = null;
        */

        public MountAdapterClass() {

        }
        public MountAdapterClass(MountAdapterClass original) {
            this.mountDef = original.mountDef;
            this.offset = original.offset;
            this.scale = original.scale;
            this.layerOrder = original.layerOrder;
            this.adapterGraphic = original.adapterGraphic;
            this.allowMoreAdapter = original.allowMoreAdapter;
            this.effectsWhenEmpty = original.effectsWhenEmpty;
        }

        Vector2? normalizedOffset_cache;
        public Vector2 NormalizedOffsetForUI {
            get {
                if (!normalizedOffset_cache.HasValue) {
                    SetParentAdapter(null);
                }
                return normalizedOffset_cache.Value;
            }
        }
        static public void SetDistancedForUI(MountAdapterClass[] adapters) {
            if (adapters.Any(t => !t.normalizedOffset_cache.HasValue)) {
                return;
            }
            adapters = adapters.OrderBy(t => Mathf.Abs(t.normalizedOffset_cache.Value.x - 0.5f)).ToArray();
            int infiLoopDetector = 10000;
            for (int i = 0; i < adapters.Length; i++) {
                if (infiLoopDetector < 0) break;
                for (int j = i - 1; j >= 0; j--) {
                    infiLoopDetector--;
                    if (infiLoopDetector < 0) break;
                    var distVec = adapters[i].NormalizedOffsetForUI- adapters[j].NormalizedOffsetForUI;
                    if (distVec.magnitude < 0.25f || (Mathf.Abs(distVec.x) < 0.25f && Mathf.Abs(distVec.y) < 0.25f)) {
                        adapters[i].normalizedOffset_cache = adapters[j].NormalizedOffsetForUI + ((distVec.x > 0 ? Vector2.right : Vector2.left) * 0.25f);
                        j = i - 1;
                    }
                }
            }
        }
        public void SetParentAdapter(MountAdapterClass parent) {
            var result = offset;
            if (parent != null) {
                result += parent.offset;
            }
            if (Mathf.Approximately(offset.y, 0)) {
                result.y = (mountDef.label[0] % 3) > 2 ? 0.75f : -0.75f;
            } else {
                result.y = offset.y > 0 ? 0.9375f : -0.9375f;
            }
            result.x = result.x * 2.5f;
            if (Mathf.Abs(result.y) > 0.74f) {
                //result.x += 0.1f;
                result.y = -result.y;
            }
            result.x = Mathf.Min(result.x, 0.9375f);
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
                attatchableParts_cache = GetAttatchableParts_Internal(true).OrderBy(t => t != null && !t.IsResearchFinished(out _));
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
            foreach (var i in effectsWhenEmpty.GetStatChangeTexts(weapon).OrderByDescending(t => t.Item2)) {
                //GUI.color = i.Item2;
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
        bool TryGetAdapterCRFor(ModularPartsMountDef mountDef, int layerOrder, Vector2 offset, Vector2 scale,ref List<MWCameraRenderer.MWCameraRequest> result,float rotation= 0) {
            var newOffset = offset+this.offset;
            var newScale = scale*this.scale;
            var newRotation = rotation + this.rotation;
            if (this.mountDef == mountDef) {
                if (this.adapterGraphic != null) {
                    result.Add(new MWCameraRenderer.MWCameraRequest(
                        this.adapterGraphic.Graphic.MatSingle, newOffset, layerOrder, newScale,newRotation)
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
                            this.adapterGraphic.Graphic.MatSingle, newOffset, layerOrder, newScale,newRotation)
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
            return this.scale;
        }
        bool TryGetScaleFor(ModularPartsMountDef mountDef, ref Vector2 parentScale) {
            parentScale *= this.scale;
            if (this.mountDef == mountDef)
                return true;
            if (!allowMoreAdapter || this.mountDef.canAdaptAs == null) {
                return false;
            }
            foreach (var i in this.mountDef.canAdaptAs) {
                if (i.TryGetScaleFor(mountDef, ref parentScale)) {
                    return true;
                }
            }
            return false;
        }

        public float GetRotationFor(ModularPartsDef partsDef) {
            float result = 0;
            if (TryGetRotationFor(partsDef.attachedTo, ref result)) {
                //return Quaternion.Euler(Vector3.up * result);
                return result;
            }
            //return Quaternion.Euler(Vector3.up * this.rotation);
            return this.rotation;
        }
        bool TryGetRotationFor(ModularPartsMountDef mountDef, ref float parentRotation) {
            parentRotation += this.rotation;
            if (this.mountDef == mountDef)
                return true;
            if (!allowMoreAdapter || this.mountDef.canAdaptAs == null) {
                return false;
            }
            foreach (var i in this.mountDef.canAdaptAs) {
                if (i.TryGetRotationFor(mountDef, ref parentRotation)) {
                    return true;
                }
            }
            return false;
        }
    }
}
