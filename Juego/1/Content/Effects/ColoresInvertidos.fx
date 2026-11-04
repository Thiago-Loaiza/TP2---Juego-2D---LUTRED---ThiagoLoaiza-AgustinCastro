#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D Textura : register(s0);

struct EntradaPixel
{
    float4 Posicion : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

// Invertir la pantalla completa y aumentar el contraste sin perder el color.
float4 DibujarInvertido(EntradaPixel entrada) : COLOR0
{
    float4 muestra = tex2D(Textura, entrada.UV);
    float3 color = muestra.rgb / max(muestra.a, 0.0001);
    color *= entrada.Color.rgb / max(entrada.Color.a, 0.0001);
    color = saturate(((1.0 - color) - 0.5) * 3.0 + 0.5);
    float alfa = muestra.a * entrada.Color.a;
    return float4(color, 1.0) * alfa;
}

technique ColoresInvertidos
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL DibujarInvertido();
    }
};
