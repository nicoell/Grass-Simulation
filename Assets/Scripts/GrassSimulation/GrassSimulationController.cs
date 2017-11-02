using GrassSimulation.Grass;
using GrassSimulation.LOD;
using UnityEngine;

namespace GrassSimulation
{
    [ExecuteInEditMode]
    public class GrassSimulationController : MonoBehaviour
    {
        public SimulationContext Context;

        private PatchHierarchy _patchHierarchy;
        
        // Use this for initialization
        private void Start()
        {
            if (!Context)
            {
                Context = ScriptableObject.CreateInstance<SimulationContext>();
                Context.Camera = Camera.main;
                Context.Settings = new SimulationSettings();
                Context.Terrain = Terrain.activeTerrain;
                Context.Transform = transform;
            }
             PrepareSimulation();  
        }

        private void PrepareSimulation()
        {
            if (!Context || !Context.Init()) return;
            
            _patchHierarchy = new PatchHierarchy(Context);
            _patchHierarchy.Init();
        }

        // Update is called once per frame
        private void Update()
        {
            if (!Context || !Context.IsReady) return;
            if (_patchHierarchy != null) _patchHierarchy.CullViewFrustum();
        }

        private void OnDrawGizmos()
        {
            if (_patchHierarchy != null) _patchHierarchy.DrawGizmo();
        }
    }
}