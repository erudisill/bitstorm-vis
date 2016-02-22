using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Vectrosity;

public class LocationEngine2D : MonoBehaviour { 

	TcpServer tcpServer; 

	public bool UseFilteredDistance = false;

    public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();

    // Use this for initialization
    void Awake() {
		tcpServer = GetComponent<TcpServer> ();
    }

    void Update() {
        while (ExecuteOnMainThread.Count > 0) {
            ExecuteOnMainThread.Dequeue().Invoke();
        }
    }

    void OnEnable() {
		if (tcpServer != null) {
            print("LocationEngine2D: Adding TcpServer '" + tcpServer.gameObject.name + "'");
            tcpServer.OnPacketSent += PacketEventHandler;
        }
    }

    void OnDisable() {
		if (tcpServer != null) {
			print("LocationEngine2D: Removing TcpServer '" + tcpServer.gameObject.name + "'");
			tcpServer.OnPacketSent -= PacketEventHandler;
        }
    }

    void PacketEventHandler(string tagID, List<TcpServer.AnchorRange> ranges) {
        LocationEngine2D.ExecuteOnMainThread.Enqueue(() => ProcessRangeReport(tagID, ranges));
    }

    void ProcessRangeReport(string tagId, List<TcpServer.AnchorRange> ranges) {

		// Build and process all packets based on ranges
        List<AnchorInfoPacket> packets = new List<AnchorInfoPacket>();
        foreach (TcpServer.AnchorRange r in ranges) {
            float d = (float)r.dist;
            AnchorInfoPacket packet = new AnchorInfoPacket();
            packet.anchorID = r.id;
            packet.blinkID = 1;
            packet.tagID = tagId;
            packet.timeDelay = d / Constants.SPEED_OF_LIGHT;
            packet.distance = d;
            packet.anchorLocation = GameObject.FindGameObjectsWithTag(TagDefs.ANCHOR_TAG).First(obj => obj.name == packet.anchorID).transform.position;

			GameObject anchorObject = (GameObject)GameObject.Find(packet.anchorID);
			packet.anchorScript = anchorObject.GetComponent<AnchorScript>();
			packet.anchorScript.ProcessPacket(packet);

            packets.Add(packet);
        }

		// Gets the 3 closest packets
		List<AnchorInfoPacket> closestPackets = new List<AnchorInfoPacket> ();
		packets.Sort((a, b) => a.distance.CompareTo(b.distance));
		closestPackets = packets.Take(3).ToList<AnchorInfoPacket>();

		// Toggle IsRanging 
		packets.ForEach((AnchorInfoPacket obj) => obj.anchorScript.IsRanging = false);
		closestPackets.ForEach((AnchorInfoPacket obj) => obj.anchorScript.IsRanging = true);

		// Update the position of the tag
		UpdateTag(tagId, closestPackets);
    }

    private class TrilaterationPoint {
        public float distance;
        public Vector3 point;

        public TrilaterationPoint(float distance, Vector3 point) {
            this.distance = distance;
            this.point = point;
        }
    }

    private void UpdateTag(string tagID, List<AnchorInfoPacket> packets) {
        if(packets.Count < 3)
            throw new ArgumentException("Must have at least 3 packets. " + packets.Count.ToString() + "were provided.");

		Vector3 newTagPosition = CalculateTagPositionByPackets(packets);

        GameObject tag = GameObject.FindGameObjectsWithTag(TagDefs.TAG_TAG).Where(t => tagID == t.name).First();

//        CPTagPositioner tagScript = (CPTagPositioner)tag.GetComponent(typeof(CPTagPositioner));
//        if(tagScript != null) {
//            tagScript.AddPosition(newTagPosition);
//        }

		TagMove tagScript = tag.GetComponent<TagMove> ();
		if(tagScript != null) {
			tagScript.Move(newTagPosition);
		}

	}


    /**
       Wrapper that converts a list of anchorInfoPackets to Trilateration points.
    */
    private Vector3 CalculateTagPositionByPackets(List<AnchorInfoPacket> packets) {
        if(packets.Count < 3)
            throw new ArgumentException("Must have at least 3 packets. " + packets.Count.ToString() + "were provided.");

        List<TrilaterationPoint> points = new List<TrilaterationPoint>();
        for(int i = 0; i < 3; ++i) {
			if (UseFilteredDistance == true) {
				points.Add(new TrilaterationPoint(packets[i].distance_filtered, packets[i].anchorLocation));
			} else {
				points.Add(new TrilaterationPoint(packets[i].distance, packets[i].anchorLocation));
			}
		}

		return CalculateTagPositionByPoints(points);
    }

    /**
        Using math from the internet we find the position of the tag based on 3 points.
    */
    private Vector3 CalculateTagPositionByPoints(List<TrilaterationPoint> points) {

        Vector3 p1 = points[0].point;
        Vector3 p2 = points[1].point;
        Vector3 p3 = points[2].point;

        float d1 = points[0].distance;
        float d2 = points[1].distance;
        float d3 = points[2].distance;

        var ex = (p2 - p1) / ((p2 - p1).magnitude);

        var i = Vector3.Dot(ex, (p3 - p1));

        var a = (p3 - p1) - (ex * i);

        var ey = a / (a.magnitude);

//        var ez = Vector3.Cross(ex, ey);

        var d = (p2 - p1).magnitude;

        var j = Vector3.Dot(ey, (p3 - p1));

        var x = (sqr(d1) - sqr(d2) + sqr(d)) / (2*d);

        var y = (sqr(d1) - sqr(d3) + sqr(i) + sqr(j)) / (2 * j) - (i / j) * x;

//        var z = Mathf.Sqrt(sqr(d1) - sqr(x) - sqr(y));

        var middle = p1 + ((ex * x) + (ey * y));

        return middle;
    }

    private float sqr(float num) {
        return num * num;
    }
}