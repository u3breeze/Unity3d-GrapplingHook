using UnityEngine;
using System.Collections;

// Require a character controller to be attached to the same game object
[RequireComponent (typeof(GUITexture))]
public class Joystick : MonoBehaviour
{

	class Boundary
	{
		public Vector2 min = Vector2.zero;
		public Vector2 max = Vector2.zero;
	}

	static private Joystick[] joysticks;					// A static collection of all joysticks
	static private bool enumeratedJoysticks = false;
	static private float tapTimeDelta = 0.3f;				// Time allowed between taps

	public bool touchPad; 									// Is this a TouchPad?
	public Rect touchZone;
	public float deadZone = 0.0f;									// Control when position is output
	public bool normalize = false; 							// Normalize output after the dead-zone?
	public Vector2 position; 									// [-1, 1] in x,y
	public int tapCount;											// Current tap count

	public int lastFingerId = -1;								// Finger last used for this joystick
	private float tapTimeWindow;							// How much time there is left for a tap to occur
	private Vector2 fingerDownPos;
//	private float fingerDownTime;
//	private float firstDeltaTime = 0.5f;
	private GUITexture gui;								// Joystick graphic
	private Rect defaultRect;								// Default position / extents of the joystick graphic
	private Boundary guiBoundary = new Boundary ();			// Boundary for joystick graphic
	private Vector2 guiTouchOffset;						// Offset to apply to touch input
	private Vector2 guiCenter;							// Center of joystick

#if !UNITY_IPHONE && !UNITY_ANDROID && !UNITY_WP8 && !UNITY_BLACKBERRY

	void Awake ()
	{
		gameObject.SetActive (false);
	}

#else

	void Start () {
		// Cache this component at startup instead of looking up every frame
		gui = GetComponent<GUITexture>();
	
		// Store the default rect for the gui, so we can snap back to it
		defaultRect = gui.pixelInset;
	
	    defaultRect.x += transform.position.x * Screen.width;// + gui.pixelInset.x; // -  Screen.width * 0.5;
	    defaultRect.y += transform.position.y * Screen.height;// - Screen.height * 0.5;
			
		Vector3 position = transform.position;
	    position.x = 0.0f;
	    position.y = 0.0f;
		transform.position = position;
	
		if (touchPad) {
			// If a texture has been assigned, then use the rect ferom the gui as our touchZone
			if (gui.texture)
				touchZone = defaultRect;
		}
		else {
			// This is an offset for touch input to match with the top left
			// corner of the GUI
			guiTouchOffset.x = defaultRect.width * 0.5f;
			guiTouchOffset.y = defaultRect.height * 0.5f;
	
			// Cache the center of the GUI, since it doesn't change
			guiCenter.x = defaultRect.x + guiTouchOffset.x;
			guiCenter.y = defaultRect.y + guiTouchOffset.y;
	
			// Let's build the GUI boundary, so we can clamp joystick movement
			guiBoundary.min.x = defaultRect.x - guiTouchOffset.x;
			guiBoundary.max.x = defaultRect.x + guiTouchOffset.x;
			guiBoundary.min.y = defaultRect.y - guiTouchOffset.y;
			guiBoundary.max.y = defaultRect.y + guiTouchOffset.y;
		}
	}
	
	public void Disable () {
		gameObject.SetActive (false);
		enumeratedJoysticks = false;
	}
	
	public void ResetJoystick () {
		// Release the finger control and set the joystick back to the default position
		gui.pixelInset = defaultRect;
		lastFingerId = -1;
		position = Vector2.zero;
		fingerDownPos = Vector2.zero;
	
		if (touchPad){
			Color gColor = gui.color;
			//gColor.a = 0.025f;
			gColor.a = 0.25f;
			gui.color = gColor;
		}
	}
	
	public bool IsFingerDown () {
		return (lastFingerId != -1);
	}
	
	void LatchedFinger (int fingerId) {
		// If another joystick has latched this finger, then we must release it
		if (lastFingerId == fingerId)
			ResetJoystick ();
	}
	
