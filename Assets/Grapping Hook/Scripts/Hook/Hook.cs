using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Hook : MonoBehaviour
{

	private Transform mTransform; 									// Reference to this transform.

	private List<Transform> hookObjects = new List<Transform> ();	// Enemies got hooked
	public float targetBondDamping = 10.0f; 						// The damping for bond target object
	public float targetBondDistance = 0.2f;							// The distance for bond target object
	public float hookRotateSpeed = 500;								// Hook rotating speed

	public HookEventListener hookEventListener = new HookEventListener(); // hook take back listener
	
	private Transform hookModelTransform;							// Hook model refence	
	private Transform ownerTrans;									// Hook owner

	public void setOwnerTrans(Transform owner){
		// set the owner of this hook
		ownerTrans = owner;
	}
	
	void Awake(){
		mTransform = transform;
		hookModelTransform = mTransform.FindChild ("hook");
	}
	
	// Use this for initialization
	void Start ()
	{

	}

	void Update()
	{
		// rotate the hook
		hookModelTransform.Rotate(Vector3.up * Time.deltaTime * hookRotateSpeed, Space.Self);
	}

	
	void OnDestroy ()
	{
		// clear the enemies got hooked
		if (hookObjects.Count > 0) {
			if (ownerTrans != null) {
					foreach (Transform trans in hookObjects) {
							// Do not ignore collision between owner and enemy
							Physics.IgnoreCollision (ownerTrans.collider, trans.collider, false);
					}
			}
			hookObjects.Clear ();
		}
	}

	void FixedUpdate ()
	{
		// enemies follow the hook
		if (hookObjects.Count > 0 && mTransform != null) {
			foreach (Transform trans in hookObjects) {
				FollowHook (mTransform, trans);
			}
		}
	}

	void OnTriggerEnter(Collider collider) 
	{
		if (collider.gameObject.tag == Tags.enemy) {
			// enemy get hooked by hook
			hookObjects.Add (collider.gameObject.transform);
			if(ownerTrans != null)
				Physics.IgnoreCollision (ownerTrans.collider, collider, true);

			// tell hook leader the hook should tack back.
			hookEventListener.NotifyTakeBack();
		}else if(collider.gameObject.tag == Tags.cylinder){
			// hook the cylinder. tell hook leader the hook got hooked.
			hookEventListener.NotifyHookSomething(mTransform.position);
		}
	}

	private void FollowHook (Transform prevNode, Transform follower)
	{
		// make follower follow the node
		Quaternion targetRotation = Quaternion.LookRotation (prevNode.position - follower.position, prevNode.up);
		targetRotation.x = 0f;
		targetRotation.z = 0f;
		follower.rotation = Quaternion.Slerp (follower.rotation, targetRotation, Time.deltaTime * targetBondDamping);

		Vector3 targetPosition = prevNode.position;
		targetPosition -= follower.rotation * Vector3.forward * targetBondDistance;
		targetPosition.y = follower.position.y;
		follower.position = Vector3.Lerp (follower.position, targetPosition, Time.deltaTime * targetBondDamping);

	}
	
}
