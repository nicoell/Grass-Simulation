using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Rigidbody))]
public class GrassCollisionController : MonoBehaviour
{
	private Renderer _renderer;
	private Rigidbody _rigidbody;
	
	// Use this for initialization
	void Start()
	{
		_renderer = GetComponent<Renderer>();
		_rigidbody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
		_renderer.sharedMaterial.SetVector("collisionVelocity", _rigidbody.velocity);
		//Shader.SetGlobalVector("customColor", new Vector4(1, 1, 1, 1) );
	}
}
