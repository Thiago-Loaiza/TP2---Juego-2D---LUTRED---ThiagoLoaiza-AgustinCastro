using System;
using System.Collections.Generic;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace _1.Scenes;

/// <summary>Vista provisional entre niveles. Sus salidas abren la prueba de colisiones.</summary>
public sealed class Seleccion : Escena
{
    private enum Estado { Neutral, Escapar, Continuar }
    private readonly Action<Escena> _cambiarEscena;
    private readonly SoundEffectInstance[] _musicas = new SoundEffectInstance[3];
    private readonly Texture2D[] _animaciones = new Texture2D[3];
    private readonly int[] _cantidadFrames = { 11, 30, 24 };
    private readonly List<(Button boton, ColoredRectangleRuntime borde,
        ColoredRectangleRuntime fondo, TextRuntime texto)> _botones = new();
    private readonly float[] _mezcla = { 1f, 0f, 0f };
    private Estado _estado;
    private Texture2D _pixel;
    private TextRuntime _titulo;
    private double _relojEstado;
    private Color _fondo = Color.White;
    private Color _tinta = Color.Black;
    private Color _borde = Color.Black;
    private Color _sinner = Color.Black;

    public Seleccion(Action<Escena> cambiarEscena) => _cambiarEscena = cambiarEscena;

    protected override void LoadContent()
    {
        MediaPlayer.Stop();
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        string[] arte = { "Neutral", "Escapar", "Continuar" };
        string[] musica = { "Neutral", "Duda", "Continuar" };
        for (int i = 0; i < 3; i++)
        {
            _animaciones[i] = Content.Load<Texture2D>($"Sprites/Sinner/PantallaEleccion/Atlas/{arte[i]}");
            // Base blanca premultiplicada: SpriteBatch puede aplicar un tono
            // uniforme sin depender de un efecto ni del negro del dibujo original.
            var pixeles = new Color[_animaciones[i].Width * _animaciones[i].Height];
            _animaciones[i].GetData(pixeles);
            for (int p = 0; p < pixeles.Length; p++)
            {
                byte alfa = pixeles[p].A;
                pixeles[p] = new Color(alfa, alfa, alfa, alfa);
            }
            _animaciones[i].SetData(pixeles);
            _musicas[i] = Content.Load<SoundEffect>($"Music/Eleccion/Eleccion_{musica[i]}").CreateInstance();
            _musicas[i].IsLooped = true;
            _musicas[i].Volume = i == 0 ? 0.45f : 0f;
        }
        // Arrancan una sola vez: las pistas silenciadas conservan su posicion.
        foreach (SoundEffectInstance pista in _musicas) pista.Play();
    }

    public override void Initialize()
    {
        base.Initialize();
        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _titulo = new TextRuntime();
        _titulo.UseCustomFont = true;
        _titulo.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        _titulo.FontScale = 2f;
        _titulo.Anchor(Anchor.Top);
        _titulo.Y = 70;
        _titulo.Text = "Y ahora que?";
        _titulo.Color = Color.Black;
        panel.Visual.Children.Add(_titulo);

        AgregarBoton(panel, "Escapar", -350, 0, () => CambiarEstado(Estado.Escapar));
        AgregarBoton(panel, "Continuar", 350, 0, () => CambiarEstado(Estado.Continuar));
        AgregarBoton(panel, "Confirmar", -350, 0, EntrarAPrueba);
        AgregarBoton(panel, "Volver", 350, 0, () => CambiarEstado(Estado.Neutral));
        // El espacio de modificadores queda vacio; este boton permite probar la salida.
        AgregarBoton(panel, "Entrar", 350, 0, EntrarAPrueba);
        ActualizarOpciones();
    }

    private void AgregarBoton(Panel panel, string texto, float x, float y, Action accion)
    {
        var boton = new Button { Text = texto, Width = 240, Height = 64 };
        boton.Anchor(Anchor.Center);
        boton.X = x;
        boton.Y = y;
        var visual = (ButtonVisual)boton.Visual;
        visual.ButtonCategory.ResetAllStates();
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;
        var borde = new ColoredRectangleRuntime();
        borde.Dock(Dock.Fill);
        visual.Children.Insert(0, borde);
        var fondo = new ColoredRectangleRuntime();
        fondo.Dock(Dock.Fill);
        fondo.Width = -4;
        fondo.Height = -4;
        borde.Children.Add(fondo);
        var etiqueta = visual.TextInstance;
        etiqueta.UseCustomFont = true;
        etiqueta.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        etiqueta.FontScale = 1.4f;
        boton.Click += (_, _) => accion();
        panel.AddChild(boton);
        _botones.Add((boton, borde, fondo, etiqueta));
    }

