using System;
using System.Collections.Generic;
using LutredLibrary;
using LutredLibrary.Graphics;
using LutredLibrary.Scenes;
using _1.Progression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Scenes;

public sealed class Salvacion : Escena
{
    private const float DuracionManos = 0.3f;
    private const float DuracionTitulo = 3.5f;
    private const float DuracionFrase = 5f;
    private const float DuracionSalida = 2.5f;
    private const float DuracionNegro = 1f;
    private const float InicioFrase = DuracionManos + DuracionTitulo;
    private const float InicioSalida = InicioFrase + DuracionFrase;
    private readonly Action<Escena> _cambiarEscena;
    private readonly string _puntaje;
    private readonly int _puntajeFinal;
    private readonly int? _nivelLlegado;
    private readonly DateTimeOffset _fechaEscape = DateTimeOffset.Now;
    private readonly string _frase = Random.Shared.Next(2) == 0
        ? "Al final pudiste.\nSabía que lo lograrías."
        : "Tuve fe en ti,\npero tú tuviste mucha más.";
    private AnimatedSprite _manos;
    private SpriteFont _fuente;
    private SpriteFont _fuenteFrase;
    private SoundEffectInstance _audio;
    private float _tiempo;
    private bool _terminado;

    public Salvacion(Action<Escena> cambiarEscena, int puntaje = 0, int? nivelLlegado = null)
    {
        _cambiarEscena = cambiarEscena;
        _nivelLlegado = nivelLlegado;
        _puntajeFinal = Math.Clamp(puntaje, 0, EstadoPuntaje.PuntajeMaximo);
        _puntaje = _puntajeFinal.ToString("D8");
    }

