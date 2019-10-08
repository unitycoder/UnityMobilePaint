using UnityEngine;
using System.Collections;
using unitycoder_MobilePaint;
using System.IO;

// testing image loading from screenshot folder

namespace unitycoder_MobilePaint_samples
{
	
	public class LoadImageFromDisk : MonoBehaviour 
	{
		MobilePaint mobilePaint;
		public KeyCode loadFileKey = KeyCode.F9;
		public string imageFolder = "Screenshots/screenshot.png";
		
		void Start() {
			mobilePaint = PaintManager.mobilePaint;
		}
		
		void Update () 
		{
			if (Input.GetKeyDown(loadFileKey)) LoadImageAsCanvas();
		}
		
		void LoadImageAsCanvas()
		{
			// NOTE: not checking if file exists and using Application.dataPath.
			var s = File.ReadAllBytes(Application.dataPath + "/" + imageFolder);
			Texture2D tex = new Texture2D(2, 2);
			tex.LoadImage(s);
			
			// assign to drawing
			mobilePaint.SetCanvasImage (tex);
			
		}
	}
}