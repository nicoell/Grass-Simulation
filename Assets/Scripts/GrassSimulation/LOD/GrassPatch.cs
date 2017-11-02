using UnityEngine;

namespace GrassSimulation.LOD
{
    public class GrassPatch : Patch
    {
        private readonly SimulationSettings _settings;
        private readonly TerrainData _terrainData;
        private Vector4 _patchTexCoord;
        private readonly int _startIndex;

        public GrassPatch(SimulationSettings settings, TerrainData terrainData, Vector4 patchTexCoord, Bounds bounds)
        {
            Bounds = bounds;
            _settings = settings;
            _terrainData = terrainData;
            _patchTexCoord = patchTexCoord;
            _startIndex =
                (int) Random.Range(0, settings.GetAmountPrecomputedBlades() - settings.GetAmountBlades() - 1);
        }

        public Vector4[] GrassDataA { get; private set; } //bladeUp.xyz, position.y
        public Vector4[] GrassDataB { get; private set; } //bladeV1.xyz, height
        public Vector4[] GrassDataC { get; private set; } //bladeV2.xyz, dirAlpha

        public override bool IsLeaf
        {
            get { return true; }
        }

        public void GeneratePerBladeData()
        {
            for (var i = _startIndex; i < _settings.GetAmountBlades(); i++)
            {
                //TODO: Continue here.
            }
        }

        public override void DrawGizmo()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(Bounds.center, 0.5f);
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);
        }
    }
}