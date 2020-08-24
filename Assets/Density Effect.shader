﻿// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Density Effect"
{
   Properties
   {
      _MainTex ("Source", 2D) = "white" {}
      _NumPoints("Number of points", int) = 5
      _DensityScale("Density", Range(0, 10)) = 1
      _DepthScale("Depth Scale", float) = 1
      _Color("Color", Color) = (1,1,1,1)
      _Power("Light Power", Range(0, 10)) = 1
      _KValue("K Value", Range(-10, 10)) = 1
   }
   SubShader
   {
      Cull Off 
      ZWrite Off 
      ZTest Always

      Pass
      {
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
			
         #include "UnityCG.cginc"
         #include "WaterDensity.hlsl"

         struct appdata
         {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
         };

         struct v2f
         {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
            float3 worldDir : TEXCOORD1;
         };

		float4x4 clipToWorld;
        float4 lightPositions[8];
        float4 lightColors[8];
        float4 lightProps[8];

         v2f vert(appdata i)
         {
            v2f o;

            o.vertex = UnityObjectToClipPos(i.vertex);
            o.uv = i.texcoord;

            float4 clip = float4(o.vertex.xy, 0, 1);
            o.worldDir = mul(clipToWorld, clip) - _WorldSpaceCameraPos;

            return o;
         }
			
         sampler2D _MainTex;
         sampler2D _CameraDepthTexture;
         float4 _MainTex_ST;
         int _NumPoints;
         float _DensityScale;
         float _DepthScale;
         float4 _Color;
         float _Power;
         float _KValue;

        float smoothMin(float a, float b, float k){
            float h = clamp(0.5f + 0.5f * (b-a) / k, 0.0, 1.0);
            return lerp(b, a, h) - k * h * (1.0 - h);
        }

        float smoothMax(float a, float b, float k){
            return smoothMin(a, b, -k);  
		}

         float sampleLightAtPoint(float3 worldPos)
        {
		    float seaDepth = max(-worldPos.y, 0);
		
		    float lightLevel = exp(-seaDepth / _DepthScale); //1 - (seaDepth / depthScale); // exp(-seaDepth * depthScale);
            
	        float total = lightLevel;
	        for (int i = 0; i < 8; i++)
	        {
		        float4 lightPos = lightPositions[i];
		        lightPos = mul(UNITY_MATRIX_IT_MV, lightPos);
		        lightPos = mul(unity_ObjectToWorld, lightPos);

		        float proximity = 1 - saturate(length(lightPos.xyz - worldPos) / lightProps[i].x);
                proximity = pow(proximity, _Power);
		        if (lightPositions[i].w > 0){
                    float l = proximity * lightColors[i].x;
                    total  = smoothMax(total, l, _KValue);
				}
	        }
	        return total;
        }
        
        float2 densityRay(float3 rayOrigin, float3 rayDirection, int numPoints, float depthValue, float densityScale, float depthScale, float3 sunDirection)
        {
	        float3 p = rayOrigin;
	        float stepSize = depthValue / (numPoints - 1);
	        float totalLight = 0;
	        float totalDensity = 0;
	
	        for (int i = 0; i < numPoints; i++)
	        {
		
		        totalDensity += densityScale * stepSize;
		        totalLight += sampleLightAtPoint(p);
		
		        p += rayDirection * stepSize;
	        }
	
	        totalLight /= numPoints;
	
	        totalDensity = min(totalDensity, 1);
	        totalLight = saturate(totalLight);
	
	        return float2(totalDensity, totalLight);

        }

         float4 frag(v2f i) : COLOR
         {
            float2 uv = UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST);
            float4 color = tex2D(_MainTex, uv);
            /*float rawDepth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
            float depth = LinearEyeDepth(rawDepth) * _ProjectionParams.w;*/
            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
			depth = LinearEyeDepth(depth);


            float3 worldPos = i.worldDir * depth + _WorldSpaceCameraPos;
            float3 worldDir = normalize(i.worldDir);

            float2 lightResult = densityRay(_WorldSpaceCameraPos, worldDir, _NumPoints, depth, _DensityScale, _DepthScale, _WorldSpaceLightPos0.xyz);
            float4 waterColor = _Color * lightResult.y;

            return lerp(color, waterColor, lightResult.x);
         }
         ENDCG
      }

   }
   Fallback Off
}