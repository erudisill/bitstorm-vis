using UnityEngine;
using System.Collections;

public class RangeTextScript : MonoBehaviour {

	public GameObject Anchor;
	public GameObject Tag;

	private TextMesh textMesh;
	private AnchorScript anchorScript;

	void Start() {
		textMesh = GetComponent<TextMesh> ();
		anchorScript = Anchor.GetComponent<AnchorScript> ();
	}

	void Update () {
		AnchorInfoPacket packet = anchorScript.GetLastAnchorInfoPacket ();

		Vector3 ab = Tag.transform.position - Anchor.transform.position;
		float dist = ab.magnitude;
		ab.Normalize();
		Vector3 pos = Anchor.transform.position + ((dist * 0.5f) * ab);
		pos.y = 0.5f;
		textMesh.transform.position = pos;

		if (packet != null) {
			textMesh.text = packet.distance_filtered.ToString ("#0.00 m");
		} else {
			textMesh.text = "0.00 m";
		}


	}
}
