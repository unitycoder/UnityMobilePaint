// basic scale guitexture or guitext to fix its size in HD devices

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint_samples
{

	public class GUIScaler : MonoBehaviour 
	{
		private float scaleAdjust = 1.0f;

		private const float BASE_WIDTH = 800;
		private const float BASE_HEIGHT = 480;

		void Awake()
		{
			float _baseHeightInverted = 1.0f/BASE_HEIGHT;
			float ratio = (Screen.height * _baseHeightInverted)*scaleAdjust;

			if (GetComponent<GUITexture>()!=null)
			{
				GUITexture _guiTextureRef = GetComponent<GUITexture>();
				_guiTextureRef.pixelInset = new Rect(_guiTextureRef.pixelInset.x* ratio, _guiTextureRef.pixelInset.y* ratio, _guiTextureRef.pixelInset.width * ratio, _guiTextureRef.pixelInset.height * ratio);
			}else{
				if (GetComponent<GUIText>()!=null)
				{
					GetComponent<GUIText>().pixelOffset = new Vector2(GetComponent<GUIText>().pixelOffset.x*ratio, GetComponent<GUIText>().pixelOffset.y*ratio);
					GetComponent<GUIText>().fontSize = (int)(GetComponent<GUIText>().fontSize*ratio);
				}
			}

			// no need anymore
			Destroy(this);

		}
	}
}