// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_STANDARD_SHADOW_INCLUDED
#define UNITY_STANDARD_SHADOW_INCLUDED

// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)


#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardUtils.cginc"
#include "FreeTess_Tessellator.cginc"

#if (defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
    #define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#define UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT 1
#endif


half4       _Color;
half        _Cutoff;
sampler2D   _MainTex;
float4      _MainTex_ST;
#ifdef UNITY_STANDARD_USE_DITHER_MASK
sampler3D   _DitherMaskLOD;
#endif

// Handle PremultipliedAlpha from Fade or Transparent shading mode
half4       _SpecColor;
half        _Metallic;
#ifdef _SPECGLOSSMAP
sampler2D   _SpecGlossMap;
#endif
#ifdef _METALLICGLOSSMAP
sampler2D   _MetallicGlossMap;
#endif

#if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
sampler2D   _ParallaxMap;
half        _Parallax;
#endif

half MetallicSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half metallicity = _Metallic;
    #ifdef _METALLICGLOSSMAP
        metallicity = tex2D(_MetallicGlossMap, uv).r;
    #endif
    return OneMinusReflectivityFromMetallic(metallicity);
}

half RoughnessSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half metallicity = _Metallic;
#ifdef _METALLICGLOSSMAP
    metallicity = tex2D(_MetallicGlossMap, uv).r;
#endif
    return OneMinusReflectivityFromMetallic(metallicity);
}

half SpecularSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half3 specColor = _SpecColor.rgb;
    #ifdef _SPECGLOSSMAP
        specColor = tex2D(_SpecGlossMap, uv).rgb;
    #endif
    return (1 - SpecularStrength(specColor));
}

// SHADOW_ONEMINUSREFLECTIVITY(): workaround to get one minus reflectivity based on UNITY_SETUP_BRDF_INPUT
#define SHADOW_JOIN2(a, b) a##b
#define SHADOW_JOIN(a, b) SHADOW_JOIN2(a,b)
#define SHADOW_ONEMINUSREFLECTIVITY SHADOW_JOIN(UNITY_SETUP_BRDF_INPUT, _ShadowGetOneMinusReflectivity)

struct VertexInput
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    float2 uv0      : TEXCOORD0;
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
        half4 tangent   : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
    V2F_SHADOW_CASTER_NOPOS
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        float2 tex : TEXCOORD1;

        #if defined(_PARALLAXMAP)
            half3 viewDirForParallax : TEXCOORD2;
        #endif
    #endif
};
#endif

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster
{
    UNITY_VERTEX_OUTPUT_STEREO
};
#endif

// We have to do these dances of outputting SV_POSITION separately from the vertex shader,
// and inputting VPOS in the pixel shader, since they both map to "POSITION" semantic on
// some platforms, and then things don't go well.


void vertShadowCaster (VertexInput v
    , out float4 opos : SV_POSITION
    #ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    , out VertexOutputShadowCaster o
    #endif
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
    , out VertexOutputStereoShadowCaster os
    #endif
)
{
    UNITY_SETUP_INSTANCE_ID(v);
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
    #endif
    TRANSFER_SHADOW_CASTER_NOPOS(o,opos)
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        o.tex = TRANSFORM_TEX(v.uv0, _MainTex);

        #ifdef _PARALLAXMAP
            TANGENT_SPACE_ROTATION;
            o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        #endif
    #endif
}

half4 fragShadowCaster (UNITY_POSITION(vpos)
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    , VertexOutputShadowCaster i
#endif
) : SV_Target
{
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        #if defined(_PARALLAXMAP) && (SHADER_TARGET >= 30)
            half3 viewDirForParallax = normalize(i.viewDirForParallax);
            fixed h = tex2D (_ParallaxMap, i.tex.xy).g;
            half2 offset = ParallaxOffset1Step (h, _Parallax, viewDirForParallax);
            i.tex.xy += offset;
        #endif

        #if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
            half alpha = _Color.a;
        #else
            half alpha = tex2D(_MainTex, i.tex.xy).a * _Color.a;
        #endif
        #if defined(_ALPHATEST_ON)
            clip (alpha - _Cutoff);
        #endif
        #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
            #if defined(_ALPHAPREMULTIPLY_ON)
                half outModifiedAlpha;
                PreMultiplyAlpha(half3(0, 0, 0), alpha, SHADOW_ONEMINUSREFLECTIVITY(i.tex), outModifiedAlpha);
                alpha = outModifiedAlpha;
            #endif
            #if defined(UNITY_STANDARD_USE_DITHER_MASK)
                // Use dither mask for alpha blended shadows, based on pixel position xy
                // and alpha level. Our dither texture is 4x4x16.
                #ifdef LOD_FADE_CROSSFADE
                    #define _LOD_FADE_ON_ALPHA
                    alpha *= unity_LODFade.y;
                #endif
                half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
                clip (alphaRef - 0.01);
            #else
                clip (alpha - _Cutoff);
            #endif
        #endif
    #endif // #if defined(UNITY_STANDARD_USE_SHADOW_UVS)

    #ifdef LOD_FADE_CROSSFADE
        #ifdef _LOD_FADE_ON_ALPHA
            #undef _LOD_FADE_ON_ALPHA
        #else
            UnityApplyDitherCrossFade(vpos.xy);
        #endif
    #endif

    SHADOW_CASTER_FRAGMENT(i)
}


// Tess shader code. Copyright (c) 2019 PDP Archydra.


