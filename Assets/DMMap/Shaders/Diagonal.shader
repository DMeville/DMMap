// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.16 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.16;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,rfrpo:True,rfrpn:Refraction,ufog:False,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:3138,x:33065,y:32720,varname:node_3138,prsc:2|custl-8141-OUT,olwid-801-OUT,olcol-2553-RGB;n:type:ShaderForge.SFN_TexCoord,id:7895,x:31207,y:32350,varname:node_7895,prsc:2,uv:0;n:type:ShaderForge.SFN_Multiply,id:8883,x:31941,y:32471,varname:node_8883,prsc:2|A-1975-R,B-8786-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8786,x:31697,y:32632,ptovrint:False,ptlb:Divisions,ptin:_Divisions,varname:node_8786,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:100;n:type:ShaderForge.SFN_Frac,id:1312,x:32117,y:32473,varname:node_1312,prsc:2|IN-8883-OUT;n:type:ShaderForge.SFN_RemapRange,id:3986,x:32337,y:32473,varname:node_3986,prsc:2,frmn:0,frmx:1,tomn:-10,tomx:100|IN-1312-OUT;n:type:ShaderForge.SFN_OneMinus,id:721,x:32522,y:32491,varname:node_721,prsc:2|IN-3986-OUT;n:type:ShaderForge.SFN_ValueProperty,id:801,x:32337,y:33012,ptovrint:False,ptlb:Outline Width,ptin:_OutlineWidth,varname:node_801,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0.1;n:type:ShaderForge.SFN_Color,id:2553,x:32337,y:32841,ptovrint:False,ptlb:Stroke Color,ptin:_StrokeColor,varname:node_2553,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.8758621,c3:1,c4:1;n:type:ShaderForge.SFN_Lerp,id:8141,x:32836,y:32602,varname:node_8141,prsc:2|A-5710-RGB,B-2553-RGB,T-6720-OUT;n:type:ShaderForge.SFN_Color,id:5710,x:32337,y:32664,ptovrint:False,ptlb:Color,ptin:_Color,varname:node_5710,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_Clamp01,id:6720,x:32694,y:32491,varname:node_6720,prsc:2|IN-721-OUT;n:type:ShaderForge.SFN_Rotator,id:3516,x:31521,y:32471,varname:node_3516,prsc:2|UVIN-9959-OUT,ANG-5916-OUT;n:type:ShaderForge.SFN_ComponentMask,id:1975,x:31697,y:32471,varname:node_1975,prsc:2,cc1:0,cc2:1,cc3:-1,cc4:-1|IN-3516-UVOUT;n:type:ShaderForge.SFN_Slider,id:5916,x:31129,y:32518,ptovrint:False,ptlb:Angle,ptin:_Angle,varname:node_5916,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-180,cur:68.65367,max:180;n:type:ShaderForge.SFN_FragmentPosition,id:4444,x:31244,y:32179,varname:node_4444,prsc:2;n:type:ShaderForge.SFN_Append,id:9959,x:31460,y:32197,varname:node_9959,prsc:2|A-4444-X,B-4444-Z;proporder:8786-801-2553-5710-5916;pass:END;sub:END;*/

Shader "DMMap/Diagonal" {
    Properties {
        _Divisions ("Divisions", Float ) = 100
        _OutlineWidth ("Outline Width", Float ) = 0.1
        _StrokeColor ("Stroke Color", Color) = (0,0.8758621,1,1)
        _Color ("Color", Color) = (0.5,0.5,0.5,1)
        _Angle ("Angle", Range(-180, 180)) = 68.65367
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
            uniform float4 _StrokeColor;
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
                return fixed4(_StrokeColor.rgb,1);
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
            uniform float _Divisions;
            uniform float4 _StrokeColor;
            uniform float4 _Color;
            uniform float _Angle;
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
                float node_3516_ang = _Angle;
                float node_3516_spd = 1.0;
                float node_3516_cos = cos(node_3516_spd*node_3516_ang);
                float node_3516_sin = sin(node_3516_spd*node_3516_ang);
                float2 node_3516_piv = float2(0.5,0.5);
                float2 node_3516 = (mul(float2(i.posWorld.r,i.posWorld.b)-node_3516_piv,float2x2( node_3516_cos, -node_3516_sin, node_3516_sin, node_3516_cos))+node_3516_piv);
                float node_8883 = (node_3516.rg.r*_Divisions);
                float node_721 = (1.0 - (frac(node_8883)*110.0+-10.0));
                float3 finalColor = lerp(_Color.rgb,_StrokeColor.rgb,saturate(node_721));
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
