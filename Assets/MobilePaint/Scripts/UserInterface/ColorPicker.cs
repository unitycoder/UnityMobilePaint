// simple palette system: Take palette color from image (using GetPixel), set color in main painter gameobject

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{

	public class ColorPicker : MonoBehaviour 
	{

		// references
		public MobilePaint canvas;
		public GameObject paletteButton;
		public GameObject selectedColor;
		public GameObject previewColor;
		public GUIStyle sliderStyle;

		// settings
		public bool closeAfterPick = true;


		// private & internal variables, no need to touch usually
		private Texture2D tex;
		private float alpha=1; // brush alpha value 0-1

		// for GUIScaling
		public float scaleAdjust = 1.0f;
		private const float BASE_WIDTH = 800;
		private const float BASE_HEIGHT = 480;
		private float ratio=1.0f;
		public int cellSize = 32; // palette color cell height (in the texture)

		private Rect guiRect;

		// init
		void Start () 
		{

			// calculate scaling ratio for different screen resolutions
			float _baseHeightInverted = 1.0f/BASE_HEIGHT;
			ratio = (Screen.height * _baseHeightInverted)*scaleAdjust;

			tex = (Texture2D)GetComponent<GUITexture>().texture;

			guiRect = GetComponent<GUITexture>().GetScreenRect();

			if (canvas==null)
			{
				Debug.LogError("Canvas gameObject not found - Have you assigned it?");
			}

			// get current alpha value, NOTE: only runs once, if alpha is adjusted outside this colorpicker, need to update it manually
			alpha = canvas.paintColor.a/255f;

		} // Start



		// guitexture clicked
		void OnMouseDown()
		{
			// get mouse hit position, inside non-scaled guitexture
			Vector3 hitPos = Input.mousePosition;// - corner;
			Color hitColor = tex.GetPixel((int)((hitPos.x-guiRect.x)/ratio),(int)((hitPos.y-guiRect.y)/ratio));
			hitColor.a = alpha; // take alpha from slider

			// send color to canvas
			canvas.paintColor = hitColor;
			selectedColor.GetComponent<GUITexture>().color = hitColor*0.5f; // half the color, otherwise too bright..unity feature?
			previewColor.GetComponent<GUITexture>().color = hitColor*0.5f;

			// close palette dialog slowly (to avoid accidental drawing after palette click)
			if (closeAfterPick)
			{
				GetComponent<GUITexture>().enabled = false; // hide guitexture just to show something is happening
				Invoke("DelayedToggle",0.5f);
			}

		} // onmousedown



		// sample old GUI element for controlling alpha value
		void OnGUI() 
		{

			// preview alpha adjust
			Color tempColor = previewColor.GetComponent<GUITexture>().color;
			tempColor.a = alpha;
			selectedColor.GetComponent<GUITexture>().color = tempColor; // half the color, otherwise too bright..unity feature?
			previewColor.GetComponent<GUITexture>().color = tempColor;

			// FIXME: slider is quite small on mobiles..
			GUI.skin.verticalSliderThumb = sliderStyle;
			alpha = GUI.VerticalSlider(new Rect(guiRect.x+GetComponent<GUITexture>().pixelInset.width+32, guiRect.y, 32, GetComponent<GUITexture>().pixelInset.height), alpha, 1.0F, 0.0F);
			GUI.Label(new Rect(guiRect.x+GetComponent<GUITexture>().pixelInset.width+30, guiRect.y-24, 32, 32),(Mathf.RoundToInt(alpha*100)).ToString());

		}


		// this is called after small delay, so that the click wouldnt register as paint event
		void DelayedToggle()
		{
			paletteButton.GetComponent<PaletteDialog>().ToggleModalBackground();
			GetComponent<GUITexture>().enabled = true;
		}


	} // class
} // namespace
