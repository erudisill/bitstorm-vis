using UnityEngine;
using System.Collections;

public class Bitstorm : MonoBehaviour {

	public delegate void PacketPositionEvent (string tagID,Vector3 pos);

	[Tooltip("Run local simulation?")]
	public bool Simulate = false;

	private Survey surveyScript = null;
	private LocationEngine2D engineScript = null;
	private TcpClient clientScript = null;
	private BitStormAPI apiScript = null;
	private Simulation simScript = null;

	// Use this for initialization
	void Start () {
		surveyScript = GetComponent<Survey> () as Survey;
		engineScript = GetComponent<LocationEngine2D> () as LocationEngine2D;
		clientScript = GetComponent<TcpClient> () as TcpClient;
		apiScript = GetComponent<BitStormAPI> () as BitStormAPI;

		if (Simulate) {
			Debug.Log ("Running simulation .. Starting in 2 seconds.");
			simScript = GetComponent<Simulation> () as Simulation;
			Invoke ("LaunchSimulation", 2);
		} else {
			Debug.Log ("Running LIVE .. Starting in 2 seconds.");
			Invoke ("LaunchLive", 2);
		}
	}

	void LaunchSimulation() {
		surveyScript.Simulate ();
		simScript.StartSimulation (engineScript.ProcessPosition);
	}

	void LaunchLive() {
		clientScript.StartClient ();
		StartCoroutine(surveyScript.DoRetrieveAnchors());
	}

}
