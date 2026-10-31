using System;
using System.Collections.Generic;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _1.UI;

// La misma lista se usa en el menu y durante la pausa.
public sealed class ListaOpciones : IDisposable
{
    private const float AltoContenido = 480;
    private readonly BotonInterruptor _correr;
    private readonly BotonInterruptor _silencio;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _fuente;
    private readonly RasterizerState _recorte = new() { ScissorTestEnable = true };
    private MouseState _ratonAnterior = Mouse.GetState();
    private KeyboardState _tecladoAnterior = Keyboard.GetState();
    private float _scroll;
    private int _seleccion = -1;
    private int _arrastrando = -1;
    private float _agarreBarra;
    private Rectangle _area;
    public bool ReanudarSeleccionado => _seleccion == 4;
    public bool AyudaVisible => _area.Contains(Mouse.GetState().Position) || (_seleccion >= 0 && _seleccion < 4);
    private float Escala => Math.Min(Core.GraphicsDevice.Viewport.Width / 1280f,
        Core.GraphicsDevice.Viewport.Height / 720f);
    private float MaxScroll => Math.Max(0, AltoContenido - _area.Height / Escala);

    public ListaOpciones()
    {
        var hoja = Core.Content.Load<Texture2D>("Sprites/UI/Buttons/ON-OFF-Sheet");
        _correr = new BotonInterruptor(hoja, OpcionesJuego.AlternarCorrer);
        _silencio = new BotonInterruptor(hoja, OpcionesJuego.SilenciarMusica);
        _fuente = Core.Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private Rectangle Area(float x, float y, float ancho, float alto) => new(
        _area.X + (int)(x * Escala), _area.Y + (int)((y - _scroll) * Escala),
        (int)(ancho * Escala), (int)(alto * Escala));
    private Rectangle Interruptor(int fila) => Area(390, fila * 120 + 10, 180, 90);
    private Rectangle Pista(int fila) => Area(350, fila * 120 + 48, 190, 10);
    private Rectangle Carril => new(_area.Right - (int)(12 * Escala), _area.Top,
        Math.Max(4, (int)(6 * Escala)), _area.Height);
    private Rectangle Barra
    {
        get
        {
            var carril = Carril;
            int alto = Math.Min(carril.Height, Math.Max((int)(36 * Escala),
                (int)(carril.Height * (_area.Height / Escala) / AltoContenido)));
            return new Rectangle(carril.X, carril.Y + (int)((carril.Height - alto)
                * (MaxScroll > 0 ? _scroll / MaxScroll : 0)), carril.Width, alto);
        }
    }

    public void SincronizarEntrada()
    {
        _ratonAnterior = Mouse.GetState();
        _tecladoAnterior = Keyboard.GetState();
        _arrastrando = -1;
    }

    public void Update(GameTime tiempo, Rectangle area, bool permitirEntrada, bool usarTab = false, bool usarEnter = true)
    {
        _area = area;
        _scroll = Math.Clamp(_scroll, 0, MaxScroll);
        _correr.Update(tiempo);
        _silencio.Update(tiempo);
        _correr.CambiarEstado(OpcionesJuego.AlternarCorrer);
        _silencio.CambiarEstado(OpcionesJuego.SilenciarMusica);
        var raton = Mouse.GetState();
        var teclado = Keyboard.GetState();
        bool pulsado(Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
        bool clic = raton.LeftButton == ButtonState.Pressed && _ratonAnterior.LeftButton == ButtonState.Released;
        if (!permitirEntrada || !Core.Instance.IsActive || raton.LeftButton == ButtonState.Released)
            _arrastrando = -1;
        if (permitirEntrada && Core.Instance.IsActive)
        {
            if (_area.Contains(raton.Position))
            {
                _scroll = Math.Clamp(_scroll - (raton.ScrollWheelValue - _ratonAnterior.ScrollWheelValue)
                    / 120f * 48, 0, MaxScroll);
                if (clic && MaxScroll > 0 && new Rectangle(Carril.X - 8, Carril.Y, Carril.Width + 16, Carril.Height).Contains(raton.Position))
                {
                    _agarreBarra = Barra.Contains(raton.Position) ? raton.Y - Barra.Y : Barra.Height / 2f;
                    _arrastrando = 4;
                }
                else if (clic)
                    for (int fila = 0; fila < 4; fila++)
                    {
                        Rectangle control = fila < 2 ? Interruptor(fila) : Area(340, fila * 120 + 25, 250, 60);
                        if (!control.Contains(raton.Position)) continue;
                        _seleccion = fila;
                        if (fila < 2) Alternar(fila);
                        else if (fila != 2 || !OpcionesJuego.SilenciarMusica) _arrastrando = fila;
                    }
            }
            if (_arrastrando == 4)
                _scroll = Math.Clamp((raton.Y - Carril.Y - _agarreBarra)
                    / Math.Max(1, Carril.Height - Barra.Height) * MaxScroll, 0, MaxScroll);
            else if (_arrastrando >= 2)
            {
                var pista = Pista(_arrastrando);
                EstablecerVolumen(_arrastrando, (int)MathF.Round((raton.X - pista.X) * 100f / pista.Width));
            }
            int cantidad = usarTab ? 5 : 4;
            if (pulsado(Keys.Down) || pulsado(Keys.S) || (usarTab && pulsado(Keys.Tab))) Seleccionar((_seleccion + 1) % cantidad);
            if (pulsado(Keys.Up) || pulsado(Keys.W)) Seleccionar(_seleccion < 0 ? cantidad - 1 : (_seleccion + cantidad - 1) % cantidad);
            if (_seleccion >= 0)
            {
                if (_seleccion < 2 && ((usarEnter && pulsado(Keys.Enter)) || pulsado(Keys.Space) || pulsado(Keys.F))) Alternar(_seleccion);
                if (_seleccion >= 2 && _seleccion < 4 && (pulsado(Keys.Left) || pulsado(Keys.Right)))
                    EstablecerVolumen(_seleccion, (_seleccion == 2 ? OpcionesJuego.VolumenMusica : OpcionesJuego.VolumenSfx)
                        + (pulsado(Keys.Right) ? 5 : -5));
            }
        }
        _ratonAnterior = raton;
        _tecladoAnterior = teclado;
    }

    private void Seleccionar(int fila)
    {
        _seleccion = fila;
        if (fila == 4) return;
        float altoVisible = _area.Height / Escala;
        if (fila * 120 < _scroll) _scroll = fila * 120;
        if (fila * 120 + 120 > _scroll + altoVisible) _scroll = fila * 120 + 120 - altoVisible;
        _scroll = Math.Clamp(_scroll, 0, MaxScroll);
    }

    private void Alternar(int fila)
    {
        if (fila == 0 && !_correr.Animando)
        {
            OpcionesJuego.AlternarCorrer = !OpcionesJuego.AlternarCorrer;
            _correr.CambiarEstado(OpcionesJuego.AlternarCorrer);
        }
        if (fila == 1 && !_silencio.Animando)
        {
            OpcionesJuego.SilenciarMusica = !OpcionesJuego.SilenciarMusica;
            _silencio.CambiarEstado(OpcionesJuego.SilenciarMusica);
        }
    }

    private static void EstablecerVolumen(int fila, int valor)
    {
        valor = Math.Clamp(valor, 0, 100);
        if (fila == 2 && !OpcionesJuego.SilenciarMusica) OpcionesJuego.VolumenMusica = valor;
        if (fila == 3) OpcionesJuego.VolumenSfx = valor;
    }

    public void Draw(Rectangle area, float opacidad = 1)
    {
        _area = area;
        var dispositivo = Core.GraphicsDevice;
        Rectangle anterior = dispositivo.ScissorRectangle;
        dispositivo.ScissorRectangle = Rectangle.Intersect(area, dispositivo.Viewport.Bounds);
        var batch = Core.SpriteBatch;
        _correr.Opacidad = _silencio.Opacidad = opacidad;
        batch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _recorte);
        _correr.Draw(batch, Interruptor(0));
        _silencio.Draw(batch, Interruptor(1));
        for (int fila = 0; fila < 4; fila++)
        {
            batch.Draw(_pixel, Area(10, fila * 120 + 118, 570, 1), Color.DarkRed * opacidad);
            if (fila < 2) continue;
            bool habilitado = fila != 2 || !OpcionesJuego.SilenciarMusica;
            Color color = (habilitado ? Color.Red : Color.DimGray) * opacidad;
            var pista = Pista(fila);
            int valor = fila == 2 ? OpcionesJuego.VolumenMusica : OpcionesJuego.VolumenSfx;
            batch.Draw(_pixel, pista, Color.DarkGray * opacidad);
            batch.Draw(_pixel, new Rectangle(pista.X, pista.Y, pista.Width * valor / 100, pista.Height), color);
            batch.Draw(_pixel, new Rectangle(pista.X + pista.Width * valor / 100 - (int)(5 * Escala),
                pista.Y - (int)(7 * Escala), (int)(10 * Escala), (int)(24 * Escala)), color);
        }
        if (MaxScroll > 0)
        {
            batch.Draw(_pixel, Carril, Color.DarkGray * (opacidad * 0.4f));
            batch.Draw(_pixel, Barra, Color.Red * opacidad);
        }
        batch.End();
        batch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: _recorte);
        string[] nombres = { "Alternar correr", "Silenciar musica", "Volumen musica", "Volumen SFX" };
        for (int fila = 0; fila < 4; fila++)
        {
            bool desactivado = fila == 2 && OpcionesJuego.SilenciarMusica;
            Color color = (desactivado ? Color.Gray : Color.White) * opacidad;
            Texto(nombres[fila], Area(20, fila * 120 + 32, 320, 45), 0.32f, color);
            if (fila >= 2)
                Texto($"{(fila == 2 ? OpcionesJuego.VolumenMusica : OpcionesJuego.VolumenSfx)}",
                    Area(550, fila * 120 + 32, 40, 45), 0.27f, color);
            if (_seleccion == fila)
                Texto(">", Area(0, fila * 120 + 32, 15, 45), 0.25f, Color.Red * opacidad);
        }
        batch.End();
        dispositivo.ScissorRectangle = anterior;
    }

