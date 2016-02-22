using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnchorScript : MonoBehaviour {

	public bool DisplayRange = false;
	public bool IsRanging = false;
	public float RangeOffset = 0.0f;
	public GameObject RangeText;

	public int SampleSize = 5;
	
	private Queue<float> samples = new Queue<float>();
	
	private AnchorInfoPacket lastPacket;


	public void Update() {
		RangeText.SetActive (DisplayRange);
	}


	public void ProcessPacket(AnchorInfoPacket packet) {

		// Tune value based on offset
		packet.distance += RangeOffset;

		// Record last packet
		lastPacket = packet;

		// Basic averaging
		samples.Enqueue (packet.distance);
		if (samples.Count >= SampleSize) {
			float junk = samples.Dequeue();
			float accum = 0;
			float[] samplesArray = samples.ToArray();
			for (int i=0;i<samplesArray.Length;i++) {
				accum += samplesArray[i];
			}
			float avg = accum / samplesArray.Length;
			packet.distance_filtered = avg;
		} else {
			packet.distance_filtered = packet.distance;
		}

	}
	
	public AnchorInfoPacket GetLastAnchorInfoPacket() {
		return lastPacket;
	}
}
