using UnityEngine;
using System.Collections;

public abstract class BaseCharacterController : JoystickController {

	public abstract bool IsMoving();
	public abstract void SlowDownMovingSpeed ();
	public abstract void NormalMovingSpeed ();
	public abstract void Shoot();
	public abstract bool IsShooting();
	public abstract void Control(bool control);
	public abstract void MoveTo (Vector3 target);
}