float _Tess;
float _maxDist;
float _ShadowLOD;
float _Displacement;
float _DispOffset;
sampler2D   _ParallaxMap;
float _Phong;

struct TessFactors {
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

struct TessData
{
    float4 vertex   : POS; 
    float3 normal   : NORMAL;
    float2 uv0      : TEXCOORD0;
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
        half4 tangent   : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

TessData vs_tess (VertexInput v) {
  TessData o = (TessData)0;
  o.vertex = v.vertex;
  #if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
  	o.tangent = v.tangent;
  #endif
  o.normal = v.normal;
  o.uv0 = v.uv0;
  return o;
}

#define FT_SHADOW_LOD //always on for deferred

#ifdef FT_EDGE_TESS
	float4 tessIt (float4 v0, float4 v1, float4 v2)
	{
		#if defined(FT_SHADOW_LOD)
			float outTess = lerp(_Tess, 32.0, _ShadowLOD);
		#else
			float outTess = _Tess;
		#endif
		outTess = max(2.0, outTess);
	    return FTSphereProjectionTess (v0, v1, v2, _Displacement, outTess);
	}

	float4 disp (float4 pos, float2 uv, float3 norm)
	{
	    float d = tex2Dlod(_ParallaxMap, float4(uv * _MainTex_ST.xy + _MainTex_ST.zw, 0, 0)).g * _Displacement;
	    d = d * 0.5 - 0.5 + _DispOffset;
	    return  float4(pos.xyz + norm * d, pos.w); 
	}
#else
	float4 tessIt (float4 v0, float4 v1, float4 v2) {
		#if defined(FT_SHADOW_LOD)
			float outTess = lerp(_Tess, 1.0, _ShadowLOD);
		#else
			float outTess = _Tess;
		#endif 
		return FTDistanceBasedTess(v0, v1, v2, _maxDist * 0.2f, _maxDist * 1.2f, outTess);
	}

	float4 disp (float4 pos, float2 uv, float3 norm)
	{
		float fadeOut = saturate((_maxDist - distance(mul(unity_ObjectToWorld, pos.xyz), _WorldSpaceCameraPos)) / (_maxDist * 0.7f));
	    float d = tex2Dlod(_ParallaxMap, float4(uv * _MainTex_ST.xy + _MainTex_ST.zw, 0, 0)).g * _Displacement;
	    d = d * 0.5 - 0.5 + _DispOffset;
	    return float4(pos.xyz + norm * d * fadeOut, pos.w);
	}

#endif

void phongIt4 (inout float4 pos, float4 vp0, float4 vp1, float4 vp2, float3 vn0, float3 vn1, float3 vn2, float3 bary) {
	float3 phPos0 = dot(vp0.xyz - pos.xyz, vn0) * vn0;
	float3 phPos1 = dot(vp1.xyz - pos.xyz, vn1) * vn1;
	float3 phPos2 = dot(vp2.xyz - pos.xyz, vn2) * vn2;

	float3 vecOffset = bary.x * phPos0 + bary.y * phPos1 + bary.z * phPos2;

	pos.xyz += vecOffset * _Phong;
}

TessFactors hs_const(InputPatch<TessData, 3> v )
{
	 TessFactors o;
	 float4 factors = tessIt(v[0].vertex, v[1].vertex, v[2].vertex);
	 o.edge[0] = factors.x;
	 o.edge[1] = factors.y;
	 o.edge[2] = factors.z;
	 o.inside = factors.w;    
	 return o;
}

[UNITY_domain("tri")]
[UNITY_partitioning("fractional_odd")]
[UNITY_outputtopology("triangle_cw")]
[UNITY_patchconstantfunc("hs_const")]
[UNITY_outputcontrolpoints(3)]
TessData hs_tess(InputPatch<TessData, 3> v, uint cpID : SV_OutputControlPointID)
{
	TessData o = (TessData)0;
	o.vertex = v[cpID].vertex;
    o.normal = v[cpID].normal;
    o.uv0 = v[cpID].uv0;
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
        o.tangent = v[cpID].tangent;
    #endif

	return o;
}

[domain("tri")]
void ds_tess(TessFactors hs_data, const OutputPatch<TessData, 3> vi, float3 bary : SV_DomainLocation,
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
  out VertexOutputShadowCaster o,
#endif
#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
  out VertexOutputStereoShadowCaster os,
#endif
  out float4 opos : SV_POSITION
)
{
	VertexInput v = (VertexInput)0;

	float fU = bary.x;
	float fV = bary.y;
	float fW = bary.z;

	float4 vertex = vi[0].vertex * fU + vi[1].vertex * fV + vi[2].vertex * fW;
	v.normal = vi[0].normal * fU + vi[1].normal * fV + vi[2].normal * fW;
    v.uv0 = vi[0].uv0 * fU + vi[1].uv0 * fV + vi[2].uv0 * fW;

    phongIt4 (vertex, vi[0].vertex, vi[1].vertex, vi[2].vertex, vi[0].normal, vi[1].normal, vi[2].normal, bary);
    v.vertex = disp(vertex, v.uv0, v.normal);

    #if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
        v.tangent = vi[0].tangent * fU + vi[1].tangent * fV + vi[2].tangent * fW;
    #endif

    v.vertex.w = 1.0;
	#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
    #endif
    TRANSFER_SHADOW_CASTER_NOPOS(o,opos)
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        o.tex = TRANSFORM_TEX(v.uv0, _MainTex);
    #endif
}


#endif // UNITY_STANDARD_SHADOW_INCLUDED
