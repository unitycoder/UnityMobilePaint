using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
	public class ClearImageButton : MonoBehaviour 
	{

		MobilePaint mobilePaint;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;

			var button = GetComponent<Button>();
			button.onClick.AddListener(delegate {this.CallClearImage();});
		}

		void CallClearImage()
		{
			mobilePaint.ClearImage();
		}
	}
}