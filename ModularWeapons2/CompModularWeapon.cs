using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
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
        protected List<ModularPartsDef> attachedParts_buffer;
        public IReadOnlyList<ModularPartsDef> AttachedParts {
            get => attachedParts;
        }
        public virtual void SetPart(int index,ModularPartsDef part) {
            attachedParts[index] = part;
            RefleshParts();
        }
        public virtual void SetParts(List<ModularPartsDef> parts) {
            attachedParts = new List<ModularPartsDef>(parts);
            RefleshParts();
        }
        public virtual void SetPartsWithBuffer() {
            SetParts(attachedParts_buffer);
        }
        protected virtual void RefleshParts() {
            cachedEOStats =
                attachedParts.Where(t=>t!=null && !t.EquippedStatOffsets.NullOrEmpty())
                .SelectMany(t1 => t1.EquippedStatOffsets.Select(t2 => (t2.stat, t2.value)))
                .GroupBy(t1 => t1.stat)
                .Select(t1 => (t1.Key, t1.Sum(t2 => t2.value)));
            requestCache = null;
            SetGraphicDirty();
        }

        public virtual void BufferCurrent(bool overrideBuffer = false) {
            if (overrideBuffer || attachedParts_buffer.NullOrEmpty()) {
                attachedParts_buffer = new List<ModularPartsDef>(attachedParts);
                //attachedParts_buffer = attachedParts.ListFullCopy();
            } else {
                var tmp = attachedParts_buffer;
                attachedParts_buffer = new List<ModularPartsDef>(attachedParts);
                SetParts(tmp);
            }
        }
        public virtual void RevertToBuffer() {
            if (attachedParts_buffer.NullOrEmpty()) {
                Log.Warning("[MW2] RevertToBuffer() ran, but buffer is null!");
                return;
            }
            SetParts(attachedParts_buffer);
        }
        public virtual IEnumerable<(ThingDef, int)> GetIngredient_Current() {
            return sorted_GetIngredient(attachedParts);
        }
        public virtual IEnumerable<(ThingDef, int)> GetIngredient_Buffer() {
            return sorted_GetIngredient(attachedParts_buffer);
        }
        IEnumerable<(ThingDef, int)> requestCache = null;
        public virtual IEnumerable<(ThingDef,int)> GetRequiredIngredients() {
            if (requestCache == null) {
                requestCache = int_GetIngredient(attachedParts_buffer)
                    .Concat(int_GetIngredient_minus(attachedParts))
                    .GroupBy(t => t.Item1)
                    .Select(t => (t.Key, t.Sum(t2 => t2.Item2)));
                //Log.Message("[MW2] ingredients cached");
            }
            return requestCache;
        }
        IEnumerable<(ThingDef, int)> sorted_GetIngredient(List<ModularPartsDef> parts) {
            return int_GetIngredient(parts)
                .GroupBy(t => t.Item1)
                .Select(t => (t.Key, t.Sum(t2 => t2.Item2)));
        }
        IEnumerable<(ThingDef, int)> int_GetIngredient(List<ModularPartsDef> parts) {
            foreach (var i in parts) {
                if (i == null)
                    continue;
                yield return (parent.Stuff ?? ThingDefOf.Steel, i.stuffCost);
                foreach (var j in i.costList) {
                    yield return (j.thingDef, j.count);
                }
            }
        }
        IEnumerable<(ThingDef, int)> int_GetIngredient_minus(List<ModularPartsDef> parts) {
            return int_GetIngredient(parts).Select(t => (t.Item1, -t.Item2));
        }

        public IEnumerable<GunsmithPresetDef> AvailableGunsmithPresets
            => DefDatabase<GunsmithPresetDef>.AllDefsListForReading.Where(t => t.weapon == this.parent.def);
        public void RandomizePartsForPawn(Pawn owner) {
            List<string> weaponTags = null;
            if (owner.kindDef != null && owner.kindDef.weaponTags != null) {
                weaponTags = owner.kindDef.weaponTags;
            }
            string parentDef = parent.def.defName;
            IEnumerable<GunsmithPresetDef> allDefs = AvailableGunsmithPresets.Where(
                t =>
                t.weaponTags.NullOrEmpty() ||
                (weaponTags != null && t.weaponTags.Any(t2 => weaponTags.Contains(t2)))
                );
            if (!allDefs.Any()) {
                //TODO
                Log.Message("[MW2]no presetDefs found: " + parentDef);
                return;
            }
            var def = allDefs.RandomElement();
            weaponOverrideLabel = def.customName ?? "";
            var partsReplace = new ModularPartsDef[Props.partsMounts.Count];
            foreach(var i in def.requiredParts) {
                partsReplace[i.index] = i.partsDef;
            }

            int optionLength = def.optionalParts.Count();
            int optionIndex = 0;
            for (int i = 0; i < def.optionalPartsCount; i++) {
                optionIndex = (int)Mathf.Repeat(optionIndex + UnityEngine.Random.Range(0, optionLength), optionLength);
                var option = def.optionalParts[optionIndex];
                partsReplace[option.index] = option.partsDef;
            }
            SetParts(partsReplace.ToList());
        }
        public override void Notify_Equipped(Pawn pawn) {
            base.Notify_Equipped(pawn);
            var owner = GetOwner(parent);
            if (owner != null && !owner.IsPlayerControlled) {
                RandomizePartsForPawn(owner);
            }
        }
        static protected Pawn GetOwner(Thing thing) {
            if (thing == null) {
                return null;
            }
            int i = 0;
            for (IThingHolder parent = thing.ParentHolder; parent != null; parent = parent.ParentHolder) {
                i++;
                if (parent is Pawn) {
                    return parent as Pawn;
                }
            }
            return null;
        }

        protected string weaponOverrideLabel = "";
        public override string TransformLabel(string label) {
            if (string.IsNullOrEmpty(weaponOverrideLabel)) {
                return base.TransformLabel(label);
            }
            return weaponOverrideLabel;
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
            Log.Message("[MW2]parts count: " + attachedParts.Count);
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
                var texture = Props.baseGraphicData.Graphic.MatSingle?.mainTexture;
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

        public void SetGraphicDirty(bool renderNow = true) {
            textureDirty = true;
            if (renderNow) {
                if (UnityData.IsInMainThread) {
                    GetTexture();
                } else {
                    LongEventHandler.QueueLongEvent(delegate () { GetTexture(); }, "MW2_GetTexture", false, null, true, null);
                }
            }
        }
    }
}
