using UnityEngine;
using System.Collections;

public class BitstormConnector : MonoBehaviour {

	public void Start() {
		Application.ExternalCall ("bitstorm_ready", "blah");
	}

	public void PositionEvent(string xyz) {
		string[] parts = xyz.Split (':'); 
		Vector3 v = new Vector3 ();
		v.x = float.Parse (parts [0]);
		v.y = float.Parse (parts [1]);
		v.z = float.Parse (parts [2]);
		Debug.Log ("PositionEvent: " + v);

		gameObject.transform.position = v;
	}
}
