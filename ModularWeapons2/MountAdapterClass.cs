using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class MountAdapterClass {
        public ModularPartsMountDef mountDef;
        public Vector2 offset = Vector2.zero;
        public int layerOrder = 0;
        [DefaultValue(null)]
        public GraphicData adapterGraphic = null;
        public bool allowMoreAdapter = true;
        [DefaultValue(null)]
        public ModularPartsDef defaultPart = null;

        public IEnumerable<ModularPartsDef> GetAttatchableParts() {
            return GetAttatchableParts_Internal(true);
        }
        IEnumerable<ModularPartsDef> GetAttatchableParts_Internal(bool containAllowEmpty) {
            if (containAllowEmpty && mountDef.allowEmpty) {
                yield return null;
            }
            foreach (var i in DefDatabase<ModularPartsDef>.AllDefsListForReading.Where(t => t.attachedTo == this.mountDef)) {
                yield return i;
            }
            if (!allowMoreAdapter || mountDef.canAdaptAs == null)
                yield break;
            foreach (var ac in mountDef.canAdaptAs) {
                foreach (var i in ac.GetAttatchableParts_Internal(false)) {
                    yield return i;
                }
            }
        }
        Vector2? normalizedOffsetInt = null;
        public Vector2 NormalizedOffset {
            get {
                if (!normalizedOffsetInt.HasValue) {
                    var result = offset.normalized;
                    float max = Mathf.Max(offset.x, offset.y);
                    result.x = Mathf.Abs(result.x) < 0.01f ? max : result.x;
                    result.y = Mathf.Abs(result.y) < 0.01f ? max : result.y;
                    normalizedOffsetInt = result;
                }
                return normalizedOffsetInt.Value;
            }
        }
    }
}
