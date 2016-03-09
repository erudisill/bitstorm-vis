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

	public void DoFirstSurvey() {
		SurveyFirstThree (OriginAnchor);
	}

	public void DoNextSurvey() {
		// For now, just iterate through all the anchors, surveying the ones not surveyed yet
		foreach (GameObject g in Anchors) {
			AnchorScript a = g.GetComponent<AnchorScript>();
			if (a.IsSurveyed == false) {
				Debug.Log("Surveying " + a.gameObject.name);
				SurveySingle (a);
				return;
			}
		}

		Debug.Log ("NO NODES TO SURVEY");
	}

	private void SurveySingle(AnchorScript c0scr) {
		// Method 1: Cheap easy way. Just find the next anchor in the list 
		//           and use the origin and closest already surveyed node to the new anchor.

		AnchorScript a0scr = OriginAnchor.GetComponent<AnchorScript> ();

		// a0 is origin, b0 is closest surveyed node
		AnchorDistance a0 = c0scr.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == OriginAnchor.name);
		AnchorDistance b0 = PickCloserThan (c0scr.AnchorDistances, 0, true, true);
		Debug.Log (string.Format("a {0} {1:0.000}\t b {2} {3:0.000}", a0.anchor.gameObject.name, a0.distance, b0.anchor.gameObject.name, b0.distance));

		// Grab the lengths
		float ab = a0.anchor.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == b0.anchor.gameObject.name).distance;
		float ac = a0.distance;
		float bc = c0scr.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == b0.anchor.gameObject.name).distance;
		Debug.Log (string.Format ("ab {0:0.000}   ac {1:0.000}   bc {2:0.000}", ab, ac, bc));

		// Find the angle at a
		float angle = FindAngle (ab, ac, bc);
		Debug.Log(string.Format("Angle: {0:0.000} rad    {1:0.000} deg", angle, Mathf.Rad2Deg *angle));

		// Calculate the position of c .. rotate from ab .. two possibilities
		// Get the pos of one possibility, check it's distance from another known anchor not involved in this tri.
		// If the distance is wrong, change the sign of the angle and re-compute.

		Vector3 cPos = CalculatePosition (a0scr.SurveyMarker, b0.anchor.SurveyMarker, ac, angle);

		GameObject xGo = Anchors.Find (delegate(GameObject obj) {
			if (obj.GetComponent<AnchorScript>().IsSurveyed) {
				int id = obj.GetInstanceID();
				if (a0.anchor.gameObject.GetInstanceID() != id && b0.anchor.gameObject.GetInstanceID() != id && c0scr.gameObject.GetInstanceID() != id) {
					return true;
				}
			}
			return false;
		});

		AnchorScript xAs = xGo.GetComponent<AnchorScript> ();
		float cxDist = (cPos - xAs.SurveyMarker.transform.position).magnitude;
		AnchorDistance x0 = c0scr.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == xGo.name);
		if (Mathf.Abs(cxDist - x0.distance) > 1f) {
			// Distance is wrong, flip the angle
			Debug.Log("Flipping angle!");
			cPos = CalculatePosition (a0scr.SurveyMarker, b0.anchor.SurveyMarker, ac, angle * (-1));
		}

		c0scr.IsSurveyed = true;
		CreateMaker (cPos);
	}

	private void SurveyFirstThree(GameObject a0) {

		// Special case first survey.  We need three anchors to get the first triangle.
		// Once these anchors are set, we can use the geometry to continue adding anchors one at a time.

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
		Debug.Log(string.Format("Angle: {0:0.000} rad    {1:0.000} deg", angle, Mathf.Rad2Deg *angle));
		
		// Calculate position of a - this is the origin, so just set it
		Vector3 aPos = a0.transform.position;
		aPos.y = 1.0f;
		a0scr.IsSurveyed = true;
		a0scr.SurveyMarker = CreateMaker (aPos);
		
		// Calculate position of b - no rotational context, so just align along z axis
		Vector3 bPos = aPos + (ab * Vector3.forward);
		bPos.y = 1.0f;
		b0.anchor.IsSurveyed = true;
		b0.anchor.SurveyMarker = CreateMaker (bPos);
		
		// Calculate position of c - using the angle, extend a directional vector for this final point
		Vector3 cDir = Quaternion.AngleAxis((angle * Mathf.Rad2Deg), Vector3.up) * Vector3.forward;
		Debug.Log ("cDir: " + cDir);
		Vector3 cPos = aPos + (ac * cDir);
		cPos.y = 1.0f;
		c0.anchor.IsSurveyed = true;
		c0.anchor.SurveyMarker = CreateMaker (cPos);
	}

	private AnchorDistance PickCloserThan(List<AnchorDistance> anchorDistances, float min, bool excludeOrigin = true, bool mustBeSurveyed = false) {
		float a1range = float.MaxValue;
		AnchorDistance result = null;
		foreach (AnchorDistance ad in anchorDistances) {
			if (mustBeSurveyed == true && ad.anchor.IsSurveyed == false)
				continue;
			if (excludeOrigin == true && ad.anchor.GetInstanceID() == OriginAnchor.GetInstanceID())
				continue;
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

	private Vector3 CalculatePosition(GameObject aMarker, GameObject bMarker, float ac, float angle) {
		Vector3 dir = bMarker.transform.position - aMarker.transform.position;
		dir.Normalize ();
		Vector3 cDir = Quaternion.AngleAxis((angle * Mathf.Rad2Deg), Vector3.up) * dir;
		Vector3 cPos = aMarker.transform.position + (ac * cDir);
		cPos.y = 1.0f;	

		Debug.Log ("dir: " + dir + "  cDir: " + cDir + "  cPos: " + cPos);

		return cPos;
	}

	private GameObject CreateMaker(Vector3 pos) {
		GameObject m = Instantiate (Marker, pos, Quaternion.identity) as GameObject;
		m.transform.parent = markerGroup.transform;
		return m;
	}
	
}
