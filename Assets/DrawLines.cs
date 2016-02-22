using UnityEngine;
using System.Collections;

public class DrawLines : MonoBehaviour
{

	// http://gamedev.stackexchange.com/questions/96964/how-to-correctly-draw-a-line-in-unity


	// Fill/drag these in from the editor
	
	// Choose the Unlit/Color shader in the Material Settings
	// You can change that color, to change the color of the connecting lines

	public bool ShowAllRanges = false;
	public bool HideAllRanges = false;

	public Material lineMatRanging;
	public Material lineMatNotRanging;
	public GameObject mainPoint;
//	public GameObject[] points;
	public AnchorScript[] points;

	// Connect all of the `points` to the `mainPoint`
	void DrawConnectingLines ()
	{
		if (mainPoint && points.Length > 0) {
			// Loop through each point to connect to the mainPoint
			foreach (AnchorScript point in points) {
				if (point.DisplayRange) {
					Vector3 mainPointPos = mainPoint.transform.position;
					Vector3 pointPos = point.transform.position;
				
					GL.Begin (GL.LINES);
					if (point.IsRanging) {
						lineMatRanging.SetPass (0);
					} else {
						lineMatNotRanging.SetPass (0);
					}
					GL.Vertex3 (mainPointPos.x, mainPointPos.y, mainPointPos.z);
					GL.Vertex3 (pointPos.x, pointPos.y, pointPos.z);
					GL.End ();
				}
			}
		}
	}
	
	// To show the lines in the game window whne it is running
	void OnPostRender ()
	{
		DrawConnectingLines ();
	}
	
	// To show the lines in the editor
	void OnDrawGizmos ()
	{
		DrawConnectingLines ();
	}
}
