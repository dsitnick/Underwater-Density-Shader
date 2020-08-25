// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Density Effect"
{
   Properties
   {
      _MainTex ("Source", 2D) = "white" {}
      _NoiseTex ("Noise Tex", 2D) = "white" {}
      _MaxDistance("Max Distance", Range(0,1000)) = 100
      _NumPoints("Number of points", int) = 5
      _DensityScale("Density", float) = 1
      _DepthScale("Depth Scale", float) = 1
      _Color("Color", Color) = (1,1,1,1)
      _Power("Light Power", Range(0, 10)) = 1
      _KValue("K Value", Range(-10, 10)) = 1
      _RedFalloff("Red Falloff", Range(0, 100)) = 1
      _GreenFalloff("Green Falloff", Range(0, 100)) = 1
      _BlueFalloff("Blue Falloff", Range(0, 100)) = 1
      _NoiseAmount("Noise Amount", Range(0, 1)) = 0.1
      _NoiseSpeed("Noise Speed", Range(0, 100)) = 1
      _NoiseScale("Noise Scale", Range(0, 100)) = 1
   }
   SubShader
   {
      Cull Off 
      ZWrite Off 
      ZTest Always

      Tags { "Queue"="Overlay"}

      Pass
      {
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
			
         #include "UnityCG.cginc"
         #include "WaterDensity.hlsl"
         #include "Lighting.cginc"

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

         v2f vert(appdata i)
         {
            v2f o;

            o.vertex = UnityObjectToClipPos(i.vertex);
            o.uv = i.texcoord;

            float4 clip = float4(o.vertex.xy, 0, 1);
            o.worldDir = mul(_ClipToWorld, clip) - _WorldSpaceCameraPos;

            return o;
         }
			
         sampler2D _MainTex, _NoiseTex;
         sampler2D _CameraDepthTexture;
         float4 _MainTex_ST;
         int _NumPoints;
         float _DensityScale, _DepthScale, _MaxDistance;
         float4 _Color;
         float _Power;
         float _KValue;
         float _NoiseAmount, _NoiseSpeed, _NoiseScale;

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
		        float4 lightPos = _LightPositions[i];
		        lightPos = mul(UNITY_MATRIX_IT_MV, lightPos);
		        lightPos = mul(unity_ObjectToWorld, lightPos);

		        float proximity = 1 - saturate(length(lightPos.xyz - worldPos) / _LightProps[i].x);
                proximity = pow(proximity, _Power);
		        if (_LightPositions[i].w > 0){
                    float l = proximity * _LightColors[i].x;
                    total  = smoothMax(total, l, _KValue);
				}
	        }
	        return total;
        }

        float4 sampleColorAtPoint(float3 worldPos){
		    float seaDepth = max(-worldPos.y, 0) + 5;
            float4 sunColor = colorFalloff(seaDepth) * _LightColor0;

            float4 lightsColor = 0;
            /*
            for (int i = 0; i < 8; i++)
	        {
		        float4 lightPos = lightPositions[i];
		        lightPos = mul(UNITY_MATRIX_IT_MV, lightPos);
		        lightPos = mul(unity_ObjectToWorld, lightPos);

		        float proximity = 1 - saturate(length(lightPos.xyz - worldPos) / lightProps[i].x);
                proximity = pow(proximity, _Power);
		        if (lightPositions[i].w > 0){
                    float l = proximity * lightColors[i].x;
                    lightsColor  = smoothMax(lightsColor, l, _KValue);
				}
	        }*/

            return sunColor + float4(lightsColor);
		}

        float4 getRayColor(float3 rayOrigin, float3 rayDirection, int numPoints, float rayLength, float densityScale, float depthScale, float4 originalColor){
              float3 p = rayOrigin;
              float stepSize = rayLength / (numPoints - 1);
              float4 resultColor = originalColor;

              for (int i = 0; i < numPoints - 1; i++){
                p += rayDirection * stepSize;

                float4 sampleColor = getLightColor(p);

                float t = stepSize / densityScale;
                t = clamp(t, 0, 1);

                resultColor = lerp(resultColor, sampleColor, t);
			  }
              resultColor.a = 1;
              return resultColor;
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

            float3 nearPos = _WorldSpaceCameraPos + i.worldDir * _ProjectionParams.x;
            if (nearPos.y > 0){
                
                if (worldPos.y >= 0){
                    return color;
				}
			}

            float4 resultColor = getRayColor(_WorldSpaceCameraPos, worldDir, _NumPoints, min(depth, _MaxDistance), _DensityScale, _DepthScale, color);
            float noiseVal = tex2D(_NoiseTex, uv * _NoiseScale + _NoiseSpeed * _Time);
            noiseVal = (2 * noiseVal) - 1;

            return resultColor + noiseVal * _NoiseAmount;

            /*float2 lightResult = densityRay(_WorldSpaceCameraPos, worldDir, _NumPoints, depth  * _ProjectionParams.w, _DensityScale, _DepthScale, _WorldSpaceLightPos0.xyz);
            float4 waterColor = _Color * lightResult.y;

            return lerp(color, waterColor, lightResult.x);*/
         }
         ENDCG
      }

   }
   Fallback Off
}