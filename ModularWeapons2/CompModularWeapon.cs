﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace ModularWeapons2 {
    public class CompModularWeapon : ThingComp, ICompUniqueGraphic {
        public CompProperties_ModularWeapon Props {
            get {
                return (CompProperties_ModularWeapon)this.props;
            }
        }
        public List<ModularPartsDef> attachedParts;

        public float GetEquippedOffset(StatDef stat) {
            return 0;
        }
        public override float GetStatFactor(StatDef stat) {
            return base.GetStatFactor(stat);
        }
        public override float GetStatOffset(StatDef stat) {
            return base.GetStatOffset(stat);
        }

        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            attachedParts = Props.partsMounts.Select(t => t.defaultPart).ToList();
        }
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Collections.Look(ref attachedParts, "attachedParts", true, LookMode.Def);
        }

        RenderTexture renderTextureInt = null;
        bool textureDirty;
        public Texture GetTexture() {
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
        public Material GetMaterial() {
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
        public IEnumerable<MWCameraRenderer.MWCameraRequest> GetRequestsForRenderCam() {
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

        public void SetGraphicDirty(bool renderNow=true) {
            textureDirty = true;
            if (renderNow)
                GetTexture();
        }
    }
}
