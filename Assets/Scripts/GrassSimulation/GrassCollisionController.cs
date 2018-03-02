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
		private MaterialPropertyBlock _propertyBlock;
	
		// Use this for initialization
		void Awake()
		{
			_renderer = GetComponent<Renderer>();
			_rigidbody = GetComponent<Rigidbody>();
			_propertyBlock = new MaterialPropertyBlock();
			_propertyBlock.SetVector("collisionVelocity", _rigidbody.velocity);
		}
	
		// Update is called once per frame
		void Update ()
		{
			_propertyBlock.SetVector("collisionVelocity", _rigidbody.velocity);
			_renderer.SetPropertyBlock(_propertyBlock);
			//_renderer.sharedMaterial.SetVector("collisionVelocity", _rigidbody.velocity);// * _rigidbody.mass
			//Shader.SetGlobalVector("customColor", new Vector4(1, 1, 1, 1) );
		}
	}
}
