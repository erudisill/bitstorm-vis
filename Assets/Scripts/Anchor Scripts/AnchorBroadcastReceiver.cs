using UnityEngine;

/* 
 * AnchorBroadcastReceiver.cs
 * 
 * This script is attached to an Anchor prefab and receives tag
 * broadcasts via a .NET event. Each received blink is then
 * packaged with additional anchor information and then send via
 * another .NET event to Base Stations
 */

public class AnchorBroadcastReceiver : MonoBehaviour {

    //Speed of Light in m/s in air
    public float distanceToTag { get; set; }

    //Event subscribed to by BaseStation to receieve packets
    public delegate void PacketEvent(AnchorInfoPacket packet);
    public static event PacketEvent OnPacketSent;

    //Subscribes to TagBroadcaster's OnBroadcast event
    void OnEnable() { TagBroadcaster.OnBroadcast += ReceiveBroadcast; }
    void OnDisable() { TagBroadcaster.OnBroadcast -= ReceiveBroadcast; }

    //The distance between the receiver and the broadcaster is used to avoid
    //limitations with unity that would make propagating a wave at the speed of
    //light inaccurate.
    float GetTimeDelay(Vector3 senderPosition) {
        distanceToTag = Vector3.Distance(transform.position, senderPosition);
        return distanceToTag / Constants.SPEED_OF_LIGHT;
    }

    //Method to be called when broadcasts are received
    public void ReceiveBroadcast(TagPacket receivedPacket) {
        print("Received Broadcast");
        Vector3 senderPosition = receivedPacket.position;
        float timeDelay = GetTimeDelay(senderPosition);

        distanceToTag = Vector3.Distance(senderPosition, transform.position);

        AnchorInfoPacket packetToSend = new AnchorInfoPacket(transform.gameObject.name,
                                                              receivedPacket.tagID,
                                                              receivedPacket.blinkID,
                                                              transform.position,        //position of this anchor
                                                              senderPosition,            //position of tag
                                                              timeDelay);                //TDOA value
        OnPacketSent(packetToSend);
    }
}

public class AnchorInfoPacket : System.IComparable<AnchorInfoPacket> {
    public string anchorID;
    public Vector3 anchorLocation;
    public float timeDelay;         //TDOA value
    public int blinkID;
    public Vector3 tagPosition;
    public string tagID;
    public float distance;
	public float distance_filtered;
	public AnchorScript anchorScript;

    //Must be defined to implement System.IComparable
    //Definition by comparison of blinkID was chosen
    //because AnchorInfoPackets are typically
    //sorted in this manner.
    public int CompareTo(AnchorInfoPacket that) {
        if (this.blinkID > that.blinkID) return -1;
        if (this.blinkID == that.blinkID) return 0;
        return 1;
    }

    public AnchorInfoPacket() {
        anchorID = "";
        anchorLocation = new Vector3();
        timeDelay = 0.0f;
        blinkID = 0;
        tagPosition = new Vector3();
        tagID = "";
    }

    public AnchorInfoPacket(string anchorID,
                            string tagID,
                            int blinkID,
                            Vector3 anchorPosition,
                            Vector3 tagPosition,
                            float delay) {
        this.anchorID = anchorID;
        this.blinkID = blinkID;
        this.anchorLocation = anchorPosition;
        this.timeDelay = delay;
        this.tagPosition = tagPosition;
        this.tagID = tagID;
    }


    public override string ToString() {
        return (string.Format("ID:{0}\r\nAnchor: {1}\r\nAnchor Location: ({2}, {3}, {4})\r\nTime Delay: {5}\r\nTag Location: ({6}, {7}, {8})\r\n",
                               blinkID,
                               anchorID,
                               anchorLocation.x,
                               anchorLocation.y,
                               anchorLocation.z,
                               timeDelay,
                               tagPosition.x,
                               tagPosition.y,
                               tagPosition.z));
    }
}