using System;

namespace GrassSimulation
{
    [Serializable]
    public class SimulationSettings
    {
        public float BladeMaxBend = 10f;
        public float BladeMaxHeight = 10f;
        public float BladeMaxWidth = 3f;
        public float BladeMinBend = 3f;
        public float BladeMinHeight = 2f;
        public float BladeMinWidth = 0.1f;
        public float GrassDensity = 1f;
        public uint PatchSize = 8;
        public uint PrecomputedFactor = 64;
        public int RandomSeed = 42;

        public uint GetAmountBlades()
        {
            return (uint) (PatchSize * PatchSize * GrassDensity);
        }

        public uint GetAmountPrecomputedBlades()
        {
            return GetAmountBlades() * PrecomputedFactor;
        }
    }
    
    [Serializable]
    public class EditorSettings
    {
        public bool DrawBoundingPatchGizmo = true;
        public bool DrawGrassPatchGizmo = true;
        public bool DrawGrassDataGizmo = true;
        
    }
}