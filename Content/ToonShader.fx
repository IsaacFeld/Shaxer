#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// EXPLICIT REGISTER BINDINGS (Forces exact 16-byte float4 slot allocation)
float4x4 WorldViewProjection : register(c0); // Occupies c0, c1, c2, c3
float4x4 World               : register(c4); // Occupies c4, c5, c6, c7
float4 LightDirection        : register(c8); // Occupies c8
float4 LightColor            : register(c9); // Occupies c9
float4 AmbientColor          : register(c10); // Occupies c10
float4 ShaderParams          : register(c11); // Occupies c11 (x = BandCount)

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