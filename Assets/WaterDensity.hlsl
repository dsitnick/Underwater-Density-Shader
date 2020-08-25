
float biasFunction(float x, float bias)
{
    float k = pow(1 - bias, 3);
    return (x * k) / (x * k - x + 1);
}

uniform float _RedFalloff, _GreenFalloff, _BlueFalloff;

uniform float4x4 _ClipToWorld;
uniform float4 _LightPositions[8];
uniform float4 _LightColors[8];
uniform float4 _LightProps[8];

float4 colorFalloff(float depth)
{
    depth = max(depth, 0);
    float r = exp(-depth / _RedFalloff);
    float g = exp(-depth / _GreenFalloff);
    float b = exp(-depth / _BlueFalloff);
    return float4(r, g, b, 1);
}

float4 getLightColor(float3 worldPos)
{
    float4 result = 0;
    for (int i = 0; i < 8; i++)
    {
        if (_LightPositions[i].w > 0)
        {
            //Point or spotlight
            float4 lightPos = _LightPositions[i];
            //lightPos = mul(UNITY_MATRIX_IT_MV, lightPos);
            //lightPos = mul(unity_ObjectToWorld, lightPos);
            
            float3 v = lightPos.xyz - worldPos;
            float d = dot(v, v);
            float intensity = 1 - d / _LightProps[i].x / _LightProps[i].x;
            intensity = max(0, intensity);
            intensity = pow(intensity, 2) * 5;
            
            result += _LightColors[i] * intensity;
        }
        else
        {
            //Directional light
            
            float3 dir = _LightPositions[i].xyz;
            if (dir.y < 0)
            {
                float seaDepth = (-worldPos.y + 1) / -dir.y;
                result += _LightColors[i] * colorFalloff(seaDepth);
            }
            
        }
    }

    result.a = 1;
    return result;
}