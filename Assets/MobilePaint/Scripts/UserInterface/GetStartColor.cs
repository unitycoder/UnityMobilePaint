using UnityEngine;
using System.Collections;

// this script takes the initial color from canvas (to this guitexture where script is attached)


namespace unitycoder_MobilePaint
{

	public class GetStartColor : MonoBehaviour {

		public GameObject canvas;


		void Start () 
		{
			GetComponent<GUITexture>().color = canvas.GetComponent<MobilePaint>().paintColor;
		}
		
	}
}