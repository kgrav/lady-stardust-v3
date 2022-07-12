using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
	public Transform Target;
	public float Smoothness;

	public Vector3 offset;
	private void Update()
	{
		if (Target)
		{
			transform.position = Vector3.Lerp(transform.position, Target.transform.position + offset, Smoothness);
			transform.rotation = Quaternion.Lerp(transform.rotation, Target.transform.rotation, 1);
		}
	}

	private void OnValidate()
	{
		Update();	
	}
}
