using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AnchorDistance = AnchorScript.AnchorDistance;
using UnityEngine.UI;
using SimpleJSON;

public class Survey : MonoBehaviour {

	[Tooltip("UI control which controls camera zoom")]
    public GameObject ZoomControl = null;

//	public GameObject Marker;

	[Tooltip("Number of two-way ranging requests during discovery, per anchor pair.")]
	public int RangeReps = 10;

	[Tooltip("Delay in ms between two-way ranging requests during discovery.")]
	public int RangeDelayMs = 10;

	[Tooltip("Maximum time in seconds to wait for a survey result.")]
	public float RangeTimeout = 1.0f;

	[Tooltip("Default rotation of surveyed anchor topology.")]
	public float DefaultMarkerRot = 0f;

	[Tooltip("Main prefab to use for Anchors. Must have AnchorScript attached.")]
	public GameObject AnchorPrefab = null;

	[Tooltip("Minimum number of anchors to operate.")]
	public int NumAnchors = 4;

//	public List<GameObject> SurveyMarkers = new List<GameObject> ();

//	[Tooltip("Simulation or live data?")]
//	public bool Simulate = false;

	[Tooltip("Pre-populated anchors. Leave empty if using discovery.")]
	public GameObject AnchorsGroup = null;


	private TcpServer tcpServerScript = null;
    private BitStormAPI bitstormScript = null;

	public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();

	private List<GameObject> Anchors = new List<GameObject>();
	private GameObject OriginAnchor;

	private string anchorSurveyId = string.Empty;
	private string targetSurveyId = string.Empty;
	private float anchorSurveyDist = 0f;

	private float prevSliderValue = 0;

	private enum SurveyStatus {
		None,
		Running,
		Complete
	}
	private SurveyStatus surveyStatus = SurveyStatus.None;

	private bool isSimulation = false;

	// Use this for initialization
	void Start () {
		tcpServerScript = GetComponent<TcpServer> ();
        bitstormScript = GetComponent<BitStormAPI>();
	}

	void Update() {
		while (ExecuteOnMainThread.Count > 0) {
			ExecuteOnMainThread.Dequeue().Invoke();
		}
	}

	public void StartLive() {
		isSimulation = false;

		// Kill anchors group if this is live data
		if (AnchorsGroup != null) {
			GameObject.Destroy (AnchorsGroup);
			AnchorsGroup = null;
		}

		// Create the group to hold the markers
		if (AnchorsGroup == null) {
			AnchorsGroup = new GameObject ();
			AnchorsGroup.name = "Anchors";
			AnchorsGroup.transform.position = Vector3.zero;
			AnchorsGroup.transform.rotation = Quaternion.identity;
		}
	}

	public void StartSimulate() {
		isSimulation = true;

		// If we don't have an anchors group, die.
		if (AnchorsGroup == null) {
			Debug.LogError ("No AnchorsGroup available for simulation.  Create one and attach Anchors to it.");
			Application.Quit ();
		}

		foreach (Transform t in AnchorsGroup.transform) {
			GameObject g = t.gameObject;
			AnchorLabelScript labelScript = g.GetComponentInChildren<AnchorLabelScript>();
			if (labelScript != null) {
				labelScript.GetComponent<TextMesh>().text = g.name;
			}
			Anchors.Add (g);
		}
	}

    public IEnumerator DoRetrieveAnchors() {
        yield return bitstormScript.DoGetSurveyedAnchors();
        Debug.Log("surveyedAnchors = " + bitstormScript.SurveyedAnchors.Count.ToString());
        if (bitstormScript.SurveyedAnchors.Count > 0) {
            foreach (BitStormAPI.AnchorDescription ad in bitstormScript.SurveyedAnchors) {
                //SubmitAnchorDescription(ad);
                DoSubmitAnchorDescription(ad);
            }
            CenterCamera();
        }
    }


    public void DiscoverAnchors() {
        foreach (GameObject go in Anchors) {
            Destroy(go);
        }
        Anchors.Clear();
        surveyStatus = SurveyStatus.None;
        bitstormScript.DiscoverAnchors();
    }

