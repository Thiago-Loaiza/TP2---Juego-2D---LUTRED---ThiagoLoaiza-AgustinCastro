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
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace _1.Scenes;

public sealed class Seleccion : Escena
{
    private enum Estado { Neutral, Escapar, Continuar }
    private readonly Action<Escena> _cambiarEscena;
    private readonly SoundEffectInstance[] _musicas = new SoundEffectInstance[3];
    private readonly Texture2D[] _animaciones = new Texture2D[3];
    private readonly int[] _cantidadFrames = { 11, 30, 24 };
    private readonly Texture2D[,] _fondosAnimados = new Texture2D[3, 2];
    private readonly int[] _cuadrosFondo = { 20, 20, 22 };
    private readonly List<(Button boton, ColoredRectangleRuntime borde,
        ColoredRectangleRuntime fondo, TextRuntime texto)> _botones = new();
    private readonly float[] _mezcla = { 1f, 0f, 0f };
    private Estado _estado;
    private TextRuntime _titulo;
    private double _relojEstado;
    private double _relojFondo;
    private float _opacidadDuda;
    private Color _fondo = Color.White;
    private Color _tinta = Color.Black;
    private Color _borde = Color.Black;
    private Color _sinner = Color.Black;

    public Seleccion(Action<Escena> cambiarEscena) => _cambiarEscena = cambiarEscena;

    protected override void LoadContent()
    {
        Core.Audio.StopSong();
        Core.Audio.SongVolume = 1f;
        string[] fondos = { "ElegirV2", "DudaV2", "ContinuarV2" };
        string[] arte = { "Neutral", "Escapar", "Continuar" };
        string[] musica = { "Neutral", "Duda", "Continuar" };
        for (int i = 0; i < 3; i++)
        {
            for (int pagina = 0; pagina < 2; pagina++)
                _fondosAnimados[i, pagina] = Content.Load<Texture2D>(
                    $"Backgrounds/Selection/Atlas/{fondos[i]}-{pagina}");
            _animaciones[i] = Content.Load<Texture2D>($"Sprites/Sinner/PantallaEleccion/Atlas/{arte[i]}");
            // la base blanca premultiplicada permite aplicar un tono uniforme sin depender del negro original.
            var pixeles = new Color[_animaciones[i].Width * _animaciones[i].Height];
            _animaciones[i].GetData(pixeles);
            for (int p = 0; p < pixeles.Length; p++)
            {
                byte alfa = pixeles[p].A;
                pixeles[p] = new Color(alfa, alfa, alfa, alfa);
            }
            _animaciones[i].SetData(pixeles);
            _musicas[i] = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>($"Music/Eleccion/Eleccion_{musica[i]}"), isLooped: true, isMusic: true);
            Core.Audio.SetVolume(_musicas[i], i == 0 ? 0.45f : 0f);
        }
        // las pistas silenciadas siguen avanzando para conservar la sincronizacion de la mezcla.
        foreach (SoundEffectInstance pista in _musicas) Core.Audio.PlaySoundEffect(pista);
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
        _relojFondo += dt;
        float suavidad = 1f - MathF.Exp(-8f * (float)dt);
        bool escapar = _estado == Estado.Escapar;
        bool continuar = _estado == Estado.Continuar;
        _opacidadDuda = MathHelper.Clamp(_opacidadDuda
            + (escapar ? 1f : -1f) * (float)dt / 0.45f, 0f, 1f);
        Color rojo = new Color(165, 12, 24);
        _fondo = Color.Lerp(_fondo, continuar ? rojo : escapar ? Color.Black : Color.White, suavidad);
        _tinta = Color.Lerp(_tinta, escapar ? Color.White : Color.Black, suavidad);
        _borde = Color.Lerp(_borde, continuar ? rojo : escapar ? Color.White : Color.Black, suavidad);
        _titulo.Color = _tinta;
        for (int i = 0; i < 3; i++)
        {
            _mezcla[i] = MathHelper.Lerp(_mezcla[i], i == (int)_estado ? 1f : 0f, suavidad);
            Core.Audio.SetVolume(_musicas[i], _mezcla[i] * 0.45f);
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
            // despues de la primera pasada, solo se repiten los seis cuadros finales.
            int framesLoop = Math.Min(6, cantidad);
            frame = cantidad - framesLoop + (int)((avance - cantidad) % framesLoop);
        }
        var origen = new Rectangle(frame % 6 * 640, frame / 6 * 360, 640, 360);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_estado == Estado.Continuar)
            DibujarFondo(2, area, 1f);
        else
        {
            // el fondo opaco debajo evita un oscurecimiento durante el fundido.
            DibujarFondo(0, area, 1f);
            if (_opacidadDuda > 0f) DibujarFondo(1, area, _opacidadDuda);
        }
        Core.SpriteBatch.Draw(_animaciones[estado], area, origen, _sinner);
        Core.SpriteBatch.End();
    }

    private void DibujarFondo(int estado, Rectangle destino, float opacidad)
    {
        int cuadro = (int)(_relojFondo * 8) % _cuadrosFondo[estado];
        int pagina = cuadro / 12;
        int celda = cuadro % 12;
        var origen = new Rectangle(celda % 3 * 1280, celda / 3 * 720, 1280, 720);
        Core.SpriteBatch.Draw(_fondosAnimados[estado, pagina], destino, origen, Color.White * opacidad);
    }

    protected override void UnloadContent()
    {
        foreach (SoundEffectInstance pista in _musicas) Core.Audio.ReleaseSoundEffect(pista);
        foreach (var item in _botones) item.boton.IsFocused = false;
    }
}
