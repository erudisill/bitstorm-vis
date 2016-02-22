using UnityEngine;
using System.Collections.Generic;

public class CPTag : MonoBehaviour {

    Queue<Vector3> positions;

	// Use this for initialization
	void Start () {
        positions = new Queue<Vector3>();
	}
	
	// Update is called once per frame
	void Update () {
        if(positions.Count >= 5) {
            transform.position = positions.Dequeue();
        }
	}

    public void AddPosition(Vector3 nextPosition) {
        print(name);
        positions.Enqueue(nextPosition);
    }
}
