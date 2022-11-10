Shader "Custom/StylisedShader" {
	//Author:Arda Hamamcioglu
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _HatchTex ("Hatch Pattern", 2D) ="white" {}
		_PatternSize("Base Pattern Size",Float) = 100
		_HatchMap ("Hatch Detail Map",2D) = "white" {}
		_EmissionMap ("Emission Map",2D) = "black" {}
		_RoughMap ("Roughness Map",2D) = "white" {}
		_Roughness ("Roughness",Range(0,1)) = 1
		_HatchIntensity ("Hatch Intensity",Range(0,1)) = 1
		_ColorRampIntensity("Color Ramp Intensity",Range(0,1)) = 0
		_HatchRampIntensity("Hatch Ramp Intensity",Range(0,1)) = 0
        _RampSteps ("Lighting Ramp Step Count", Range(1,10)) = 1
	   //_DepthScale ("Pattern Depth Scale",Float) = 5

	   [Toggle]
	   _ColoredHatch("Colored Hatch",Float) = 0
	}
	CGINCLUDE
	
	fixed3 Saturation(fixed3 color,fixed saturation)
    {
        return saturate(lerp((color.r+color.g+color.b)/3,color,saturation));
    }

	fixed SmoothHS(fixed input,fixed treshold)
	{
		return smoothstep(treshold-0.2,treshold,input);
	}

	fixed Ramp(fixed input,fixed steps)
	{
		return floor(input*steps)/steps;
	}

	ENDCG
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Stylised fullForwardShadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
       	sampler2D _HatchTex;
		sampler2D _HatchMap;
		sampler2D _EmissionMap;
		sampler2D _RoughMap;

		float4 _HatchTex_ST;

		struct StylisedSurfaceOutput{
			fixed3 Albedo;
    		float2 ScreenPos;
    		fixed3 Emission;
    		fixed Alpha;
    		fixed3 Normal;
			fixed3 Rough;
			fixed HatchMap;
			//fixed3 WorldPos;
		};

		struct Input {
			float2 uv_MainTex;
			float4 screenPos;
			float3 worldPos;
		};

		fixed4 _Color;
		fixed _Roughness;
       	fixed _RampSteps;
		fixed _PatternSize;
		fixed _HatchIntensity;
		fixed _ColorRampIntensity;
		fixed _HatchRampIntensity;
		//fixed _DepthScale;

		fixed _ColoredHatch;

       //LIGTHING MODEL
        half4 LightingStylised (StylisedSurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			//DEPTH... IF NEEDED SOMEDAY
			//half depth = 1-distance(s.WorldPos,_WorldSpaceCameraPos)/_DepthScale;

			//DIFFUSE LIGHTING
			fixed gradientTex = tex2D(_HatchTex, frac(s.ScreenPos*_PatternSize)).r;
        	fixed NdotL = dot(s.Normal, lightDir);
			fixed NdotLRamp = Ramp(NdotL,_RampSteps);
			fixed diff;
			
			//SPECULAR LIGTHING
			fixed3 reflectionDirection = reflect(lightDir,s.Normal);
			fixed towardsReflection = dot(viewDir,-reflectionDirection);
			fixed specularChange = fwidth(towardsReflection);
			fixed roughness = (_Roughness*s.Rough)*0.5+0.5;
			fixed spec = smoothstep(1-roughness,1-roughness+specularChange,towardsReflection);
			spec *= 1-roughness;

			//HATCH TEXTURE
			fixed hatchDiff = lerp(NdotL,NdotLRamp,_HatchRampIntensity);
			hatchDiff = saturate(hatchDiff*(4-3*_HatchIntensity));
			half HatchRamp = SmoothHS(hatchDiff,gradientTex);

			//EXCLUDE HATCH FROM SPEC
			HatchRamp = max(0.,max(HatchRamp,spec*2)-s.Emission);

			//HATCH INTENSITY
			HatchRamp = saturate(HatchRamp*(4-3*_HatchIntensity));

			fixed colorDiff = lerp(NdotL,NdotLRamp,_ColorRampIntensity)*0.5+0.5;
			fixed3 color = Saturation(s.Albedo,2-colorDiff+spec)*colorDiff;

        	half4 c;
        	c.a = s.Alpha;
			color = lerp(color*HatchRamp,Saturation(color,2-HatchRamp)*(HatchRamp*0.75+0.25),_ColoredHatch);
			c.rgb = color+spec+s.Emission;
        	return c;
        }

		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

        //SURFACE FUNCTION
	   	void surf (Input i, inout StylisedSurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, i.uv_MainTex) * _Color;
			fixed3 r = tex2D(_RoughMap,i.uv_MainTex);
			fixed h = tex2D(_HatchMap,i.uv_MainTex);
			fixed3 e = tex2D(_EmissionMap,i.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;

			float aspect = _ScreenParams.x / _ScreenParams.y;
    		o.ScreenPos = i.screenPos.xy / i.screenPos.w;
    		o.ScreenPos = TRANSFORM_TEX(o.ScreenPos, _HatchTex);
    		o.ScreenPos.x = o.ScreenPos.x * aspect;
			
			o.Rough = r;
			o.HatchMap = h;
			o.Emission = e;
			//o.WorldPos = i.worldPos;
		}
		ENDCG
	}
	FallBack "Diffuse"
}