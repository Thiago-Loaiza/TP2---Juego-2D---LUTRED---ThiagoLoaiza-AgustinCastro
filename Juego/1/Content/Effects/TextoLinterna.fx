#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D Textura : register(s0);
float2 Tamano;
float2 Cursor;
float Radio;

struct EntradaPixel
{
    float4 Posicion : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

float4 Iluminar(EntradaPixel entrada) : COLOR0
{
    float4 muestra = tex2D(Textura, entrada.UV);
    float distancia = length(entrada.UV * Tamano - Cursor);
    float luz = 1.0 - smoothstep(Radio * 0.30, Radio, distancia);
    // Atenuar tambien los limites de la superficie para que nunca aparezcan cortes rectos.
    float2 pixel = entrada.UV * Tamano;
    float2 borde = min(pixel, Tamano - pixel);
    float margen = smoothstep(0.0, 100.0, borde.x) * smoothstep(0.0, 90.0, borde.y);
    return muestra * luz * margen * entrada.Color;
}

technique TextoLinterna
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL Iluminar();
    }
}
