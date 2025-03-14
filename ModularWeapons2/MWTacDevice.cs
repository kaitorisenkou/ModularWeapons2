using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ModularWeapons2 {
    public abstract class MWTacDevice {
        public GraphicData graphicData = null;
        public string descriptionString = "";

        public virtual void DrawEffect(Vector3 posA, Vector3 posB) { }
        public virtual void OnStanceBegin(Pawn caster, LocalTargetInfo target) { }
    }

    public class MWTacDevice_Laser : MWTacDevice {
        public float layer = 2f;
        public float lineWidth = 0.1f;

        public override void DrawEffect(Vector3 posA, Vector3 posB) {
            GenDraw.DrawLineBetween(posA + (posB - posA).normalized * 0.6875f, posB, layer, graphicData.Graphic.MatSingle, lineWidth);
        }
    }
    public class MWTacDevice_Flashlight : MWTacDevice {
        public float layer = 2f;
        public float lineWidth = 5f;
        public float lineLength = 10f;

        public float hediffDistance = 10f;
        public HediffDef hediffDef = null;
        public bool lightExpose = true;

        public override void DrawEffect(Vector3 posA, Vector3 posB) {
            posA.y = posB.y;
            var normalizedLen = (posB - posA).normalized;
            Vector3 pos = ((posA + normalizedLen * 1.75f) + (posA + normalizedLen * lineLength)) / 2f;
            pos.y = layer;
            Quaternion q = Quaternion.LookRotation(posA - posB);
            Vector3 s = new Vector3(lineWidth, 1f, lineLength);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, q, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphicData.Graphic.MatSingle, 0);
        }
        public override void OnStanceBegin(Pawn caster, LocalTargetInfo target) {
            if (!target.TryGetPawn(out Pawn targetPawn)) {
                return;
            }
            if (lightExpose && targetPawn.HasComp<CompNoctolEyes>()) {
                targetPawn.health.hediffSet.TryGetHediff(HediffDefOf.LightExposure, out Hediff hediffNoctol);
                if (hediffNoctol != null) {
                    hediffNoctol.Severity = 1.0f;
                }
            }
            if (hediffDef != null && caster.Position.DistanceTo(target.Cell) < hediffDistance) {
                var records = targetPawn.def?.race?.body?.GetPartsWithDef(BodyPartDefOf.Eye);
                if (records != null && !targetPawn.health.hediffSet.TryGetHediff(hediffDef, out _)) {
                    foreach (var i in records) {
                        if (targetPawn.health.hediffSet.HasBodyPart(i)) {
                            targetPawn.health.AddHediff(hediffDef, i);
                        }
                    }
                }
            }
        }
    }
}
