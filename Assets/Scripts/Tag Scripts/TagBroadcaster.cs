using UnityEngine;


/* TagBroadcaster can be attached to any game object
 * and that object will then act as a tag. Anchor prefabs
 * will automatically accept tag broadcasts with no extra setup.
 */
public class TagBroadcaster : MonoBehaviour {

    //Time between broadcasts in seconds
    public float broadcastInterval;

    //Event subscribed to by anchors to pick up broadcasts
    public delegate void BroadcastEvent(TagPacket receievedPacket);
    public static event BroadcastEvent OnBroadcast;

    private static int blinkID = 1;

    void Start() {
        //Repeatedly calls "Broadcast" asynchronously
        InvokeRepeating("Broadcast", broadcastInterval, broadcastInterval);

    }

    //This emulates a tag "blip" within Unity. 
    void Broadcast() {
        if (OnBroadcast != null) {
            TagPacket packet = new TagPacket(transform.position, GetBlinkID(), name);
            OnBroadcast(packet);
        }
    }

    static int GetBlinkID() {
        return blinkID++;
    }
}

//TagPacket holds any information that needs to be sent from a tag
//to anchors in each of the tags "blinks"
public class TagPacket {
    public Vector3 position;
    public int blinkID;
    public string tagID;

    public TagPacket(Vector3 position, int blinkID, string tagID) {
        this.position = position;
        this.blinkID = blinkID;
        this.tagID = tagID;
    }
}
