using UnityEngine;
using System.Collections;
using unitycoder_MobilePaint;


namespace unitycoder_MobilePaint_samples
	{

	public class LoadTextureFromWeb : MonoBehaviour 
	{

		MobilePaint mobilePaint;


		public string url = "https://upload.wikimedia.org/wikipedia/commons/e/ef/Armlet_3_%28PSF%29.png";
		
		IEnumerator Start() {
			mobilePaint = PaintManager.mobilePaint;

			// Start b download of the given URL
			WWW www = new WWW(url);
			
			// Wait for download to complete
			yield return www;

			if (!string.IsNullOrEmpty(www.error)) 
			{
				Debug.Log(www.error);
			}else{
				// assign texture, NOTE
				mobilePaint.SetMaskImage(www.texture);
			}
		}
	}
}