using UnityEngine;
using System.Collections;

public abstract class JoystickController : MonoBehaviour {

	public GameObject joystickPrefab;
	protected Joystick joystickLeft;
	protected Joystick joystickRight;
	protected GameObject joystickRightGO;
	protected Rect joystickLeftRect;
	protected Rect joystickRightRect;
	protected void InitJoystick()
	{
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
		if (joystickPrefab) {
			// Create left joystick
			GameObject joystickLeftGO = Instantiate (joystickPrefab) as GameObject;
			joystickLeftGO.name = "Joystick Left";
			joystickLeft = joystickLeftGO.GetComponent<Joystick> ();
			
			GUITexture guiTex = joystickLeftGO.GetComponent<GUITexture> ();
			joystickLeftRect = new Rect(0, 0,//Screen.height - guiTex.pixelInset.y - guiTex.pixelInset.height,
				guiTex.pixelInset.x + guiTex.pixelInset.width, guiTex.pixelInset.y + guiTex.pixelInset.height);
			
			// Create right joystick
			joystickRightGO = Instantiate (joystickPrefab) as GameObject;
			joystickRightGO.name = "Joystick Right";
			joystickRight = joystickRightGO.GetComponent<Joystick> ();	
			joystickRight.touchPad = true;
			GUITexture guiTexRight = joystickRightGO.GetComponent<GUITexture> ();
			
			string texture = "Common/Joystick/ShootButton";//"Textures/ThumbStickPad";
			Texture2D inputTexture = Resources.Load(texture, typeof(Texture2D)) as Texture2D;
			guiTexRight.texture = inputTexture;
		}		
		#endif
	}
	protected void resetJoystickPos()
	{
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
		// Move to right side of screen
		GUITexture guiTex = joystickRightGO.GetComponent<GUITexture> ();
		Rect insetRect = guiTex.pixelInset;
		insetRect.x = Screen.width - guiTex.pixelInset.x - guiTex.pixelInset.width;
		guiTex.pixelInset = insetRect;
		
		joystickRightRect = new Rect(guiTex.pixelInset.x, 0,
				guiTex.pixelInset.width, guiTex.pixelInset.y + guiTex.pixelInset.height);
			
		#endif	
	}
	
	
	public bool JoystickShoot(){
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY		
		return joystickRight.IsFingerDown();
		#else
		return false;
		#endif
	}
	public bool IsLeftJoystickArea(Vector2 touch){
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY		
		return touch.x >= joystickLeftRect.x && touch.x <= joystickLeftRect.x + joystickLeftRect.width && 
			touch.y >= joystickLeftRect.y && touch.y <= joystickLeftRect.y + joystickLeftRect.height;//joystickLeft.IsFingerDown();
		#else
		return false;
		#endif
	}
	public bool IsRightJoystickArea(Vector2 touch){
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY		
		return touch.x >= joystickRightRect.x && touch.x <= joystickRightRect.x + joystickRightRect.width && 
			touch.y >= joystickRightRect.y && touch.y <= joystickRightRect.y + joystickRightRect.height;//joystickLeft.IsFingerDown();
		#else
		return false;
		#endif
	}
}
