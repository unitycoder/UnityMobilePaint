using UnityEngine;
using System.Collections;
using unitycoder_MobilePaint;
using System.IO;

namespace unitycoder_MobilePaint_samples
{

	public class SaveImageToFile : MonoBehaviour {

		MobilePaint mobilePaint;
		public KeyCode screenshotKey = KeyCode.F12;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;
		}
		
		void Update () 
		{
		
			if (Input.GetKeyDown(screenshotKey)) StartCoroutine(TakeScreenShot());

		}


		IEnumerator TakeScreenShot()
		{
			// NOTE: this has to be called first, otherwise ReadPixels() fails inside GetScreenshot
			yield return new WaitForEndOfFrame();

			//byte[] bytes = mobilePaint.GetScreenshot().EncodeToPNG();
			byte[] bytes = mobilePaint.GetCanvasAsTexture().EncodeToPNG(); // this works also, but only returns drawing layer

			string filename = "screenshot.png";
	//		string folder = Application.persistentDataPath + "/Screenshots/"; // this works also
			string folder = Application.dataPath + "/Screenshots/";
			string path = folder + filename;

			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}

			File.WriteAllBytes(path, bytes);

			Debug.Log("Screenshot saved at: "+path);
		}

	}
}