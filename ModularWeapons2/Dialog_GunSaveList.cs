using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace ModularWeapons2 {

    //このへん前作からコピペ+微修正

    public abstract class Dialog_GunSaveList : Dialog_FileList {
        protected Thing weapon;
        protected override void ReloadFiles() {
            this.files.Clear();
            if (weapon == null) return;
            foreach (FileInfo fileInfo in AllFiles(weapon)) {
                try {
                    SaveFileInfo saveFileInfo = new SaveFileInfo(fileInfo);
                    saveFileInfo.LoadData();
                    this.files.Add(saveFileInfo);
                } catch (Exception ex) {
                    Log.Error("Exception loading " + fileInfo.Name + ": " + ex.ToString());
                }
            }
        }
        public static IEnumerable<FileInfo> AllFiles(Thing weapon) {
            DirectoryInfo directoryInfo = new DirectoryInfo(FilePath(weapon));
            if (!directoryInfo.Exists) {
                directoryInfo.Create();
            }
            return from f in directoryInfo.GetFiles()
                   where f.Extension == ".rmwg"
                   orderby f.LastWriteTime descending
                   select f;
        }
        public static string FilePath(Thing weapon) {
            string text1 = Path.Combine(GenFilePaths.SaveDataFolderPath, "MW2Gunsmith");
            DirectoryInfo directoryInfo1 = new DirectoryInfo(text1);
            if (!directoryInfo1.Exists) {
                directoryInfo1.Create();
            }
            string text2 = Path.Combine(text1, weapon.def.defName);
            DirectoryInfo directoryInfo2 = new DirectoryInfo(text2);
            if (!directoryInfo2.Exists) {
                directoryInfo2.Create();
            }
            return text2;
        }
        public static string AbsPath(Thing weapon, string fileName) {
            return Path.Combine(FilePath(weapon), fileName + ".rmwg");
        }
    }
    public class Dialog_Gunsmith_Load : Dialog_GunSaveList {
        private CompModularWeapon comp;
        Action returner = null;
        public Dialog_Gunsmith_Load(CompModularWeapon comp, Action returner=null) {
            this.interactButLabel = "LoadGameButton".Translate();
            this.comp = comp;
            this.weapon = comp.parent;
            this.returner = returner;
            ReloadFiles();
        }
        protected override void DoFileInteraction(string fileName) {
            string absPath = AbsPath(weapon, fileName);
            CMW_Exposer exposer;
            if(TryLoadGunsmith(absPath, out exposer)) {
                comp.UnpackExposer(exposer);
            }
            returner();
            this.Close(true);
        }
        public static bool TryLoadGunsmith(string absPath, out CMW_Exposer exposer) {
            try {
                exposer = new CMW_Exposer();
                Scribe.loader.InitLoading(absPath);
                try {
                    Scribe_Deep.Look(ref exposer, "gunsmith", Array.Empty<object>());
                    Scribe.loader.FinalizeLoading();
                } catch {
                    Scribe.ForceStop();
                    throw;
                }
            } catch (Exception ex) {
                Log.Error("Exception loading gunsmith: " + ex.ToString());
                Scribe.ForceStop();
                exposer = null;
            }
            return exposer != null;
        }

    }
    public class Dialog_Gunsmith_Save : Dialog_GunSaveList {
        public Dialog_Gunsmith_Save(Thing weapon, string label) {
            this.interactButLabel = "OverwriteButton".Translate();
            this.typingName = label;
            this.weapon = weapon;
            ReloadFiles();
        }

        protected override bool ShouldDoTypeInField {
            get {
                return true;
            }
        }

        protected override void DoFileInteraction(string fileName) {
            fileName = GenFile.SanitizedFileName(fileName);
            string absPath = AbsPath(weapon, fileName);
            LongEventHandler.QueueLongEvent(
                delegate () { SaveGunsmith(weapon.TryGetComp<CompModularWeapon>(), absPath); },
                "SavingLongEvent", false, null, true);
            Messages.Message("SavedAs".Translate(fileName), MessageTypeDefOf.SilentInput, false);
            this.Close(true);
        }

        public static bool isSavingOrLoading = false;
        public static void SaveGunsmith(CompModularWeapon comp, string absFilePath) {
            try {
                Dialog_Gunsmith_Save.isSavingOrLoading = true;
                //comp.fileName = Path.GetFileNameWithoutExtension(absFilePath);
                SafeSaver.Save(absFilePath, "savedgunsmith", delegate {
                    CMW_Exposer exposer = comp.CreateExposer();
                    ScribeMetaHeaderUtility.WriteMetaHeader();
                    Scribe_Deep.Look(ref exposer, "gunsmith", Array.Empty<object>());
                }, false);
            } catch (Exception ex) {
                Log.Error("Exception while saving gunsmith: " + ex.ToString());
            } finally {
                Dialog_Gunsmith_Save.isSavingOrLoading = false;
            }
        }
    }
    public class CMW_Exposer : IExposable {
        public void ExposeData() {
            Scribe_Collections.Look(ref attachHelpers, "attachHelpers", LookMode.Deep);
            Scribe_Collections.Look(ref decalHelpers, "decalHelpers", LookMode.Deep, new object[] { null, null, null });
            Scribe_Values.Look(ref weaponOverrideLabel, "weaponOverrideLabel", defaultValue: "");
        }
        public List<PartsAttachHelper> attachHelpers = new List<PartsAttachHelper>();
        public List<DecalPaintHelper> decalHelpers = new List<DecalPaintHelper>();
        public string weaponOverrideLabel = null;

        public CMW_Exposer() { }
        public CMW_Exposer(List<PartsAttachHelper> attachHelpers, List<DecalPaintHelper> decalHelpers, string weaponOverrideLabel) {
            this.attachHelpers = attachHelpers;
            this.decalHelpers = decalHelpers;
            this.weaponOverrideLabel = weaponOverrideLabel;
        }
    }
}
