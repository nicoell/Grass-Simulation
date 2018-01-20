using UnityEngine;

namespace GrassSimulation
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Renderer))]
	[RequireComponent(typeof(Rigidbody))]
	public class GrassCollisionController : MonoBehaviour
	{
		private Renderer _renderer;
		private Rigidbody _rigidbody;
	
		// Use this for initialization
		void Awake()
		{
			_renderer = GetComponent<Renderer>();
			_rigidbody = GetComponent<Rigidbody>();
		}
	
		// Update is called once per frame
		void Update () {
			_renderer.sharedMaterial.SetVector("collisionVelocity", _rigidbody.velocity);// * _rigidbody.mass
			_renderer.sharedMaterial.SetVector("objectCenter", transform.position + _rigidbody.centerOfMass);
			//Shader.SetGlobalVector("customColor", new Vector4(1, 1, 1, 1) );
		}
	}
}
