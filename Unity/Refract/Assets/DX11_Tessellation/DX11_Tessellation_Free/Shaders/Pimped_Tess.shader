Shader "Tessellation/Pimped Distance" {
        Properties {
            _Tess ("Tessellation", Range(1,32)) = 4
            _maxDist ("Tess Fade Distance", Range(0, 500.0)) = 25.0

            _MainTex ("Base (RGB) Displacement (A)", 2D) = "white" {}
            _DispTex ("Disp Texture", 2D) = "gray" {}
            _BumpMap ("Normalmap", 2D) = "bump" {}
            _BumpScale("Normalmap Scale", Float) = 1.0
            _Displacement ("Displacement", Range(0, 1.0)) = 0.3
            _DispOffset ("Disp Offset", Range(0, 1)) = 0.5
            _DispPhong ("Disp Phong", Range(0, 1)) = 0
            _Displacement_TO ("Displacement Map Tile / Offset", Vector) = (1,1,0,0)
            _Color ("Color", color) = (1,1,1,0)
            [Gamma] _Metallic ("Metallic", Range(0, 1)) = 0.5
            _Glossiness ("Smoothness", Range(0, 1)) = 0.5
            [Gamma] _SpecAO ("Specular AO", Range(-1, 1)) = 0
            _DiffuseAO ("Diffuse AO", Range(0, 1)) = 0
            _SpecBurn ("SpecularBurn", Range(0, 1)) = 0
            _GI_AO ("GI AO", Range(0, 1)) = 0
            _DetailAO("Detail Mask AO", Range(-1, 1)) = 0.5
			_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
			_DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
			_DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        }
        SubShader {
            Tags { "RenderType"="Opaque" }
            LOD 500
            
            CGPROGRAM
            #pragma surface surf Standard addshadow fullforwardshadows vertex:disp tessellate:tessDistance tessphong:_DispPhong
            #pragma target 4.6
            #include "FreeTess_Tessellator.cginc"

            #pragma shader_feature _NORMALMAP
			#pragma shader_feature _DETAIL_MULX2
			#pragma shader_feature _DISPALPHA

            struct appdata {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
            };

            float _DispPhong;
            float _Tess;
            float _maxDist;

            float4 tessDistance (appdata v0, appdata v1, appdata v2) {
                return FTDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, _maxDist * 0.2f, _maxDist * 1.2f, _Tess);
            }

            sampler2D _MainTex;

            #ifdef _DISPALPHA
            	uniform float4 _Displacement_TO;
            #else
            	sampler2D _DispTex;
            	uniform float4 _DispTex_ST;
            #endif

            fixed4 _Color;
            half _Metallic;
            half _Glossiness;
            
   			half _SpecAO;
   			half _DiffuseAO;
   			half _GI_AO;
   			half _SpecBurn;
   			
            float _Displacement;
            float _DispOffset;

            #ifdef _NORMALMAP
            	sampler2D _BumpMap;
            	half _BumpScale;
            #endif  

            #ifdef _DETAIL_MULX2
	            sampler2D _DetailAlbedoMap;
				half _DetailAO;
				sampler2D _DetailNormalMap;
				half _DetailNormalMapScale;
			#endif

			#include "FreeTess_Utils.cginc"

            void disp (inout appdata v)
            {
            	const float fadeOut= saturate((_maxDist - distance(mul(unity_ObjectToWorld, v.vertex), _WorldSpaceCameraPos)) / (_maxDist * 0.7f));

            	#ifdef _DISPALPHA
           			float d = tex2Dlod(_MainTex, float4(v.texcoord.xy * _Displacement_TO.xy + _Displacement_TO.zw, 0, 0)).a * _Displacement;
           		#else
                	float d = tex2Dlod(_DispTex, float4(v.texcoord.xy * _DispTex_ST.xy + _DispTex_ST.zw, 0, 0)).r * _Displacement;
                #endif

                d = d * 0.5 - 0.5 +_DispOffset;
                v.vertex.xyz += v.normal * d * fadeOut;
            }

            struct Input {
                float2 uv_MainTex;
                float2 uv_BumpMap;
                #ifdef _DETAIL_MULX2
                	float2 uv_DetailAlbedoMap;
                #endif
            };

            void surf (Input IN, inout SurfaceOutputStandard o) {

            	#ifdef _DISPALPHA         
              		half d = tex2D (_MainTex, IN.uv_BumpMap).a;
              	#else
              		half d = tex2D (_DispTex, IN.uv_BumpMap).r;
              	#endif

                #ifdef _NORMALMAP
	                #ifdef _DETAIL_MULX2
	                	o.Albedo = FTAlbedo(float4(IN.uv_MainTex, IN.uv_DetailAlbedoMap), IN.uv_BumpMap);
	                #else
	                	o.Albedo = FTAlbedo(float4(IN.uv_MainTex, IN.uv_BumpMap), IN.uv_BumpMap);
	                #endif
	            #else
	            	#ifdef _DETAIL_MULX2
	                	o.Albedo = FTAlbedo(float4(IN.uv_MainTex, IN.uv_DetailAlbedoMap), IN.uv_MainTex);
	                #else
	                	o.Albedo = FTAlbedo(float4(IN.uv_MainTex, IN.uv_MainTex), IN.uv_MainTex);
	                #endif
            	#endif

            	half sao = _SpecAO < 0 ? -_SpecAO * d : (1 - d) * _SpecAO;
            	half b = saturate(exp(tex2D (_MainTex, IN.uv_BumpMap).r - _SpecBurn));

                o.Metallic = saturate(_Metallic - sao);
                o.Smoothness = saturate(_Glossiness - sao * b);
                o.Occlusion = saturate(1 - ((1 - d) * _GI_AO));

                 #ifdef _NORMALMAP
	                #ifdef _DETAIL_MULX2
	                	o.Normal = FTNormalInTangentSpace(float4(IN.uv_BumpMap, IN.uv_DetailAlbedoMap));
	               	#else
	               		o.Normal = FTNormalInTangentSpace(float4(IN.uv_BumpMap, IN.uv_BumpMap));
	                #endif
                #endif

            }
            ENDCG
        }
        FallBack "Standard"
        CustomEditor "FreeTessPimped_GUI"
    }