using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Simulation : MonoBehaviour {

	[Tooltip("GameObject containing the Anchors in the scene.")]
	public GameObject AnchorsGroup = null;

	[Tooltip("Prefab for tag.")]
	public GameObject TagPrefab = null;

	[Tooltip("Tag ID to be used.")]
	public string TagId = "7777";

	[Tooltip("Delay in seconds between steps.")]
	public float StepDelay = 1f;

	private GameObject tag = null;

	private List<Vector3> path = new List<Vector3> ();
	private int pathIndex = 0;

	public void StartSimulation() {

		// Get bounds
		List<Transform> transforms = new List<Transform> ();
		foreach (Transform t in AnchorsGroup.transform)
			transforms.Add (t);
		Bounds b = new Bounds(transforms[0].position, Vector3.zero);
		for (int i = 1; i < transforms.Count; i++)
			b.Encapsulate (transforms [i].position);

		// Create the tag
		tag = GameObject.Find (TagId);
		if (tag == null) {
			tag = Instantiate (TagPrefab, new Vector3 (0f, 0.5f, 0f), Quaternion.identity) as GameObject;
		}

		CreatePathEllipse(b);
		MoveToNextStep ();

	}

	public void CreatePathEllipse(Bounds b) {
		float width = b.extents.x;
		float height = b.extents.z;
		float xc = b.center.x;
		float yc = b.center.z;	

		for (float i = 0; i < Mathf.PI * 2f; i += (Mathf.PI * 2f) / 24f) {
			float x = xc + (width * Mathf.Cos (i));
			float y = yc + (height * Mathf.Sin (i));
			Vector3 pos = new Vector3 (x, 0.5f, y);
			path.Add (pos);
			//Instantiate (TagPrefab, pos, Quaternion.identity);
		}
	}

	public void MoveToNextStep() {
		pathIndex++;
		if (pathIndex >= path.Count)
			pathIndex = 0;
		tag.transform.position = path [pathIndex];
		Invoke ("MoveToNextStep", StepDelay);
	}

}
