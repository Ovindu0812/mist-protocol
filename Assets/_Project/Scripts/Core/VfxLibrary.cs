using System.Collections;
using UnityEngine;

namespace MistProtocol.Core
{
    /// <summary>Holds the shared VFX materials (assigned by the level builder so shaders are included in builds).</summary>
    public class VfxLibrary : MonoBehaviour
    {
        public static VfxLibrary Instance { get; private set; }
        public Material particleMaterial;
        public Material tracerMaterial;
        private void Awake() => Instance = this;
    }
}
