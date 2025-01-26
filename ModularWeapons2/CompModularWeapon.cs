using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;
using static UnityEngine.Random;

namespace ModularWeapons2 {
    public class CompModularWeapon : ThingComp, ICompUniqueGraphic {
        public CompProperties_ModularWeapon Props {
            get {
                return (CompProperties_ModularWeapon)this.props;
            }
        }
        protected List<ModularPartsDef> attachedParts;
        public IReadOnlyList<ModularPartsDef> AttachedParts {
            get => attachedParts;
        }
        public void SetPart(int index,ModularPartsDef part) {
            attachedParts[index] = part;
            RefleshParts();
        }

        protected virtual void RefleshParts() {
            cachedEOStats =
                attachedParts.Where(t=>t!=null && !t.EquippedStatOffsets.NullOrEmpty())
                .SelectMany(t1 => t1.EquippedStatOffsets.Select(t2 => (t2.stat, t2.value)))
                .GroupBy(t1 => t1.stat)
                .Select(t1 => (t1.Key, t1.Sum(t2 => t2.value)));
        }

        public void SetGraphicDirty(bool renderNow = true) {
            textureDirty = true;
            if (renderNow)
                GetTexture();
        }

        public virtual float GetEquippedOffset(StatDef stat) {
            float value = 0;
            foreach (var i in attachedParts) {
                if (i?.EquippedStatOffsets == null) continue;
                var mod = i.EquippedStatOffsets.FirstOrFallback(t => t != null && t.stat == stat, null);
                value += mod == null ? 0 : mod.value;
            }
            return value;
        }
        protected IEnumerable<(StatDef stat, float value)> cachedEOStats = null;
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats() {
            if (cachedEOStats != null) {
                foreach (var pair in cachedEOStats) {
                    if (pair.stat == null ||
                        (//!Dialog_Gunsmith.ForceShowEquippedStats &&
                        !parent.def.equippedStatOffsets.NullOrEmpty() &&
                        parent.def.equippedStatOffsets.Any(t => t.stat == pair.stat))) {
                        continue;
                    }
                    StringBuilder stringBuilder = new StringBuilder(pair.stat.description);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("MW2_StatsReport_ByCustomParts" + ": " + pair.stat.ValueToString(pair.value, ToStringNumberSense.Offset, pair.stat.finalizeEquippedStatOffset));
                    float value = StatWorker.StatOffsetFromGear(parent, pair.stat);
                    yield return new StatDrawEntry(StatCategoryDefOf.EquippedStatOffsets, pair.stat, value, StatRequest.ForEmpty(), ToStringNumberSense.Offset, null, true).SetReportText(stringBuilder.ToString());
                }
            }
        }

        public override float GetStatFactor(StatDef stat) {
            float value = base.GetStatFactor(stat);
            foreach (var i in attachedParts) {
                if (i?.StatFactors == null) continue;
                var mod = i.StatFactors.FirstOrFallback(t => t!=null && t.stat == stat, null);
                value += mod == null ? 0 : mod.value;
            }
            return value;
        }
        public override float GetStatOffset(StatDef stat) {
            float value = base.GetStatOffset(stat);
            foreach(var i in attachedParts) {
                if (i?.StatOffsets == null) continue;
                var mod = i.StatOffsets.FirstOrFallback(t =>t != null && t.stat == stat, null);
                value += mod == null ? 0 : mod.value;
            }
            return value;
        }

        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            attachedParts = Props.partsMounts.Select(t => t.defaultPart).ToList();
            RefleshParts();
        }
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Collections.Look(ref attachedParts, "attachedParts", true, LookMode.Def);
        }

        RenderTexture renderTextureInt = null;
        bool textureDirty;
        public virtual Texture GetTexture() {
            if (renderTextureInt == null) {
                var texture = Props.baseGraphicData.Graphic.MatSingle.mainTexture;
                renderTextureInt = new RenderTexture(texture.width*2, texture.height*2, 32, RenderTextureFormat.ARGB32);
                textureDirty = true;
            }
            if (textureDirty) {
                MWCameraRenderer.Render(renderTextureInt, this);
                if (materialInt != null) {
                    materialInt.mainTexture = renderTextureInt;
                }
                textureDirty = false;
            }
            return renderTextureInt;
        }
        Material materialInt = null;
        public virtual Material GetMaterial() {
            if (materialInt == null) {
                MaterialRequest req = new MaterialRequest {
                    mainTex = GetTexture(),
                    shader = ShaderTypeDefOf.Cutout.Shader,
                    color = Color.white,
                    colorTwo = Color.white
                };
                materialInt = MaterialPool.MatFrom(req);
            }
            return materialInt;
        }
        public virtual IEnumerable<MWCameraRenderer.MWCameraRequest> GetRequestsForRenderCam() {
            /*
            Material mat1 = ThingDefOf.WoodLog.graphic.MatSingle;
            Material mat2 = new Material(ThingDefOf.Steel.graphic.MatSingle);
            mat2.shader = ShaderDatabase.CutoutSkinOverlay;
            mat2.SetTexture(ShaderPropertyIDs.MaskTex, mat1.mainTexture);
            yield return mat1;
            yield return mat2;
            */
            yield return new MWCameraRenderer.MWCameraRequest(Props.baseGraphicData.Graphic.MatSingle, Vector2.zero, 0);
            /*
            foreach(var i in attachedParts) {
                yield return i.graphicData.Graphic.MatSingle;
            }
            */
            for(int i = 0; i < Props.partsMounts.Count; i++) {
                if (attachedParts[i] == null) continue;
                if (Props.partsMounts[i].adapterGraphic != null) {
                    yield return new MWCameraRenderer.MWCameraRequest(
                        Props.partsMounts[i].adapterGraphic.Graphic.MatSingle,
                        Props.partsMounts[i].offset,
                        Props.partsMounts[i].layerOrder
                        );
                }
                yield return new MWCameraRenderer.MWCameraRequest(
                    attachedParts[i].graphicData.Graphic.MatSingle,
                    Props.partsMounts[i].offset,
                    Props.partsMounts[i].layerOrder
                    );
            }
        }
    }
}
