using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;

namespace ModularWeapons2 {
    public class Dialog_Gunsmith : Window {
        public override Vector2 InitialSize {
            get {
                return new Vector2(Mathf.Min(960f, UI.screenWidth), Mathf.Min(760f, UI.screenHeight));
            }
        }
        protected override float Margin => 4f;

        public CompModularWeapon weaponComp;
        public Thing weaponThing;
        public Graphic weaponGraphic;
        public Material weaponMat;
        float weaponDrawSize = 1f;
        List<MountAdapterClass> adapters;
        List<ModularPartsDef> attachedParts;
        public Thing gunsmithStation;
        public Pawn worker;
        public Dialog_Gunsmith(CompModularWeapon weapon, Thing gunsmithStation = null, Pawn worker = null) {
            this.weaponComp = weapon;
            this.weaponThing = weapon.parent;
            weaponGraphic = weaponThing.Graphic;
            weaponMat = weaponGraphic.MatSingleFor(weaponThing);
            weaponDrawSize = weaponGraphic.drawSize.y;
            this.gunsmithStation = gunsmithStation;
            adapters = weaponComp.Props.partsMounts;
            attachedParts = weaponComp.attachedParts;
            this.worker = worker;
        }
        public override void DoWindowContents(Rect inRect) {
            CalcRectSizes(inRect);
            DoHeadContents(headRect);
            DoStatusContents(statusRect);
            DoWeaponScreenContents(weaponRect);
            DoPartsDescContents(partsRect);
            DoLowerContents(lowerRect);
        }
        protected virtual void DoHeadContents(Rect inRect) {
            var fontSize = Text.Font;
            Text.Font = GameFont.Medium;
            var fontAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;

            float currentX = inRect.x;
            GUI.DrawTexture(new Rect(currentX, inRect.y, inRect.height, inRect.height).ScaledBy(weaponDrawSize), weaponMat.mainTexture);
            currentX += inRect.height;
            Widgets.Label(new Rect(currentX, inRect.y, inRect.width - currentX, inRect.height), weaponThing.Label.CapitalizeFirst());

            Text.Font = fontSize;
            Text.Anchor = fontAnchor;
        }
        protected virtual void DoStatusContents(Rect inRect) {
            Widgets.DrawBox(inRect);
            Widgets.Label(inRect,"Weapon status\n will be here");
        }
        Rect partsButtonRect = new Rect(0, 0, 48, 48);
        int selectedPartsIndex = -1;
        protected virtual void DoWeaponScreenContents(Rect inRect) {
            Widgets.DrawBox(inRect);
            Rect weaponRect = new Rect(inRect) {
                size = Vector2.one * Mathf.Min(inRect.width, inRect.height) * weaponGraphic.drawSize,
                center = inRect.center
            };
            GUI.DrawTexture(weaponRect, weaponMat.mainTexture);
            var mounts = weaponComp.Props.partsMounts;
            var parts = weaponComp.attachedParts;
            Vector2 buttonPosScale = inRect.size * new Vector2(0.4375f, -0.4375f);//0.5 - 0.0625
            Vector2 linePosScale = inRect.size / new Vector2(weaponDrawSize, -weaponDrawSize);
            var fontAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.LowerLeft;
            var fontSize = Text.Font;
            Text.Font = GameFont.Tiny;
            for (int i = 0; i < mounts.Count; i++) {
                partsButtonRect.center = inRect.center + mounts[i].offset.normalized * buttonPosScale;
                if (selectedPartsIndex == i) {
                    Widgets.DrawLine(inRect.center + mounts[i].offset * linePosScale, partsButtonRect.center, Color.white, 1f);
                }
                Widgets.DrawWindowBackground(partsButtonRect);
                if (parts[i] != null) {
                    Widgets.DrawTextureFitted(partsButtonRect, parts[i].graphicData.Graphic.MatSingle.mainTexture, 2f);
                }
                Widgets.Label(partsButtonRect, mounts[i].mountDef.label.CapitalizeFirst());
                if (Mouse.IsOver(partsButtonRect)) {
                    Widgets.DrawHighlight(partsButtonRect);
                }
                if (selectedPartsIndex == i) {
                    Widgets.DrawHighlightSelected(partsButtonRect);
                }
                if (Widgets.ButtonInvisible(partsButtonRect, true)) {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    selectedPartsIndex = selectedPartsIndex == i ? -1 : i;
                }
            }
            Text.Anchor = fontAnchor;
            Text.Font = fontSize;
        }
        Vector2 scrollPos_PartsSelect = new Vector2();
        Rect viewRect_PartsSelect = new Rect(0, 0, 0, 48);
        private ScrollPositioner scrollPositioner = new ScrollPositioner();
        protected virtual void DoPartsDescContents(Rect inRect) {
            if (selectedPartsIndex < 0) {
                Widgets.DrawBoxSolid(inRect, Color.yellow);
            } else {
                var fontSize = Text.Font;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(inRect.x+4,inRect.y+2,inRect.width,inRect.height), adapters[selectedPartsIndex].mountDef.label.CapitalizeFirst());

                var fontAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.LowerLeft;
                Text.Font = GameFont.Tiny;
                Rect partsSelectOuterRect = inRect.BottomPartPixels(52);
                Widgets.DrawWindowBackground(partsSelectOuterRect);
                Widgets.ScrollHorizontal(partsSelectOuterRect, ref scrollPos_PartsSelect, viewRect_PartsSelect, 10f);
                Widgets.BeginScrollView(partsSelectOuterRect, ref scrollPos_PartsSelect, viewRect_PartsSelect, false);

                viewRect_PartsSelect.width = 0;
                foreach(var part in adapters[selectedPartsIndex].GetAttatchableParts()) {
                    Rect boxRect = new Rect(new Rect(viewRect_PartsSelect.xMax, viewRect_PartsSelect.y, 52, 52));
                    Rect partButtonRect = new Rect(new Rect(viewRect_PartsSelect.xMax+2, viewRect_PartsSelect.y+1, 48, 50));
                    //Widgets.DrawBox(boxRect, 1);
                    Widgets.DrawWindowBackground(boxRect, new Color(1.5f, 1.5f, 1.5f));
                    if (part == null) {
                        Widgets.Label(partButtonRect, adapters[selectedPartsIndex].mountDef.emptyLabel.CapitalizeFirst());
                        if (attachedParts[selectedPartsIndex] == null) {
                            Widgets.DrawHighlightSelected(boxRect);
                        } else {
                            if (Mouse.IsOver(boxRect)) {
                                Widgets.DrawHighlight(boxRect);
                            }
                            if (Widgets.ButtonInvisible(boxRect, true)) {
                                SoundDefOf.Click.PlayOneShotOnCamera();
                                attachedParts[selectedPartsIndex] = null;
                                SetWeaponGraphicDirty();
                            }
                        }

                    } else {
                        var texture = part.graphicData.Graphic.MatSingle.mainTexture;
                        Rect textureRect = new Rect(0, 0, texture.width, texture.height) { center = partButtonRect.center };
                        Widgets.DrawTextureFitted(textureRect, texture, 1f);
                        Widgets.Label(partButtonRect, part.labelShort.CapitalizeFirst());
                        if (attachedParts[selectedPartsIndex] == part) {
                            Widgets.DrawHighlightSelected(boxRect);
                        } else {
                            if (Mouse.IsOver(boxRect)) {
                                Widgets.DrawHighlight(boxRect);
                            }
                            if (Widgets.ButtonInvisible(boxRect, true)) {
                                SoundDefOf.Click.PlayOneShotOnCamera();
                                attachedParts[selectedPartsIndex] = part;
                                SetWeaponGraphicDirty();
                            }
                        }
                    }
                    viewRect_PartsSelect.width += 52;
                }
                viewRect_PartsSelect.width += 1000;

                Widgets.EndScrollView();
                scrollPositioner.ScrollHorizontally(ref scrollPos_PartsSelect, partsSelectOuterRect.size);
                Text.Anchor = fontAnchor;
                Text.Font = fontSize;
            }
        }
        protected virtual void DoLowerContents(Rect inRect) {
            Widgets.DrawBox(inRect);
            Widgets.Label(inRect, "Material requirements will be displayed here");
            var acceptButtonRect = inRect.RightPart(0.2f).GetInnerRect();
            if (Widgets.ButtonText(acceptButtonRect,"Accept".Translate(),true, true, true, null)) {
                this.Close(true);
            }
        }

        protected virtual void CalcRectSizes(Rect inRect) {
            if (rectCached) return;
            headRect = new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeightOf(GameFont.Medium) * 2f);
            lowerRect = new Rect(inRect.x, inRect.yMax - headRect.height, inRect.width, Text.LineHeightOf(GameFont.Medium) * 2f);
            statusRect = new Rect(inRect.x, inRect.y + headRect.height, inRect.width / 3, inRect.height - Text.LineHeightOf(GameFont.Medium) * 4f);
            weaponRect = new Rect(inRect.x + statusRect.width, statusRect.y, statusRect.width * 2, statusRect.height / 2);
            partsRect = new Rect(weaponRect.x, weaponRect.y + weaponRect.height, weaponRect.width, weaponRect.height);

            rectCached = true;
        }
        bool rectCached;
        protected Rect headRect;
        protected Rect statusRect;
        protected Rect weaponRect;
        protected Rect partsRect;
        protected Rect lowerRect;

        protected void SetWeaponGraphicDirty() {
            weaponComp.SetGraphicDirty(); 
            weaponMat = weaponGraphic.MatSingleFor(weaponThing);
        }
    }
}
