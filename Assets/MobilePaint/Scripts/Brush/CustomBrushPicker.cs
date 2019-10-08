// basic custom brush list, using selectiongrid

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{
	public class CustomBrushPicker : MonoBehaviour {

		public GameObject canvas;
		public bool closeAfterPick = true;

		public GameObject functionButton;

		private bool isEnabled = true;

		private int selGridInt = -1; // selected item

		void OnGUI()
		{

			if (!isEnabled) return;

			// selection grid of custom brushes
			selGridInt = GUILayout.SelectionGrid(selGridInt, canvas.GetComponent<MobilePaint>().customBrushes, 8, GUILayout.MinHeight(64), GUILayout.MaxHeight(64));

			if (selGridInt>-1)
			{

				isEnabled = false; // hide guitexture just to show something is happening

				canvas.GetComponent<MobilePaint>().selectedBrush = selGridInt;
				canvas.GetComponent<MobilePaint>().ReadCurrentCustomBrush();
				selGridInt = -1;

				Invoke("DelayedToggle",0.4f);
			} // if


		} // OnGUI



		void DelayedToggle()
		{
			functionButton.GetComponent<GUITexture>().texture = canvas.GetComponent<MobilePaint>().customBrushes[canvas.GetComponent<MobilePaint>().selectedBrush] as Texture;
			functionButton.GetComponent<CustomBrushDialog>().ToggleModalBackground();
			isEnabled = true;
		}

	} // class
} // namespace