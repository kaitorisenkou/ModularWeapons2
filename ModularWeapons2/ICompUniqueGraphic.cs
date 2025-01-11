using RimWorld;
using System.Collections.Generic;
using UnityEngine;

namespace ModularWeapons2 {
    public interface ICompUniqueGraphic {
        Texture GetTexture();
        Material GetMaterial();
    }
}
