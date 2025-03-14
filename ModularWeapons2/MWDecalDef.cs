using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace ModularWeapons2 {
    public class MWDecalDef : Def {
        public int ListOrder = 0;
        public GraphicData graphicData = null;
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        public Material GetMaskedMaterial(Texture mask, Color color) {
            var mat = new Material(graphicData.Graphic.MatSingle);
            mat.SetTexture("_MaskTex", mask);
            mat.color = color * 2;

            return mat;
        }

        public static IEnumerable<MWDecalDef> GetAllDecalDefs() {
            yield return null;
            foreach(var i in DefDatabase<MWDecalDef>.AllDefsListForReading) {
                yield return i;
            }
        }
    }
    public class DecalPaintHelper : IExposable {
        public void ExposeData() {
            Scribe_Defs.Look(ref decalDef, "decalDef");
            Scribe_Defs.Look(ref attachMountDef, "attachMountDef");
            Scribe_Values.Look(ref color, "color", Color.white);
        }

        public MWDecalDef decalDef;
        public Color color;
        public ModularPartsMountDef attachMountDef = null;

        public DecalPaintHelper(MWDecalDef decalDef, Color? color=null, ModularPartsMountDef attachMountDef = null) {
            this.decalDef = decalDef;
            this.color = color ?? Color.white;
            this.attachMountDef = attachMountDef;
        }

        public Material GetMaskedMaterial(Texture mask) {
            return decalDef.GetMaskedMaterial(mask, color);
        }

        public bool CanReplacedTo(DecalPaintHelper other) {
            if (this.attachMountDef != null && other.decalDef != null && this.decalDef == other.decalDef) {
                return true;
            }
            return false;
        }
    }
}