    protected override void LoadContent()
    {
        Core.Audio.StopSong();
        Core.Audio.SongVolume = 1f;
        Texture2D hojaManos = Content.Load<Texture2D>("Backgrounds/LUTRED_ESCAPE");
        const int cantidadFrames = 4;
        int anchoFrame = hojaManos.Width / cantidadFrames;
        var cuadros = new List<TextureRegion>();
        for (int i = 0; i < cantidadFrames; i++)
            cuadros.Add(new TextureRegion(hojaManos, i * anchoFrame, 0, anchoFrame, hojaManos.Height));
        _manos = new AnimatedSprite(new Animation(cuadros, TimeSpan.FromSeconds(DuracionManos / cantidadFrames)))
        {
            IsLooping = false,
            Origin = new Vector2(anchoFrame, hojaManos.Height) * 0.5f
        };
        _fuente = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _fuenteFrase = Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _audio = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>("SFX/LUTRED_Salvation"));
        Core.Audio.PlaySoundEffect(_audio);
    }

    public override void Update(GameTime gameTime)
    {
        if (_terminado) return;
        if (_tiempo < DuracionManos) _manos.Update(gameTime);
        _tiempo += (float)gameTime.ElapsedGameTime.TotalSeconds;
        float salida = Math.Clamp((_tiempo - InicioSalida) / DuracionSalida, 0f, 1f);
        Core.Audio.SetVolume(_audio, 1f - salida);
        if (_tiempo >= InicioSalida + DuracionSalida + DuracionNegro)
        {
            _terminado = true;
            _cambiarEscena(new RegistrarPuntaje(_cambiarEscena, _puntajeFinal, _fechaEscape, _nivelLlegado));
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(_tiempo < InicioFrase ? Color.White : Color.Black);
        var vista = Core.GraphicsDevice.Viewport;
        Vector2 centro = new(vista.Width * 0.5f, vista.Height * 0.5f);
        float escala = Math.Min(vista.Width / 1280f, vista.Height / 720f);
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: _tiempo >= InicioFrase ? SamplerState.LinearClamp : SamplerState.PointClamp);
        if (_tiempo < DuracionManos)
        {
            float ajuste = Math.Min(vista.Width / (float)_manos.Region.Width,
                vista.Height / (float)_manos.Region.Height);
            _manos.Position = centro;
            _manos.Scale = new Vector2(ajuste);
            _manos.Draw(batch);
        }
        else if (_tiempo < InicioFrase)
        {
            DibujarCintas(centro, escala, vista.Width, vista.Height);
        }
        else if (_tiempo < InicioSalida + DuracionSalida)
        {
            DibujarFrase(centro, escala, vista.Width);
        }
        batch.End();
    }

    private void DibujarCintas(Vector2 centro, float escala, int ancho, int alto)
    {
        const string texto = "LUTRED";
        var batch = Core.SpriteBatch;
        Vector2 medida = _fuente.MeasureString(texto);
        float tiempo = _tiempo - DuracionManos;
        float tamano = 2.6f * escala;
        float paso = medida.X * tamano + 8f * escala;
        // Nueve cintas verticales con texto girado; las copias se reciclan fuera de pantalla.
        for (int columna = -4; columna <= 4; columna++)
        {
            float direccion = columna % 2 == 0 ? 1f : -1f;
            float avance = ((tiempo * 480f * escala * direccion + columna * paso / 9f) % paso + paso) % paso;
            float x = centro.X + columna * 38f * escala;
            Color tinta = columna % 3 == 0 ? Color.Black : new Color(80, 80, 80);
            for (float y = -paso + avance; y < alto + paso; y += paso)
            {
                batch.DrawString(_fuente, texto, new Vector2(x, y), tinta,
                    -MathHelper.PiOver2, medida * 0.5f, tamano, SpriteEffects.None, 0f);
            }
        }

        // La cinta horizontal cruza las verticales durante toda la fase del titulo.
        float avanceHorizontal = tiempo * 650f * escala % paso;
        for (float x = -paso - avanceHorizontal; x < ancho + paso; x += paso)
        {
            Vector2 posicion = new(x, centro.Y);
            Vector2 origen = new(0, medida.Y * 0.5f);
            // El contorno blanco permite leer el cruce sin ocultar las cintas de atras.
            for (int i = 0; i < 8; i++)
            {
                float angulo = i * MathHelper.TwoPi / 8f;
                Vector2 borde = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * 2f * escala;
                batch.DrawString(_fuente, texto, posicion + borde, Color.White,
                    0f, origen, tamano, SpriteEffects.None, 0f);
            }
            batch.DrawString(_fuente, texto, posicion, Color.Black,
                0f, origen, tamano, SpriteEffects.None, 0f);
        }

        // El titulo fijo se dibuja por encima de las cintas para conservar la legibilidad.
        float tamanoTitulo = Math.Min(4.5f * escala, ancho * 0.75f / medida.X);
        for (int i = 0; i < 16; i++)
        {
            float angulo = i * MathHelper.TwoPi / 16f;
            Vector2 borde = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * 5f * escala;
            batch.DrawString(_fuente, texto, centro + borde, Color.Black,
                0f, medida * 0.5f, tamanoTitulo, SpriteEffects.None, 0f);
        }
        batch.DrawString(_fuente, texto, centro, Color.White,
            0f, medida * 0.5f, tamanoTitulo, SpriteEffects.None, 0f);
    }

    private void DibujarFrase(Vector2 centro, float escala, int ancho)
    {
        float tiempoFrase = _tiempo - InicioFrase;
        float salida = Math.Clamp((_tiempo - InicioSalida) / DuracionSalida, 0f, 1f);
        float opacidad = 1f - salida * salida * (3f - 2f * salida);
        string puntaje = "PUNTAJE " + _puntaje;
        float escalaPuntaje = 1.25f * escala;
        float entradaPuntaje = Math.Clamp((tiempoFrase - 1.6f) / 0.4f, 0f, 1f);
        _1.UI.MarcadorPuntaje.DibujarTexto(_fuente, puntaje,
            centro + new Vector2(-_fuente.MeasureString(puntaje).X * escalaPuntaje * 0.5f, 110f * escala),
            escalaPuntaje, opacidad * entradaPuntaje);
        string[] lineas = _frase.Split('\n');
        int indiceLetra = 0;
        float retrasoLetra = 1.6f / Math.Max(1, _frase.Length - 1);
        float escalaFuente = 1.5f * escala * _fuente.LineSpacing / _fuenteFrase.LineSpacing;
        foreach (string linea in lineas)
            escalaFuente = Math.Min(escalaFuente, ancho * 0.85f / _fuenteFrase.MeasureString(linea).X);

        for (int i = 0; i < lineas.Length; i++)
        {
            string linea = lineas[i];
            Vector2 medida = _fuenteFrase.MeasureString(linea);
            Vector2 posicion = centro - medida * escalaFuente * 0.5f
                + new Vector2(0f, (i - (lineas.Length - 1) * 0.5f) * 45f * escala);
            for (int letra = 0; letra < linea.Length; letra++)
            {
                float entrada = Math.Clamp((tiempoFrase - (indiceLetra + letra) * retrasoLetra) / 0.4f, 0f, 1f);
                string caracter = linea[letra].ToString();
                float x = _fuenteFrase.MeasureString(linea.Substring(0, letra + 1)).X
                    - _fuenteFrase.MeasureString(caracter).X;
                Core.SpriteBatch.DrawString(_fuenteFrase, caracter, posicion + new Vector2(x * escalaFuente, 0f),
                    Color.White * (entrada * opacidad), 0f, Vector2.Zero, escalaFuente, SpriteEffects.None, 0f);
            }
            indiceLetra += linea.Length + 1;
        }
    }

    protected override void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_audio);
    }
}
