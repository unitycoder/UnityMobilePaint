using UnityEngine;

namespace unitycoder_MobilePaint_samples
{

	public class SetSortingOrderAndLayer : MonoBehaviour {

		public int sortingOrder = 0;
		public string layerName = "Default";

		void Awake()
		{
			GetComponent<Renderer>().sortingOrder = sortingOrder;
			GetComponent<Renderer>().sortingLayerName = layerName;
		}

	}
}