using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using static HarmonyLib.Code;
using static UnityEngine.Random;

namespace ModularWeapons2 {
    public class CompModularWeapon : ThingComp, ICompUniqueGraphic, IVerbOwner, IReloadableComp, ICompWithCharges, IRenameable {
        //初期化
        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            //attachedParts = Props.partsMounts.Select(t => t.defaultPart).ToList();
            if (verbTracker == null)
                verbTracker = new VerbTracker(this);
            SetParts(DefaultParts);
            RefleshParts();
        }
        //セーブ
        void ScribeInt() {
            Scribe_Collections.Look(ref attachedParts, "attachedParts", true, LookMode.Def);
            //Scribe_Collections.Look(ref attachedParts_buffer, "attachedParts_buffer", true, LookMode.Def);
            Scribe_Collections.Look(ref attachHelpers, "attachHelpers", LookMode.Deep);
            Scribe_Collections.Look(ref attachHelpers_buffer, "attachHelpers_buffer", LookMode.Deep);
            Scribe_Collections.Look(ref decalHelpers, "decalHelpers", LookMode.Deep, new object[] { null, null, null });
            Scribe_Deep.Look(ref verbTracker, "verbTracker", new object[] { this });
            Scribe_Deep.Look(ref ability, "ability", Array.Empty<object>());
            Scribe_Values.Look(ref abilityDirty, "abilityDirty", defaultValue: true);
            Scribe_Values.Look(ref weaponOverrideLabel, "weaponOverrideLabel", defaultValue: "");
        }
        public override void PostExposeData() {
            base.PostExposeData();
            ScribeInt();
            if (Scribe.mode != LoadSaveMode.Saving) {
#if V15
                LongEventHandler.QueueLongEvent(delegate () { RefleshParts(); }, "MW2_RefleshParts", false, null, true, null);
#else
                LongEventHandler.QueueLongEvent(delegate () { RefleshParts(); }, "MW2_RefleshParts", false, null, true,false, null);
#endif
                //RefleshParts(true);
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                var holder = GetHolder();
                if (holder != null && this.AbilityForReading != null) {
                    this.AbilityForReading.pawn = holder;
                    this.AbilityForReading.verb.caster = holder;
                }
            }
        }
        public string fileName;
        public CMW_Exposer CreateExposer() {
            return new CMW_Exposer(
                this.attachHelpers,
                this.decalHelpers,
                this.weaponOverrideLabel
                );
        }
        public void UnpackExposer(CMW_Exposer exposer) {
            this.decalHelpers = exposer.decalHelpers;
            this.weaponOverrideLabel = exposer.weaponOverrideLabel;
            SetParts(exposer.attachHelpers);
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

        //NPC用ランダマイズ&アビリティ更新
        bool onceEquipped;
        public override void Notify_Equipped(Pawn pawn) {
            base.Notify_Equipped(pawn);
            var owner = GetOwner(parent);
            if (!onceEquipped) {
                if (owner != null && !owner.IsPlayerControlled) {
                    RandomizePartsForPawn(owner);
                }
                onceEquipped = true;
            }
            if (AbilityForReading != null) {
                AbilityForReading.pawn = owner;
                AbilityForReading.verb.caster = owner;
                owner.abilities.Notify_TemporaryAbilitiesChanged();
            }
        }
        public override void Notify_Unequipped(Pawn pawn) {
            pawn.abilities.Notify_TemporaryAbilitiesChanged();
        }

        //------------------------------------//
        //      ここからパーツ脱着関連        //
        //------------------------------------//
        protected List<PartsAttachHelper> attachHelpers = new List<PartsAttachHelper>();
        protected List<PartsAttachHelper> attachHelpers_buffer = new List<PartsAttachHelper>();
        public IReadOnlyList<MountAdapterClass> MountAdapters {
            get {
                if (mountAdapters.NullOrEmpty()) {
                    mountAdapters = PartsMounts.Select(t => (MountAdapterClass)t).ToList();
                    //MountAdapterClass.SetDistancedForUI(mountAdapters.ToArray());
                }
                return mountAdapters;
            }
        }
        protected List<MountAdapterClass> mountAdapters = new List<MountAdapterClass>();
        protected List<Vector2> adapterTextureOffset = new List<Vector2>();
        public IReadOnlyList<Vector2> AdapterTextureOffset {
            get => adapterTextureOffset;
        }
        protected List<ModularPartsDef> attachedParts = Enumerable.Empty<ModularPartsDef>().ToList();
        //protected List<ModularPartsDef> attachedParts_buffer = Enumerable.Empty<ModularPartsDef>().ToList();
        public IReadOnlyList<ModularPartsDef> AttachedParts {
            get => attachedParts;
        }
        public virtual void SetPart(PartsAttachHelper helper) {
            SetPartInt(helper);
            RefleshParts();
            return;
        }
        protected void SetPartInt(PartsAttachHelper helper) {
            //attachedParts[index] = part;
            if (attachHelpers == null) {
                attachHelpers = new List<PartsAttachHelper>();
            }
            int index = attachHelpers.FirstIndexOf(t => t.CanReplacedTo(helper));
            if (index >= 0) {
                attachHelpers[index] = helper;
            } else {
                attachHelpers.Add(helper);
            }
            return;
        }
        public virtual void SetParts(List<PartsAttachHelper> helpers, bool reset = true) {
            if (reset || attachHelpers == null) {
                attachHelpers = new List<PartsAttachHelper>();
            }
            if (!helpers.NullOrEmpty()) {
                //attachHelpers.AddRange(helpers);
                foreach (var i in helpers) {
                    SetPartInt(i);
                }
            }
            RefleshParts();
        }
        public virtual void SetParts(List<PartsAttachHelperClass> helpers, bool reset = true) {
            if (reset || attachHelpers == null) {
                attachHelpers = new List<PartsAttachHelper>();
            }
            if (!helpers.NullOrEmpty()) {
                foreach (var i in helpers) {
                    //attachHelpers.Add(i);
                    SetPartInt(i);
                }
            }
            RefleshParts();
        }
        public virtual void SetPartsWithBuffer() {
            //attachedParts = new List<ModularPartsDef>(attachedParts_buffer);
            attachHelpers = new List<PartsAttachHelper>(attachHelpers_buffer);
            RefleshParts();
        }
        public virtual void RefleshParts(bool scribe = false) {
            attachedParts = SolveAttachHelpers(attachHelpers);
            if (attachedParts.NullOrEmpty()) {
                cachedEOStats = Enumerable.Empty<(StatDef, float)>();
            } else {
                cachedEOStats =
                    attachedParts.Where(t => t != null && !t.EquippedStatOffsets.NullOrEmpty())
                    .SelectMany(t1 => t1.EquippedStatOffsets.Select(t2 => (t2.stat, t2.value)))
                    .GroupBy(t1 => t1.stat)
                    .Select(t1 => (t1.Key, t1.Sum(t2 => t2.value)));
            }
            requestCache = null;
            if (!scribe) {
                tools = null;
                verbPropertiesCached = null;
                abilityDirty = true;
                var compEq = parent.TryGetComp<CompEquippable>();
                if (compEq != null) {
                    compEq?.verbTracker?.InitVerbsFromZero();
                    foreach (Verb verb in compEq.AllVerbs) {
                        verb.caster = GetHolder();
                    }
                }
                verbTracker.InitVerbsFromZero();
                foreach (Verb verb in verbTracker.AllVerbs) {
                    verb.caster = GetHolder();
                }
                Pawn_MeleeVerbs.PawnMeleeVerbsStaticUpdate();
            }
            SetGraphicDirty();
            tacDeviceDirty = true;
        }
        protected List<ModularPartsDef> SolveAttachHelpers(List<PartsAttachHelper> attachHelpers) {
            if (attachHelpers == null) attachHelpers = new List<PartsAttachHelper>();
            mountAdapters = new List<MountAdapterClass>(PartsMounts);
            MountAdapterClass.ResetAdaptersParent(PartsMounts);
            int count = mountAdapters.Count;
            adapterTextureOffset = Enumerable.Repeat(Vector2.zero, count).ToList();
            List<ModularPartsDef> targetList = new ModularPartsDef[count].ToList();
            for (int i = 0; i < count; i++) {
                PartsAttachHelper helper = attachHelpers.LastOrDefault(t => t.CanAttachTo(mountAdapters[i]));
                if (helper.partsDef == null && !mountAdapters[i].mountDef.allowEmpty) {
                    helper = new PartsAttachHelper(mountAdapters[i].mountDef.fallbackPart, mountAdapters[i].mountDef);
                }
                if (helper.partsDef != null) {
                    var part = helper.partsDef;
                    targetList[i] = part;
                    var additionalAdapters = part.AdditionalAdapters;
                    mountAdapters.AddRange(additionalAdapters);
                    MountAdapterClass.ResetAdaptersParent(additionalAdapters, mountAdapters[i]);
                    adapterTextureOffset.AddRange(additionalAdapters.Select(t => adapterTextureOffset[i] + mountAdapters[i].offset));
                    targetList.AddRange(Enumerable.Repeat<ModularPartsDef>(null, additionalAdapters.Count));
                    count += additionalAdapters.Count;
                }
            }
            return targetList;
        }

        public virtual void BufferCurrent(bool overrideBuffer = false) {
            var tmp = attachHelpers_buffer;
            attachHelpers_buffer = new List<PartsAttachHelper>(attachHelpers);
            if (!overrideBuffer) {
                attachHelpers = tmp;
                if (attachHelpers.NullOrEmpty()) {
                    attachHelpers = new List<PartsAttachHelper>();
                }
            }
            RefleshParts();
        }
        public virtual void RevertToBuffer() {
            /*if (attachedParts_buffer.NullOrEmpty()) {
                Log.Warning("[MW2] RevertToBuffer() called, but buffer is null!");
                return;
            }
            attachedParts = new List<ModularPartsDef>(attachedParts_buffer);*/
            if (attachHelpers_buffer.NullOrEmpty()) {
                MWDebug.LogWarning("[MW2] RevertToBuffer() called, but buffer is null!");
                attachHelpers = new List<PartsAttachHelper>();
                RefleshParts();
                return;
            }
            attachHelpers = new List<PartsAttachHelper>(attachHelpers_buffer);
            RefleshParts();
        }
        public virtual IEnumerable<(ThingDef, int)> GetIngredient_Current() {
            return sorted_GetIngredient(attachedParts);
        }
        public virtual IEnumerable<(ThingDef, int)> GetIngredient_Buffer() {
            return sorted_GetIngredient(attachHelpers_buffer);
        }
        IEnumerable<(ThingDef, int)> requestCache = null;
        public virtual IEnumerable<(ThingDef, int)> GetRequiredIngredients() {
            if (requestCache == null) {
                requestCache = int_GetIngredient(attachHelpers_buffer)
                    .Concat(int_GetIngredient_minus(attachedParts))
                    .GroupBy(t => t.Item1)
                    .Select(t => (t.Key, t.Sum(t2 => t2.Item2)));
            }
            return requestCache;
        }
        IEnumerable<(ThingDef, int)> sorted_GetIngredient(List<ModularPartsDef> parts) {
            return int_GetIngredient(parts)
                .GroupBy(t => t.Item1)
                .Select(t => (t.Key, t.Sum(t2 => t2.Item2)));
        }
        IEnumerable<(ThingDef, int)> sorted_GetIngredient(List<PartsAttachHelper> helpers) {
            return int_GetIngredient(helpers)
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
        IEnumerable<(ThingDef, int)> int_GetIngredient(List<PartsAttachHelper> helpers) {
            foreach (var i in helpers) {
                if (i.partsDef == null)
                    continue;
                yield return (parent.Stuff ?? ThingDefOf.Steel, i.partsDef.stuffCost);
                foreach (var j in i.partsDef.costList) {
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
                //TODO 完全ランダム
                MWDebug.LogWarning("[MW2]no presetDefs found: " + parentDef);
                return;
            }
            var def = allDefs.RandomElement();
            weaponOverrideLabel = def.customName ?? "";
            //必須パーツ
            var helpers = new List<PartsAttachHelperClass>(def.requiredParts);
            //任意パーツ
            int optionLength = def.optionalParts.Count();
            HashSet<int> optionIndexes =
                new int[def.optionalPartsCount]
                .Select(t => UnityEngine.Random.Range(0, optionLength))
                .ToHashSet();
            foreach (var i in optionIndexes) {
                helpers.Add(def.optionalParts[i]);
            }
            SetParts(helpers);
        }


        //------------------------------------//
        //      パーツ脱着関連 ここまで       //
        //------------------------------------//
        //         ここから描画関連           //
        //------------------------------------//

        RenderTexture renderTextureInt = null;
        bool textureDirty;
        public virtual Texture GetTexture() {
            if (renderTextureInt == null) {
                var texture = BaseGraphic.MatSingle?.mainTexture;
                renderTextureInt = new RenderTexture(texture.width * 2, texture.height * 2, 32, RenderTextureFormat.ARGB32);
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
            var baseMat =
                /*MW2Mod.settings.useStyledTexture && Props.autoStyledGraphic && parent.StyleDef != null ?
                parent.StyleDef.Graphic.MatSingle :
                Props.baseGraphicData.Graphic.MatSingle;*/
                BaseGraphic.MatSingle;
            yield return new MWCameraRenderer.MWCameraRequest(baseMat, Vector2.zero, 0);
            var baseDecal = decalHelpers.FirstOrFallback(t => t.attachMountDef == null);
            if (baseDecal?.decalDef != null) {
                yield return new MWCameraRenderer.MWCameraRequest(
                    baseDecal.GetMaskedMaterial(baseMat.mainTexture),
                    Vector2.zero, 0);
            }
            for (int i = 0; i < MountAdapters.Count; i++) {
                if (attachedParts[i] == null) continue;
                //var offset = MountAdapters[i].offset + adapterTextureOffset[i];
                var offset = MountAdapters[i].GetOffsetFor(attachedParts[i]) + adapterTextureOffset[i];
                var scale = MountAdapters[i].GetScaleFor(attachedParts[i]);
                /*var adapterGra = MountAdapters[i].GetAdapterGraphicFor(attachedParts[i]);
                if (adapterGra != null) {
                    yield return new MWCameraRenderer.MWCameraRequest(
                        adapterGra.Graphic.MatSingle,
                        offset,
                        MountAdapters[i].layerOrder, 
                        scale
                        );
                }*/
                var adapterCRs =
                    MountAdapters[i].GetAdapterCRFor(attachedParts[i], adapterTextureOffset[i], MountAdapters[i].layerOrder);
                foreach (var cr in adapterCRs) {
                    yield return cr;
                }
                if (attachedParts[i].graphicData != null) {
                    var partMat = attachedParts[i].graphicData.Graphic.MatSingle;
                    yield return new MWCameraRenderer.MWCameraRequest(
                        partMat,
                        offset,
                        MountAdapters[i].layerOrder,
                        scale
                        );

                    var decal = decalHelpers.FirstOrFallback(t => t.attachMountDef == MountAdapters[i].mountDef);
                    if (decal?.decalDef != null) {
                        yield return new MWCameraRenderer.MWCameraRequest(
                            decal.GetMaskedMaterial(partMat.mainTexture),
                            offset,
                            MountAdapters[i].layerOrder,
                            scale
                            );
                    }
                }
            }
        }

        public void SetGraphicDirty(bool renderNow = true) {
            textureDirty = true;
            if (renderNow) {
                if (UnityData.IsInMainThread) {
                    GetTexture();
                } else {
#if V15
                    LongEventHandler.QueueLongEvent(delegate () { GetTexture(); }, "MW2_GetTexture", false, null, true, null);
#else
                    LongEventHandler.QueueLongEvent(delegate () { GetTexture(); }, "MW2_GetTexture", false, null, true, false, null);
#endif
                }
            }
        }

        //    ----    迷彩関連    ----    //

        protected List<DecalPaintHelper> decalHelpers = new List<DecalPaintHelper>();
        public DecalPaintHelper GetPaintHelperOfBase() {
            return GetPaintHelperOf(null);
        }
        public DecalPaintHelper GetPaintHelperOf(ModularPartsMountDef attachMountDef) {
            var result = decalHelpers.FirstOrFallback(t => t.attachMountDef == attachMountDef);
            if (result == null) {
                result = SetDecal(new DecalPaintHelper(null, attachMountDef: attachMountDef));
            }
            return result;
        }
        public virtual DecalPaintHelper SetDecal(DecalPaintHelper helper) {
            SetDecalInt(helper);
            RefleshParts();
            return helper;
        }
        protected void SetDecalInt(DecalPaintHelper helper) {
            if (decalHelpers == null) {
                decalHelpers = new List<DecalPaintHelper>();
            }
            int index = decalHelpers.FirstIndexOf(t => t.CanReplacedTo(helper));
            if (index >= 0) {
                decalHelpers[index] = helper;
            } else {
                decalHelpers.Add(helper);
            }
            return;
        }

        //------------------------------------//
        //         描画関連 ここまで          //
        //------------------------------------//

        protected ModularPartEffects GetPartEffectsAt(int index) {
            if (index < 0 || index >= (attachedParts?.Count??0)) {
                return new ModularPartEffects();
            }
            if (attachedParts[index] == null) {
                return mountAdapters[index].effectsWhenEmpty;
            }
            return attachedParts[index].effects;
        }

        //------------------------------------//
        //    ここからステータス数値関連      //
        //------------------------------------//

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
                    stringBuilder.AppendLine("MW2_StatsReport_ByCustomParts".Translate() + ": " + pair.stat.ValueToString(pair.value, ToStringNumberSense.Offset, pair.stat.finalizeEquippedStatOffset));
                    float value = StatWorker.StatOffsetFromGear(parent, pair.stat);
                    yield return new StatDrawEntry(StatCategoryDefOf.EquippedStatOffsets, pair.stat, value, StatRequest.ForEmpty(), ToStringNumberSense.Offset, null, true).SetReportText(stringBuilder.ToString());
                }
            }
            if (!attachedParts.NullOrEmpty()) {
                StringBuilder sb = new StringBuilder("MW2_SpecialStatDesc".Translate());
                sb.AppendLine();
                sb.AppendLine();
                if (attachedParts.Count(t => t != null) < 1) {
                    sb.AppendLine("MW2_SpecialStatEmpty".Translate());
                }
                foreach (var i in attachedParts) {
                    if (i == null) continue;
                    sb.AppendLine(i.label.CapitalizeFirst().Indented());
                }
                yield return new StatDrawEntry(StatCategoryDefOf.Basics, "MW2_SpecialStatLabel".Translate(), attachedParts.Count(t => t != null).ToString(), sb.ToString(), 0);
            }
        }

        public virtual float GetEquippedOffset(StatDef stat) {
            float value = 0;
            if (!attachedParts.NullOrEmpty()) {
                for (int i = 0; i < mountAdapters.Count; i++) {
                    ModularPartEffects effects = GetPartEffectsAt(i);
                    var mod = effects.equippedStatOffsets?.FirstOrFallback(t => t != null && t.stat == stat, null);
                    value += mod == null ? 0 : mod.value;
                }
            }
            return value;
        }
        public override float GetStatFactor(StatDef stat) {
            float value = base.GetStatFactor(stat);
            if (attachedParts.NullOrEmpty()) {
                return value;
            }
            for (int i = 0; i < mountAdapters.Count; i++) {
                ModularPartEffects effects = GetPartEffectsAt(i);
                var mod = effects.statFactors?.FirstOrFallback(t => t != null && t.stat == stat, null);
                value += mod == null ? 0 : mod.value;
            }
            return value;
        }
        public override float GetStatOffset(StatDef stat) {
            float value = base.GetStatOffset(stat);
            if (attachedParts.NullOrEmpty()) {
                return value;
            }
            for (int i = 0; i < mountAdapters.Count; i++) {
                ModularPartEffects effects = GetPartEffectsAt(i);
                var mod = effects.statOffsets?.FirstOrFallback(t => t != null && t.stat == stat, null);
                value += mod == null ? 0 : mod.value;
            }
            return value;
        }
        //------------------------------------//
        //    ステータス数値関連 ここまで     //
        //------------------------------------//
        //    ここからVerb数値(射撃)関連      //
        //------------------------------------//

        List<VerbProperties> verbPropertiesCached = null;
        public virtual List<VerbProperties> VerbPropertiesForOverride {
            get {
                if (verbPropertiesCached == null) {
                    verbPropertiesCached = CalcVerbPropertiesForOverride().ToList();
                }
                return verbPropertiesCached;
            }
        }
        public IEnumerable<VerbProperties> CalcVerbPropertiesForOverride() {
            foreach (var verbProp in parent.def.Verbs) {
                var clone = verbProp.MemberwiseClone();
                /*
                for (int i = 0; i < mountAdapters.Count; i++) {
                    var offset = GetPartEffectsAt(i).verbPropsOffset;
                    if (offset == null) continue;
                    offset.AffectVerbProps(clone);
                }
                */
                var offsets = mountAdapters.Select((_, i) => GetPartEffectsAt(i).verbPropsOffset).Where(t => t != null).OrderBy(t => t.priority);
                foreach (var i in offsets) {
                    i.AffectVerbProps(clone);
                }
                yield return clone;
            }
        }

        //------------------------------------//
        //       Verb数値関連 ここまで        //
        //------------------------------------//
        //      ここからTool(銃剣)関連        //
        //------------------------------------//

        VerbTracker verbTracker;
        public VerbTracker VerbTracker => this.verbTracker ?? (this.verbTracker = new VerbTracker(this));

        List<VerbProperties> verbProperties = new List<VerbProperties>();
        public List<VerbProperties> VerbProperties => verbProperties ?? (this.verbProperties = new List<VerbProperties>());

        public Pawn GetHolder() {
            ThingWithComps parent = this.parent;
            Pawn_EquipmentTracker pawn_EquipmentTracker = ((parent != null) ? parent.ParentHolder : null) as Pawn_EquipmentTracker;
            if (pawn_EquipmentTracker == null) {
                return null;
            }
            return pawn_EquipmentTracker.pawn;
        }
        public List<Verb> AllVerbs {
            get {
                var verbs = this.verbTracker.AllVerbs;
                var holder = GetHolder();
                if (holder != null) {
                    foreach (var i in verbs) {
                        i.caster = holder;
                    }
                }
                return verbs;
            }
        }

        List<Tool> tools = null;
        public List<Tool> Tools {
            get {
                if (tools == null) {
                    tools = GetToolsInt().ToList();
                }
                return tools;
            }
        }
        protected IEnumerable<Tool> GetToolsInt() {
            for (int i = 0; i < mountAdapters.Count; i++) {
                var effect = GetPartEffectsAt(i);
                if (effect.tools.NullOrEmpty())
                    continue;
                foreach (var tool in effect.tools) {
                    yield return tool;
                }
            }
        }

        public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.Weapon;

        public Thing ConstantCaster => null;

        public string UniqueVerbOwnerID() {
            return "CompModularWeapon_" + this.parent.ThingID;
        }
        public bool VerbsStillUsableBy(Pawn p) {
            Apparel item;
            if ((item = (this.parent as Apparel)) != null) {
                return p.apparel.WornApparel.Contains(item);
            }
            return p.equipment.AllEquipmentListForReading.Contains(this.parent);
        }

        //------------------------------------//
        //         Tool関連 ここまで          //
        //------------------------------------//
        //     ここからAbility関連 (UBGL)     //
        //------------------------------------//

        MWAbilityProperties abilityProperties = null;
        bool abilityDirty = true;
        public MWAbilityProperties AbilityProperties {
            get {
                if (abilityDirty)
                    abilityProperties = attachedParts?.Select(t => t?.Ability)?.FirstOrFallback(t => t != null);
                return abilityProperties;
            }
        }

        Ability ability = null;
        public Ability AbilityForReading {
            get {
                if (abilityDirty) {
                    MWAbilityProperties prop;
                    if ((prop = AbilityProperties) != null) {
                        var pawn = GetOwner(this.parent);
                        ability = AbilityUtility.MakeAbility(prop.abilityDef, pawn);
                        ability.maxCharges = prop.maxCharges;
                        ability.RemainingCharges = 0;
                    }
                    abilityDirty = false;
                }
                return this.ability;
            }
        }
        public int RemainingCharges {
            get { return AbilityForReading?.RemainingCharges ?? 0; }
            set { if (AbilityForReading != null) AbilityForReading.RemainingCharges = value; }
        }

        public Thing ReloadableThing => this.parent;

        public ThingDef AmmoDef => AbilityProperties?.ammoDef;

        private int replenishInTicks = -1;
        public int BaseReloadTicks => AbilityProperties?.baseReloadTicks ?? -1;

        public int MaxCharges => AbilityProperties?.maxCharges ?? 0;

        public string LabelRemaining => string.Format("{0} / {1}", this.RemainingCharges, this.MaxCharges);

        public bool CanBeUsed(out string reason) {
            reason = "";
            if (this.RemainingCharges <= 0) {
                reason = this.DisabledReason(this.MinAmmoNeeded(false), this.MaxAmmoNeeded(false));
                return false;
            }
            return true;
        }

        public bool NeedsReload(bool allowForceReload) {
            if (AbilityProperties?.ammoDef == null) {
                return false;
            }
            if (AbilityProperties.ammoCountToRefill == 0) {
                return this.RemainingCharges != this.MaxCharges;
            }
            if (!allowForceReload) {
                return this.RemainingCharges == 0;
            }
            return this.RemainingCharges != this.MaxCharges;
        }

        public int MinAmmoNeeded(bool allowForcedReload) {
            if (!this.NeedsReload(allowForcedReload)) {
                return 0;
            }
            if (AbilityProperties.ammoCountToRefill != 0) {
                return AbilityProperties.ammoCountToRefill;
            }
            return AbilityProperties.ammoCountPerCharge;
        }

        public int MaxAmmoNeeded(bool allowForcedReload) {
            if (!this.NeedsReload(allowForcedReload)) {
                return 0;
            }
            if (AbilityProperties.ammoCountToRefill != 0) {
                return AbilityProperties.ammoCountToRefill;
            }
            return AbilityProperties.ammoCountPerCharge * (this.MaxCharges - this.RemainingCharges);
        }

        public int MaxAmmoAmount() {
            if (AbilityProperties?.ammoDef == null) {
                return 0;
            }
            if (AbilityProperties.ammoCountToRefill == 0) {
                return AbilityProperties.ammoCountPerCharge * this.MaxCharges;
            }
            return AbilityProperties.ammoCountToRefill;
        }

        public void ReloadFrom(Thing ammo) {
            if (!this.NeedsReload(true)) {
                return;
            }
            if (AbilityProperties.ammoCountToRefill != 0) {
                if (ammo.stackCount < AbilityProperties.ammoCountToRefill) {
                    return;
                }
                ammo.SplitOff(AbilityProperties.ammoCountToRefill).Destroy(DestroyMode.Vanish);
                this.RemainingCharges = this.MaxCharges;
            } else {
                if (ammo.stackCount < AbilityProperties.ammoCountPerCharge) {
                    return;
                }
                int num = Mathf.Clamp(ammo.stackCount / AbilityProperties.ammoCountPerCharge, 0, this.MaxCharges - this.RemainingCharges);
                ammo.SplitOff(num * AbilityProperties.ammoCountPerCharge).Destroy(DestroyMode.Vanish);
                this.RemainingCharges += num;
            }
            if (AbilityProperties.soundReload != null) {
                AbilityProperties.soundReload.PlayOneShot(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
            }
        }

        public string DisabledReason(int minNeeded, int maxNeeded) {
            if (AbilityProperties == null) {
                return "MW2_NoUBGL".Translate();
            }
            if (AbilityProperties.replenishAfterCooldown) {
                return "CommandReload_Cooldown".Translate(AbilityProperties.CooldownVerbArgument, this.replenishInTicks.ToStringTicksToPeriod(true, false, true, true, false).Named("TIME"));
            }
            if (AbilityProperties.ammoDef == null) {
                return "CommandReload_NoCharges".Translate(AbilityProperties.ChargeNounArgument);
            }
            string arg;
            if (AbilityProperties.ammoCountToRefill != 0) {
                arg = AbilityProperties.ammoCountToRefill.ToString();
            } else {
                arg = ((minNeeded == maxNeeded) ? minNeeded.ToString() : string.Format("{0}-{1}", minNeeded, maxNeeded));
            }
            return "CommandReload_NoAmmo".Translate(AbilityProperties.ChargeNounArgument, AbilityProperties.ammoDef.Named("AMMO"), arg.Named("COUNT"));
        }
        public override void CompTick() {
            base.CompTick();
            if (AbilityProperties != null) {
                if (AbilityProperties.replenishAfterCooldown && this.RemainingCharges == 0) {
                    if (this.replenishInTicks <= 0) {
                        this.RemainingCharges = this.MaxCharges;
                    }
                    this.replenishInTicks--;
                }
            }
        }

        //------------------------------------//
        //        Ability関連 ここまで        //
        //------------------------------------//
        //    ここからフラッシュライト関連    //
        //------------------------------------//

        MWTacDevice tacDevice = null;
        bool tacDeviceDirty = true;
        public MWTacDevice TacDevice {
            get {
                if (tacDeviceDirty) {
                    tacDevice = attachedParts?.Select(t => t?.effects.tacDevice).FirstOrFallback(t => t != null);
                    tacDeviceDirty = false;
                }
                return tacDevice;
            }
        }

        public void DrawTacDevice() {
            if (TacDevice == null) return;
            var holder = GetHolder();
            Stance_Busy stance;
            if (holder?.stances != null &&
                 (stance = (holder.stances.curStance as Stance_Busy)) != null &&
                 stance.verb != null &&
                 stance.verb is Verb_Shoot
                ) {
                //DrawTacDevice(holder.TrueCenter(), stance_Warmup.verb.CurrentTarget.CenterVector3);
                TacDevice.DrawEffect(holder.TrueCenter(), stance.verb.CurrentTarget.CenterVector3);
            }
        }
        public void OnStanceBusy(Verb verb) {
            if (TacDevice == null) return;
            var pawn = verb.caster as Pawn;
            if (pawn != null)
                TacDevice.OnStanceBegin(pawn, verb.CurrentTarget);
        }

        //------------------------------------//
        //   フラッシュライト関連 ここまで    //
        //------------------------------------//

        //IRenamable関連
        public string RenamableLabel {
            get {
                if (weaponOverrideLabel.NullOrEmpty()) {
                    return null;
                }
                return weaponOverrideLabel;
            }
            set {
                weaponOverrideLabel = value;
            }
        }
        public string weaponOverrideLabel = null;
        public string BaseLabel => this.parent.def.label.CapitalizeFirst();
        public string InspectLabel => RenamableLabel;

        public override string TransformLabel(string label) {
            return RenamableLabel ?? base.TransformLabel(label);
        }

        //------------------------------------//
        //            プロパティ              //
        //------------------------------------//
        bool IsCreated_StyleExtension = false;
        ModExtension_ModularStyledWeapon StyleExtension = null;
        public CompProperties_ModularWeapon Props {
            get {
                return (CompProperties_ModularWeapon)this.props;
            }
        }
        public CompProperties_ModularWeapon StyledProps {
            get {
                if (!MW2Mod.settings.useStyledTexture) {
                    return Props;
                }
                if (!IsCreated_StyleExtension) {
                    StyleExtension = parent.StyleDef?.GetModExtension<ModExtension_ModularStyledWeapon>();
                }
                return StyleExtension?.properties;
            }
        }
        public virtual List<MountAdapterClass> PartsMounts {
            get {
                return StyledProps?.partsMounts ?? Props.partsMounts;
            }
        }
        public virtual List<PartsAttachHelperClass> DefaultParts {
            get {
                return StyledProps?.defaultParts ?? Props.defaultParts;
            }
        }
        public virtual Graphic BaseGraphic {
            get {
                if (MW2Mod.settings.useStyledTexture && Props.autoStyledGraphic) {
                    return 
                        StyledProps?.baseGraphicData?.Graphic ?? 
                        parent.StyleDef?.Graphic ?? 
                        Props.baseGraphicData.Graphic;
                }
                return Props.baseGraphicData.Graphic;
            }
        }
    }
}