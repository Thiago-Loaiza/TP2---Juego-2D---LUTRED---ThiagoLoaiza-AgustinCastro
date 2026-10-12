using System;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace _1.Scenes;

public class Inicio : Escena
{
    private readonly Action<Escena> _cambiarEscena;
    private Button _jugar;

    public Inicio(Action<Escena> cambiarEscena)
    {
        _cambiarEscena = cambiarEscena;
    }

    public override void Initialize()
    {
        base.Initialize();

        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _jugar = new Button();
        _jugar.Text = "Jugar";
        var visual = (ButtonVisual)_jugar.Visual;
        visual.ButtonCategory.ResetAllStates();
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;

        var borde = new ColoredRectangleRuntime();
        borde.Color = Color.White;
        borde.Dock(Dock.Fill);
        visual.Children.Insert(0, borde);

        var fondo = new ColoredRectangleRuntime();
        fondo.Color = Color.Black;
        fondo.Dock(Dock.Fill);
        fondo.Width = -4;
        fondo.Height = -4;
        borde.Children.Add(fondo);

        var texto = visual.TextInstance;
        texto.UseCustomFont = true;
        texto.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        texto.FontScale = 1.5f;
        texto.Color = Color.White;
        _jugar.Width = 240;
        _jugar.Height = 64;
        _jugar.Anchor(Anchor.Center);
        _jugar.Click += Jugar;
        panel.AddChild(_jugar);
        _jugar.IsFocused = true;
    }

    private void Jugar(object sender, EventArgs args)
    {
        _cambiarEscena(new Partida());
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
            _jugar.IsFocused = false;
        }
    }
}
