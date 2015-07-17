using UnityEngine;
using System.Collections;

public class GuiTextSet : MonoBehaviour {

	// Use this for initialization
	void Start () {
		#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
		guiText.text = "\nLeft Joystick or Touch terrain: Move\nRight Joystick or Slide finger: Shoot hook";
		#else
		guiText.text = "\nArrow Keys or Click terrain: Move\nSpace Key or Press left mouse button and slide: Shoot hook";
		#endif
	}

}
