#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4 LightDirection;
    float4 LightColor;
    float4 AmbientColor;
    float4 ShaderParams;
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
    
    // Explicit matrix multiplication
    output.Position = mul(input.Position, WorldViewProjection);
    output.Normal = mul(input.Normal, (float3x3)World);
    output.Color = input.Color;
    
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float3 N = normalize(input.Normal);
    float3 L = normalize(-LightDirection.xyz);

    float NdotL = max(0.0, dot(N, L));

    // Quantize light intensity
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