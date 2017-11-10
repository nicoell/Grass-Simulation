using System;

namespace GrassSimulation
{
    [Serializable]
    public class SimulationSettings
    {
        //TODO: Split up and categorize Settings
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
        
        //TODO: Manage this stuff better
        public uint GetAmountBlades()
        {
            return (uint) (PatchSize * PatchSize * GrassDensity);
        }

        public uint GetDummyMeshSize()
        {
            return PatchSize * PatchSize;
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
        public bool DrawGrassDataDetailGizmo;
    }
}