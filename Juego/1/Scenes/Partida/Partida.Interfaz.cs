using System;
using _1.Entities;
using _1.UI;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace _1.Scenes;

public partial class Partida
{
    private MarcadorPuntaje _marcador;
    private SpriteFont _fuenteContador;
    private Texture2D _indicadorOrbes;
    private const float DuracionTemblorContador = 0.28f;
    private float _temblorContadorRestante;
    private float _relojContador;
    private RelojNivel _relojNivel;
    private PantallaDerrota _pantallaDerrota;
    private BotonAnimado _volverInicio;
    private BordeAnimado _bordePantalla;

    private void CrearInterfazDerrota()
    {
        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();
        _volverInicio = new BotonAnimado(
            Content.Load<Texture2D>("Sprites/UI/Buttons/Button-Sheet"),
            400, 200, 9, TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
        {
            Text = "Volver a la pantalla de inicio",
            Width = 640,
            Height = 100,
            IsEnabled = false
        };
        _volverInicio.Anchor(Anchor.Center);
        _volverInicio.Y = 190;
        _volverInicio.Visual.Visible = false;
        _volverInicio.Click += VolverAlInicio;
        panel.AddChild(_volverInicio);
    }

    private void DibujarBorde()
    {
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        _bordePantalla.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        Core.SpriteBatch.End();
    }

    private void DibujarTemporizador()
    {
        double tiempo = _shame.Activo
            ? _shame.TiempoHastaContacto(_sinner.Colision) : _relojNivel.Restante;
        string texto = RelojNivel.Formatear(tiempo);
        Vector2 posicion = new(
            MathF.Round((Core.GraphicsDevice.Viewport.Width - _fuenteContador.MeasureString(texto).X) * 0.5f),
            24f);
        Color color = _shame.Activo || _relojNivel.Rojo ? Color.Red : Color.White;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    Core.SpriteBatch.DrawString(_fuenteContador, texto,
                        posicion + new Vector2(x, y) * 2f, Color.Black);
                }
            }
        }
        Core.SpriteBatch.DrawString(_fuenteContador, texto, posicion, color);
    }

    private void DibujarContadorOrbes()
    {
        string texto = _enFellDoor ? $"{(int)Math.Ceiling(_supervivenciaFellDoor)}" : $"{_orbesRecogidos}/{TotalOrbes}";
        Vector2 tamano = _fuenteContador.MeasureString(texto);
        Vector2 origen = tamano * 0.5f;
        Vector2 posicion = new(Core.GraphicsDevice.Viewport.Width * 0.5f, 24f + _fuenteContador.LineSpacing + 8f + origen.Y);

        float intensidad = _temblorContadorRestante / DuracionTemblorContador;
        float amplitud = (PocosOrbes ? 3f : 2f) * intensidad * intensidad;
        posicion += new Vector2(
            MathF.Sin(_relojContador * 83f), MathF.Cos(_relojContador * 107f)) * amplitud;
        posicion = new Vector2(MathF.Round(posicion.X), MathF.Round(posicion.Y));

        float pulso = PocosOrbes
            ? 0.5f - 0.5f * MathF.Cos(_relojContador * MathHelper.TwoPi * 1.35f)
            : 0f;
        float rebote = 0.1f * MathF.Sin(intensidad * MathHelper.Pi);
        float escala = 1f + rebote + pulso * 0.025f;
        Color rojo = new(230, 20, 35);

        if (PocosOrbes)
        {
            // El halo respira alrededor del borde; el relleno siempre se dibuja negro encima.
            float radioHalo = 3f + pulso * 3f;
            Color brillo = rojo * (0.04f + pulso * 0.18f);
            for (int i = 0; i < 12; i++)
            {
                float angulo = i * MathHelper.TwoPi / 12f;
                Vector2 desplazamiento = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * radioHalo;
                Core.SpriteBatch.DrawString(_fuenteContador, texto, posicion + desplazamiento,
                    brillo, 0f, origen, escala, SpriteEffects.None, 0f);
            }
        }

        Color borde = Color.Lerp(rojo, new Color(255, 65, 65), pulso);
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }
                Core.SpriteBatch.DrawString(_fuenteContador, texto,
                    posicion + new Vector2(x, y) * 2f, borde,
                    0f, origen, escala, SpriteEffects.None, 0f);
            }
        }
        Core.SpriteBatch.DrawString(_fuenteContador, texto, posicion, Color.Black,
            0f, origen, escala, SpriteEffects.None, 0f);
    }

    private void DibujarIndicadorOrbes(Matrix transformacion)
    {
        if (_enFellDoor || _orbes.Count == 0)
        {
            return;
        }

        Vector2 centroSinner = _sinner.Colision.Centro;
        Vector2 objetivo = _orbes[0].Colision.Centro;
        float distanciaMinima = Vector2.DistanceSquared(centroSinner, objetivo);
        for (int i = 1; i < _orbes.Count; i++)
        {
            Vector2 candidato = _orbes[i].Colision.Centro;
            float distancia = Vector2.DistanceSquared(centroSinner, candidato);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                objetivo = candidato;
            }
        }

        Viewport viewport = Core.GraphicsDevice.Viewport;
        Vector2 origen = Vector2.Transform(centroSinner, transformacion);
        Vector2 objetivoPantalla = Vector2.Transform(objetivo, transformacion);
        Vector2 direccion = objetivoPantalla - origen;
        float distanciaObjetivo = direccion.Length();
        if (distanciaObjetivo < 0.001f)
        {
            return;
        }
        direccion /= distanciaObjetivo;
        Vector2 pivote = new(_indicadorOrbes.Width * 0.5f, _indicadorOrbes.Height * 0.5f);
        // La diagonal deja el icono completo dentro de la pantalla incluso al girar.
        float margen = Math.Min(pivote.Length() + 16f,
            Math.Min(viewport.Width, viewport.Height) * 0.5f - 1f);
        float hastaX = direccion.X == 0f ? float.PositiveInfinity
            : ((direccion.X > 0f ? viewport.Width - margen : margen) - origen.X) / direccion.X;
        float hastaY = direccion.Y == 0f ? float.PositiveInfinity
            : ((direccion.Y > 0f ? viewport.Height - margen : margen) - origen.Y) / direccion.Y;
        float distanciaBorde = Math.Max(0f, Math.Min(hastaX, hastaY));
        // Apuntar antes del orbe: cerca sigue al objetivo, lejos queda limitado al borde.
        float separacionOrbe = Orbe.Radio + pivote.Length() + 8f;
        float distanciaIndicador = MathHelper.Clamp(distanciaObjetivo - separacionOrbe,
            Math.Min(64f, distanciaBorde), distanciaBorde);
        Vector2 posicion = origen + direccion * distanciaIndicador;
        // El dibujo original apunta hacia arriba.
        float rotacion = MathF.Atan2(direccion.Y, direccion.X) + MathHelper.PiOver2;
        Core.SpriteBatch.Draw(_indicadorOrbes,
            new Vector2(MathF.Round(posicion.X), MathF.Round(posicion.Y)),
            null, Color.White, rotacion, pivote, 1f, SpriteEffects.None, 0f);
    }
}
