using UnityEngine;
using System.Collections;

// Test script for overriding some methods from MobilePaint.cs
// Do not use this script on your project, as it might change in future, just make similar script for yourself
// then assign this into "DrawingPlaneCanvas" gameobject where you want to draw

namespace unitycoder_MobilePaint
{

	public class OverrideTestForUI : MobilePaint // inherit from MobilePaint
	{

		// instead of using original HideUI, we will override it here
		public override void HideUI()
		{
			Debug.Log("Do your overriding HideUI() here");

		}

		public override void ShowUI()
		{
			Debug.Log("Do your overriding ShowUI() here");
		}

	}
}