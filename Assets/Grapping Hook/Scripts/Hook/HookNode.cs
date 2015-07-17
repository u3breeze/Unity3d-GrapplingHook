using UnityEngine;
using System.Collections;

public class HookNode : MonoBehaviour
{	
	private Transform mTransform;													// Reference to this transform.
	
	public HookEventListener hookEventListener = new HookEventListener();	// hook movement listener

	[HideInInspector]
	public bool wasCollided;														// if this node has collied wall

//	[HideInInspector]
//	public bool canReflect = true;													
	
	private Vector3 savePosition;													// original position

	void Awake()
	{		
		mTransform = transform;
		// save original position
		savePosition = mTransform.position;	
	}

	public void RemoveMe ()
	{ 
		// destroy self
		Destroy (gameObject);
	}

	void OnCollisionEnter (Collision collision)
	{
				
		if (collision.gameObject.tag == Tags.wall) {
			// dont collide if this has collided wall
			if (wasCollided){ //|| !canReflect){
				return;
			}

			// reflect this node
			ContactPoint contact = collision.contacts [0];
			
			Vector3 curDir = mTransform.TransformDirection (Vector3.forward);
			
			Vector3 newDir = Vector3.Reflect (curDir, contact.normal);
		
			float dotValue = Vector3.Dot (contact.normal, newDir);
			float angle = Mathf.Acos (dotValue) * Mathf.Rad2Deg;
			if (float.IsNaN (angle) || angle <= 5 || angle >= 175) {
				
				hookEventListener.NotifyTakeBack();
				
			} else {
				
				mTransform.rotation = Quaternion.FromToRotation (Vector3.forward, newDir);
				mTransform.position = savePosition;
				
			}
			wasCollided = true;
		} 
	}
	
}