using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

// Enemigo de pantalla: aparece cuando Sinner abusa de la tecla de parry, se lo bloquea y lo ralentiza.
public sealed class TooManyParries
{
    private const int TamanoFrame = 200;
    private const int CantidadFrames = 17;
    private const float SegundosPorFrame = 0.08f;
    // Pulsaciones de la tecla de parry, acertadas o no, que lo invocan dentro de la ventana.
    private const int PulsacionesParaAparecer = 5;
    private const float VentanaParrys = 3f;
    private const float DuracionPresencia = 5f;
    private const float DuracionPresenciaPaz = 3.5f;
    public const float MultiplicadorVelocidad = 0.8f;
    private const float DuracionFundido = 0.3f;
    private const float Margen = 24f;

    private Texture2D _textura;
    private readonly Queue<float> _pulsaciones = new();
    private float _reloj;
    private float _tiempoActivo;
    private float _duracion;
    public bool Activo { get; private set; }

    public void LoadContent(ContentManager content)
    {
        _textura = content.Load<Texture2D>("Sprites/TooManyParries/modelo-TooManyParrys-Sheet");
    }

    public void RegistrarPulsacion(bool pazActiva)
    {
        if (Activo) { return; }
        _pulsaciones.Enqueue(_reloj);
        while (_pulsaciones.Count > 0 && _reloj - _pulsaciones.Peek() >= VentanaParrys) { _pulsaciones.Dequeue(); }
        if (_pulsaciones.Count < PulsacionesParaAparecer) { return; }
        _pulsaciones.Clear();
        _tiempoActivo = 0f;
        _duracion = pazActiva ? DuracionPresenciaPaz : DuracionPresencia;
        Activo = true;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _reloj += dt;
        if (!Activo) { return; }
        _tiempoActivo += dt;
        if (_tiempoActivo >= _duracion) { Activo = false; }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (!Activo) { return; }
        float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        float alfa = Math.Min(1f, Math.Min(_tiempoActivo, _duracion - _tiempoActivo) / DuracionFundido);
        int frame = (int)(_tiempoActivo / SegundosPorFrame) % CantidadFrames;
        // Anclado a la esquina superior derecha, sin importar el tamano de la ventana.
        Vector2 posicion = new(viewport.Width - (Margen + TamanoFrame) * escala, Margen * escala);
        spriteBatch.Draw(_textura, posicion, new Rectangle(frame * TamanoFrame, 0, TamanoFrame, TamanoFrame),
            Color.White * alfa, 0f, Vector2.Zero, escala, SpriteEffects.None, 0f);
    }
}
