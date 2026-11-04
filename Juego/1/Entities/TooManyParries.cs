using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

// Al aparecer cuenta nuevos intentos; solo bloquea el parry al alcanzar el limite.
public sealed class TooManyParries
{
    private const int TamanoFrame = 200;
    private const int CantidadFrames = 17;
    private const float SegundosPorFrame = 0.08f;
    // Pulsaciones de la tecla de parry, acertadas o no, que lo invocan dentro de la ventana.
    private const int PulsacionesParaAparecer = 5;
    private const float VentanaParrys = 3f;
    private const float DuracionPresencia = 5f;
    private const float DuracionBloqueo = 5f;
    public const float MultiplicadorVelocidad = 0.8f;
    private const float DuracionFundido = 0.3f;
    private const float Margen = 24f;

    private Texture2D _textura;
    private SpriteFont _fuente;
    private readonly Queue<float> _pulsaciones = new();
    private float _reloj;
    private float _tiempoActivo;
    private float _duracion;
    public bool Activo { get; private set; }
    public int ContadorParrys { get; private set; }
    public int LimiteParrys { get; private set; } = 3;
    public bool BloqueandoParrys => Activo && ContadorParrys >= LimiteParrys;

    public void LoadContent(ContentManager content)
    {
        _textura = content.Load<Texture2D>("Sprites/TooManyParries/modelo-TooManyParrys-Sheet");
        _fuente = content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
    }

    public void RegistrarPulsacion(bool pazActiva)
    {
        if (Activo)
        {
            if (BloqueandoParrys) return;
            ContadorParrys++;
            if (ContadorParrys >= LimiteParrys)
                _duracion = _tiempoActivo + DuracionBloqueo;
            return;
        }
        _pulsaciones.Enqueue(_reloj);
        while (_pulsaciones.Count > 0 && _reloj - _pulsaciones.Peek() >= VentanaParrys) { _pulsaciones.Dequeue(); }
        if (_pulsaciones.Count < PulsacionesParaAparecer) { return; }
        _pulsaciones.Clear();
        _tiempoActivo = 0f;
        _duracion = DuracionPresencia;
        ContadorParrys = 0;
        LimiteParrys = pazActiva ? 5 : 3;
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
        string numero = ContadorParrys.ToString();
        float escalaTexto = escala;
        Vector2 tamano = _fuente.MeasureString(numero) * escalaTexto;
        Vector2 posicionNumero = posicion + new Vector2((TamanoFrame * escala - tamano.X) * 0.5f,
            TamanoFrame * escala + 4f * escala);
        Color borde = Color.Lerp(new Color(200, 200, 200), Color.Black,
            (float)ContadorParrys / LimiteParrys);
        _1.UI.MarcadorPuntaje.DibujarTexto(_fuente, numero, posicionNumero, escalaTexto,
            opacidad: alfa, color: Color.Black, colorBorde: borde, grosorBorde: 2f * escala);
    }
}
