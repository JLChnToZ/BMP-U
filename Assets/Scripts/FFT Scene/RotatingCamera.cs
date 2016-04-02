using UnityEngine;
public class RotatingCamera : MonoBehaviour {

	public float rotationSpeed;
	public float distance = 10;
	public float yawAngle = 45;
	public Transform target;
	
	float currentAngle;
	
	void Update () {
		Vector2 offset = new Vector2(Mathf.Cos(yawAngle * Mathf.Deg2Rad), Mathf.Sin(yawAngle * Mathf.Deg2Rad)) * distance;
		currentAngle = (currentAngle + rotationSpeed * Time.deltaTime) % 360;
		transform.position = target.position + new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad) * offset.x, offset.y, Mathf.Sin(currentAngle * Mathf.Deg2Rad) * offset.x);
		transform.LookAt(target);
	}
}
