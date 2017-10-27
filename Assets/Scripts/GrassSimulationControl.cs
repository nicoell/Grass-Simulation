using GrassSim;
using UnityEngine;

[ExecuteInEditMode]
public class GrassSimulationControl : MonoBehaviour
{
	public Terrain targetTerrain;
	public Camera targetCamera;

	private PatchHierarchy m_patchHierarchy;

	// Use this for initialization
	private void Start()
	{
		if (targetTerrain == null) return;
		if (targetCamera == null) targetCamera = Camera.main;
		m_patchHierarchy = new PatchHierarchy(targetTerrain.terrainData, transform);
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