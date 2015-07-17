using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HookLeader : MonoBehaviour
{

	// Hook status
	private enum HookStatus
	{
		shooting, 
		takeback,
		reverseInit,
		reverse
	} 
	private HookStatus hookStatus = HookStatus.shooting;		// Hook is shooting state when it was created

	public int maxNodes = 70; 									// Max number of nodes that can be in the chain

	private List<GameObject> nodes = new List<GameObject> ();	// Nodes in the chain

	private Transform mTransform;
	public float bondDistance = 0.15f;							// Distance between nodes
	public float bondDamping = 100f;							// The damping for bond node

	public GameObject hookNodePrefab;							// Node Prefab
	public GameObject hookPrefab;								// Hook Prefab
	private GameObject hook;									// Hook gameObject
	private BaseCharacterController ownerController;			// Owner controller
	private Transform hookStartTransform;						// The transform of hook start 

	public float hookBondDistance = 0.25f;						// Distance between hook and last hook node.


	public float extendInterval = 0.05f;						// Bond node interval
	private float extendTime;									// Time last node bond
	public float takeBackInterval = 0f;							// Interval take back hook node 
	private float takeBackTime;									// Time last node tack back 
	public int shootHookSpeed = 1;								// Bond hook node number every time
	public int takeBackHookSpeed = 2;							// Take back hook node number every time
	private int nodeCount;										// Current count of hook node 
	private float updateOrderTime;								// Time Last update node order
	public float updateOrderInterval = 0f;						// Interval update node order
	public float updateOrderAngle = 110f;						// The angle between owner and hook chain
	public int minUpdateOrderNum = 5;							// Update node order when node count > this parameter
	public int updateOrderSpeed = 2;							// Update hook node number every time
	private LineRenderer lineRenderer;							// Hook Chain's LineRenderer
	private bool shouldKeepPosition;							// Whether or not keep hook leader position
	private Vector3 keepPosition;								// The position that hook should keep


	void StartTakeBackHook ()
	{
		// pull back hook
		hookStatus = HookStatus.takeback;
	}

	void HookSomething (Vector3 hookContactPoint)
	{
		// hook something
		if (nodes.Count >= minUpdateOrderNum) {
			keepPosition = hookContactPoint;
			hookStatus = HookStatus.reverseInit;
		}
	}
		
	void HookLogic ()
	{
		if (!shouldKeepPosition) {
			// keep hook leader follow owner's position
			mTransform.position = hookStartTransform.position;
		}
		
		// Pull back hook owner
		if (hookStatus == HookStatus.reverseInit) {
			shouldKeepPosition = true;
			// set leader position to hook contact point
			mTransform.position = keepPosition; 
			// set leader rotation to hook contact point
			//mTransform.rotation = Quaternion.LookRotation (hook.transform.position - keepPosition, Vector3.up);
			// reverse hook nodes
			nodes.Reverse ();
			// start reverse hook
			hookStatus = HookStatus.reverse;
			// Owner can not be controlled
			ownerController.Control(false);
		}
		
		
		// Hook shooting
		if (hookStatus == HookStatus.shooting) {
			if (nodeCount < maxNodes) {
				if(extendInterval > 0){
					if (Time.time - extendTime > extendInterval) {
						extendTime = Time.time;
						addHookNode (shootHookSpeed);
						nodeCount += shootHookSpeed;
					}
				}else{
					addHookNode (shootHookSpeed);
					nodeCount += shootHookSpeed;
				}
			} else {
				hookStatus = HookStatus.takeback;
			}
		}
		

		// Adjust hook nodes transform when owner position change
		if (hookStatus != HookStatus.reverse && nodes.Count >= minUpdateOrderNum) {
			float angle = Quaternion.Angle (hookStartTransform.rotation, mTransform.rotation);
			if (ownerController.IsMoving () && angle < updateOrderAngle) {
				bool updateNodeOrder = false;
				if (updateOrderInterval > 0) {
					if (Time.time - updateOrderTime > updateOrderInterval) {
						updateOrderTime = Time.time;
						updateNodeOrder = true;
					}
				} else {
					updateNodeOrder = true;
				}
				if (updateNodeOrder) {
					takeBackHook (updateOrderSpeed);
					addHookNode (updateOrderSpeed);			
				}
			}
		}
		
			
		// Pull back hook or reverse pull back hook.
		if (hookStatus == HookStatus.takeback || hookStatus == HookStatus.reverse) {
			
			if (nodes.Count > 0) {

				int speed = takeBackHookSpeed;
				if (takeBackInterval > 0) {
					if (Time.time - takeBackTime > takeBackInterval) {
						takeBackTime = Time.time;
						takeBackHook (speed);
					}
				} else {
					takeBackHook (speed);
				}
				
				if (hookStatus == HookStatus.reverse && hook != null) {//pull back hook owner
					FollowPrev (hook.transform, ownerTransform);
				}
			}
			
			if (nodes.Count <= 0) {
				Destroy (hook);
				Destroy (gameObject);
			}
		}
		
	}
	
	private void addHookNode (int speed)
	{
		// Bond new node
		for (int i=0; i<speed; i++) {

			Transform preTransform = LastNode ();

			Vector3 position = nextPosition(preTransform);
			Quaternion rotation = nextRotation(preTransform, position);
			GameObject hookNodeClone = Instantiate (hookNodePrefab, position, rotation) as GameObject;
			HookNode node = hookNodeClone.GetComponent<HookNode> ();
			node.hookEventListener.StartTakeBackHook += StartTakeBackHook;
//			node.canReflect = canReflect;

			Physics.IgnoreCollision (hookNodeClone.collider, ownerTransform.gameObject.collider);
			if(nodes.Count < maxNodes){
				nodes.Add (hookNodeClone);		
			}
		}
	}
	
	private void takeBackHook (int speed)
	{
		// Remove node for take back hook chain
		for (int i=0; i<speed; i++) {
			if (nodes.Count > 0) {
				HookNode node = nodes [0].GetComponent<HookNode> ();
				node.RemoveMe ();
				nodes.RemoveAt (0);	
				if (nodes.Count == 0) {
					break;
				}
			} 
		}
	}

	private Transform LastNode ()
	{ 		
		if (nodes.Count > 0) {
			return nodes [nodes.Count - 1].transform;
		} else {
			return mTransform;
		}
	}
	
	private Transform ownerTransform;
	public void Init (Transform shootPointTransform)
	{
		if (updateOrderSpeed > minUpdateOrderNum) {
			updateOrderSpeed = minUpdateOrderNum;
			//throw new System.ArgumentException("updateOrderSpeed can not be greater than minUpdateOrderNum");
		} 
		
		mTransform = transform;
		this.ownerTransform = shootPointTransform.parent;//ownerTransform;
		
		ownerController = ownerTransform.gameObject.GetComponent<BaseCharacterController> ();
		hookStartTransform = shootPointTransform;//ownerTransform.gameObject.transform.FindChild("HookShootPoint");
			
		// Instantiate hook
		Vector3 position = nextPosition(mTransform.transform);
		Quaternion rotation = nextRotation(mTransform.transform, position);
		hook = Instantiate (hookPrefab, position, rotation) as GameObject;
		Hook hookScript = hook.GetComponent<Hook> ();
		hookScript.setOwnerTrans (ownerTransform);
		hookScript.hookEventListener.StartTakeBackHook += StartTakeBackHook;
		hookScript.hookEventListener.HookSomething += HookSomething;

		Physics.IgnoreCollision (hook.collider, ownerTransform.gameObject.collider);

		// Slow down owner speed
		ownerController.SlowDownMovingSpeed ();
	}
	
	void Awake ()
	{
		lineRenderer = GetComponent<LineRenderer> ();
		if(lineRenderer)
			lineRenderer.enabled = false;
	}
	
	void Start ()
	{
		
	}
	
	void OnDestroy ()
	{
		// Recover owner speed
		ownerController.NormalMovingSpeed ();
		// Owner can be controlled
		ownerController.Control(true);
	}

	void FixedUpdate ()
	{
				
		HookLogic ();

		// update hook nodes transform
		for (int i = 0; i < nodes.Count; i++) {
			FollowPrev (i == 0 ? mTransform : nodes [i - 1].transform, nodes [i].transform);
		}
		
		// update hook transform
		if (hook != null) {
			HookFollowLast(LastNode (), hook.transform);
		}
		
		// Renderer hook path
		if(lineRenderer && nodes.Count >= 5){
			lineRenderer.enabled = true;
			lineRenderer.SetVertexCount (nodes.Count);
			for (int i = 0; i < nodes.Count; i++) {
				lineRenderer.SetPosition (i, nodes [i].transform.position);
			}
		}

	}
	
	private Vector3 nextPosition (Transform prevNode)
	{
		// Get next node position
	
		// Convert the angle into a rotation
		Quaternion currentRotation = Quaternion.Euler (0, prevNode.eulerAngles.y, 0);

		Vector3 position = prevNode.position;
		position -= currentRotation * Vector3.forward * bondDistance;
		return position;
	}
	private Quaternion nextRotation (Transform prevNode, Vector3 position)
	{
		// Get next node rotation
		return Quaternion.LookRotation (prevNode.position - position, prevNode.up);
	}

	private void HookFollowLast (Transform prevNode, Transform node)
	{

		float targetRotationAngle = prevNode.eulerAngles.y;
		float currentRotationAngle = node.transform.eulerAngles.y;
		// Calculate the current rotation angles
		currentRotationAngle = Mathf.LerpAngle (currentRotationAngle, targetRotationAngle, bondDamping * Time.deltaTime);
		// Convert the angle into a rotation
		Quaternion currentRotation = Quaternion.Euler (0, currentRotationAngle, 0);
		// bondDistance meters behind the prevNode
		node.transform.position = prevNode.position;
		node.transform.position -= currentRotation * Vector3.forward * hookBondDistance;
		//Always look at the prevNode
		node.transform.LookAt (prevNode);
	}
	
	private void FollowPrev (Transform prevNode, Transform node)
	{
		// Set node's rotation and position by the previous node

		Quaternion targetRotation = Quaternion.LookRotation (prevNode.position - node.position, prevNode.up);
		targetRotation.x = 0;
		targetRotation.z = 0;
		node.rotation = Quaternion.Slerp (node.rotation, targetRotation, Time.deltaTime * bondDamping);
		
		Vector3 targetPosition = prevNode.position;
		targetPosition -= node.transform.rotation * Vector3.forward * bondDistance;
		targetPosition.y = node.position.y;
		node.position = Vector3.Lerp (node.position, targetPosition, Time.deltaTime * bondDamping); 
	}

}