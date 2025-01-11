using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class MountAdapterClass {
        public ModularPartsMountDef mountDef;
        public Vector2 offset = Vector2.zero;
        public Texture adapterTexture = null;
        public bool allowMoreAdapter = true;
        [DefaultValue(null)]
        public ModularPartsDef defaultPart = null;
    }
}
