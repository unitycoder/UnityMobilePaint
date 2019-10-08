// sends picked color to MobilePaint script

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace unitycoder_MobilePaint
{

	public class ColorUIManager : MonoBehaviour 
	{
		MobilePaint mobilePaint;
		public Button[] colorpickers; // colors are taken from these buttons

		public bool offsetSelected=true; // should we move the pencil when its selected
		public float defaultOffset=-46;
		public float moveOffsetX=-24;

		[HideInInspector] public Image currentColorImage;

		void Awake()
		{
			mobilePaint = PaintManager.mobilePaint;

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned at "+transform.name, gameObject);
			if (colorpickers.Length<1) Debug.LogWarning("No colorpickers assigned at "+transform.name, gameObject);

			currentColorImage = GetComponent<Image>();
			if (currentColorImage==null) Debug.LogError("No image component founded at "+transform.name, gameObject);


			// Add event listeners to pencil buttons
			for (int i=0;i<colorpickers.Length;i++)
			{
				var button = colorpickers[i];
				if (button!=null)
				{
					button.onClick.AddListener(delegate {this.SetCurrentColor(button);});
				}
			}
		}

		// some button was clicked, lets take color from it and send to mobilepaint canvas 
 		public void SetCurrentColor(Button button)
		{ 
			Color newColor = button.gameObject.GetComponent<Image>().color;

			currentColorImage.color = newColor; // set current color image

			// send new color
			mobilePaint.SetPaintColor(newColor);
//			mobilePaint.paintColor = newColor;

			if (offsetSelected)
			{
				ResetAllOffsets();
				SetButtonOffset(button,moveOffsetX);
			}

		}


		void ResetAllOffsets()
		{
			for (int i=0;i<colorpickers.Length;i++)
			{
				SetButtonOffset(colorpickers[i],defaultOffset); 
			}
		}


		void SetButtonOffset(Button button,float offsetX)
		{
			RectTransform rectTransform = button.GetComponent<RectTransform>();
			rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,offsetX,rectTransform.rect.width);
		}

	} // class
} // namespace