	public void SubmitAnchor (string anchorId)
	{
		Survey.ExecuteOnMainThread.Enqueue (() => DoSubmitAnchor (anchorId));
	}

    public void SubmitAnchorDescription(BitStormAPI.AnchorDescription ad) {
        Survey.ExecuteOnMainThread.Enqueue (() => DoSubmitAnchorDescription(ad));
    }

	public void SubmitSurveyResult (string anchorId, string targetId, float dist, int errors) {
		Survey.ExecuteOnMainThread.Enqueue(() => DoSubmitSurveyResult(anchorId, targetId, dist, errors));
	}

    public void UpdateServerAnchors()
    {
        StartCoroutine(DoUpdateServerAnchors());
    }

    private IEnumerator DoUpdateServerAnchors()
    {
        foreach (GameObject go in Anchors)
        {
            yield return bitstormScript.DoUpdateAnchor(go.name, go.transform.position);
        }
    }

    private void DoSubmitAnchor(string anchorId) {
        BitStormAPI.AnchorDescription ad = new BitStormAPI.AnchorDescription();
        ad.id = anchorId;
        ad.position = new Vector3();
        ad.is_surveyed = false;
        DoSubmitAnchorDescription(ad);
    }

    private void DoSubmitAnchorDescription(BitStormAPI.AnchorDescription ad) {
        string anchorId = ad.id;

		GameObject x = Anchors.Find ((GameObject obj) => obj.name == anchorId);
        if (x != null) {
            // If anchor exists and is surveyed, update... Otherwise kill it and create a new one.
            if (ad.is_surveyed) {
                x.transform.position = ad.position;
                return;
            }

            Destroy(x);
        }

		x = Instantiate<GameObject>(AnchorPrefab);
		x.name = anchorId;
        if (ad.is_surveyed) {
            x.transform.position = ad.position;
        }
        else {
            x.transform.position = new Vector3(0 + Anchors.Count, 0.5f, 0);
        }
		x.transform.rotation = Quaternion.identity;
		x.transform.parent = AnchorsGroup.transform;

        AnchorLabelScript labelScript = x.GetComponentInChildren<AnchorLabelScript>();
        if (labelScript != null) {
            labelScript.GetComponent<TextMesh>().text = anchorId;
        }

        x.BroadcastMessage("Build", anchorId);

		Anchors.Add(x);

		OriginAnchor = Anchors [0];

        if (ad.is_surveyed == false) {
    		if (Anchors.Count == NumAnchors && surveyStatus == SurveyStatus.None) {
    			StartCoroutine (GetRanges ());
    		}
        }
	}

	private void DoSubmitSurveyResult (string anchorId, string targetId, float dist, int errors) {
		anchorSurveyId = anchorId;
		targetSurveyId = targetId;
		anchorSurveyDist = dist;
		surveyStatus = SurveyStatus.Complete;
	}



	public void DoFirstSurvey() {
		SurveyFirstThree (OriginAnchor);
		//markerGroup.transform.Rotate(new Vector3(0, DefaultMarkerRot, 0));
		AnchorsGroup.transform.Rotate(new Vector3(0, 0, 0));
	}

