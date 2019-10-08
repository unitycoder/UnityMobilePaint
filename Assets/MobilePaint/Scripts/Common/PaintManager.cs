using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{

	public class PaintManager : MonoBehaviour 
	{
		public MobilePaint mobilePaintReference;

		public static MobilePaint mobilePaint;

		void Awake()
		{
			if (mobilePaintReference==null) Debug.LogError("No MobilePaint assigned at "+transform.name,gameObject);

			mobilePaint = mobilePaintReference;

		}

	}
}