using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AnchorDistance = AnchorScript.AnchorDistance;

public class Survey : MonoBehaviour {

	public GameObject Marker;
	public List<GameObject> Anchors;

	// Use this for initialization
	void Start () {
		// Create matrix of ranges .. Normally, this would come in from the field
		// but for simulation sake, just calculate magnitude between each anchor.

		foreach (GameObject g1 in Anchors) {
			AnchorScript g1as = g1.GetComponent<AnchorScript>();
			foreach (GameObject g2 in Anchors) { 
				//TODO: Use sqrMagnitude for quicker math later
				float dist = (g1.transform.position - g2.transform.position).magnitude;

				g1as.AnchorRanges.Add(dist);

				AnchorDistance ad = new AnchorDistance();
				ad.anchor = g2.GetComponent<AnchorScript>();
				ad.distance = dist;
				ad.isSurveyed = false;
				g1as.AnchorDistances.Add(ad);
			}
		}

		string line = "";

		foreach (GameObject g1 in Anchors) {
			AnchorScript g1as = g1.GetComponent<AnchorScript>();
			foreach (float r in g1as.AnchorRanges) {
				line += string.Format("{0:0.000}\t", r);
			}
			line += "\r\n";
		}

		Debug.Log (line);
	}

	public void DoSurvey() {
		int b0ndx, c0ndx;
		
		GameObject a0 = Anchors [0];
		
		AnchorScript a0scr = a0.GetComponent<AnchorScript> ();
		
		
		// Find the closest two nodes
		PickClosestTwo (a0scr, out b0ndx, out c0ndx);
		GameObject b0 = Anchors [b0ndx];
		GameObject c0 = Anchors [c0ndx];
		Debug.Log (string.Format("Closest two:\t{0} {1:0.000}\t{2} {3:0.000}", b0.name, a0scr.AnchorRanges [b0ndx], c0.name, a0scr.AnchorRanges [c0ndx]));
		
		// Setup the lengh variables, grab the distance between b and c
		float ab = a0scr.AnchorRanges [b0ndx];
		float ac = a0scr.AnchorRanges [c0ndx];
		float bc = Anchors [b0ndx].GetComponent<AnchorScript> ().AnchorRanges [c0ndx];
		
		Debug.Log (string.Format ("ab {0:0.000}   ac {1:0.000}   bc {2:0.000}", ab, ac, bc));
		
		// Find the angle at a
		float angle = FindAngle (ab, ac, bc);
		Debug.Log(string.Format("Angle: {0:0.000}rad  {1:0.000}", angle, Mathf.Rad2Deg *angle));
		
		// Calculate position of a
		Vector3 aPos = a0.transform.position;
		aPos.y = 1.0f;
		
		// Calculate position of b
		Vector3 bPos = aPos + (ab * Vector3.right);
		bPos.y = 1.0f;
		
		// Calculate position of c
		Vector3 cDir = Quaternion.AngleAxis((angle * Mathf.Rad2Deg) + 90f, Vector3.up) * Vector3.forward;
		Debug.Log ("cDir: " + cDir);
		Vector3 cPos = aPos + (ac * cDir);
		cPos.y = 1.0f;
		
		GameObject markerGroup = new GameObject ();
		markerGroup.name = "Markers";
		markerGroup.transform.position = aPos;
		markerGroup.transform.rotation = Quaternion.identity;
		
		GameObject aMarker = Instantiate (Marker, aPos, Quaternion.identity) as GameObject;
		aMarker.transform.parent = markerGroup.transform;
		GameObject bMarker = Instantiate (Marker, bPos, Quaternion.identity) as GameObject;
		bMarker.transform.parent = markerGroup.transform;
		GameObject cMarker = Instantiate (Marker, cPos, Quaternion.identity) as GameObject;
		cMarker.transform.parent = markerGroup.transform;
		
	}

	public void DoSurvey2() {
		int b0ndx, c0ndx;

		GameObject a0 = Anchors [0];

		AnchorScript a0scr = a0.GetComponent<AnchorScript> ();


		// Find the closest two nodes
		AnchorDistance b0 = PickCloserThanAnchorDistance (a0scr.AnchorDistances, 0);
		AnchorDistance c0 = PickCloserThanAnchorDistance (a0scr.AnchorDistances, b0.distance);
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

		// Calculate position of b
		Vector3 bPos = aPos + (ab * Vector3.right);
		bPos.y = 1.0f;

		// Calculate position of c
		Vector3 cDir = Quaternion.AngleAxis((angle * Mathf.Rad2Deg) + 90f, Vector3.up) * Vector3.forward;
		Debug.Log ("cDir: " + cDir);
		Vector3 cPos = aPos + (ac * cDir);
		cPos.y = 1.0f;

		GameObject markerGroup = new GameObject ();
		markerGroup.name = "Markers";
		markerGroup.transform.position = aPos;
		markerGroup.transform.rotation = Quaternion.identity;

		GameObject aMarker = Instantiate (Marker, aPos, Quaternion.identity) as GameObject;
		aMarker.transform.parent = markerGroup.transform;
		GameObject bMarker = Instantiate (Marker, bPos, Quaternion.identity) as GameObject;
		bMarker.transform.parent = markerGroup.transform;
		GameObject cMarker = Instantiate (Marker, cPos, Quaternion.identity) as GameObject;
		cMarker.transform.parent = markerGroup.transform;

	}

	private void PickClosestTwo(AnchorScript a0as, out int a1, out int a2) {
		a1 = PickCloserThan (a0as.AnchorRanges, 0);
		float a1range = a0as.AnchorRanges [a1];
		a2 = PickCloserThan (a0as.AnchorRanges, a1range);
	}

	private int PickCloserThan(List<float> anchorRanges, float min) {
		float a1range = float.MaxValue;
		int i = 0;
		int j = 0;
		foreach (float f in anchorRanges) {
			if (f > min && f < a1range) {
				j = i;
				a1range = f;
			}
			i++;
		}
		return j;
	}

	private AnchorDistance PickCloserThanAnchorDistance(List<AnchorDistance> anchorDistances, float min) {
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
	
}
