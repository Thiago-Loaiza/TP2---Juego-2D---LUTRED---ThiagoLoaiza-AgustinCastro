#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D Textura : register(s0);

// estos valores se configuran desde el juego porque los valores por defecto pueden variar entre plataformas.
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
    // recuperar el color sin premultiplicar conserva los bordes transparentes.
    float3 color = muestra.rgb / max(muestra.a, 0.0001);
    float gris = dot(color, float3(0.299, 0.587, 0.114));
    // fijar el grano a la textura evita parpadeos cuando se mueve la camara.
    float ruido = Ruido(floor(entrada.UV * EscalaGrano)) - 0.5;
    float tinta = saturate((gris - Umbral + ruido * Grano) * Contraste + 0.5);
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
