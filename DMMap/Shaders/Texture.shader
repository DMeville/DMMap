// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.16 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.16;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,rfrpo:True,rfrpn:Refraction,ufog:False,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:3138,x:33074,y:32471,varname:node_3138,prsc:2|custl-3114-OUT,olwid-801-OUT,olcol-2553-RGB;n:type:ShaderForge.SFN_ValueProperty,id:801,x:32337,y:33012,ptovrint:False,ptlb:Outline Width,ptin:_OutlineWidth,varname:node_801,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0.1;n:type:ShaderForge.SFN_Color,id:2553,x:32337,y:32841,ptovrint:False,ptlb:Outline Color,ptin:_OutlineColor,varname:node_2553,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.8758621,c3:1,c4:1;n:type:ShaderForge.SFN_Color,id:5710,x:32337,y:32664,ptovrint:False,ptlb:Color,ptin:_Color,varname:node_5710,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_FragmentPosition,id:4444,x:32290,y:32389,varname:node_4444,prsc:2;n:type:ShaderForge.SFN_Append,id:9959,x:32506,y:32407,varname:node_9959,prsc:2|A-4444-X,B-4444-Z;n:type:ShaderForge.SFN_Tex2d,id:2163,x:32665,y:32407,ptovrint:False,ptlb:Texture,ptin:_Texture,varname:node_2163,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:b66bceaf0cc0ace4e9bdc92f14bba709,ntxv:0,isnm:False|UVIN-9959-OUT;n:type:ShaderForge.SFN_Multiply,id:3114,x:32791,y:32603,varname:node_3114,prsc:2|A-2163-RGB,B-5710-RGB;proporder:801-2553-5710-2163;pass:END;sub:END;*/

Shader "DMMap/Texture" {
    Properties {
        _OutlineWidth ("Outline Width", Float ) = 0.1
        _OutlineColor ("Outline Color", Color) = (0,0.8758621,1,1)
        _Color ("Color", Color) = (0.5,0.5,0.5,1)
        _Texture ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Back
			ColorMask RGBA
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma exclude_renderers gles3 metal d3d11_9x xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform float _OutlineWidth;
            uniform float4 _OutlineColor;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + v.normal*_OutlineWidth,1));
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
/////// Vectors:
                return fixed4(_OutlineColor.rgb,1);
            }
            ENDCG
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma exclude_renderers gles3 metal d3d11_9x xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform float4 _Color;
            uniform sampler2D _Texture; uniform float4 _Texture_ST;
            struct VertexInput {
                float4 vertex : POSITION;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
/////// Vectors:
////// Lighting:
                float2 node_9959 = float2(i.posWorld.r,i.posWorld.b);
                float4 _Texture_var = tex2D(_Texture,TRANSFORM_TEX(node_9959, _Texture));
                float3 finalColor = (_Texture_var.rgb*_Color.rgb);
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