    private void Texto(string texto, Rectangle area, float escala, Color color)
    {
        Core.SpriteBatch.DrawString(_fuente, texto, new Vector2(area.Left, area.Center.Y), color,
            0, new Vector2(0, _fuente.MeasureString(texto).Y * 0.5f),
            escala * Escala, SpriteEffects.None, 0);
    }

    public void DibujarAyuda(Rectangle area, float opacidad = 1)
    {
        if (!AyudaVisible) return;
        var batch = Core.SpriteBatch;
        int borde = Math.Max(1, (int)(2 * Escala));
        batch.Begin(samplerState: SamplerState.LinearClamp);
        batch.Draw(_pixel, area, Color.Red * opacidad);
        batch.Draw(_pixel, new Rectangle(area.X + borde, area.Y + borde,
            area.Width - borde * 2, area.Height - borde * 2), new Color(12, 4, 4) * opacidad);
        float escalaFuente = 0.27f * Escala;
        var lineas = new List<string>();
        string linea = "";
        foreach (string palabra in Ayuda.Split(' '))
        {
            string propuesta = linea.Length == 0 ? palabra : linea + " " + palabra;
            if (linea.Length > 0 && _fuente.MeasureString(propuesta).X * escalaFuente > area.Width - 48 * Escala)
            {
                lineas.Add(linea);
                linea = palabra;
            }
            else linea = propuesta;
        }
        lineas.Add(linea);
        batch.DrawString(_fuente, string.Join("\n", lineas), new Vector2(area.X + 24 * Escala, area.Y + 24 * Escala),
            Color.White * opacidad, 0, Vector2.Zero, escalaFuente, SpriteEffects.None, 0);
        batch.End();
    }

    public string Ayuda
    {
        get
        {
            var raton = Mouse.GetState();
            int fila = _area.Contains(raton.Position)
                ? (int)((raton.Y - _area.Top) / Escala + _scroll) / 120 : _seleccion;
            return fila switch
            {
                0 => "OFF: manten Shift. ON: pulsa Shift para alternar.",
                1 => "Silencia la musica y conserva su volumen.",
                2 when OpcionesJuego.SilenciarMusica => "Desactiva Silenciar musica para ajustar el volumen.",
                2 => "Arrastra para ajustar la musica de 0 a 100.",
                3 => "Arrastra para ajustar los efectos de 0 a 100.",
                _ => "Rueda para bajar. Flechas para elegir y ajustar."
            };
        }
    }

    public void Dispose()
    {
        _correr.Dispose();
        _silencio.Dispose();
        _pixel.Dispose();
        _recorte.Dispose();
    }
}
