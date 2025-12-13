using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.AI;
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

        public static Lazy<Texture2D> saveTex=new Lazy<Texture2D>(()=> ContentFinder<Texture2D>.Get("UI/Gunsmith/Save", true));
        public static Lazy<Texture2D> loadTex = new Lazy<Texture2D>(() => ContentFinder<Texture2D>.Get("UI/Gunsmith/Load", true));
        public static Lazy<Texture2D> renameTex => new Lazy<Texture2D>(() => TexUI.RenameTex);
        public static Lazy<Texture2D> backTex = new Lazy<Texture2D>(() => ContentFinder<Texture2D>.Get("UI/Gunsmith/GunsmithBack", true));

        public CompModularWeapon weaponComp;
        public Thing weaponThing;
        public Graphic weaponGraphic;
        public Material weaponMat;
        protected float weaponDrawSize = 1f;
        protected IReadOnlyList<MountAdapterClass> adapters;
        protected IReadOnlyList<ModularPartsDef> attachedParts;
        public Thing gunsmithStation;
        public Pawn worker;
        public Map map;
        public Dialog_Gunsmith(CompModularWeapon weapon, Thing gunsmithStation = null, Pawn worker = null) {
            this.weaponComp = weapon;
            this.weaponThing = weapon.parent;
            weaponGraphic = weaponThing.Graphic;
            weaponMat = weaponGraphic.MatSingleFor(weaponThing);
            weaponDrawSize = weaponGraphic.drawSize.y;
            this.gunsmithStation = gunsmithStation;
            adapters = weaponComp.MountAdapters;
            attachedParts = weaponComp.AttachedParts;
            this.worker = worker;
            this.map = weaponThing?.Map ?? gunsmithStation?.Map ?? worker?.Map;
        }

        static Action<Thing> CEBreakPoint_PostOpenGunsmith => MW2Mod.CEBreakPoint_PostOpenGunsmith;
        public override void PostOpen() {
            base.PostOpen();
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;

            statEnrties_Initial = GetStatEnrties();
            statEnrties_Current = GetStatEnrties();

            CEBreakPoint_PostOpenGunsmith?.Invoke(this.weaponThing);

            weaponComp.BufferCurrent(overrideBuffer: true);
        }
        bool canceled = false;
        public override void PostClose() {
            base.PostClose();
            if (canceled) {
                weaponComp.RevertToBuffer();
            } else {
                if (gunsmithStation != null) {
                    weaponComp.BufferCurrent();
                    var costs = weaponComp.GetRequiredIngredients();
                    var job = JobMaker.MakeJob(MW2DefOf.ConsumeIngredientsForGunsmith);
                    var queueA = job.GetTargetQueue(TargetIndex.A);
                    job.SetTarget(TargetIndex.B, weaponThing);
                    job.SetTarget(TargetIndex.C, gunsmithStation);
                    var countQueue = new List<int>();
                    bool error = false;
                    if (costs != null) {
                        foreach (var i in costs) {
                            var queueAdd = FindIngredients(i.Item1, i.Item2);
                            queueA.AddRange(queueAdd.Select(t => t.Item1));
                            countQueue.AddRange(queueAdd.Select(t => t.Item2));
                            if (queueAdd.Sum(t => t.Item2) < i.Item2) {
                                error = true;
                                break;
                            }
                        }
                        job.countQueue = countQueue;
                    }
                    if (!error) {
                        worker.jobs.TryTakeOrderedJob(job);
                    } else {
                        Log.Warning("[ModularWeapons] no ingredients for gunsmith!");
                    }
                } else if(DebugSettings.godMode){

                }
            }
        }
        IEnumerable<(LocalTargetInfo, int)> FindIngredients(ThingDef thingDef, int count) {
            var things = worker.Map.listerThings.ThingsOfDef(thingDef).OrderBy(t => t.Position.DistanceToSquared(worker.Position)).ToArray();
            int i = 0;
            int c = Mathf.Max(0, count);
            for (; c > 0 && things.Length > i; i++) {
                if (worker.CanReach(things[i], PathEndMode.Touch, Danger.Some, false, false, TraverseMode.ByPawn) && !things[i].IsForbidden(worker)) {
                    yield return (things[i], Mathf.Clamp(things[i].stackCount, 0, c));
                    c -= things[i].stackCount;
                }
            }
            yield break;
        }

        public override void DoWindowContents(Rect inRect) {
            CalcRectSizes(inRect);
            DoHeadContents(headRect);
            DoStatusContents(statusRect);
            DoWeaponScreenContents(weaponRect);
            DoPartsDescContents(partsRect);
            DoLowerContents(lowerRect);
        }

        //--------------------------------------------------------//
        //    最上段パネル: 武器名ラベル, 保存/読込/改名ボタン    //    
        //--------------------------------------------------------//
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

            saveButtonRect.center = new Vector2(inRect.xMax - saveButtonSpace / 2, inRect.center.y);
            if (Widgets.ButtonImage(saveButtonRect, loadTex.Value, true, "Load".Translate())) {
                Find.WindowStack.Add(new Dialog_Gunsmith_Load(weaponComp, () => { OnPartsChanged(); }));
            }
            saveButtonRect.x -= saveButtonSpace;
            if (Widgets.ButtonImage(saveButtonRect, saveTex.Value, true, "Save".Translate())) {
                Find.WindowStack.Add(new Dialog_Gunsmith_Save(weaponThing, weaponComp.weaponOverrideLabel));
            }
            saveButtonRect.x -= saveButtonSpace;
            if(Widgets.ButtonImage(saveButtonRect, renameTex.Value, true, "Rename".Translate())) {
                Find.WindowStack.Add(new Dialog_RenameGunsmith(weaponComp));
            }
        }
        readonly float saveButtonSpace = 48;
        Rect saveButtonRect = new Rect(0, 0, 32, 32);

        //--------------------------------------//
        //      左側パネル: ステータス表示      //    
        //--------------------------------------//
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
                var entry_Init = statEnrties_Initial.FirstOrFallback(t => t.Item1.LabelCap == entry_Current.Item1.LabelCap, (null, 0));
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

        //--------------------------------------------------------//
        //    右側上パネル: 武器プレビュー, パーツ装着箇所選択    //    
        //--------------------------------------------------------//
        Rect partsButtonRect = new Rect(0, 0, 56, 56);
        protected int selectedPartsIndex = -1;
        //protected MountAdapterClass selectedMount = null;
        protected virtual void DoWeaponScreenContents(Rect inRect) {
            GUI.DrawTexture(inRect, backTex.Value);
            Widgets.DrawBox(inRect);
            Rect weaponRect = new Rect(inRect) {
                size = Vector2.one * Mathf.Min(inRect.width, inRect.height) * 2/*weaponGraphic.drawSize*/,
                center = inRect.center
            };
            GUI.DrawTexture(weaponRect, weaponMat.mainTexture);
            //var adapters = weaponComp.Props.partsMounts;
            //var attachedParts = weaponComp.attachedParts;
            Vector2 buttonPosScale = inRect.size * new Vector2(0.40625f, -0.40625f);//0.5 - 0.125 + 0.03125
            Vector2 linePosScale = inRect.size / weaponDrawSize * new Vector2(1, -2);
            var fontAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.LowerLeft;
            var fontSize = Text.Font;
            Text.Font = GameFont.Tiny;
            MountAdapterClass.SetDistancedForUI(adapters.ToArray());
            for (int i = 0; i < adapters.Count; i++) {
                if (i >= adapters.Count || i >= attachedParts.Count) {
                    break;
                }
                var adapter = adapters[i];
                partsButtonRect.center = inRect.center + adapter.NormalizedOffsetForUI * buttonPosScale;
                if (selectedPartsIndex == i) {
                    Widgets.DrawLine(inRect.center + (adapter.offset + weaponComp.AdapterTextureOffset[i]) * linePosScale, partsButtonRect.center, Color.white, 1f);
                    //Widgets.DrawHighlightSelected(partsButtonRect);
                    Widgets.DrawWindowBackground(partsButtonRect.ExpandedBy(4f));
                }
                Widgets.DrawWindowBackground(partsButtonRect);
                if (attachedParts[i] != null) {
                    Widgets.DrawTextureFitted(partsButtonRect, attachedParts[i].graphicData.Graphic.MatSingle.mainTexture, attachedParts[i].GUIScale);
                }
                Widgets.Label(partsButtonRect, adapter.mountDef.LabelShort.CapitalizeFirst());
                if (Mouse.IsOver(partsButtonRect)) {
                    Widgets.DrawHighlight(partsButtonRect);
                }
                if (Widgets.ButtonInvisible(partsButtonRect, true)) {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    selectedPartsIndex = selectedPartsIndex == i ? -1 : i;
                    //selectedMount = attachedParts[i];
                }
            }
            Text.Anchor = fontAnchor;
            Text.Font = fontSize;

            //左右キーで選択切替
            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.keyCode == KeyCode.D) {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    selectedPartsIndex = Mathf.RoundToInt(Mathf.Repeat(selectedPartsIndex + 1, adapters.Count));
                }
                if (Event.current.keyCode == KeyCode.A) {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    selectedPartsIndex = Mathf.RoundToInt(Mathf.Repeat(selectedPartsIndex - 1, adapters.Count));
                }
                if (Event.current.keyCode == KeyCode.W) {
                    MWDebug.LogMessage("[MW2] i:" + selectedPartsIndex + " in " + adapters.Count + ", " + adapters[selectedPartsIndex].NormalizedOffsetForUI);
                }
            }
        }

        //--------------------------------//
        //    右側下パネル: パーツ編集    //    
        //--------------------------------//
        protected Vector2 scrollPos_PartsSelect = new Vector2();
        protected Rect viewRect_PartsSelect = new Rect(0, 0, 0, 64);
        private ScrollPositioner scrollPositioner = new ScrollPositioner();
        protected string partsTabLabel = "MW2_Parts".Translate();
        protected string paintTabLabel = "MW2_Paint".Translate();
        protected TabRecord partsTab = null;
        protected TabRecord paintTab = null;
        protected readonly Color colorFactor = new Color(1f, 1f, 1f, 0.5f);
        protected virtual void DoPartsDescContents(Rect inRect) {
            if (partsTab == null) {
                partsTab = new TabRecord(partsTabLabel, () => { SoundDefOf.RowTabSelect.PlayOneShotOnCamera(null); partsTab.selected = true; paintTab.selected = false; }, true); 
                paintTab = new TabRecord(paintTabLabel, () => { SoundDefOf.RowTabSelect.PlayOneShotOnCamera(null); partsTab.selected = false; paintTab.selected = true; }, false);
            }
            var fontSize = Text.Font;
            var fontAnchor = Text.Anchor;
            var paintRect = new Rect(inRect);
            paintRect.yMin += Text.LineHeightOf(GameFont.Medium);
            if (selectedPartsIndex < 0) {
                //ラベル
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(inRect.x + 4, inRect.y + 2, inRect.width, inRect.height), "MW2_Base".Translate());
                //塗装
                DoDecalPaint(paintRect, weaponComp.GetPaintHelperOfBase());
            } else {
                //ラベル
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(inRect.x+4,inRect.y+2,inRect.width,inRect.height), adapters[selectedPartsIndex].mountDef.label.CapitalizeFirst());

                //    ----    パーツ交換    ----    //
                if (partsTab.Selected) {
                    ModularPartsDef mouseOverPartDef = null;
                    //下部のパーツ選択ボタン
                    Text.Anchor = TextAnchor.LowerLeft;
                    Text.Font = GameFont.Tiny;
                    Rect partsSelectOuterRect = inRect.BottomPartPixels(80);
                    Widgets.DrawWindowBackground(partsSelectOuterRect);
                    partsSelectOuterRect = partsSelectOuterRect.ContractedBy(1, 0);
                    Widgets.ScrollHorizontal(partsSelectOuterRect, ref scrollPos_PartsSelect, viewRect_PartsSelect, 10f);
                    Widgets.BeginScrollView(partsSelectOuterRect, ref scrollPos_PartsSelect, viewRect_PartsSelect, true);

                    viewRect_PartsSelect.width = 0;
                    foreach (var part in adapters[selectedPartsIndex].GetAttatchableParts()) {
                        if (part?.Ability != null && weaponThing.def.tickerType != TickerType.Normal) {
                            continue;
                        }
                        Rect boxRect = new Rect(new Rect(viewRect_PartsSelect.xMax, viewRect_PartsSelect.y, 68, 68));
                        Rect partButtonRect = new Rect(new Rect(viewRect_PartsSelect.xMax + 2, viewRect_PartsSelect.y + 1, 64, 66));
                        Widgets.DrawWindowBackground(boxRect, new Color(1.5f, 1.5f, 1.5f));
                        if (part == null) {//パーツ削除ボタン
                            Widgets.Label(partButtonRect, adapters[selectedPartsIndex].mountDef.EmptyLabel.CapitalizeFirst());
                            if (attachedParts[selectedPartsIndex] == null) {
                                Widgets.DrawHighlightSelected(boxRect);
                            } else {
                                if (Mouse.IsOver(boxRect)) {
                                    Widgets.DrawHighlight(boxRect);
                                }
                                if (Widgets.ButtonInvisible(boxRect, true)) {
                                    SoundDefOf.Click.PlayOneShotOnCamera();
                                    //weaponComp.SetPart(selectedPartsIndex, null);
                                    weaponComp.SetPart(adapters[selectedPartsIndex].GenerateHelper(null));
                                    OnPartsChanged();
                                }
                            }
                        } else {//パーツ装着ボタン
                            bool isResearchFinished = part.IsResearchFinished(out _);
                            if (isResearchFinished) {
                                var texture = part.graphicData.Graphic.MatSingle.mainTexture;
                                //Rect textureRect = new Rect(0, 0, texture.width, texture.height) { center = partButtonRect.center };
                                Widgets.DrawTextureFitted(partButtonRect, texture, part.GUIScale);
                                Widgets.Label(partButtonRect, part.labelShort.CapitalizeFirst());
                            } else {
                                Widgets.Label(partButtonRect, "???");
                            }
                            if (attachedParts[selectedPartsIndex] == part) {
                                Widgets.DrawHighlightSelected(boxRect);
                            } else {
                                if (Mouse.IsOver(boxRect)) {
                                    Widgets.DrawHighlight(boxRect);
                                    mouseOverPartDef = part;
                                }
                                if (isResearchFinished && Widgets.ButtonInvisible(boxRect, true)) {
                                    SoundDefOf.Click.PlayOneShotOnCamera();
                                    //weaponComp.SetPart(selectedPartsIndex, part);
                                    weaponComp.SetPart(adapters[selectedPartsIndex].GenerateHelper(part));
                                    OnPartsChanged();
                                }
                            }
                        }
                        viewRect_PartsSelect.width += 68;
                    }
                    viewRect_PartsSelect.width += 1000;

                    Widgets.EndScrollView();
                    scrollPositioner.ScrollHorizontally(ref scrollPos_PartsSelect, partsSelectOuterRect.size);

                    //パーツ概要テキスト
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    Rect descRect = new Rect(inRect) {
                        y = inRect.y + Text.LineHeightOf(GameFont.Medium),
                        height = inRect.height - Text.LineHeightOf(GameFont.Medium) - 80
                    };
                    Widgets.DrawWindowBackground(descRect, colorFactor);
                    if (mouseOverPartDef != null) {
                        mouseOverPartDef.DrawDescription(descRect, weaponComp);
                    } else if (attachedParts[selectedPartsIndex] != null) {
                        //Widgets.DrawWindowBackground(descRect);
                        attachedParts[selectedPartsIndex].DrawDescription(descRect, weaponComp);
                    } else {
                        adapters[selectedPartsIndex].DrawEmptyDescription(descRect, weaponComp);
                    }
                }
                //    ----    おわり    ----    //

                if (paintTab.Selected) {
                    //塗装
                    DoDecalPaint(paintRect, weaponComp.GetPaintHelperOf(adapters[selectedPartsIndex].mountDef));
                }

                //タブ
                var tabRect = inRect.TopPartPixels(Text.LineHeightOf(GameFont.Medium)).RightPart(0.25f);
                tabRect.y += 1;
                if (Widgets.ButtonImage(tabRect.RightPartPixels(tabRect.height), DevGUI.Close)) {
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    selectedPartsIndex = -1;
                }
                tabRect.x -= tabRect.height;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                paintTab.Draw(tabRect);
                if (!paintTab.selected && Widgets.ButtonInvisible(tabRect, true)) paintTab.clickedAction();
                tabRect.x -= tabRect.width;
                partsTab.Draw(tabRect);
                if (!partsTab.selected && Widgets.ButtonInvisible(tabRect, true)) partsTab.clickedAction();

            }
            //おかたづけ
            Text.Anchor = fontAnchor;
            Text.Font = fontSize;
        }

        //--------------------------------------------------------//
        //        最下段パネル: 合計コスト表示, 終了ボタン        //    
        //--------------------------------------------------------//
        bool lackResource = false;
        protected virtual void DoLowerContents(Rect inRect) {
            Widgets.DrawBox(inRect);
            var cancelButtonRect = inRect.LeftPart(0.125f).ContractedBy(6f);
            if (Widgets.ButtonText(cancelButtonRect, "Cancel".Translate(), true, true, true, null)) {
                canceled = true;
                this.Close(true);
            }
            var fontAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            var ingredientsRect = inRect.RightPart(0.875f);
            ingredientsRect.x += 12f;
            lackResource = false;
            foreach (var i in weaponComp.GetRequiredIngredients()) {
                int ingredientCount = Mathf.Max(0, i.Item2 * -1);
                if (ingredientCount < 1) continue;
                Color tmp = GUI.color;//色
                if (ingredientCount > map.resourceCounter.GetCount(i.Item1)) {
                    GUI.color = Color.red;
                    lackResource = true;
                }
                ingredientsRect.width = 32;
                Widgets.DrawTextureFitted(ingredientsRect, i.Item1.graphic.MatSingle.mainTexture, 1);
                ingredientsRect.x += ingredientsRect.width;
                string text = string.Format(" x{0}  " , ingredientCount);
                ingredientsRect.width = Text.CalcSize(text).x;
                Widgets.Label(ingredientsRect, text);
                ingredientsRect.x += ingredientsRect.width;
                GUI.color = tmp;//色もどし
            }
            Text.Anchor = fontAnchor;
            var acceptButtonRect = inRect.RightPart(0.125f).ContractedBy(6f);
            if (lackResource && !DebugSettings.godMode) {
                Widgets.DrawHighlight(acceptButtonRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(acceptButtonRect, "MW2_lackResource".Translate());
                Text.Anchor = fontAnchor;
            } else if (Widgets.ButtonText(acceptButtonRect, "Accept".Translate(), true, true, true, null)) {
                canceled = false;
                this.Close(true);
            }
        }

        //塗装
        bool paintDirty = false;
        bool currentlyDragging = false;
        Vector2 decalScrollPos = Vector2.zero;
        Rect decalViewRect;
        MWDecalDef clipbordDecalDef = null;
        Color clipbordColor = Color.white;
        protected virtual void DoDecalPaint(Rect inRect, DecalPaintHelper target) {
            var fontAnchor = Text.Anchor;
            var fontSize = Text.Font;
            Rect slidersRect = inRect.LeftPart(0.3f);
            Widgets.DrawWindowBackground(slidersRect);
            Color colorNew = target.color;
            //カラーホイール
            Rect colorWheelRect = new Rect(inRect.x, inRect.y, slidersRect.width, slidersRect.width).ContractedBy(8f);
            Widgets.HSVColorWheel(colorWheelRect, ref colorNew, ref currentlyDragging);
            //色スライダー
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            slidersRect.yMin += slidersRect.width;
            slidersRect.x += 8;
            slidersRect.width -= 16;
            slidersRect.height /= 4;
            Color contentColor = GUI.color;
            Widgets.Label(slidersRect, "R:"+ colorNew.r);
            GUI.color = Color.red;
            colorNew.r = Mathf.Round(GUI.HorizontalSlider(slidersRect, colorNew.r, 0f, 1f) * 20f) / 20f;
            slidersRect.y += slidersRect.height;
            GUI.color = contentColor;
            Widgets.Label(slidersRect, "G:"+ colorNew.g);
            GUI.color = Color.green;
            colorNew.g = Mathf.Round(GUI.HorizontalSlider(slidersRect, colorNew.g, 0f, 1f) * 20f) / 20f;
            slidersRect.y += slidersRect.height;
            GUI.color = contentColor;
            Widgets.Label(slidersRect, "B:"+ colorNew.b);
            GUI.color = Color.blue;
            colorNew.b = Mathf.Round(GUI.HorizontalSlider(slidersRect, colorNew.b, 0f, 1f) * 20f) / 20f;
            slidersRect.y += slidersRect.height;
            GUI.color = contentColor;
            if (colorNew != target.color) {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                target.color = colorNew;
                paintDirty = true;
            }
            Text.Anchor = fontAnchor;
            Text.Font = fontSize;

            //コピペボタン
            slidersRect.width = slidersRect.height;
            if(Widgets.ButtonImageFitted(slidersRect, TexButton.Copy)) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                clipbordDecalDef = target.decalDef;
                clipbordColor = target.color;
            }
            slidersRect.x += slidersRect.width;
            if (clipbordDecalDef != null && Widgets.ButtonImageFitted(slidersRect, TexButton.Paste)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                target.decalDef= clipbordDecalDef;
                target.color = clipbordColor;
                paintDirty = true;
            }

            //迷彩柄
            Rect selecterRect = inRect.RightPart(0.7f);
            Widgets.DrawWindowBackground(selecterRect);
            float rectHeight = 0;
            float rectWidth = selecterRect.width - 16;
            Vector2 buttonSize = Vector2.one * (rectWidth / 5);
            var allDecals = MWDecalDef.GetAllDecalDefs().ToArray();
            Widgets.BeginScrollView(selecterRect, ref decalScrollPos, decalViewRect);
            int rowCount = 0;
            for (int i = 0; i < allDecals.Length; i++) {
                Rect buttonRect = new Rect(rowCount * buttonSize.x, rectHeight, buttonSize.x, buttonSize.y);
                GUI.DrawTexture(buttonRect.ContractedBy(4f), allDecals[i]?.graphicData.Graphic.MatSingle.mainTexture ?? BaseContent.BadTex);
                if (Mouse.IsOver(buttonRect)) {
                    Widgets.DrawHighlight(buttonRect);
                }
                if (target.decalDef== allDecals[i]) {
                    Widgets.DrawHighlightSelected(buttonRect);
                }
                if (Widgets.ButtonInvisible(buttonRect)) {
                    target.decalDef = allDecals[i];
                    paintDirty = true;
                }
                rowCount++;
                if (rowCount > 4) {
                    rectHeight += buttonSize.y;
                    rowCount = 0;
                }
            }
            Widgets.EndScrollView();
            decalViewRect = new Rect(0, 0, rectWidth, rectHeight + buttonSize.y);
            //更新処理
            if (paintDirty && !Input.GetMouseButton(0)) {
                OnPartsChanged();
                paintDirty = false;
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
            adapters = weaponComp.MountAdapters;
            attachedParts = weaponComp.AttachedParts;
        }
    }
}
