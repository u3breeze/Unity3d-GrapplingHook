using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShootHook : MonoBehaviour
{
	#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
	private class TouchInfo
	{
		public Touch touch;															// one finger touch 
		public Vector3 slideStartTouchPos;											// slide start touch position
		public float slideStartTime;												// slide start time	 
		public bool isSlide;														// if the finger is sliding on screen
	}
	private Dictionary<int,TouchInfo> touchDic = new Dictionary<int,TouchInfo>();	// save the touch
	#else 
	private Vector3 slideStartTouchPos;												// slide start touch position
	private float slideStartTime;													// slide start touch position
	private bool isSlide;															// if the mouse is sliding on screen 
	#endif
	
	public float minSlideDis = 8f;													// The minimum slide distance shooting need
	public float maxSlideTime = 1.5f;												// The max slide time shooting need

	private Transform whoShootTransform;											// who shoot
	public GameObject hookLeaderPerfab;												// hook leader perfab
	private BaseCharacterController characterController;							// owner controller
	public static GameObject hookLeaderObject;										// hook leader GameObject

	private float shootTime;														// shoot time
	public float shootInterval = 0.2f;												// shoot interval
	
	private Transform mTransform;													// Reference to this transform

	void Awake(){
		
	}
	
	void Start ()
	{
		mTransform = transform;
		// This gameObject's parent is the owner
		whoShootTransform = mTransform.parent;
		// Reference to the owner's controller.
		characterController = whoShootTransform.gameObject.GetComponent<BaseCharacterController> ();
	}
	
	void Update()
	{
		
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{		
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY

		if (Input.touchCount > 0) {
			for(int i=0; i<Input.touchCount; i++){
				Touch touch = Input.GetTouch (i);

				if(characterController.IsRightJoystickArea(touch.position)){
					// shoot hook if touch right joystick.
					if(touch.phase == TouchPhase.Began){ 
						Shooting(mTransform.rotation);
					}
				}else if(!characterController.IsLeftJoystickArea(touch.position)){ // if not touch left joystick.
					switch(touch.phase){
						case TouchPhase.Began:
							//save the touch infomation
							TouchInfo info = new TouchInfo();
							info.touch = touch;
							info.isSlide = true;
							info.slideStartTouchPos = touch.position;
							info.slideStartTime = Time.time;
							touchDic[touch.fingerId] = info;

						break;
						case TouchPhase.Stationary:
							// it is not slide action
							touchDic[touch.fingerId].isSlide = false;
						break;
						case TouchPhase.Ended:
							// save touch position
							Vector3 slideEndTouchPos = touch.position;
							// get this touch infomation
							TouchInfo tinfo =  touchDic[touch.fingerId];
							// slide distance
							float touchDis = (slideEndTouchPos - tinfo.slideStartTouchPos).magnitude;
							if(!tinfo.isSlide || touchDis < minSlideDis || Time.time -  tinfo.slideStartTime > maxSlideTime){
								// move owner to the touch position on ground
								moveOwner(slideEndTouchPos);
							}else{
								// shoot a hook
								Shooting(tinfo.slideStartTouchPos, slideEndTouchPos);
							}
							// remove this touch infomation
							touchDic.Remove(touch.fingerId);
						break;
					}
				}

			}

		}
		#else

//		if (Input.GetButton("Shoot")) {
		if (Input.GetKey(KeyCode.Space)) {
			// shoot hook if press shoot key
			Shooting(mTransform.rotation);
		}else{
			if(Input.GetMouseButtonDown (0)){
				// save click position
				slideStartTouchPos = Input.mousePosition;
				// save click time
				slideStartTime = Time.time;
			}else if(Input.GetMouseButtonUp(0)){
				// save click position
				Vector3 slideEndTouchPos = Input.mousePosition;
				// slide distance
				float touchDis = (slideEndTouchPos - slideStartTouchPos).magnitude;
				if(touchDis < minSlideDis || Time.time -  slideStartTime > maxSlideTime){
					// move owner to the click position on ground
					moveOwner(slideEndTouchPos);
				}else{
					// shoot a hook
					Shooting(slideStartTouchPos, slideEndTouchPos);
				}

			}
		}

		#endif		
	}

	private void Shooting(Vector3 slideStartTouchPos, Vector3 slideEndTouchPos)
	{
		if (hookLeaderObject != null 
		    || characterController.IsShooting() 
		    || Time.time - shootTime < shootInterval) {
			return;
		}

		// ray to touchpad
		int layermask = 1 << LayerMask.NameToLayer("TouchPad");
		RaycastHit shit;
		RaycastHit ehit;

		// the start touch position ray.
		Ray sray = Camera.main.ScreenPointToRay(slideStartTouchPos);
		// the end touch position ray.
		Ray eray = Camera.main.ScreenPointToRay(slideEndTouchPos);	
			
		if (Physics.Raycast (sray, out shit, 1000, layermask) &&
		 	Physics.Raycast (eray, out ehit, 1000, layermask)) {
			// position on ground from start touch ray
			Vector3 slideStartPos = shit.point;
			// position on ground from end touch ray
			Vector3 slideEndPos = ehit.point;

			// direction to shoot 
			Vector3 shootDirection = - (slideEndPos - slideStartPos).normalized;
			// hook leader's directon
			Quaternion hookRotation = Quaternion.LookRotation (shootDirection, Vector3.up);//Quaternion.FromToRotation(mTransform.forward, shootDirection);
			// shook a hook
			Shooting(hookRotation);
		}

//		Vector3 slideStartPos = new Vector3(slideStartTouchPos.x, 0, slideStartTouchPos.y);
//		Vector3 slideEndPos = new Vector3(slideEndTouchPos.x, 0, slideEndTouchPos.y);
//		Vector3 shootDirection = - (slideEndPos - slideStartPos).normalized;
//		Quaternion hookRotation = Quaternion.LookRotation (shootDirection, Vector3.up);//Quaternion.FromToRotation(mTransform.forward, shootDirection);
//		Shooting(hookRotation);

	}

	private void Shooting(Quaternion hookRotation)
	{
		if (hookLeaderObject != null 
		    || characterController.IsShooting() 
		    || Time.time - shootTime < shootInterval) {
			// if one hook has created, dont create more.
			return;
		}

		// save shoot time.
		shootTime = Time.time;
		// let owner to do shoot action
		characterController.Shoot();
		// create hook gameObject
		hookLeaderObject = Instantiate (hookLeaderPerfab, mTransform.position, hookRotation) as GameObject;
		// get hook leader 
		HookLeader hookLeader =  hookLeaderObject.GetComponent<HookLeader>();
		// Instantiate the hook
		hookLeader.Init(mTransform); 
	}

	private void moveOwner(Vector3 touchScreenPosition)
	{
		// Get position on the Ground by touched point.
		Ray ray = Camera.main.ScreenPointToRay (touchScreenPosition); 
		RaycastHit hit;  
		int layermask = 1 << LayerMask.NameToLayer("Ground");
		if (Physics.Raycast (ray, out hit, 1000, layermask)) {
			// Move the owner to touched position on Ground
			characterController.MoveTo (hit.point); 
		}
	}

//	IEnumerator Shoot(Quaternion hookRotation) {
//		
//        yield return new WaitForSeconds(0.1f);
//		hookLeaderObject = Instantiate (hookLeaderPerfab, mTransform.position, hookRotation) as GameObject;
//		HookLeader hookLeader =  hookLeaderObject.GetComponent<HookLeader>();
//		hookLeader.Init(mTransform);
//    }

}
