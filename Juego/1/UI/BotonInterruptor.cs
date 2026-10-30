using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;

namespace _1.UI;

public sealed class BotonInterruptor : Button, IDisposable
{
    private const int AnchoCuadro = 200;
    private const int AltoCuadro = 100;
    private const int CantidadCuadros = 25;
    private const double Duracion = 0.5;
    private const double SegundosPorCuadroWiggle = 0.1;
    private readonly SpriteRuntime _imagen;
    private readonly Texture2D _hoja;
    private int _cuadroActual;
    private bool _activo;
    private double _tiempo;
    private double _tiempoWiggle;
    private bool _disposed;

    public bool Animando { get; private set; }
    public float Opacidad { get; set; } = 1f;

    public BotonInterruptor(Texture2D hoja, bool activo)
    {
        ArgumentNullException.ThrowIfNull(hoja);
        _hoja = hoja;
        if (hoja.Width != AnchoCuadro * CantidadCuadros || hoja.Height != AltoCuadro)
            throw new ArgumentException("El interruptor necesita 25 cuadros de 200 x 100.", nameof(hoja));

        var visual = (ButtonVisual)Visual;
        visual.ButtonCategory.ResetAllStates();
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;
        visual.TextInstance.Visible = false;
        visual.WidthUnits = DimensionUnitType.Absolute;
        visual.HeightUnits = DimensionUnitType.Absolute;
        Width = AnchoCuadro;
        Height = AltoCuadro;
        _imagen = new SpriteRuntime
        {
            Texture = hoja,
            TextureAddress = Gum.Managers.TextureAddress.Custom,
            TextureTop = 0,
            TextureWidth = AnchoCuadro,
            TextureHeight = AltoCuadro
        };
        _imagen.Dock(Gum.Wireframe.Dock.Fill);
        visual.Children.Insert(0, _imagen);
        _activo = activo;
        ActualizarCuadro(activo ? CantidadCuadros - 1 : 0);
    }

    public void CambiarEstado(bool activo)
    {
        if (_disposed || Animando || activo == _activo) return;
        _activo = activo;
        _tiempo = 0;
        _tiempoWiggle = 0;
        Animando = true;
    }

    public void Update(GameTime gameTime)
    {
        if (_disposed) return;
        if (Animando)
        {
            _tiempo = Math.Min(Duracion, _tiempo + gameTime.ElapsedGameTime.TotalSeconds);
            int paso = Math.Min(CantidadCuadros - 1, (int)(_tiempo / Duracion * CantidadCuadros));
            ActualizarCuadro(_activo ? paso : CantidadCuadros - 1 - paso);
            Animando = _tiempo < Duracion;
            if (!Animando) _tiempoWiggle = 0;
        }
        else
        {
            _tiempoWiggle = (_tiempoWiggle + gameTime.ElapsedGameTime.TotalSeconds)
                % (5 * SegundosPorCuadroWiggle);
            int cuadro = (int)(_tiempoWiggle / SegundosPorCuadroWiggle);
            // OFF usa 0..4 y ON 20..24; el cambio completo sigue durando medio segundo.
            ActualizarCuadro(_activo ? 20 + cuadro : cuadro);
        }
        _imagen.Color = Color.White * Opacidad;
    }

    private void ActualizarCuadro(int cuadro)
    {
        _cuadroActual = cuadro;
        _imagen.TextureLeft = cuadro * AnchoCuadro;
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle destino)
    {
        spriteBatch.Draw(_hoja, destino,
            new Rectangle(_cuadroActual * AnchoCuadro, 0, AnchoCuadro, AltoCuadro), Color.White * Opacidad);
    }

    public void Dispose()
    {
        if (_disposed) return;
        IsFocused = false;
        IsEnabled = false;
        _disposed = true;
    }
}
