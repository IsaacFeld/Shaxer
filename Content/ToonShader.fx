#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_2_0
    #define PS_SHADERMODEL ps_2_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// --- UNIFORMS (Set from C#) ---
matrix WorldViewProjection;
matrix World;

float3 LightDirection;
float3 LightColor;
float3 AmbientColor;
float BandCount; // Number of lighting bands (e.g., 3.0 or 4.0)

// --- INPUT / OUTPUT STRUCTS ---
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

// --- VERTEX SHADER ---
VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;
    
    // Transform vertex position into screen space
    output.Position = mul(input.Position, WorldViewProjection);
    
    // Transform normal into world space
    output.Normal = mul(float4(input.Normal, 0.0), World).xyz;
    output.Color = input.Color;

    return output;
}

// --- PIXEL SHADER ---
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float3 N = normalize(input.Normal);
    float3 L = normalize(-LightDirection);

    // Standard Diffuse (Lambertian) Lighting (-1 to 1 mapped to 0 to 1)
    float NdotL = max(0.0, dot(N, L));

    // QUANTIZATION: Step continuous lighting into flat bands
    float quantizedNdotL = floor(NdotL * BandCount) / BandCount;

    // Combine stepped directional light with ambient color
    float3 totalLight = AmbientColor + (LightColor * quantizedNdotL);

    // Apply lighting to vertex color
    float3 finalColor = input.Color.rgb * totalLight;

    return float4(finalColor, input.Color.a);
}

// --- TECHNIQUE ---
technique ToonTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};