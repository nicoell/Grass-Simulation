using GrassSimulation.Core;
using GrassSimulation.Core.Attribute;
using GrassSimulation.Core.Patches;
using GrassSimulation.StandardContainers;
using UnityEngine;

//TODO: Revisit namespaces
namespace GrassSimulation
{
    [ExecuteInEditMode]
    public class GrassSimulationController : MonoBehaviour
    {
        //TODO: Revisit the null reference tests used all over the place
        [EmbeddedScriptableObject]
        public SimulationContext Context;

        private PatchContainer _patchHierarchy;

        // Use this for initialization
        private void OnEnable()
        {
            if (Context == null)
            {
                Context = ScriptableObject.CreateInstance<SimulationContext>();
            }
             PrepareSimulation();  
        }

        public void PrepareSimulation()
        {
            if (Context == null || !Context.Init()) return;
            _patchHierarchy = new UniformGridHierarchyPatchContainer(Context);
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
            //GUI.DrawTexture(new Rect(0, 0, 512, 512), Context.GrassInstance.ParameterTexture);
            //if (_patchHierarchy != null) _patchHierarchy.DebugDraw();
        }
    }
}