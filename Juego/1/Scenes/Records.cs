using System;
using System.Globalization;
using System.Linq;
using _1.Progression;
using _1.UI;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

namespace _1.Scenes;

public sealed class Records : Escena
{
    private const int FilasVisibles = 8;
    private const float DuracionTransicion = 0.28f;
    private readonly Action<Escena> _cambiarEscena;
    private readonly HistorialRecords _historial = new();
    private RegistroRecord[] _registros = Array.Empty<RegistroRecord>();
    private SpriteFont _fuente;
    private Texture2D _pixel;
    private BotonAnimado _volver;
    private KeyboardState _tecladoAnterior = Keyboard.GetState();
    private int _ruedaAnterior = Mouse.GetState().ScrollWheelValue;
    private int _primeraFila;
    private bool _saliendo;
    private float _tiempoTransicion;
    private float _opacidad;
    private float _desplazamiento = 90f;

    public Records(Action<Escena> cambiarEscena) => _cambiarEscena = cambiarEscena;

    protected override void LoadContent()
    {
        _historial.Cargar();
        _registros = _historial.Registros.OrderByDescending(r => r.Puntaje).ThenByDescending(r => r.Fecha).ToArray();
        _fuente = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
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
        var teclado = Keyboard.GetState();
        var raton = Mouse.GetState();
        bool Pulsada(Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
        if (!_saliendo && _volver.IsEnabled && Core.Instance.IsActive)
        {
            if (Pulsada(Keys.Down) || Pulsada(Keys.S) || Pulsada(Keys.Right) || Pulsada(Keys.D)) _primeraFila++;
            if (Pulsada(Keys.Up) || Pulsada(Keys.W) || Pulsada(Keys.Left) || Pulsada(Keys.A)) _primeraFila--;
            if (Pulsada(Keys.PageDown)) _primeraFila += FilasVisibles;
            if (Pulsada(Keys.PageUp)) _primeraFila -= FilasVisibles;
            if (Pulsada(Keys.Home)) _primeraFila = 0;
            if (Pulsada(Keys.End)) _primeraFila = _registros.Length - FilasVisibles;
            _primeraFila -= (raton.ScrollWheelValue - _ruedaAnterior) / 120;
            _primeraFila = Math.Clamp(_primeraFila, 0, Math.Max(0, _registros.Length - FilasVisibles));
        }
        _tecladoAnterior = teclado;
        _ruedaAnterior = raton.ScrollWheelValue;
        _volver.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
        var vista = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(vista.Width / 1280f, vista.Height / 720f);
        Vector2 centro = new(vista.Width * 0.5f, vista.Height * 0.5f + _desplazamiento);
        Rectangle Area(float x, float y, float ancho, float alto) => new(
            (int)(centro.X + x * escala), (int)(centro.Y + y * escala),
            (int)(ancho * escala), (int)(alto * escala));
        void Texto(string texto, float x, float y, float tamano, bool derecha = false, bool centrado = false)
        {
            Vector2 posicion = centro + new Vector2(x, y) * escala;
            if (derecha) posicion.X -= _fuente.MeasureString(texto).X * tamano * escala;
            else if (centrado) posicion.X -= _fuente.MeasureString(texto).X * tamano * escala * 0.5f;
            MarcadorPuntaje.DibujarTexto(_fuente, texto, posicion, tamano * escala, _opacidad);
        }
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_pixel, Area(-530, -250, 1060, 470), new Color(15, 8, 8) * _opacidad);
        Texto("RECORDS", -_fuente.MeasureString("RECORDS").X * 0.9f, -215, 1.8f);
        Texto("FECHA", -480, -145, 1f);
        Texto("NOMBRE", -210, -145, 1f);
        Texto("NIVEL LLEGADO", 210, -145, 0.85f, centrado: true);
        Texto("PUNTAJE", 480, -145, 1f, derecha: true);
        batch.Draw(_pixel, Area(-490, -115, 980, 2), new Color(130, 130, 130) * _opacidad);
        if (_historial.Error != null || _registros.Length == 0)
        {
            string mensaje = _historial.Error != null ? "NO SE PUDO CARGAR EL HISTORIAL" : "TODAVÍA NO HAY RÉCORDS";
            Texto(mensaje, -_fuente.MeasureString(mensaje).X * 0.45f, 0, 0.9f);
        }
        for (int i = 0; i < FilasVisibles && _primeraFila + i < _registros.Length; i++)
        {
            RegistroRecord registro = _registros[_primeraFila + i];
            float y = -95f + i * 36f;
            Texto(registro.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), -480, y, 0.75f);
            float tamanoNombre = Math.Min(0.95f, 290f / Math.Max(1f, _fuente.MeasureString(registro.Nombre).X));
            Texto(registro.Nombre, -210, y, tamanoNombre);
            string nivel = registro.NivelLlegado?.ToString(CultureInfo.InvariantCulture) ?? "-";
            float tamanoNivel = Math.Min(0.95f, 190f / Math.Max(1f, _fuente.MeasureString(nivel).X));
            Texto(nivel, 210, y, tamanoNivel, centrado: true);
            Texto(registro.Puntaje.ToString("D8", CultureInfo.InvariantCulture), 480, y, 1.05f, derecha: true);
        }
        if (_registros.Length > FilasVisibles)
        {
            batch.Draw(_pixel, Area(505, -100, 4, 288), new Color(65, 65, 65) * _opacidad);
            float alto = Math.Max(24, 288f * FilasVisibles / _registros.Length);
            float y = -100f + (288f - alto) * _primeraFila / (_registros.Length - FilasVisibles);
            batch.Draw(_pixel, Area(505, y, 4, alto), Color.White * _opacidad);
        }
        batch.End();
    }

    protected override void UnloadContent()
    {
        if (_volver != null) { _volver.Click -= Volver; _volver.Dispose(); }
        _pixel?.Dispose();
    }
}
