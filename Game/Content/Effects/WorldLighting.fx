#define MAX_POINT_LIGHTS 8

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 AmbientColor;
float3 DirectionalDirection;
float3 DirectionalColor;

float3 FogColor;
float FogStart;
float FogEnd;

float3 CameraPosition;

int PointLightCount;
float3 PointLightPosition[MAX_POINT_LIGHTS];
float3 PointLightColor[MAX_POINT_LIGHTS];
float PointLightRadius[MAX_POINT_LIGHTS];
float PointLightIntensity[MAX_POINT_LIGHTS];

texture BaseTexture;
sampler TextureSampler = sampler_state
{
    Texture = <BaseTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPos : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
    float Distance : TEXCOORD3;
};

VSOutput VSMain(VSInput input)
{
    VSOutput output;

    float4 worldPos = mul(input.Position, World);
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)World));
    float4 viewPos = mul(worldPos, View);

    output.Position = mul(viewPos, Projection);
    output.WorldPos = worldPos.xyz;
    output.Normal = worldNormal;
    output.TexCoord = input.TexCoord;
    output.Distance = distance(worldPos.xyz, CameraPosition);
    return output;
}

float3 ComputePointLights(float3 normal, float3 worldPos)
{
    float3 total = float3(0.0f, 0.0f, 0.0f);

    [unroll]
    for (int i = 0; i < MAX_POINT_LIGHTS; i++)
    {
        if (i >= PointLightCount)
        {
            break;
        }

        float3 toLight = PointLightPosition[i] - worldPos;
        float dist = length(toLight);
        if (dist <= 0.0001f)
        {
            continue;
        }

        float3 dir = toLight / dist;
        float radius = max(PointLightRadius[i], 0.0001f);
        float attenuation = saturate(1.0f - dist / radius);
        float diffuse = saturate(dot(normal, dir));
        float falloff = PointLightIntensity[i] * attenuation * attenuation;
        total += diffuse * PointLightColor[i] * falloff;
    }

    return total;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    float3 normal = normalize(input.Normal);
    float4 tex = tex2D(TextureSampler, input.TexCoord);
    float3 albedo = tex.rgb;
    float alpha = tex.a;

    float3 lighting = AmbientColor;
    float3 lightDir = normalize(-DirectionalDirection);
    float directional = saturate(dot(normal, lightDir));
    lighting += directional * DirectionalColor;
    lighting += ComputePointLights(normal, input.WorldPos);

    float3 shaded = albedo * lighting;

    float fogRange = max(FogEnd - FogStart, 0.0001f);
    float fogFactor = saturate((FogEnd - input.Distance) / fogRange);
    float3 finalColor = lerp(FogColor, shaded, fogFactor);

    return float4(saturate(finalColor), alpha);
}

technique WorldLighting
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSMain();
        PixelShader = compile ps_3_0 PSMain();
    }
}
