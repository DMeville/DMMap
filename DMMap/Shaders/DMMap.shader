Shader "DMMap/DMMap" {
	Properties {
		_MainTex ("Map RenderTexture", 2D) = "white" {}
		_Mask ("Map Mask (R)", 2D) = "white" {}
		_Opacity("Opacity", Range(0, 1)) = 1
	}

	Category {
	    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	    Blend SrcAlpha OneMinusSrcAlpha
	    Cull Off Lighting Off ZWrite Off Fog {Mode Off}
	   
	    SubShader {
	        Pass {
	            SetTexture [_MainTex] {
	                //combine texture * primary
	            }
				SetTexture [_Mask]{
					combine previous, texture *previous 
				}
				SetTexture[_Mask]{
					ConstantColor (1, 1, 1, [_Opacity])
					combine previous, previous*constant
				}
	        }
	    }
	}
}
