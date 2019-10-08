Shader "UnityCoder/CanvasWithAlpha" 
{
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		//Blend SrcAlpha OneMinusSrcAlpha // can only paint on black?
		//Blend OneMinusSrcAlpha SrcAlpha // camera background color controls image black color
		//Blend SrcAlpha OneMinusDstAlpha // reveal black areas from canvas texture by painting *floodfill bit broken, detects painted areas..
		Blend OneMinusSrcAlpha OneMinusSrcAlpha // best so far.. camera background color is correct, canvas texture is ok
		
		LOD 100
		
		Pass {
			Tags { "LightMode" = "Vertex" }
			Lighting Off
			SetTexture [_MainTex] 
			{
				combine texture 
//				combine texture lerp (texture) previous, previous*texture
			} 
		}
	}
}



