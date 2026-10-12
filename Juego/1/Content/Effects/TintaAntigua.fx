#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// SpriteBatch asigna la textura que se esta dibujando al sampler 0.
sampler2D Textura : register(s0);

// Se configuran desde C#: no depender de valores por defecto en OpenGL.
float Umbral;
float Contraste;
float Grano;
float EscalaGrano;
float2 TamanoTexel;
float GrosorTinta;

struct EntradaPixel
{
    float4 Posicion : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

float Ruido(float2 celda)
{
    return frac(sin(dot(celda, float2(12.9898, 78.233))) * 43758.5453);
}

float Luminosidad(float2 uv)
{
    float4 muestra = tex2D(Textura, uv);
    return dot(muestra.rgb / max(muestra.a, 0.0001), float3(0.299, 0.587, 0.114));
}

float4 DibujarTinta(EntradaPixel entrada) : COLOR0
{
    float4 muestra = tex2D(Textura, entrada.UV);
    // Recuperar RGB sin premultiplicar para conservar bordes transparentes.
    float3 color = muestra.rgb / max(muestra.a, 0.0001);
    float gris = dot(color, float3(0.299, 0.587, 0.114));
    // Grano fijo sobre la textura: no parpadea cuando se mueve la camara.
    float ruido = Ruido(floor(entrada.UV * EscalaGrano)) - 0.5;
    float tinta = saturate((gris - Umbral + ruido * Grano) * Contraste + 0.5);
    // Expandir las sombras cercanas marca las juntas como tinta negra.
    float2 paso = TamanoTexel * GrosorTinta;
    float sombra = min(gris, Luminosidad(entrada.UV + float2(paso.x, 0)));
    sombra = min(sombra, Luminosidad(entrada.UV - float2(paso.x, 0)));
    sombra = min(sombra, Luminosidad(entrada.UV + float2(0, paso.y)));
    sombra = min(sombra, Luminosidad(entrada.UV - float2(0, paso.y)));
    tinta *= smoothstep(0.70, 0.78, sombra);
    float alfa = muestra.a * entrada.Color.a;
    return float4(tinta, tinta, tinta, 1.0) * alfa;
}

technique TintaAntigua
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL DibujarTinta();
    }
};
