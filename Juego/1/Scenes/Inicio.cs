using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
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
    private SoundEffectInstance _musicaInicio;
    private Texture2D _hojaBoton;
    private Texture2D _hojaCreditos;
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
    }

    public override void Initialize()
    {
        base.Initialize();

        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _jugar = AgregarBoton(panel, "Jugar", -150);
        _jugar.Click += Jugar;
        AgregarBoton(panel, "Opciones", 0);
        AgregarBoton(panel, "Records", 150);

        var creditos = new BotonAnimado(_hojaCreditos, 50, 50, 9, TimeSpan.FromSeconds(0.12))
        {
            Text = "",
            Width = 50,
            Height = 50
        };
        creditos.Anchor(Anchor.BottomRight);
        creditos.X = -24;
        creditos.Y = -24;
        panel.AddChild(creditos);
        _botones.Add(creditos);
        _jugar.IsFocused = true;
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
        foreach (BotonAnimado boton in _botones) boton.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
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
