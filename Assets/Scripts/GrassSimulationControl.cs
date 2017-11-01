using GrassSim;
using GrassSim.Grass;
using UnityEngine;

[ExecuteInEditMode]
public class GrassSimulationControl : MonoBehaviour
{
	public Terrain targetTerrain;
	public Camera targetCamera;
	public Settings globalSettings;

	private PatchHierarchy m_patchHierarchy;

	// Use this for initialization
	private void Start()
	{
		if (targetTerrain == null) return;
		if (targetCamera == null) targetCamera = Camera.main;
		PrecomputedGrassData.Instance.Build(globalSettings);
		m_patchHierarchy = new PatchHierarchy(globalSettings, targetTerrain.terrainData, transform);
	}

	// Update is called once per frame
	private void Update()
	{
		if (m_patchHierarchy != null)
		{
			if (targetCamera != null) m_patchHierarchy.CullViewFrustum(targetCamera);
		}
	}

	private void OnDrawGizmos()
	{
		if (m_patchHierarchy != null){ m_patchHierarchy.DrawGizmo();}
	}
}