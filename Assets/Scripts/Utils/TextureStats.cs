using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TextureStats : MonoBehaviour
{
	public Texture2D Texture2D;
	[TextArea]
	public string stats;
	
	// Use this for initialization
	void Start () {
		if (Texture2D)
		{
			stats = "Mipmap Count: "+Texture2D.mipmapCount;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
