using Verse;

namespace ModularWeapons2 {
    public class PartsAttachHelperClass {
        public ModularPartsDef partsDef;
        /*[DefaultValue(null)]
        public string attachTag = null;*/
        [DefaultValue(null)]
        public ModularPartsMountDef attachMountDef = null;
    }
    public struct PartsAttachHelper :IExposable {
        public void ExposeData() {
            Scribe_Defs.Look(ref partsDef, "partsDef");
            Scribe_Defs.Look(ref attachMountDef, "attachMountDef");
        }

        public ModularPartsDef partsDef;
        //public string attachTag;
        public ModularPartsMountDef attachMountDef;

        public PartsAttachHelper(ModularPartsDef partsDef, string attachTag, ModularPartsMountDef attachMountDef) {
            this.partsDef = partsDef;
            //this.attachTag = attachTag;
            this.attachMountDef = attachMountDef;
        }
        /*public PartsAttachHelper(ModularPartsDef partsDef, string attachTag) {
            this.partsDef = partsDef;
            this.attachTag = attachTag;
            this.attachMountDef = null;
        }*/
        public PartsAttachHelper(ModularPartsDef partsDef,ModularPartsMountDef attachMountDef) {
            this.partsDef = partsDef;
            //this.attachTag = null;
            this.attachMountDef = attachMountDef;
        }

        public static implicit operator PartsAttachHelper(PartsAttachHelperClass helperClass) {
            if (helperClass == null) {
                return new PartsAttachHelper(null, null, null);
            }
            return new PartsAttachHelper(helperClass.partsDef,/* helperClass.attachTag,*/ helperClass.attachMountDef);
        }
        public bool CanAttachTo(MountAdapterClass adapter) {
            /*if(!this.attachTag.NullOrEmpty() && !adapter.tagString.NullOrEmpty() && adapter.tagString.Equals(this.attachTag)) {
                return true;
            }*/
            if (this.attachMountDef != null && this.attachMountDef == adapter.mountDef) {
                return true;
            }
            return false;
        }
        public bool CanReplacedTo(PartsAttachHelper other) {
            /*if (!this.attachTag.NullOrEmpty() && !other.attachTag.NullOrEmpty() && other.attachTag.Equals(this.attachTag)) {
                return true;
            }*/
            if (this.attachMountDef != null && other.attachMountDef != null && this.attachMountDef == other.attachMountDef) {
                return true;
            }
            return false;
        }

    }
}
