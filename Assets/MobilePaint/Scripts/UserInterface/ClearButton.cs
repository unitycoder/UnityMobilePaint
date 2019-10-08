// simple button: call ClearImage on paint plane

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{
	public class ClearButton : MonoBehaviour 
	{

		MobilePaint mobilePaint;

		void OnMouseDown()
		{
			mobilePaint = PaintManager.mobilePaint;

			// send message to clear image
			mobilePaint.ClearImage();
		}
	}
}