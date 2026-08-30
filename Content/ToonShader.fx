#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 ViewProjection;
float4 LightDirection;
float4 LightColor;
float4 AmbientColor;
float4 ShaderParams;

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
    
    // input.Position is already in World Space from C# LoadContent
    output.Position = mul(input.Position, ViewProjection);
    
    // input.Normal is already in World Space from C# LoadContent! 
    // No matrix multiplication needed, bypassing the MojoShader optimization crash entirely.
    output.Normal = input.Normal;
    
    output.Color = input.Color;
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float3 N = normalize(input.Normal);
    float3 L = normalize(-LightDirection.xyz);

    float NdotL = max(0.0, dot(N, L));

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