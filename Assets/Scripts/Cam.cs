using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
	public float followSpeed = 1, sensitivity = 1;

	
	public Transform head;


	public void ChangeSense(float newSense)
	{
		sensitivity = newSense;
	}

	private void Start()
	{
		//remove if you have a menu
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	// Update is called once per frame
	void Update()
	{

		transform.position = Vector3.Slerp(transform.position, head.position, followSpeed * Time.deltaTime);
		//transform.position = head.position;


		//Handles input
		float angleY = -Input.GetAxisRaw("Mouse Y") * sensitivity;
		//print(angleY);
		if (transform.rotation.eulerAngles.x + angleY > 90f && transform.rotation.eulerAngles.x + angleY < 180)
		{
			angleY = 90f - transform.rotation.eulerAngles.x;
		}
		//print(transform.rotation.eulerAngles.x);

		if (transform.rotation.eulerAngles.x + angleY < 270 && transform.rotation.eulerAngles.x + angleY > 180)
		{
			angleY = 270 - transform.rotation.eulerAngles.x;
		}



		float angleX = Input.GetAxisRaw("Mouse X") * sensitivity;
		transform.rotation = Quaternion.Euler(angleY + transform.rotation.eulerAngles.x, angleX + transform.rotation.eulerAngles.y, 0);


	}
}
	