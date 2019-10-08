using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaintAreaEventListener : MonoBehaviour {

	public bool debugMode=false;

	public unitycoder_MobilePaint.MobilePaint mobilePaint;
	public GameObject prefabFullFx;



	Dictionary<int, string> shapeDictionary =	new Dictionary<int, string>();

	void Start () {

		// Example sizes & shape names (you have to manually enter these for your image)
		// You could include here any data that you need to access when shape is painted
		shapeDictionary.Add(11464,"Hearth");
		shapeDictionary.Add(33165,"Square");
		shapeDictionary.Add(28458,"Freeform");
		shapeDictionary.Add(64711,"Circle");
		shapeDictionary.Add(34680,"Triangle");
		shapeDictionary.Add(12512,"Letter X");

		mobilePaint.AreaPaintedEvent+=CheckAreaFill;
	}
	
	// this function is called when paint event finished
	// NOTE: filled area is actually some pixels larger than it looks in the image, because it goes few pixels under the black border (to avoid white edges)
	void CheckAreaFill(int fullArea, int filledArea, float percentageFilled, Vector3 worldPoint)
	{
	
		// NOTE: Use this debug line to get your area sizes 
		if (debugMode) Debug.Log("fullArea:"+fullArea+" | filledArea:"+filledArea+" | percentageFilled:"+percentageFilled);

		// EXAMPLE: check if this shape is in the list
		if (!shapeDictionary.ContainsKey(fullArea)) return; // no match, early return

		if (debugMode) Debug.Log( "You are filling shape: "+shapeDictionary[fullArea] + " | Filled:"+(int)percentageFilled+"%");

		if (percentageFilled<99.9f) return; // not full yet, early return

		// play success sound
		//if (GetComponent<AudioSource>()!=null) 
		GetComponent<AudioSource>().Play();

		// instantiate particle effect where to we painted
		var clone = Instantiate(prefabFullFx, worldPoint, prefabFullFx.transform.rotation) as GameObject;
		clone.GetComponent<ParticleSystem>().startColor = mobilePaint.paintColor; // set particle system color to current paint color
		Destroy(clone, 5); // destroy it after 5secs

		// remove this item from list so it wont trigger fill event again
		shapeDictionary.Remove(fullArea);
	}


}
