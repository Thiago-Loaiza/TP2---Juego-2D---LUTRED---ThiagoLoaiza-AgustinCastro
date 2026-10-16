using System;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using _1.UI;

namespace _1.Scenes;

public class Inicio : Escena
{
    private readonly Action<Escena> _cambiarEscena;
    private BotonAnimado _jugar;
    private Texture2D _hojaBoton;

    public Inicio(Action<Escena> cambiarEscena)
    {
        _cambiarEscena = cambiarEscena;
    }

    protected override void LoadContent()
    {
        _hojaBoton = Content.Load<Texture2D>("Sprites/UI/Buttons/Button-Sheet");
    }

    public override void Initialize()
    {
        base.Initialize();

        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _jugar = new BotonAnimado(_hojaBoton, 400, 200, 9,
            TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
        {
            Text = "Jugar",
            Width = 320,
            Height = 130
        };
        _jugar.Anchor(Anchor.Center);
        _jugar.Click += Jugar;
        panel.AddChild(_jugar);
        _jugar.IsFocused = true;
    }

    private void Jugar(object sender, EventArgs args)
    {
        _cambiarEscena(new Seleccion(_cambiarEscena));
    }

    public override void Update(GameTime gameTime)
    {
        _jugar.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
    }

    protected override void UnloadContent()
    {
        if (_jugar != null)
        {
            _jugar.Click -= Jugar;
            _jugar.Dispose();
        }
    }
}
