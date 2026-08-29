#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_2_0
    #define PS_SHADERMODEL ps_2_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Pack matrices and vectors to 16-byte boundaries (float4 / float4x4)
cbuffer Parameters : register(b0)
{
    matrix WorldViewProjection;
    matrix World;

    float4 LightDirection; // Use float4 instead of float3 (xyz = direction, w = unused)
    float4 LightColor;     // Use float4 instead of float3 (rgb = color, a = unused)
    float4 AmbientColor;   // Use float4 instead of float3 (rgb = color, a = unused)
    float4 ShaderParams;   // x = BandCount, y,z,w = unused padding
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 Normal : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Normal = mul(float4(input.Normal, 0.0), World).xyz;
    output.Color = input.Color;
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float3 N = normalize(input.Normal);
    float3 L = normalize(-LightDirection.xyz);

    float NdotL = max(0.0, dot(N, L));

    // ShaderParams.x stores BandCount
    float bandCount = max(1.0, ShaderParams.x);
    float quantizedNdotL = floor(NdotL * bandCount) / bandCount;

    float3 totalLight = AmbientColor.rgb + (LightColor.rgb * quantizedNdotL);
    float3 finalColor = input.Color.rgb * totalLight;

    return float4(finalColor, input.Color.a);
}

technique ToonTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};