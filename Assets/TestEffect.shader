// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "EATGQTG Effect"
{
   Properties
   {
      _MainTex ("Source", 2D) = "white" {}
      _NumPoints("Number of points", int) = 5
      _DensityScale("Density", Range(0, 10)) = 1
      _DepthScale("Depth Scale", float) = 1
      _Color("Color", Color) = (1,1,1,1)
   }
   SubShader
   {
      Cull Off 
      ZWrite Off 
      ZTest Always

      Pass
      {
         CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
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
        //float4[] lightPositions;
        //float4[] lightColors;

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

            return float4(worldPos, 1);

            float2 lightResult = densityRay(_WorldSpaceCameraPos, worldDir, _NumPoints, depth, _DensityScale, _DepthScale, _WorldSpaceLightPos0.xyz);
            float4 waterColor = _Color * lightResult.y;

            return lerp(color, waterColor, lightResult.x);
         }

         

       /*float sampleLightAtPoint(float3 worldPos)
        {
	        float total = 0;
	        for (int i = 0; i < 8; i++)
	        {
		        float4 lightPos = lightPositions[i];
		        lightPos = mul(UNITY_MATRIX_IT_MV, lightPos);
		        lightPos = mul(unity_ObjectToWorld, lightPos);

		        float proximity = saturate(length(lightPos.xyz - worldPos));
		
		        total += lightColors[i].x;
		        //total += proximity * unity_LightColor[i].x;
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
		        float seaDepth = max(-p.y, 0);
		
		        float lightLevel = exp(-seaDepth / depthScale); //1 - (seaDepth / depthScale); // exp(-seaDepth * depthScale);
		
		        totalDensity += densityScale * stepSize;
		        totalLight += 1 * depthScale;// * lightLevel;
		
		        p += rayDirection * stepSize;
	        }
	
	        totalLight /= numPoints;
	
	        totalDensity = min(totalDensity, 1);
	        totalLight = saturate(totalLight);
	
	        return float2(totalDensity, totalLight);

        }*/

         ENDCG
      }

   }
   Fallback "Diffuse"
}