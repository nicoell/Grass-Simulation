using GrassSimulation.Grass;
using GrassSimulation.LOD;
using UnityEngine;

//TODO: Revisit namespaces
namespace GrassSimulation
{
    [ExecuteInEditMode]
    public class GrassSimulationController : MonoBehaviour
    {
        //TODO: Revisit the null reference tests used all over the place
        public SimulationContext Context;

        private PatchHierarchy _patchHierarchy;
        
        // Use this for initialization
        private void OnEnable()
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
                _patchHierarchy.Draw();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (Context == null || !Context.IsReady) return;
            if (_patchHierarchy != null) _patchHierarchy.DrawGizmo();
        }

        
        
        //TODO: Need to revisit the correct way to destroy/dispose/release ComputeBuffers so the warnings go away
        private void OnDisable()
        {
            if (Context == null || !Context.IsReady) return;
            if (_patchHierarchy != null)
            {
                _patchHierarchy.Destroy();
            }
        }

        private void OnGUI()
        {
            //GUI.DrawTexture(new Rect(0, 0, 512, 512), Context.SharedGrassData.ParameterTexture);
            //if (_patchHierarchy != null) _patchHierarchy.DebugDraw();
        }
    }
}