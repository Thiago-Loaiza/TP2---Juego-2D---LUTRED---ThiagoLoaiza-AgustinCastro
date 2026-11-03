using System;
using _1.UI;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace _1.Scenes;

public sealed class Misc : Escena
{
    private const float DuracionTransicion = 0.28f;
    private readonly Action<Escena> _cambiarEscena;
    private Texture2D _pixel;
    private SpriteFont _fuente;
    private BotonAnimado _volver;
    private bool _saliendo;
    private float _tiempoTransicion;
    private float _opacidad;
    private float _desplazamiento = 90f;

    public Misc(Action<Escena> cambiarEscena) => _cambiarEscena = cambiarEscena;

    protected override void LoadContent()
    {
        _fuente = Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
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
            Text = "Volver", Width = 320, Height = 130, Opacidad = 0f, IsEnabled = false
        };
        _volver.Anchor(Anchor.Center);
        _volver.Y = 260 + _desplazamiento;
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
        _tiempoTransicion = Math.Min(DuracionTransicion,
            _tiempoTransicion + (float)gameTime.ElapsedGameTime.TotalSeconds);
        float t = _tiempoTransicion / DuracionTransicion;
        float suave = t * t * (3f - 2f * t);
        _opacidad = _saliendo ? 1f - suave : suave;
        _desplazamiento = 90f * (_saliendo ? suave : 1f - suave);
        _volver.Y = 260 + _desplazamiento;
        _volver.Opacidad = _opacidad;
        if (_tiempoTransicion >= DuracionTransicion)
        {
            if (_saliendo)
            {
                _cambiarEscena(new Inicio(_cambiarEscena, animarEntrada: true));
                return;
            }
            if (!_volver.IsEnabled) { _volver.IsEnabled = true; _volver.IsFocused = true; }
        }
        _volver.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
        var vista = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(vista.Width / 1280f, vista.Height / 720f);
        var area = new Rectangle((int)(vista.Width * 0.5f - 550 * escala),
            (int)(vista.Height * 0.5f - 285 * escala + _desplazamiento),
            (int)(1100 * escala), (int)(480 * escala));
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.LinearClamp);
        batch.Draw(_pixel, area, new Color(85, 12, 12) * _opacidad);
        int borde = Math.Max(1, (int)(2 * escala));
        batch.Draw(_pixel, new Rectangle(area.X + borde, area.Y + borde,
            area.Width - borde * 2, area.Height - borde * 2), new Color(15, 8, 8) * _opacidad);
        const string texto = "Proximamente... LUTRED DLC";
        float tamano = Math.Min(0.6f * escala, (area.Width - 80 * escala) / _fuente.MeasureString(texto).X);
        batch.DrawString(_fuente, texto, new Vector2(area.Center.X, area.Center.Y),
            Color.White * _opacidad, 0f, _fuente.MeasureString(texto) * 0.5f,
            tamano, SpriteEffects.None, 0f);
        batch.End();
    }

    protected override void UnloadContent()
    {
        if (_volver != null) { _volver.Click -= Volver; _volver.Dispose(); }
        _pixel?.Dispose();
    }
}