	public void DoNextSurvey() {
		// For now, just iterate through all the anchors, surveying the ones not surveyed yet
		foreach (GameObject g in Anchors) {
			AnchorScript a = g.GetComponent<AnchorScript>();
			if (a.IsSurveyed == false) {
				//Debug.Log("Surveying " + a.gameObject.name);
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
		//Debug.Log (string.Format("a {0} {1:0.000}\t b {2} {3:0.000}", a0.anchor.gameObject.name, a0.distance, b0.anchor.gameObject.name, b0.distance));

		// Grab the lengths
		float ab = a0.anchor.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == b0.anchor.gameObject.name).distance;
		float ac = a0.distance;
		float bc = c0scr.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == b0.anchor.gameObject.name).distance;
		//Debug.Log (string.Format ("ab {0:0.000}   ac {1:0.000}   bc {2:0.000}", ab, ac, bc));

		// Find the angle at a
		float angle = FindAngle (ab, ac, bc);
		//Debug.Log(string.Format("Angle: {0:0.000} rad    {1:0.000} deg", angle, Mathf.Rad2Deg *angle));

		// Calculate the position of c .. rotate from ab .. two possibilities
		// Get the pos of one possibility, check it's distance from another known anchor not involved in this tri.
		// If the distance is wrong, change the sign of the angle and re-compute.
//		Vector3 cPos = CalculatePosition (a0scr.SurveyMarker, b0.anchor.SurveyMarker, ac, angle);
		Vector3 cPos = CalculatePosition (a0scr.gameObject, b0.anchor.gameObject, ac, angle);

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
//		float cxDist = (cPos - xAs.SurveyMarker.transform.position).magnitude;
		float cxDist = (cPos - xAs.gameObject.transform.position).magnitude;
		AnchorDistance x0 = c0scr.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == xGo.name);

		if (Mathf.Abs(cxDist - x0.distance) > 1f) {
			// Distance is wrong, flip the angle
//			Debug.Log("Flipping angle!");
//			cPos = CalculatePosition (a0scr.SurveyMarker, b0.anchor.SurveyMarker, ac, angle * (-1));
			cPos = CalculatePosition (a0scr.gameObject, b0.anchor.gameObject, ac, angle * (-1));
		}

		c0scr.IsSurveyed = true;
//		c0scr.SurveyMarker = CreateMaker (cPos);
		c0scr.gameObject.transform.position = cPos;
//		SurveyMarkers.Add (c0scr.gameObject);
	}

	private void SurveyFirstThree(GameObject a0) {

		// Special case first survey.  We need three anchors to get the first triangle.
		// Once these anchors are set, we can use the geometry to continue adding anchors one at a time.

		AnchorScript a0scr = a0.GetComponent<AnchorScript> ();
		
		// Find the closest two nodes
		AnchorDistance b0 = PickCloserThan (a0scr.AnchorDistances, 0);
		AnchorDistance c0 = PickCloserThan (a0scr.AnchorDistances, b0.distance);
		//Debug.Log (string.Format("Closest two:\t{0} {1:0.000}\t{2} {3:0.000}", b0.anchor.gameObject.name, b0.distance, c0.anchor.gameObject.name, c0.distance));

		// Setup the lengh variables, grab the distance between b and c
		float ab = b0.distance;
		float ac = c0.distance;
		float bc = b0.anchor.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == c0.anchor.gameObject.name).distance;
		//Debug.Log (string.Format ("ab {0:0.000}   ac {1:0.000}   bc {2:0.000}", ab, ac, bc));

		// Find the angle at a
		float angle = FindAngle (ab, ac, bc);
		//Debug.Log(string.Format("Angle: {0:0.000} rad    {1:0.000} deg", angle, Mathf.Rad2Deg *angle));
		
		// Calculate position of a - this is the origin, so just set it
		Vector3 aPos = a0.transform.position;
		aPos.y = 1.0f;
		a0scr.IsSurveyed = true;
		//a0scr.SurveyMarker = CreateMaker (aPos);
		a0scr.gameObject.transform.position = aPos;
//		SurveyMarkers.Add (a0scr.gameObject);
		
		// Calculate position of b - no rotational context, so just align along z axis
		Vector3 bPos = aPos + (ab * Vector3.forward);
		bPos.y = 1.0f;
		b0.anchor.IsSurveyed = true;
//		b0.anchor.SurveyMarker = CreateMaker (bPos);
		b0.anchor.gameObject.transform.position = bPos;
//		SurveyMarkers.Add (b0.anchor.gameObject);
		
		// Calculate position of c - using the angle, extend a directional vector for this final point
		Vector3 cDir = Quaternion.AngleAxis((angle * Mathf.Rad2Deg), Vector3.up) * Vector3.forward;
		//Debug.Log ("cDir: " + cDir);
		Vector3 cPos = aPos + (ac * cDir);
		cPos.y = 1.0f;
		c0.anchor.IsSurveyed = true;