    private void CambiarEstado(Estado estado)
    {
        _estado = estado;
        // El sprite mantiene un color fijo durante toda la animacion y su loop.
        _sinner = estado == Estado.Escapar ? Color.White : Color.Black;
        _relojEstado = 0;
        _titulo.Text = estado switch
        {
            Estado.Neutral => "Y ahora que?",
            Estado.Escapar => "Estas seguro?",
            _ => ""
        };
        ActualizarOpciones();
    }

    private void ActualizarOpciones()
    {
        for (int i = 0; i < _botones.Count; i++)
        {
            _botones[i].boton.IsFocused = false;
            _botones[i].boton.Visual.Visible = _estado switch
            {
                Estado.Neutral => i < 2,
                Estado.Escapar => i == 2 || i == 3,
                _ => i == 4
            };
            _botones[i].boton.IsEnabled = _botones[i].boton.Visual.Visible;
        }
        _botones[_estado == Estado.Neutral ? 0 : _estado == Estado.Escapar ? 3 : 4].boton.IsFocused = true;
    }

    private void EntrarAPrueba() => _cambiarEscena(new Partida());

    public override void Update(GameTime gameTime)
    {
        double dt = gameTime.ElapsedGameTime.TotalSeconds;
        _relojEstado += dt;
        float suavidad = 1f - MathF.Exp(-8f * (float)dt);
        bool escapar = _estado == Estado.Escapar;
        bool continuar = _estado == Estado.Continuar;
        Color rojo = new Color(165, 12, 24);
        _fondo = Color.Lerp(_fondo, continuar ? rojo : escapar ? Color.Black : Color.White, suavidad);
        _tinta = Color.Lerp(_tinta, escapar ? Color.White : Color.Black, suavidad);
        _borde = Color.Lerp(_borde, continuar ? rojo : escapar ? Color.White : Color.Black, suavidad);
        _titulo.Color = _tinta;
        for (int i = 0; i < 3; i++)
        {
            _mezcla[i] = MathHelper.Lerp(_mezcla[i], i == (int)_estado ? 1f : 0f, suavidad);
            _musicas[i].Volume = _mezcla[i] * 0.45f;
        }
        foreach (var item in _botones)
        {
            item.borde.Color = _borde;
            item.fondo.Color = _fondo;
            item.texto.Color = _tinta;
            item.borde.Height = item.boton.IsFocused ? 68 : 64;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(_fondo);
        var viewport = Core.GraphicsDevice.Viewport;
        var area = new Rectangle(0, 0, viewport.Width, viewport.Height);
        int estado = (int)_estado;
        double fps = estado == 0 ? 8 : 24;
        int cantidad = _cantidadFrames[estado];
        double avance = Math.Floor(_relojEstado * fps);
        int frame;
        if (estado == 0)
            frame = (int)(avance % cantidad);
        else if (avance < cantidad)
            frame = (int)avance;
        else
        {
            // Tras la primera pasada completa, repetir los seis frames finales.
            int framesLoop = Math.Min(6, cantidad);
            frame = cantidad - framesLoop + (int)((avance - cantidad) % framesLoop);
        }
        var origen = new Rectangle(frame % 6 * 640, frame / 6 * 360, 640, 360);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Core.SpriteBatch.Draw(_animaciones[estado], area, origen, _sinner);
        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin();
        const int margen = 20;
        const int grosor = 3;
        Core.SpriteBatch.Draw(_pixel, new Rectangle(margen, margen, viewport.Width - margen * 2, grosor), _borde);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(margen, viewport.Height - margen - grosor, viewport.Width - margen * 2, grosor), _borde);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(margen, margen, grosor, viewport.Height - margen * 2), _borde);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(viewport.Width - margen - grosor, margen, grosor, viewport.Height - margen * 2), _borde);
        Core.SpriteBatch.End();
    }

    protected override void UnloadContent()
    {
        foreach (SoundEffectInstance pista in _musicas) { pista?.Stop(); pista?.Dispose(); }
        foreach (var item in _botones) item.boton.IsFocused = false;
        _pixel?.Dispose();
    }
}
