using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AnchorDistance = AnchorScript.AnchorDistance;

public class Survey : MonoBehaviour {

	public GameObject Marker;
	public GameObject OriginAnchor;
	public List<GameObject> Anchors;

	private GameObject markerGroup = null;

	// Use this for initialization
	void Start () {
		// Create matrix of ranges .. Normally, this would come in from the field
		// but for simulation sake, just calculate magnitude between each anchor.

		foreach (GameObject g1 in Anchors) {
			AnchorScript g1as = g1.GetComponent<AnchorScript>();
			foreach (GameObject g2 in Anchors) { 
				//TODO: Use sqrMagnitude for quicker math later
				float dist = (g1.transform.position - g2.transform.position).magnitude;

				AnchorDistance ad = new AnchorDistance();
				ad.anchor = g2.GetComponent<AnchorScript>();
				ad.distance = dist;
				g1as.AnchorDistances.Add(ad);

				ad.anchor.IsSurveyed = false;
			}
		}

		// Create the group to hold the markers
		markerGroup = new GameObject ();
		markerGroup.name = "Markers";
		markerGroup.transform.position = OriginAnchor.transform.position;;
		markerGroup.transform.rotation = Quaternion.identity;
	}

	public void DoSurvey() {
		GameObject a0 = OriginAnchor;

		AnchorScript a0scr = a0.GetComponent<AnchorScript> ();

		// Find the closest two nodes
		AnchorDistance b0 = PickCloserThan (a0scr.AnchorDistances, 0);
		AnchorDistance c0 = PickCloserThan (a0scr.AnchorDistances, b0.distance);
		Debug.Log (string.Format("Closest two:\t{0} {1:0.000}\t{2} {3:0.000}", b0.anchor.gameObject.name, b0.distance, c0.anchor.gameObject.name, c0.distance));



		// Setup the lengh variables, grab the distance between b and c
		float ab = b0.distance;
		float ac = c0.distance;
		float bc = b0.anchor.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == c0.anchor.gameObject.name).distance;
		Debug.Log (string.Format ("ab {0:0.000}   ac {1:0.000}   bc {2:0.000}", ab, ac, bc));



		// Find the angle at a
		float angle = FindAngle (ab, ac, bc);
		Debug.Log(string.Format("Angle: {0:0.000}rad  {1:0.000}", angle, Mathf.Rad2Deg *angle));



		// Calculate position of a
		Vector3 aPos = a0.transform.position;
		aPos.y = 1.0f;
		a0scr.IsSurveyed = true;
		CreateMaker (aPos);

		// Calculate position of b
		Vector3 bPos = aPos + (ab * Vector3.right);
		bPos.y = 1.0f;
		b0.anchor.IsSurveyed = true;
		CreateMaker (bPos);

		// Calculate position of c
		Vector3 cDir = Quaternion.AngleAxis((angle * Mathf.Rad2Deg) + 90f, Vector3.up) * Vector3.forward;
		Debug.Log ("cDir: " + cDir);
		Vector3 cPos = aPos + (ac * cDir);
		cPos.y = 1.0f;
		c0.anchor.IsSurveyed = true;
		CreateMaker (cPos);

	}

	private AnchorDistance PickCloserThan(List<AnchorDistance> anchorDistances, float min) {
		float a1range = float.MaxValue;
		AnchorDistance result = null;
		foreach (AnchorDistance ad in anchorDistances) {
			float f = ad.distance;
			if (f > min && f < a1range) {
				a1range = f;
				result = ad;
			}
		}
		return result;
	}

	private float FindAngle(float a, float b, float c) {
		float cosA = ( (a*a) + (b*b) - (c*c) ) / (2*a*b) ;
		float angle = Mathf.Acos(cosA);
		return angle;
	}

	private void CreateMaker(Vector3 pos) {
		GameObject m = Instantiate (Marker, pos, Quaternion.identity) as GameObject;
		m.transform.parent = markerGroup.transform;
	}
	
}
