using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TagMove : MonoBehaviour {

	public float LerpDuration = 1;


	public int sampleSize = 5;


	private Vector3 startPos;
	private Vector3 targetPos;
	private float countdown;


	private Queue<Vector3> samples = new Queue<Vector3>();

	//Experimentally the natural log function was found to provide the best representation of the
	//tag's actual position.
	private Func<float, float> averageFunc = (x => (float)Math.Log(x, Math.E));

	void Start() {
		startPos = transform.position;
		targetPos = transform.position;
	}

	void Update () {
        if (countdown > 0) {
			float t = (LerpDuration - countdown) / LerpDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            countdown -= Time.deltaTime;
        }
    }

	public void Move(Vector3 newPos) {
        targetPos = newPos;
        startPos = transform.position;
		countdown = LerpDuration;
	}


	public void PushPosition(Vector3 pos) {
		samples.Enqueue(pos);
		Vector3 avgPos = FindWeightedAverage (pos);
		Move (avgPos);
	}

	/**
     * Calculates a weighted average based on the current averageFunction. This smooths
     * the movement of the tag by averaging the distance to the anchors.
     */
	private Vector3 FindWeightedAverage(Vector3 defaultValue) {
		if (samples.Count >= sampleSize) {
			float y = defaultValue.y;
			Vector3 average = Vector3.zero;
			Vector3[] samplesArray = samples.ToArray();

			List<float> averageScaleNumbers = GenerateAverageRange(sampleSize, averageFunc);
			for (int i = 0; i < samplesArray.Length; i++) {
				average += samplesArray[i] * averageScaleNumbers[i];
			}
			samples.Dequeue();
			average.y = y;
			return average;
		} else {
			return defaultValue;
		}
	}

	/**
     * To generate a weighted average a function is applied over the range [1,n]. Each of
     * these range values is divided by the sum of the range. This provides set of n numbers
     * less than 1 whose sum is 1.
     */
	private List<float> GenerateAverageRange(int n, Func<float, float> func) {
		List<float> range = new List<float>();

		float sum = 0;
		for(int i = 1; i <= n; i++) {
			sum += func(i);
		}
		for(int i = 1; i <= n; i++) {
			range.Add(func(i) / sum);
		}

		return range;
	}
}
