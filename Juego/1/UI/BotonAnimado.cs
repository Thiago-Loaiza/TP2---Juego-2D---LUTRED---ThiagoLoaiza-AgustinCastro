using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using LutredLibrary.Graphics;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;

namespace _1.UI;

public sealed class BotonAnimado : Button, IDisposable
{
    private static readonly List<BotonAnimado> BotonesConTexto = new();
    private readonly SpriteFont _fuenteSuave;
    private readonly AnimatedSprite _animacion;
    private readonly SpriteRuntime _imagen;
    private readonly TextRuntime _etiqueta;
    private Color _colorTexto;
    private bool _cursorEncima;
    private bool _usarFoco;
    private bool _disposed;
    private MouseState _mouseAnterior;

    public Color ColorNormal { get; set; } = new Color(155, 155, 155);
    public Color ColorIluminado { get; set; } = Color.White;
    public float Opacidad { get; set; } = 1f;
    public bool Iluminado => IsEnabled && (_cursorEncima || (IsFocused && _usarFoco));

    public BotonAnimado(Texture2D hoja, int anchoCuadro, int altoCuadro,
        int cantidadCuadros, TimeSpan demora, Rectangle? recorte = null)
    {
        ArgumentNullException.ThrowIfNull(hoja);
        if (anchoCuadro <= 0 || altoCuadro <= 0
            || hoja.Width % anchoCuadro != 0 || hoja.Height % altoCuadro != 0)
            throw new ArgumentException("La hoja debe contener cuadros completos.");

        int columnas = hoja.Width / anchoCuadro;
        if (cantidadCuadros <= 0 || cantidadCuadros > columnas * (hoja.Height / altoCuadro))
            throw new ArgumentOutOfRangeException(nameof(cantidadCuadros));

        Rectangle area = recorte ?? new Rectangle(0, 0, anchoCuadro, altoCuadro);
        if (area.Width <= 0 || area.Height <= 0 || area.X < 0 || area.Y < 0
            || area.Right > anchoCuadro || area.Bottom > altoCuadro)
            throw new ArgumentOutOfRangeException(nameof(recorte));

        var cuadros = new List<TextureRegion>();
        for (int i = 0; i < cantidadCuadros; i++)
            cuadros.Add(new TextureRegion(hoja,
                i % columnas * anchoCuadro + area.X,
                i / columnas * altoCuadro + area.Y, area.Width, area.Height));

        _animacion = new AnimatedSprite(new Animation(cuadros, demora));
        var visual = (ButtonVisual)Visual;
        visual.ButtonCategory.ResetAllStates();
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;
        visual.WidthUnits = DimensionUnitType.Absolute;
        visual.HeightUnits = DimensionUnitType.Absolute;

        _imagen = new SpriteRuntime
        {
            Texture = hoja,
            TextureAddress = TextureAddress.Custom
        };
        _imagen.Dock(Gum.Wireframe.Dock.Fill);
        visual.Children.Insert(0, _imagen);

        _etiqueta = visual.TextInstance;
        _etiqueta.UseCustomFont = true;
        _etiqueta.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        _etiqueta.FontScale = 1.5f;
        // Conservamos la etiqueta para medir su tamano y posicion originales.
        // Sus letras se dibujan con la fuente de alta resolucion al terminar Gum.
        _etiqueta.Alpha = 0;
        _fuenteSuave = Core.Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        BotonesConTexto.Add(this);
        Width = area.Width;
        Height = area.Height;
        _mouseAnterior = Mouse.GetState();
        Visual.RollOn += Entrar;
        Visual.RollOff += Salir;
        ActualizarVisual();
    }

    public void CambiarHoja(Texture2D hoja)
    {
        ArgumentNullException.ThrowIfNull(hoja);
        if (hoja.Width != _imagen.Texture.Width || hoja.Height != _imagen.Texture.Height)
            throw new ArgumentException("La nueva hoja debe tener las mismas dimensiones.", nameof(hoja));
        _imagen.Texture = hoja;
    }

    public void Update(GameTime gameTime)
    {
        if (_disposed) return;
        if (!IsVisible) { _cursorEncima = false; return; }

        var mouse = Mouse.GetState();
        var teclado = Keyboard.GetState();
        var mando = GamePad.GetState(PlayerIndex.One);
        if (teclado.IsKeyDown(Keys.Tab) || teclado.IsKeyDown(Keys.Up)
            || teclado.IsKeyDown(Keys.W) || teclado.IsKeyDown(Keys.S)
            || teclado.IsKeyDown(Keys.Down) || teclado.IsKeyDown(Keys.Left)
            || teclado.IsKeyDown(Keys.Right)
            || mando.DPad.Up == ButtonState.Pressed || mando.DPad.Down == ButtonState.Pressed
            || mando.DPad.Left == ButtonState.Pressed || mando.DPad.Right == ButtonState.Pressed
            || mando.ThumbSticks.Left.LengthSquared() > 0.25f)
            _usarFoco = true;
        if (mouse.Position != _mouseAnterior.Position || mouse.LeftButton == ButtonState.Pressed)
            _usarFoco = false;
        _mouseAnterior = mouse;

        // no actualizar conserva tanto el cuadro como el tiempo acumulado de la animacion.
        if (IsEnabled && !Iluminado) _animacion.Update(gameTime);
        ActualizarVisual();
    }

    private void Entrar(object sender, EventArgs args)
    {
        _cursorEncima = true;
        ActualizarVisual();
    }

    private void Salir(object sender, EventArgs args)
    {
        _cursorEncima = false;
        ActualizarVisual();
    }

    private void ActualizarVisual()
    {
        Rectangle cuadro = _animacion.Region.Bounds;
        _imagen.TextureLeft = cuadro.X;
        _imagen.TextureTop = cuadro.Y;
        _imagen.TextureWidth = cuadro.Width;
        _imagen.TextureHeight = cuadro.Height;
        Color color = !IsEnabled ? ColorNormal * 0.5f
            : Iluminado ? ColorIluminado : ColorNormal;
        _imagen.Color = color * Opacidad;
        _colorTexto = color * Opacidad;
        _etiqueta.Alpha = 0;
    }

    public static void DibujarTextosSuaves()
    {
        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        foreach (BotonAnimado boton in BotonesConTexto)
        {
            if (boton._disposed || !boton._etiqueta.AbsoluteVisible || string.IsNullOrEmpty(boton.Text))
                continue;
            TextRuntime etiqueta = boton._etiqueta;
            etiqueta.BitmapFont.GetRequiredWidthAndHeight(new[] { boton.Text }, out int ancho, out int alto);
            Vector2 medida = boton._fuenteSuave.MeasureString(boton.Text);
            if (medida.X <= 0 || medida.Y <= 0) continue;
            // Ajustar a las medidas anteriores mantiene el tamano y la ubicacion del texto.
            Vector2 escala = new(ancho * etiqueta.FontScale / medida.X, alto * etiqueta.FontScale / medida.Y);
            Vector2 centro = new(etiqueta.AbsoluteLeft + etiqueta.GetAbsoluteWidth() * 0.5f,
                etiqueta.AbsoluteTop + etiqueta.GetAbsoluteHeight() * 0.5f);
            Core.SpriteBatch.DrawString(boton._fuenteSuave, boton.Text, centro,
                boton._colorTexto, 0f, medida * 0.5f, escala, SpriteEffects.None, 0f);
        }
        Core.SpriteBatch.End();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Visual.RollOn -= Entrar;
        Visual.RollOff -= Salir;
        IsFocused = false;
        IsEnabled = false;
        BotonesConTexto.Remove(this);
        _disposed = true;
    }
}
