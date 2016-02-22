using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/* BaseStationPacketReceiver.cs
 *
 * This Receives AnchorInfoPackets from anchor events and puts them
 * into a list. Then calculates the tag position whenever enough 
 */

public class BaseStationPacketReceiver : MonoBehaviour {

    List<AnchorInfoPacket> anchorPackets;

//    int nextBlinkID = 1;

    void Start() {
        anchorPackets = new List<AnchorInfoPacket>();
    }

    //Subscribes to AnchorBroadcast OnPacketSent event
    void OnEnable() {
        AnchorBroadcastReceiver.OnPacketSent += ReceivePacket;
    }
    void OnDisable() {
        AnchorBroadcastReceiver.OnPacketSent -= ReceivePacket;
    }

    public void ReceivePacket(AnchorInfoPacket packet) {
        anchorPackets.Add(packet);
        print("New Packet");
    }

    void PrintPacketList() {
        if (anchorPackets.Count == 0)
            print("No packets");
        using (StreamWriter sw = new StreamWriter("testfile.txt")) {
            foreach (AnchorInfoPacket packet in anchorPackets) {
                sw.WriteLine(packet);
            }
        }
    }
}