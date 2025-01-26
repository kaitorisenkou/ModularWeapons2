﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;
using static RimWorld.ColonistBar;
using static RimWorld.PsychicRitualRoleDef;
using static UnityEngine.Random;

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
        protected float weaponDrawSize = 1f;
        protected List<MountAdapterClass> adapters;
        protected IReadOnlyList<ModularPartsDef> attachedParts;
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
            attachedParts = weaponComp.AttachedParts;
            this.worker = worker;
            statEnrties_Initial = GetStatEnrties();
            statEnrties_Current = GetStatEnrties();
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
        protected Vector2 scrollPos_Status = new Vector2();
        protected Rect viewRect_Status = new Rect();
        protected float statusHeight = 100;
        readonly protected Color disabledGrayColor = new Color(0.25f, 0.25f, 0.25f);
        protected virtual void DoStatusContents(Rect inRect) {
            Widgets.DrawBox(inRect);
            //Widgets.Label(inRect,"Weapon status here");
            statusHeight = 0;

            var contentColor = GUI.contentColor;
            Widgets.BeginScrollView(inRect, ref scrollPos_Status, viewRect_Status, true);
            for (int i = 0; i < statEnrties_Current.Count; i++) {
                var entry_Current = statEnrties_Current[i];
                var heightAdd = entry_Current.Item1.Draw(0, statusHeight, viewRect_Status.width, false, false, false, delegate () { }, delegate () { }, scrollPos_Status, inRect);

                string labelString = "---";
                Color labelColor = disabledGrayColor;
                var addRect = new Rect(0, statusHeight, inRect.width, heightAdd).RightPart(0.125f);
                var entry_Init= statEnrties_Initial.FirstOrFallback(t => t.Item1.LabelCap == entry_Current.Item1.LabelCap, (null, 0));
                if (entry_Current.Item2.HasValue) {
                    float valueDiff = entry_Current.Item2.Value - entry_Init.Item2.Value;
                    valueDiff = ProcessStatStyle(valueDiff, entry_Current.Item1.stat);
                    if (valueDiff >= 0.01f) {
                        labelColor = MW2Mod.lessIsBetter.Contains(entry_Current.Item1.LabelCap) ? Color.red : Color.green;
                    } else if (valueDiff <= -0.01f) {
                        labelColor = MW2Mod.lessIsBetter.Contains(entry_Current.Item1.LabelCap) ? Color.green : Color.red;
                    }
                    labelString = (valueDiff).ToStringWithSign();
                } else {
                    if (entry_Init.Item1 == null || entry_Init.Item1.ValueString != entry_Current.Item1.ValueString) {
                        labelColor = Color.yellow;
                        labelString = entry_Init.Item1.ValueString;
                    }
                }
                GUI.contentColor = labelColor;
                Widgets.Label(addRect, labelString);
                GUI.contentColor = contentColor;
                statusHeight += heightAdd;
            }
            Widgets.EndScrollView();
            viewRect_Status = new Rect(0, 0, inRect.width - 16f, statusHeight + 100f);
            //Widgets.Label(inRect, "statusHeight: " + statusHeight);
        }
        Rect partsButtonRect = new Rect(0, 0, 48, 48);
        protected int selectedPartsIndex = -1;
        protected virtual void DoWeaponScreenContents(Rect inRect) {
            Widgets.DrawBox(inRect);
            Rect weaponRect = new Rect(inRect) {
                size = Vector2.one * Mathf.Min(inRect.width, inRect.height) * weaponGraphic.drawSize,
                center = inRect.center
            };
            GUI.DrawTexture(weaponRect, weaponMat.mainTexture);
            //var adapters = weaponComp.Props.partsMounts;
            //var attachedParts = weaponComp.attachedParts;
            Vector2 buttonPosScale = inRect.size * new Vector2(0.4375f, -0.4375f);//0.5 - 0.0625
            Vector2 linePosScale = inRect.size / new Vector2(weaponDrawSize, -weaponDrawSize);
            var fontAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.LowerLeft;
            var fontSize = Text.Font;
            Text.Font = GameFont.Tiny;
            for (int i = 0; i < adapters.Count; i++) {
                partsButtonRect.center = inRect.center + adapters[i].NormalizedOffset * buttonPosScale;
                if (selectedPartsIndex == i) {
                    Widgets.DrawLine(inRect.center + adapters[i].offset * linePosScale, partsButtonRect.center, Color.white, 1f);
                }
                Widgets.DrawWindowBackground(partsButtonRect);
                if (attachedParts[i] != null) {
                    Widgets.DrawTextureFitted(partsButtonRect, attachedParts[i].graphicData.Graphic.MatSingle.mainTexture, attachedParts[i].GUIScale);
                }
                Widgets.Label(partsButtonRect, adapters[i].mountDef.label.CapitalizeFirst());
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
        protected Vector2 scrollPos_PartsSelect = new Vector2();
        protected Rect viewRect_PartsSelect = new Rect(0, 0, 0, 48);
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
                Rect partsSelectOuterRect = inRect.BottomPartPixels(64);
                Widgets.DrawWindowBackground(partsSelectOuterRect);
                partsSelectOuterRect = partsSelectOuterRect.ContractedBy(1, 0);
                Widgets.ScrollHorizontal(partsSelectOuterRect, ref scrollPos_PartsSelect, viewRect_PartsSelect, 10f);
                Widgets.BeginScrollView(partsSelectOuterRect, ref scrollPos_PartsSelect, viewRect_PartsSelect, true);

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
                                //attachedParts[selectedPartsIndex] = null;
                                weaponComp.SetPart(selectedPartsIndex, null);
                                OnPartsChanged();
                            }
                        }

                    } else {
                        var texture = part.graphicData.Graphic.MatSingle.mainTexture;
                        //Rect textureRect = new Rect(0, 0, texture.width, texture.height) { center = partButtonRect.center };
                        Widgets.DrawTextureFitted(partButtonRect, texture, part.GUIScale);
                        Widgets.Label(partButtonRect, part.labelShort.CapitalizeFirst());
                        if (attachedParts[selectedPartsIndex] == part) {
                            Widgets.DrawHighlightSelected(boxRect);
                        } else {
                            if (Mouse.IsOver(boxRect)) {
                                Widgets.DrawHighlight(boxRect);
                            }
                            if (Widgets.ButtonInvisible(boxRect, true)) {
                                SoundDefOf.Click.PlayOneShotOnCamera();
                                //attachedParts[selectedPartsIndex] = part;
                                weaponComp.SetPart(selectedPartsIndex, part);
                                OnPartsChanged();
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
            statusRect = new Rect(inRect.x, inRect.y + headRect.height, inRect.width *0.375f, inRect.height - Text.LineHeightOf(GameFont.Medium) * 4f);
            weaponRect = new Rect(inRect.x + statusRect.width, statusRect.y, inRect.width - statusRect.width, statusRect.height / 2);
            partsRect = new Rect(weaponRect.x, weaponRect.y + weaponRect.height, weaponRect.width, weaponRect.height);

            rectCached = true;
        }
        bool rectCached;
        protected Rect headRect;
        protected Rect statusRect;
        protected Rect weaponRect;
        protected Rect partsRect;
        protected Rect lowerRect;

        protected List<(StatDrawEntry,float?)> statEnrties_Initial = null;
        protected List<(StatDrawEntry, float?)> statEnrties_Current = null;
        //static public bool ForceShowEquippedStats = false;
        protected virtual List<(StatDrawEntry, float?)> GetStatEnrties() {
            var resultEntry = new List<(StatDrawEntry, float?)>();
            var request = StatRequest.For(weaponThing);
            //ForceShowEquippedStats = true;
            foreach (var i in DefDatabase<StatDef>.AllDefs.Where(t => t.Worker.ShouldShowFor(request))) {
                var value = weaponThing.GetStatValue(i);
                if (!ShowStat(i, value)) {
                    continue;
                }
                resultEntry.Add((new StatDrawEntry(i.category, i, value, request, ToStringNumberSense.Undefined, null, false), value));
            }
            foreach (var i in weaponThing.def.SpecialDisplayStats(request)) {
                var value = TryGetValueFromEntry(i.ValueString);
                if (!ShowStat(i, value)) {
                    continue;
                }
                resultEntry.Add((i, value));
            }
            foreach (var i in weaponThing.SpecialDisplayStats()) {
                var value = TryGetValueFromEntry(i.ValueString);
                resultEntry.Add((i, value));
            }
            //ForceShowEquippedStats = false;
            return resultEntry.OrderBy(t => t.Item1.category.displayOrder).ToList();
        }
        protected virtual bool ShowStat(StatDef statDef,float? value) {
            if (MW2Mod.statDefsShow.Contains(statDef)) {
                return true;
            }
            if (MW2Mod.statCategoryShow.Contains(statDef.category)) {
                return true;
            }

            //return Mathf.Abs(value - statDef.defaultBaseValue) > 0.001f;
            return false;
        }
        protected virtual bool ShowStat(StatDrawEntry entry,float? value) {
            if (entry.stat != null && MW2Mod.statDefsShow.Contains(entry.stat)) {
                return true;
            }
            if (MW2Mod.statCategoryShow.Contains(entry.category)) {
                return true;
            }
            return false;
        }
        readonly static Regex entryRegex = new Regex(@"(?<![0-9.])[0-9][0-9.]*");
        protected static float? TryGetValueFromEntry(string entry) {
            var matches = entryRegex.Matches(entry);
            if (matches.Count != 1) {
                return null;
            }
            if (float.TryParse(entryRegex.Match(entry).ToString(), out float result)) {
                return result;
            }
            return null;
        }
        protected static float ProcessStatStyle(float value,StatDef stat) {
            if (stat == null) return value;
            if (stat == StatDefOf.AccuracyLong ||
                stat == StatDefOf.AccuracyMedium ||
                stat == StatDefOf.AccuracyShort ||
                stat == StatDefOf.AccuracyTouch) {
                value *= 100;
            }
            return value;
        }


        protected void OnPartsChanged() {
            weaponComp.SetGraphicDirty(); 
            weaponMat = weaponGraphic.MatSingleFor(weaponThing);
            statEnrties_Current = GetStatEnrties();
        }
    }
}
