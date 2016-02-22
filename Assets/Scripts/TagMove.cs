using UnityEngine;
using System.Collections;

public class TagMove : MonoBehaviour {

	public float SmoothnessFactor = 1;

	private Vector3 startPos;
	private Vector3 targetPos;
	private float countdown;

	void Start() {
		startPos = transform.position;
		targetPos = transform.position;
	}

	void Update () {
		if (countdown > 0) {
			float t = (SmoothnessFactor - countdown) / SmoothnessFactor;
			transform.position = Vector3.Lerp (startPos, targetPos, t);
			countdown -= Time.deltaTime;
		}

	}

	public void Move(Vector3 newPos) {
		targetPos = newPos;
		startPos = transform.position;
		countdown = SmoothnessFactor;
	}
}
