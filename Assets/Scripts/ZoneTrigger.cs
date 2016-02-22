using UnityEngine;
using System.Collections;

public class ZoneTrigger : MonoBehaviour {

	private Light myLight;
	private MeshRenderer myText;
	 
	void Awake() {
		myLight = GetComponentInChildren<Light> ();
		myText = GetComponentInChildren<MeshRenderer> ();
	}

	void OnTriggerEnter(Collider other) {
		myLight.enabled = true;
		myText.enabled = true;
	}

	void OnTriggerExit(Collider other) {
		myLight.enabled = false;
		myText.enabled = false;
	}
}
