using UnityEngine;
using System.Collections;

public class ZoneTrigger : MonoBehaviour {

	private Light myLight;
	private MeshRenderer myText;
    private UnisonAPI api;

	void Awake() {
		myLight = GetComponentInChildren<Light> ();
		myText = GetComponentInChildren<MeshRenderer> ();
        api = GetComponent<UnisonAPI>();
	}

	void OnTriggerEnter(Collider other) {
		myLight.enabled = true;
		myText.enabled = true;
        api.MoveInventory();
	}

	void OnTriggerExit(Collider other) {
		myLight.enabled = false;
		myText.enabled = false;
	}
}
