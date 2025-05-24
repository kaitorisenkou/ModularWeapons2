using RimWorld;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class Dialog_RenameGunsmith : Dialog_Rename<CompModularWeapon> {
        public Dialog_RenameGunsmith(CompModularWeapon renaming) : base(renaming) {
            this.comp = renaming;
        }
        public override Vector2 InitialSize {
            get {
                var size = base.InitialSize;
                size.y += 50;
                return size;
            }
        }
        public override void DoWindowContents(Rect inRect) {
            base.DoWindowContents(inRect);
            var textRect = new Rect(0f, 100f, inRect.width, 35f);
            Widgets.Label(textRect, "MW_EmptyToDefault".Translate());
        }
        CompModularWeapon comp;
        protected override AcceptanceReport NameIsValid(string name) {
            return true;
        }

    }
}
