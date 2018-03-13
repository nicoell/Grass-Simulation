using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ExtendedColliderSpawner : MonoBehaviour {

	public GameObject[] GrassColliders;
	public KeyCode[] SpawnKeys = {
		KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3
	};
	[Range(0, 100)]
	public float TestSequenceDuration = 10f;
	[Range(1, 128)]
	public int ColliderCount = 4;
	[Range(0.01f, 10)]
	public float MinScale = 0.5f;
	[Range(0.01f, 10)]
	public float MaxScale = 1f;
	protected float TestSequenceTimer;
	protected bool TestSequenceRunning;

	public Mesh GizmoPlane;
	// Use this for initialization
	void Start()
	{
		var obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
		GizmoPlane = obj.GetComponent<MeshFilter>().sharedMesh;
		GameObject.Destroy(obj);
	}
	
	// Update is called once per frame
	void Update ()
	{
		/*if (TestSequenceRunning)
		{
			TestSequenceTimer += Time.deltaTime;
			if (TestSequenceTimer >= TestSequenceDuration) 
				TestSequenceRunning = false;
			else
				return;
			
		}*/
		for (int i = 0; i < Mathf.Min(SpawnKeys.Length, GrassColliders.Length); i++)
		{
			var spawnKey = SpawnKeys[i];
			var grassollider = GrassColliders[i];
			if (!Input.GetKeyDown(spawnKey)) continue;
			
			TestSequenceRunning = true;
			TestSequenceTimer = 0f;

			var oldState = Random.state;
			Random.InitState(42);
			
			for (float x = -1; x <= 1.0; x += 2f / (ColliderCount - 1) )
			for (float z = -1; z <= 1.0; z += 2f / (ColliderCount - 1) )
			{
				var pos = transform.localPosition;
				pos.x += x * transform.localScale.x;
				pos.z += z * transform.localScale.z;
				var grassCollider = Instantiate(grassollider, pos, transform.rotation);
				var rigidBody = grassCollider.GetComponent<Rigidbody>();
				rigidBody.velocity = transform.TransformDirection(Vector3.down + Random.onUnitSphere);
				rigidBody.transform.localScale = Random.Range(MinScale, MaxScale) * grassCollider.transform.localScale;
				Destroy(grassCollider, TestSequenceDuration);
			}

			Random.state = oldState;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		
		Gizmos.DrawWireMesh(GizmoPlane, transform.localPosition, Quaternion.identity, transform.localScale);
	}
}
