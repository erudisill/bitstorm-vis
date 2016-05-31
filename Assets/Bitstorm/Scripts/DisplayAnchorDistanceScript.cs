using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//This just prints out the distances between the tag and each anchor.
//I am using to to provide me with data to test the LocationEngine with
public class DisplayAnchorDistanceScript : MonoBehaviour {
	void Start () {
        var anchors = GameObject.FindGameObjectsWithTag(TagDefs.ANCHOR_TAG);

        foreach(var anchor in anchors) {
            var position = anchor.transform.position;
            var anchorName = anchor.name;

            print("Anchor " + anchorName + ":" + (Vector3.Distance(transform.position, position).ToString()));
        }
	}
}
