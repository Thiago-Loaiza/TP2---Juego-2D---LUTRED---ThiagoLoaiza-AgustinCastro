using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum;
using _1.UI;

namespace _1.Scenes;

public class Inicio : Escena
{
    private readonly Action<Escena> _cambiarEscena;
    private BotonAnimado _jugar;
    private AnimatedSprite _titulo;
    private SoundEffectInstance _musicaInicio;
    private Texture2D _hojaBoton;
    private Texture2D _hojaCreditos;
    private Texture2D _hojaMusica;
    private Texture2D _hojaMisc;
    private readonly List<BotonAnimado> _botones = new();

    public Inicio(Action<Escena> cambiarEscena)
    {
        _cambiarEscena = cambiarEscena;
    }

    protected override void LoadContent()
    {
        Core.Audio.SongVolume = 0.45f;
        Core.Audio.StopSong();
        _musicaInicio = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Inicio"), isLooped: true, isMusic: true);
        Core.Audio.PlaySoundEffect(_musicaInicio);
        _hojaBoton = Content.Load<Texture2D>("Sprites/UI/Buttons/Button-Sheet");
        _hojaCreditos = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Credits-Sheet");
        _hojaMusica = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Music-Sheet");
        _hojaMisc = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Misc-Sheet");
        Texture2D hojaTitulo = Content.Load<Texture2D>("Sprites/UI/Titulo-Sheet");
        var cuadrosTitulo = new List<TextureRegion>();
        for (int i = 0; i < 16; i++)
        {
            cuadrosTitulo.Add(new TextureRegion(hojaTitulo, i * 800, 0, 800, 400));
        }
        _titulo = new AnimatedSprite(new Animation(cuadrosTitulo, TimeSpan.FromSeconds(0.12)))
        {
            Origin = new Vector2(400, 200)
        };
    }

    public override void Initialize()
    {
        base.Initialize();

        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _jugar = AgregarBoton(panel, "Jugar", -50);
        _jugar.Click += Jugar;
        AgregarBoton(panel, "Opciones", 85);
        AgregarBoton(panel, "Records", 220);

        // Musica, Misc y Creditos, de izquierda a derecha, con 12 px de separacion.
        AgregarBotonInferior(panel, _hojaMusica, -148);
        AgregarBotonInferior(panel, _hojaMisc, -86);
        AgregarBotonInferior(panel, _hojaCreditos, -24);
        _jugar.IsFocused = true;
    }

    private void AgregarBotonInferior(Panel panel, Texture2D hoja, float x)
    {
        var boton = new BotonAnimado(hoja, 50, 50, 9, TimeSpan.FromSeconds(0.12))
        {
            Text = "",
            Width = 50,
            Height = 50
        };
        boton.Anchor(Anchor.BottomRight);
        boton.X = x;
        boton.Y = -24;
        panel.AddChild(boton);
        _botones.Add(boton);
    }
    private BotonAnimado AgregarBoton(Panel panel, string texto, float y)
    {
        var boton = new BotonAnimado(_hojaBoton, 400, 200, 9,
            TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
        {
            Text = texto,
            Width = 320,
            Height = 130
        };
        boton.Anchor(Anchor.Center);
        boton.Y = y;
        panel.AddChild(boton);
        _botones.Add(boton);
        return boton;
    }

    private void Jugar(object sender, EventArgs args)
    {
        var mejoras = new _1.Progression.EstadoMejoras();
        if (Game1.EmpezarConMejoras)
        {
            _cambiarEscena(new Mejoras(() => _cambiarEscena(new Seleccion(_cambiarEscena, mejoras: mejoras)), mejoras));
            return;
        }
        _cambiarEscena(Game1.EmpezarConEleccion
            ? new Seleccion(_cambiarEscena, mejoras: mejoras)
            : new Partida(_cambiarEscena, mejoras: mejoras));
    }

    public override void Update(GameTime gameTime)
    {
        _titulo.Update(gameTime);
        foreach (BotonAnimado boton in _botones) boton.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
        var viewport = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        _titulo.Position = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f - 210f * escala);
        _titulo.Scale = new Vector2(0.85f * escala);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _titulo.Draw(Core.SpriteBatch);
        Core.SpriteBatch.End();
    }

    protected override void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_musicaInicio);
        if (_jugar != null)
            _jugar.Click -= Jugar;
        foreach (BotonAnimado boton in _botones) boton.Dispose();
        _botones.Clear();
    }
}