	void Update () {
		if (!enumeratedJoysticks) {
			// Collect all joysticks in the game, so we can relay finger latching messages
			joysticks = FindObjectsOfType (typeof(Joystick)) as Joystick[];
			enumeratedJoysticks = true;
		}
	
		int count = Input.touchCount;
	
		// Adjust the tap time window while it still available
		if (tapTimeWindow > 0)
			tapTimeWindow -= Time.deltaTime;
		else
			tapCount = 0;
	
		if (count == 0) {
			ResetJoystick ();
		}
		else {
			for (int i = 0; i < count; i++) {
				Touch touch = Input.GetTouch (i);
				//Vector2 guiTouchPos = touch.position - guiTouchOffset;
	
				bool shouldLatchFinger = false;
				if (touchPad) {
					if (touchZone.Contains (touch.position))
						shouldLatchFinger = true;
				}
				else if (gui.HitTest (touch.position)) {
					shouldLatchFinger = true;
				}
	
				// Latch the finger if this is a new touch
				if (shouldLatchFinger && (lastFingerId == -1 || lastFingerId != touch.fingerId)) {
	
					if (touchPad) {
						Color gColor = gui.color;
						gColor.a = 0.15f;
						gui.color = gColor;
	
						lastFingerId = touch.fingerId;
						fingerDownPos = touch.position;
						//fingerDownTime = Time.time;
					}
	
					lastFingerId = touch.fingerId;
	
					// Accumulate taps if it is within the time window
					if (tapTimeWindow > 0) {
						tapCount++;
					}
					else {
						tapCount = 1;
						tapTimeWindow = tapTimeDelta;
					}
	
					// Tell other joysticks we've latched this finger
					foreach (Joystick j in joysticks) {
						if (j != null && j != this)
							j.LatchedFinger (touch.fingerId);
					}
				}
	
				if (lastFingerId == touch.fingerId) {
					// Override the tap count with what the iPhone SDK reports if it is greater
					// This is a workaround, since the iPhone SDK does not currently track taps
					// for multiple touches
					if (touch.tapCount > tapCount)
						tapCount = touch.tapCount;
	
					if (touchPad) {
						// For a touchpad, let's just set the position directly based on distance from initial touchdown
						position.x = Mathf.Clamp ((touch.position.x - fingerDownPos.x) / (touchZone.width / 2), -1, 1);
						position.y = Mathf.Clamp ((touch.position.y - fingerDownPos.y) / (touchZone.height / 2), -1, 1);
					}
					else {
						// Change the location of the joystick graphic to match where the touch is
						position.x = (touch.position.x - guiCenter.x) / guiTouchOffset.x;
						position.y = (touch.position.y - guiCenter.y) / guiTouchOffset.y;
					}
	
					if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
						ResetJoystick ();
				}
			}
		}
	
		// Calculate the length. This involves a squareroot operation,
		// so it's slightly expensive. We re-use this length for multiple
		// things below to avoid doing the square-root more than one.
		float length = position.magnitude;
	
	
		if (length < deadZone) {
			// If the length of the vector is smaller than the deadZone radius,
			// set the position to the origin.
			position = Vector2.zero;
		}
		else {
			if (length > 1) {
				// Normalize the vector if its length was greater than 1.
				// Use the already calculated length instead of using Normalize().
				position = position / length;
			}
			else if (normalize) {
				// Normalize the vector and multiply it with the length adjusted
				// to compensate for the deadZone radius.
				// This prevents the position from snapping from zero to the deadZone radius.
				position = position / length * Mathf.InverseLerp (length, deadZone, 1);
			}
		}
	
		if (!touchPad) {
			// Change the location of the joystick graphic to match the position
			Rect pixelInset = gui.pixelInset;
			pixelInset.x = (position.x - 1) * guiTouchOffset.x + guiCenter.x;
			pixelInset.y = (position.y - 1) * guiTouchOffset.y + guiCenter.y;
			gui.pixelInset = pixelInset;
		}
	}
	
	#endif
}
