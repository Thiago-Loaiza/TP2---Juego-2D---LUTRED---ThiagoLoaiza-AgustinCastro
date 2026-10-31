using System;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using _1.UI;

namespace _1.Scenes;

public sealed class Creditos : Escena
{
    // Edita estos textos para cambiar el titulo y el mensaje bajo los integrantes.
    // Usa \n dentro del mensaje para escribir en otra linea.
    private const string Titulo = "Integrantes";
    private const string MensajeInferior = "Gracias por jugar LUTRED!";

    private const int CuadrosNombres = 9;
    private const float DuracionTransicion = 0.28f;
    private const float DesplazamientoTransicion = 90f;
    private const float PosicionVertical = -35f;
    private readonly Action<Escena> _cambiarEscena;
    private Texture2D _nombreThiago;
    private Texture2D _nombreAgustin;
    private Texture2D _pixel;
    private SpriteFont _fuente;
    private BotonAnimado _volver;
    private SoundEffectInstance _musica;
    private double _tiempo;
    private bool _saliendo;
    private float _tiempoTransicion;
    private float _opacidad;
    private float _desplazamiento = DesplazamientoTransicion;
    private float OffsetY => PosicionVertical + _desplazamiento;

    public Creditos(Action<Escena> cambiarEscena) => _cambiarEscena = cambiarEscena;

    protected override void LoadContent()
    {
        _nombreThiago = Content.Load<Texture2D>("Pharses/Misc/Credits/TL-Sheet");
        _nombreAgustin = Content.Load<Texture2D>("Pharses/Misc/Credits/Sprite-AC-Sheet");
        _fuente = Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        Core.Audio.SongVolume = 0.45f;
        Core.Audio.StopSong();
        _musica = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>("Music/Misc/Creditos"),
            isLooped: true, isMusic: true);
        Core.Audio.PlaySoundEffect(_musica);
    }

    public override void Initialize()
    {
        base.Initialize();
        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();
        _volver = new BotonAnimado(Content.Load<Texture2D>("Sprites/UI/Buttons/Button-Sheet"),
            400, 200, 9, TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
        {
            Text = "Volver",
            Width = 320,
            Height = 130
        };
        _volver.Anchor(Anchor.Center);
        _volver.Y = 250 + OffsetY;
        _volver.Opacidad = 0f;
        _volver.IsEnabled = false;
        panel.AddChild(_volver);
        _volver.Click += Volver;
        _volver.Update(new GameTime());
    }

    private void Volver(object sender, EventArgs args) => VolverAlInicio();

    public void VolverAlInicio()
    {
        if (_saliendo || _tiempoTransicion < DuracionTransicion) return;
        _saliendo = true;
        _tiempoTransicion = 0f;
        _volver.IsFocused = false;
        _volver.IsEnabled = false;
    }

    public override void Update(GameTime gameTime)
    {
        _tiempo += gameTime.ElapsedGameTime.TotalSeconds;
        _tiempoTransicion = Math.Min(DuracionTransicion,
            _tiempoTransicion + (float)gameTime.ElapsedGameTime.TotalSeconds);
        float t = _tiempoTransicion / DuracionTransicion;
        float suave = t * t * (3f - 2f * t);
        _opacidad = _saliendo ? 1f - suave : suave;
        _desplazamiento = DesplazamientoTransicion * (_saliendo ? suave : 1f - suave);
        _volver.Y = 250 + OffsetY;
        _volver.Opacidad = _opacidad;
        if (_tiempoTransicion >= DuracionTransicion)
        {
            if (_saliendo)
            {
                _cambiarEscena(new Inicio(_cambiarEscena, animarEntrada: true));
                return;
            }
            if (!_volver.IsEnabled)
            {
                _volver.IsEnabled = true;
                _volver.IsFocused = true;
            }
        }
        _volver.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
        var vista = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(vista.Width / 1280f, vista.Height / 720f);
        Rectangle Area(float x, float y, float ancho, float alto) => new(
            (int)(vista.Width * 0.5f + x * escala), (int)(vista.Height * 0.5f + (y + OffsetY) * escala),
            (int)(ancho * escala), (int)(alto * escala));
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_pixel, Area(-520, -250, 1040, 440), new Color(12, 4, 4) * _opacidad);
        int cuadro = (int)(_tiempo / 0.12) % CuadrosNombres;
        // Izquierda: Thiago Loaiza. Derecha: Agustin Castro.
        DibujarNombre(_nombreThiago, cuadro, -250, -30, escala);
        DibujarNombre(_nombreAgustin, cuadro, 250, -30, escala);
        batch.End();

        batch.Begin(samplerState: SamplerState.LinearClamp);
        TextoCentrado(Titulo, -180, 0.6f, 900, escala);
        TextoCentrado(MensajeInferior, 105, 0.32f, 900, escala);
        batch.End();
    }

    private void DibujarNombre(Texture2D hoja, int cuadro, float x, float y, float escala)
    {
        int ancho = hoja.Width / CuadrosNombres;
        float factor = Math.Min(430f / ancho, 100f / hoja.Height) * escala;
        var vista = Core.GraphicsDevice.Viewport;
        Core.SpriteBatch.Draw(hoja, new Vector2(vista.Width * 0.5f + x * escala,
            vista.Height * 0.5f + (y + OffsetY) * escala), new Rectangle(cuadro * ancho, 0, ancho, hoja.Height),
            Color.White * _opacidad, 0, new Vector2(ancho, hoja.Height) * 0.5f, factor, SpriteEffects.None, 0);
    }

    private void TextoCentrado(string texto, float y, float tamano, float anchoMaximo, float escala)
    {
        Vector2 medida = _fuente.MeasureString(texto);
        float factor = Math.Min(tamano, anchoMaximo / Math.Max(1, medida.X)) * escala;
        var vista = Core.GraphicsDevice.Viewport;
        Core.SpriteBatch.DrawString(_fuente, texto,
            new Vector2(vista.Width * 0.5f, vista.Height * 0.5f + (y + OffsetY) * escala),
            Color.White * _opacidad, 0, medida * 0.5f, factor, SpriteEffects.None, 0);
    }

    protected override void UnloadContent()
    {
        if (_volver != null)
        {
            _volver.Click -= Volver;
            _volver.Dispose();
        }
        Core.Audio.ReleaseSoundEffect(_musica);
        _pixel?.Dispose();
    }
}
