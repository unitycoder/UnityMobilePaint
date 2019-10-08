Shader "UnityCoder/CanvasWithMask" 
{
	Properties {
		_MainTex ("Canvas (RGBA)", 2D) = "white" {}
		_MaskTex ("Mask (RGBA)", 2D) = "white" {}
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		//Blend SrcAlpha OneMinusSrcAlpha 
		//Blend One One
		
		LOD 100
		Pass {
			Tags { "LightMode" = "Vertex" }
			Lighting Off
			
			
			SetTexture [_MainTex]
			{
				combine texture
			}
			
			SetTexture [_MaskTex] 
			{ 
//				combine previous*texture
				combine texture lerp (texture) previous, previous+texture
			}
			

		} // pass
		
	} // subshader
}