//		c0.anchor.SurveyMarker = CreateMaker (cPos);
		c0.anchor.gameObject.transform.position = cPos;
//		SurveyMarkers.Add (c0.anchor.gameObject);
	}

	private AnchorDistance PickCloserThan(List<AnchorDistance> anchorDistances, float min, bool excludeOrigin = true, bool mustBeSurveyed = false) {
		float a1range = float.MaxValue;
		AnchorDistance result = null;
		foreach (AnchorDistance ad in anchorDistances) {
			if (mustBeSurveyed == true && ad.anchor.IsSurveyed == false)
				continue;
			if (excludeOrigin == true && ad.anchor.gameObject.GetInstanceID() == OriginAnchor.GetInstanceID())
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

		//Debug.Log ("dir: " + dir + "  cDir: " + cDir + "  cPos: " + cPos);

		return cPos;
	}

//	private GameObject CreateMaker(Vector3 pos) {
//		GameObject m = Instantiate (Marker, pos, Quaternion.identity) as GameObject;
//		m.transform.parent = markerGroup.transform;
//		return m;
//	}

	IEnumerator GetRanges() {
		int i = 0;
		int j = 0;
		bool done = false;
		surveyStatus = SurveyStatus.Running;

		while (done == false) {
			done = true;
			i = 0;

			while (i < Anchors.Count) {
				GameObject go1 = Anchors [i++];	
				AnchorScript as1 = go1.GetComponent<AnchorScript> ();

				j = 0;
				while (j < Anchors.Count) {
					GameObject go2 = Anchors [j++];

					// Don't range with ourselves
					if (go1.name == go2.name)
						continue;

					// Don't range if we already have a result
					AnchorDistance adTest = as1.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == go2.name);
					if (adTest != null)
						continue;

					// Okay, range!
					anchorSurveyId = go1.name;
					targetSurveyId = go2.name;
					anchorSurveyDist = 0;
					surveyStatus = SurveyStatus.Running;
					done = false;


                    //Debug.Log(string.Format("Submitting survey request {0} to {1}", anchorSurveyId, targetSurveyId));
                    if (bitstormScript != null)
                    {
                        yield return StartCoroutine(bitstormScript.DoSurvey(anchorSurveyId, targetSurveyId, RangeReps, RangeDelayMs));
                        if (bitstormScript.LastError.Length > 0)
                        {
                            Debug.Log("Error submitting survey request.  Skip");
                        }
                        else
                        {
                            yield return new WaitForSeconds(RangeTimeout);
                            yield return StartCoroutine(bitstormScript.DoGetAnchor(anchorSurveyId));
                            if (bitstormScript.LastError.Length > 0)
                            {
                                Debug.Log("Could not fetch anchor info from server. Skip.");
                            }
                            else
                            {
                                var anchorServerInfo = JSON.Parse(bitstormScript.LastResult);
                                anchorSurveyDist = anchorServerInfo["distances"][targetSurveyId].AsFloat;
                                Debug.Log(string.Format("Survey results for {0} to {1} are {2}", anchorSurveyId, targetSurveyId, anchorSurveyDist));
                                surveyStatus = SurveyStatus.Complete;
                            }
                        }

                    }
                    else if (tcpServerScript != null)
                    {
                        tcpServerScript.SendSurveyRequest(go1.name, go2.name, RangeReps, RangeDelayMs);
                        // Wait for results
                        float startTime = Time.time;
                        while (surveyStatus == SurveyStatus.Running)
                        {
                            float elapsed = Time.time - startTime;
                            if (elapsed > RangeTimeout)
                            {
                                Debug.Log("GetRanges: timeout");
                                break;
                            }
                            yield return null;
                        }
                    }
                    else
                    {
                        Debug.Log("No BitStormAPI or TcpServer scripts attached.  Done.");
                        yield return null;
                    }



					// If we didn't time out and received a proper distance, add it to the list
					if (surveyStatus == SurveyStatus.Complete) {
						if (anchorSurveyDist != 0f) {
							AnchorDistance ad1 = new AnchorDistance ();
							ad1.anchor = go2.GetComponent<AnchorScript> ();
							ad1.distance = anchorSurveyDist;
							as1.AnchorDistances.Add (ad1);
							//Debug.Log ("Added " + ad1.anchor.gameObject.name + ":" + ad1.distance.ToString ("0.00m") + " to " + as1.gameObject.name);

							// Add the distance to the other anchor's distances as well
							adTest = ad1.anchor.AnchorDistances.Find ((AnchorDistance obj) => obj.anchor.gameObject.name == go1.name);
							if (adTest == null) {
								AnchorDistance ad2 = new AnchorDistance ();
								ad2.anchor = as1;
								ad2.distance = ad1.distance;
								ad1.anchor.AnchorDistances.Add (ad2);
								//Debug.Log ("Added [converse] " + ad2.anchor.gameObject.name + ":" + ad2.distance.ToString ("0.00m") + " to " + ad1.anchor.gameObject.name);
							}
						}
					}
				}
			}
		}
		surveyStatus = SurveyStatus.None;
		Survey.ExecuteOnMainThread.Enqueue (() => DoFirstSurvey ());
		Survey.ExecuteOnMainThread.Enqueue (() => DoNextSurvey ());
		Survey.ExecuteOnMainThread.Enqueue (() => CenterCamera ());
        Survey.ExecuteOnMainThread.Enqueue (() => PrintSurveyRanges());

	}

	public void PrintSurveyRanges() {
		string output = "";
		foreach (GameObject go in Anchors) {
			output += go.name + ":";
			AnchorScript as1 = go.GetComponent<AnchorScript>();
			foreach (AnchorDistance ad in as1.AnchorDistances) {
				output += "\t" +  ad.anchor.gameObject.name + ": " + ad.distance.ToString("0.00m");
			}
			Debug.Log(output);
			output = "";
		}
	}

	public void FlipAnchorsOnZ(){

		AnchorsGroup.transform.Rotate (new Vector3 (0, 0, 180f));

		Vector3 newPosition;
		newPosition = AnchorsGroup.transform.position;
		newPosition.y = 1;
		AnchorsGroup.transform.position = newPosition;

		CenterCamera ();
		Slider s =  GameObject.Find("Slider").GetComponent<UnityEngine.UI.Slider>();
		s.value = 0;
	}

	public void FlipAnchorsOnX(){
		
		AnchorsGroup.transform.Rotate (new Vector3 (180f, 0, 0));


		Vector3 newPosition;
		newPosition = AnchorsGroup.transform.position;
		newPosition.y = 1;
		AnchorsGroup.transform.position = newPosition;
		
		CenterCamera ();
	}
	
	public void CenterCamera(){
		Camera mainCam;
		mainCam = Camera.main;

		Vector3 groupVectors = new Vector3();
		Vector3 center;

        foreach (GameObject go in Anchors) {
			groupVectors += go.transform.position;
		}

		center = groupVectors / Anchors.Count;
		center.y = (groupVectors.magnitude * 2f) / Anchors.Count + 5;

		mainCam.transform.position = center;

        if (ZoomControl != null) {
            SlideTransformScript sts = ZoomControl.GetComponent<SlideTransformScript>();
            if (sts != null)
                sts.UpdatePosition();
        }
	}

	public void RotateMarkers(Slider slider)
	{
		float ry = slider.value - prevSliderValue;
		prevSliderValue = slider.value;
		AnchorsGroup.transform.Rotate(new Vector3(0, ry, 0));

	}

    public void ToggleLabels(Toggle toggle) {
        foreach (GameObject go in Anchors) {
            AnchorLabelScript als = go.GetComponentInChildren<AnchorLabelScript>();
            if (als != null) {
                als.Toggle(toggle.isOn);
            }
        }
    }
}
