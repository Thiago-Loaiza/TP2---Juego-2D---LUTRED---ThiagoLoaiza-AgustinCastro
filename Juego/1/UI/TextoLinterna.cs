using System;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.UI;

// Texto fijo en el mundo; la luz se calcula en coordenadas locales de su superficie.
public sealed class TextoLinterna : IDisposable
{
    private const int MargenHorizontal = 110;
    private const int MargenVertical = 96;
    private const float Separacion = 240f;
    private const float EscalaTexto = 0.46f;
    private const float RadioLuz = 128f;
    private static readonly Color Fondo = new(32, 32, 32);
    private readonly RenderTarget2D[] _superficies;
    private readonly Effect _linterna;
    private static readonly string[] Frases =
    {
        "Hazlo bien.",
        "Confiesa.",
        "¿Quieres mejorar?",
        "¿Te arrepientes de ellos?",
        "¿Los conoces?"
    };

    public TextoLinterna(ContentManager content, Texture2D pixel)
    {
        _linterna = content.Load<Effect>("Effects/TextoLinterna");
        SpriteFont fuente = content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        var dispositivo = Core.GraphicsDevice;
        _superficies = new RenderTarget2D[Frases.Length];
        var anteriores = dispositivo.GetRenderTargets();
        Viewport vistaAnterior = dispositivo.Viewport;
        var batch = Core.SpriteBatch;
        for (int i = 0; i < Frases.Length; i++)
        {
            Vector2 medida = fuente.MeasureString(Frases[i]) * EscalaTexto;
            int ancho = (int)Math.Ceiling(medida.X) + MargenHorizontal * 2;
            int alto = (int)Math.Ceiling(medida.Y) + MargenVertical * 2;
            var superficie = new RenderTarget2D(dispositivo, ancho, alto);
            _superficies[i] = superficie;
            dispositivo.SetRenderTarget(superficie);
            dispositivo.Clear(Fondo);
            batch.Begin(samplerState: SamplerState.LinearClamp);
            Vector2 posicion = new((ancho - medida.X) * 0.5f, (alto - medida.Y) * 0.5f);
            batch.DrawString(fuente, Frases[i], posicion, Color.White, 0f, Vector2.Zero,
                EscalaTexto, SpriteEffects.None, 0f);
            if (i == 1) RasgarConfesion(batch, pixel, posicion, medida);
            batch.End();
        }
        dispositivo.SetRenderTargets(anteriores);
        dispositivo.Viewport = vistaAnterior;
    }

    private static void RasgarConfesion(SpriteBatch batch, Texture2D pixel, Vector2 posicion, Vector2 medida)
    {
        // Los cortes se mantienen fijos: incluso bajo la luz, Confiesa queda censurada.
        var azar = new Random(471);
        float centroY = posicion.Y + medida.Y * 0.48f;
        for (int i = 0; i < 13; i++)
        {
            Vector2 inicio = new(posicion.X - 4f, centroY + MathHelper.Lerp(-13f, 13f, azar.NextSingle()));
            Vector2 fin = new(posicion.X + medida.X + 4f, centroY + MathHelper.Lerp(-13f, 13f, azar.NextSingle()));
            Linea(inicio, fin, MathHelper.Lerp(2f, 5f, azar.NextSingle()), Fondo);
        }
        for (int i = 0; i < 10; i++)
        {
            float x = posicion.X + azar.NextSingle() * medida.X;
            Linea(new Vector2(x - 6f, centroY - 14f), new Vector2(x + 6f, centroY + 14f), 2f, Fondo);
        }
        Linea(new Vector2(posicion.X - 6f, centroY), new Vector2(posicion.X + medida.X + 6f, centroY), 1.5f, Color.White);

        void Linea(Vector2 a, Vector2 b, float grosor, Color color)
        {
            Vector2 delta = b - a;
            batch.Draw(pixel, a, null, color, MathF.Atan2(delta.Y, delta.X), new Vector2(0f, 0.5f),
                new Vector2(delta.Length(), grosor), SpriteEffects.None, 0f);
        }
    }

    public void Draw(Vector2 centroSuperior, Vector2 posicionSinner, Matrix transformacion)
    {
        var batch = Core.SpriteBatch;
        for (int i = 0; i < _superficies.Length; i++)
        {
            RenderTarget2D superficie = _superficies[i];
            Vector2 tamano = new(superficie.Width, superficie.Height);
            Vector2 inicio = centroSuperior + new Vector2(0f, i * Separacion) - tamano * 0.5f;
            Vector2 cursor = posicionSinner - inicio;
            if (cursor.X < -RadioLuz || cursor.Y < -RadioLuz
                || cursor.X > tamano.X + RadioLuz || cursor.Y > tamano.Y + RadioLuz) continue;
            _linterna.Parameters["Tamano"].SetValue(tamano);
            _linterna.Parameters["Cursor"].SetValue(cursor);
            _linterna.Parameters["Radio"].SetValue(RadioLuz);
            batch.Begin(samplerState: SamplerState.LinearClamp, effect: _linterna, transformMatrix: transformacion);
            batch.Draw(superficie, inicio, Color.White);
            batch.End();
        }
    }

    public void Dispose()
    {
        foreach (var superficie in _superficies) superficie.Dispose();
    }
}
