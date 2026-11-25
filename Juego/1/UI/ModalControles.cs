using System;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _1.UI;

public sealed class ModalControles : IDisposable
{
    private const float DuracionTransicion = 0.28f;
    private static readonly string[] Acciones =
    {
        "Moverse / navegar", "Parry", "Correr", "Dash (Aureola divina)",
        "Aceptar en menus", "Volver / cancelar", "Pausar / reanudar", "Borrar letra del nombre"
    };
    private static readonly string[] Teclado =
    {
        "WASD (flechas en menus)", "Espacio", "Shift", "Q",
        "Espacio / Enter", "Escape", "Escape", "Backspace"
    };
    private const int AnchoCuadroArcade = 600;
    private const float DuracionCuadroArcade = 0.12f;
    private readonly Texture2D _ayudaArcade;
    private float _tiempoArcade;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _fuente;
    private KeyboardState _tecladoAnterior;
    private MouseState _ratonAnterior;
    private float _progreso;
    private bool _cerrando;
    public bool Abierto { get; private set; }
    private float Suave => _progreso * _progreso * (3f - 2f * _progreso);
    private float Escala => Math.Min(Core.GraphicsDevice.Viewport.Width / 1280f,
        Core.GraphicsDevice.Viewport.Height / 720f);

    public ModalControles()
    {
        _fuente = Core.Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _ayudaArcade = Core.Content.Load<Texture2D>("Sprites/UI/ARCADE-AYUDA-Sheet");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Abrir()
    {
        Abierto = true;
        _cerrando = false;
        _progreso = 0f;
        _tiempoArcade = 0f;
        _tecladoAnterior = Keyboard.GetState();
        _ratonAnterior = Mouse.GetState();
    }

    public void Cerrar()
    {
        if (Abierto) _cerrando = true;
    }

    private Rectangle Area(float x, float y, float ancho, float alto)
    {
        var vista = Core.GraphicsDevice.Viewport;
        float desplazamiento = (1f - Suave) * 60f;
        return new Rectangle((int)(vista.Width * 0.5f + x * Escala),
            (int)(vista.Height * 0.5f + (y + desplazamiento) * Escala),
            Math.Max(1, (int)(ancho * Escala)), Math.Max(1, (int)(alto * Escala)));
    }

    private Rectangle AreaCerrar => Area(-110, 257, 220, 42);

    public void Update(GameTime tiempo)
    {
        if (!Abierto) return;
        if (Game1.Arcade)
            _tiempoArcade = (_tiempoArcade + (float)tiempo.ElapsedGameTime.TotalSeconds)
                % (_ayudaArcade.Width / AnchoCuadroArcade * DuracionCuadroArcade);
        float paso = (float)tiempo.ElapsedGameTime.TotalSeconds / DuracionTransicion;
        _progreso = Math.Clamp(_progreso + (_cerrando ? -paso : paso), 0f, 1f);
        if (_cerrando && _progreso == 0f) { Abierto = false; return; }
        var teclado = Keyboard.GetState();
        var raton = Mouse.GetState();
        bool Pulsada(Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
        bool clic = raton.LeftButton == ButtonState.Pressed && _ratonAnterior.LeftButton == ButtonState.Released;
        if (Core.Instance.IsActive && !_cerrando && _progreso == 1f
            && (Pulsada(Keys.Space) || Pulsada(Keys.Enter) || Pulsada(Keys.Escape)
                || (Game1.Arcade && Pulsada(Keys.B)) || (clic && AreaCerrar.Contains(raton.Position))))
            Cerrar();
        _tecladoAnterior = teclado;
        _ratonAnterior = raton;
    }

    public void Draw()
    {
        if (!Abierto) return;
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.LinearClamp);
        batch.Draw(_pixel, Core.GraphicsDevice.Viewport.Bounds, Color.Black * (0.85f * Suave));
        Rectangle marco = Area(-530, -326, 1060, 652);
        int borde = Math.Max(1, (int)(2 * Escala));
        batch.Draw(_pixel, marco, Color.White * Suave);
        batch.Draw(_pixel, new Rectangle(marco.X + borde, marco.Y + borde,
            marco.Width - borde * 2, marco.Height - borde * 2), Color.Black * Suave);
        Texto("CONTROLES", -450, -305, 900, 32, centrado: true);
        if (Game1.Arcade)
        {
            batch.End();
            batch.Begin(samplerState: SamplerState.PointClamp);
            int cuadro = (int)(_tiempoArcade / DuracionCuadroArcade) % (_ayudaArcade.Width / AnchoCuadroArcade);
            batch.Draw(_ayudaArcade, Area(-420, -249, 840, 490),
                new Rectangle(cuadro * AnchoCuadroArcade, 0, AnchoCuadroArcade, _ayudaArcade.Height), Color.White * Suave);
            batch.End();
            batch.Begin(samplerState: SamplerState.LinearClamp);
        }
        else
        {
            Texto("PC", -450, -259, 900, 32, centrado: true);
            DibujarControles(Teclado, -208);
        }
        bool hover = AreaCerrar.Contains(Mouse.GetState().Position);
        batch.Draw(_pixel, AreaCerrar, (hover ? Color.White : new Color(170, 170, 170)) * Suave);
        Rectangle interior = AreaCerrar;
        interior.Inflate(-borde, -borde);
        batch.Draw(_pixel, interior, Color.Black * Suave);
        Texto("Cerrar", -100, 263, 200, 28, centrado: true);
        Texto(Game1.Arcade ? "Escape / Boton 4 para volver" : "Escape para volver",
            -450, 304, 900, 16, centrado: true);
        batch.End();
    }

    private void DibujarControles(string[] teclas, float y)
    {
        for (int i = 0; i < Acciones.Length; i++)
        {
            Texto(Acciones[i], -450, y + i * 54, 425, 40);
            Texto(teclas[i], 5, y + i * 54, 445, 40);
        }
    }

    private void Texto(string texto, float x, float y, float ancho, float alto, bool centrado = false)
    {
        Rectangle area = Area(x, y, ancho, alto);
        Vector2 medida = _fuente.MeasureString(texto);
        float escala = Math.Min(area.Width / Math.Max(1f, medida.X), area.Height / Math.Max(1f, medida.Y));
        Vector2 posicion = centrado ? area.Center.ToVector2() : new Vector2(area.X, area.Center.Y);
        Vector2 origen = new(centrado ? medida.X * 0.5f : 0f, medida.Y * 0.5f);
        Core.SpriteBatch.DrawString(_fuente, texto, posicion, Color.White * Suave,
            0f, origen, escala, SpriteEffects.None, 0f);
    }

    public void Dispose() => _pixel.Dispose();
}
