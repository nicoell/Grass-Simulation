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
            if (Context == null)
            {
                Context = new SimulationContext
                {
                    Camera = Camera.main,
                    Terrain = Terrain.activeTerrain,
                    Transform = transform
                };
            }
             PrepareSimulation();  
        }

        public void PrepareSimulation()
        {
            if (Context == null || !Context.Init()) return;
            
            _patchHierarchy = new PatchHierarchy(Context);
            _patchHierarchy.Init();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Context == null || !Context.IsReady) return;
            if (_patchHierarchy != null)
            {
                _patchHierarchy.CullViewFrustum();
                _patchHierarchy.Draw();
            }
        }

        private void OnRenderObject()
        {
            if (Context == null || !Context.IsReady) return;
            //if (_patchHierarchy != null) _patchHierarchy.Draw();
        }

        private void OnDrawGizmos()
        {
            if (Context == null || !Context.IsReady) return;
            if (_patchHierarchy != null) _patchHierarchy.DrawGizmo();
        }

        private void OnDestroy()
        {
            if (Context == null || !Context.IsReady) return;
            if (_patchHierarchy != null) _patchHierarchy.Destroy();
        }
    }
}