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
    private readonly bool _incluirFun;
    private int CantidadOpciones => _incluirFun ? 10 : 9;
    private static readonly (int Fila, string Titulo)[] Categorias =
    { (0, "JUGABILIDAD"), (1, "AUDIO"), (4, "RANGOS"), (7, "VISUAL"), (9, "\"FUN\"") };
    private float AltoContenido => InicioFila(CantidadOpciones - 1) + 120;
    private static float InicioFila(int fila)
    {
        float y = fila * 120;
        foreach (var categoria in Categorias)
            if (categoria.Fila <= fila) y += 45;
        return y;
    }
    private readonly Dictionary<int, (BotonInterruptor Boton, Func<bool> Leer, Action<bool> Escribir)> _interruptores = new();
    private readonly Texture2D _pixel;
    private readonly SpriteFont _fuente;
    private readonly RasterizerState _recorte = new() { ScissorTestEnable = true };
    private MouseState _ratonAnterior = Game1.RatonVirtual();
    private KeyboardState _tecladoAnterior = Keyboard.GetState();
    private readonly RepeticionNavegacion _vertical = new();
    private readonly RepeticionNavegacion _horizontal = new();
    private float _scroll;
    private int _seleccion = -1;
    private int _arrastrando = -1;
    private float _agarreBarra;
    private Rectangle _area;
    public bool ReanudarSeleccionado => _seleccion == CantidadOpciones;
    public bool SalirSeleccionado => _seleccion == CantidadOpciones + 1;
    public bool AyudaVisible => _area.Contains(Game1.RatonVirtual().Position) || (_seleccion >= 0 && _seleccion < CantidadOpciones);
    private float Escala => Math.Min(Game1.VistaPartida.Width / 1280f,
        Game1.VistaPartida.Height / 720f);
    private float MaxScroll => Math.Max(0, AltoContenido - _area.Height / Escala);

    public ListaOpciones(bool incluirFun = false)
    {
        _incluirFun = incluirFun;
        var hoja = Core.Content.Load<Texture2D>("Sprites/UI/Buttons/ON-OFF-Sheet");
        void Agregar(int fila, Func<bool> leer, Action<bool> escribir)
            => _interruptores.Add(fila, (new BotonInterruptor(hoja, leer()), leer, escribir));
        Agregar(0, () => OpcionesJuego.AlternarCorrer, v => OpcionesJuego.AlternarCorrer = v);
        Agregar(1, () => OpcionesJuego.SilenciarMusica, v => OpcionesJuego.SilenciarMusica = v);
        Agregar(4, () => OpcionesJuego.SilenciarLocutor, v => OpcionesJuego.SilenciarLocutor = v);
        Agregar(6, () => OpcionesJuego.MultiplicadorTotal, v => OpcionesJuego.MultiplicadorTotal = v);
        Agregar(7, () => OpcionesJuego.MostrarFps, v => OpcionesJuego.MostrarFps = v);
        Agregar(8, () => OpcionesJuego.PantallaCompleta, v => OpcionesJuego.PantallaCompleta = v);
        if (_incluirFun) Agregar(9, () => Game1.FastTrack, v => Game1.FastTrack = v);
        _fuente = Core.Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private Rectangle Area(float x, float y, float ancho, float alto) => new(
        _area.X + (int)(x * Escala), _area.Y + (int)((y - _scroll) * Escala),
        (int)(ancho * Escala), (int)(alto * Escala));
    private Rectangle Interruptor(int fila) => Area(390, InicioFila(fila) + 10, 180, 90);
    private bool EsInterruptor(int fila) => _interruptores.ContainsKey(fila);
    private static bool EsSlider(int fila) => fila == 2 || fila == 3 || fila == 5;
    private static int MinimoSlider(int fila) => fila == 5 ? 25 : 0;
    private static int ValorSlider(int fila) => fila == 2 ? OpcionesJuego.VolumenMusica
        : fila == 3 ? OpcionesJuego.VolumenSfx : OpcionesJuego.VisibilidadRangos;
    private Rectangle Pista(int fila) => Area(350, InicioFila(fila) + 48, 190, 10);
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
        _ratonAnterior = Game1.RatonVirtual();
        _tecladoAnterior = Keyboard.GetState();
        _vertical.Reiniciar();
        _horizontal.Reiniciar();
        _arrastrando = -1;
    }

    public void Update(GameTime tiempo, Rectangle area, bool permitirEntrada, bool usarTab = false, bool usarEnter = true,
        bool incluirSalir = false)
    {
        _area = area;
        _scroll = Math.Clamp(_scroll, 0, MaxScroll);
        foreach (var control in _interruptores.Values)
        {
            control.Boton.Update(tiempo);
            control.Boton.CambiarEstado(control.Leer());
        }
        var raton = Game1.RatonVirtual();
        var teclado = Keyboard.GetState();
        bool entradaActiva = permitirEntrada && Core.Instance.IsActive;
        int vertical = _vertical.Update(tiempo, RepeticionNavegacion.Arriba(teclado), RepeticionNavegacion.Abajo(teclado), entradaActiva);
        int horizontal = _horizontal.Update(tiempo, RepeticionNavegacion.Izquierda(teclado), RepeticionNavegacion.Derecha(teclado), entradaActiva);
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
                    _arrastrando = CantidadOpciones;
                }
                else if (clic)
                    for (int fila = 0; fila < CantidadOpciones; fila++)
                    {
                        Rectangle control = EsInterruptor(fila) ? Interruptor(fila) : Area(340, InicioFila(fila) + 25, 250, 60);
                        if (!control.Contains(raton.Position)) continue;
                        _seleccion = fila;
                        if (EsInterruptor(fila)) Alternar(fila);
                        else if (fila != 2 || !OpcionesJuego.SilenciarMusica) _arrastrando = fila;
                    }
            }
            if (_arrastrando == CantidadOpciones)
                _scroll = Math.Clamp((raton.Y - Carril.Y - _agarreBarra)
                    / Math.Max(1, Carril.Height - Barra.Height) * MaxScroll, 0, MaxScroll);
            else if (EsSlider(_arrastrando))
            {
                var pista = Pista(_arrastrando);
                int minimo = MinimoSlider(_arrastrando);
                EstablecerSlider(_arrastrando, minimo + (int)MathF.Round((raton.X - pista.X) * (100f - minimo) / pista.Width));
            }
            int cantidad = CantidadOpciones + (incluirSalir ? 2 : usarTab ? 1 : 0);
            bool izquierda = horizontal < 0;
            bool derecha = horizontal > 0;
            if (vertical > 0 || (usarTab && pulsado(Keys.Tab))) Seleccionar((_seleccion + 1) % cantidad);
            if (vertical < 0) Seleccionar(_seleccion < 0 ? cantidad - 1 : (_seleccion + cantidad - 1) % cantidad);
            bool ajustarVolumen = EsSlider(_seleccion);
            if (incluirSalir && _seleccion >= CantidadOpciones && (izquierda || derecha))
                Seleccionar(izquierda ? CantidadOpciones + 1 : CantidadOpciones);
            else if (!ajustarVolumen && (izquierda || derecha))
                Seleccionar(_seleccion < 0 ? (derecha ? 0 : cantidad - 1)
                    : (_seleccion + (derecha ? 1 : cantidad - 1)) % cantidad);
            if (_seleccion >= 0)
            {
                if (EsInterruptor(_seleccion) && ((usarEnter && pulsado(Keys.Enter)) || pulsado(Keys.Space))) Alternar(_seleccion);
                if (ajustarVolumen && (izquierda || derecha))
                    EstablecerSlider(_seleccion, ValorSlider(_seleccion)
                        + (derecha ? 5 : -5));
            }
        }
        _ratonAnterior = raton;
        _tecladoAnterior = teclado;
    }

    private void Seleccionar(int fila)
    {
        _seleccion = fila;
        if (fila >= CantidadOpciones) return;
        float altoVisible = _area.Height / Escala;
        float inicio = InicioFila(fila);
        foreach (var categoria in Categorias)
            if (categoria.Fila == fila) inicio -= 45;
        if (inicio < _scroll) _scroll = inicio;
        if (InicioFila(fila) + 120 > _scroll + altoVisible) _scroll = InicioFila(fila) + 120 - altoVisible;
        _scroll = Math.Clamp(_scroll, 0, MaxScroll);
    }

    private void Alternar(int fila)
    {
        if (_interruptores.TryGetValue(fila, out var control) && !control.Boton.Animando)
        {
            control.Escribir(!control.Leer());
            control.Boton.CambiarEstado(control.Leer());
        }
    }

    private static void EstablecerSlider(int fila, int valor)
    {
        valor = Math.Clamp(valor, 0, 100);
        if (fila == 2 && !OpcionesJuego.SilenciarMusica) OpcionesJuego.VolumenMusica = valor;
        if (fila == 3) OpcionesJuego.VolumenSfx = valor;
        if (fila == 5) OpcionesJuego.VisibilidadRangos = valor;
    }

    public void Draw(Rectangle area, float opacidad = 1)
    {
        _area = area;
        var dispositivo = Core.GraphicsDevice;
        Rectangle anterior = dispositivo.ScissorRectangle;
        dispositivo.ScissorRectangle = Rectangle.Intersect(area, dispositivo.Viewport.Bounds);
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _recorte);
        foreach (var entrada in _interruptores)
        {
            entrada.Value.Boton.Opacidad = opacidad;
            entrada.Value.Boton.Draw(batch, Interruptor(entrada.Key));
        }
        foreach (var categoria in Categorias)
            if (categoria.Fila < CantidadOpciones)
                batch.Draw(_pixel, Area(10, InicioFila(categoria.Fila) - 45, 570, 45), new Color(55, 8, 12) * opacidad);
        for (int fila = 0; fila < CantidadOpciones; fila++)
        {
            batch.Draw(_pixel, Area(10, InicioFila(fila) + 118, 570, 1), Color.DarkRed * opacidad);
            if (EsInterruptor(fila)) continue;
            bool habilitado = fila != 2 || !OpcionesJuego.SilenciarMusica;
            Color color = (habilitado ? Color.Red : Color.DimGray) * opacidad;
            var pista = Pista(fila);
            int minimo = MinimoSlider(fila);
            int avance = (int)(pista.Width * (ValorSlider(fila) - minimo) / (100f - minimo));
            batch.Draw(_pixel, pista, Color.DarkGray * opacidad);
            batch.Draw(_pixel, new Rectangle(pista.X, pista.Y, avance, pista.Height), color);
            batch.Draw(_pixel, new Rectangle(pista.X + avance - (int)(5 * Escala),
                pista.Y - (int)(7 * Escala), (int)(10 * Escala), (int)(24 * Escala)), color);
        }
        if (MaxScroll > 0)
        {
            batch.Draw(_pixel, Carril, Color.DarkGray * (opacidad * 0.4f));
            batch.Draw(_pixel, Barra, Color.Red * opacidad);
        }
        batch.End();
        batch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: _recorte);
        string[] nombres = { "Alternar correr", "Silenciar musica", "Volumen musica", "Volumen SFX", "Silenciar locutor",
            "Visibilidad rangos", "Multiplicador total", "Mostrar FPS", "Pantalla completa", "Fast Track" };
        foreach (var categoria in Categorias)
            if (categoria.Fila < CantidadOpciones)
                Texto(categoria.Titulo, Area(20, InicioFila(categoria.Fila) - 45, 320, 45), 0.32f, Color.White * opacidad);
        for (int fila = 0; fila < CantidadOpciones; fila++)
        {
            bool desactivado = fila == 2 && OpcionesJuego.SilenciarMusica;
            Color color = (desactivado ? Color.Gray : Color.White) * opacidad;
            Texto(nombres[fila], Area(20, InicioFila(fila) + 32, 320, 45), 0.32f, color);
            if (!EsInterruptor(fila))
                Texto($"{ValorSlider(fila)}{(fila == 5 ? "%" : "")}",
                    Area(550, InicioFila(fila) + 32, 40, 45), fila == 5 ? 0.22f : 0.27f, color);
            if (_seleccion == fila)
                Texto(">", Area(0, InicioFila(fila) + 32, 15, 45), 0.25f, Color.Red * opacidad);
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
        string ayuda = Ayuda;
        bool fun = ayuda == DescripcionFastTrack;
        foreach (string palabra in ayuda.Split(' '))
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
        float altoLinea = _fuente.LineSpacing * escalaFuente;
        float altoTexto = _fuente.MeasureString(string.Join("\n", lineas)).Y * escalaFuente;
        float y = fun ? area.Top + 18 * Escala : area.Center.Y - altoTexto * 0.5f;
        foreach (string texto in lineas)
        {
            float anchoTexto = _fuente.MeasureString(texto).X * escalaFuente;
            batch.DrawString(_fuente, texto, new Vector2(area.Center.X - anchoTexto * 0.5f, y),
                Color.White * opacidad, 0, Vector2.Zero, escalaFuente, SpriteEffects.None, 0);
            y += altoLinea;
        }
        if (fun)
        {
            y += 12 * Escala;
            const string aviso = "AL JUGAR UNA RUN CON ESTA OPCION ACTIVADA NO SE SUMARA A LOS RECORDS NI SE GUARDARA";
            string actual = "";
            foreach (string palabra in aviso.Split(' '))
            {
                string propuesta = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (actual.Length > 0 && _fuente.MeasureString(propuesta).X * escalaFuente > area.Width - 48 * Escala)
                {
                    DibujarAviso(actual);
                    actual = palabra;
                }
                else actual = propuesta;
            }
            DibujarAviso(actual);
            void DibujarAviso(string texto)
            {
                batch.DrawString(_fuente, texto, new Vector2(area.Center.X - _fuente.MeasureString(texto).X * escalaFuente / 2, y),
                    Color.White * (opacidad * 0.5f), 0, Vector2.Zero, escalaFuente, SpriteEffects.None, 0);
                y += altoLinea;
            }
        }
        batch.End();
    }

    private const string DescripcionFastTrack = "Permite avanzar en el LUTRED de una manera mas rapida, perfecta para Pecadores Impacientes pero con una mala imagen frente a DIOS";
    public string Ayuda
    {
        get
        {
            if (OpcionesJuego.ErrorGuardado != null) return OpcionesJuego.ErrorGuardado;
            var raton = Game1.RatonVirtual();
            int fila = _seleccion;
            if (_area.Contains(raton.Position))
            {
                float y = (raton.Y - _area.Top) / Escala + _scroll;
                for (int i = 0; i < CantidadOpciones; i++)
                    if (y >= InicioFila(i) && y < InicioFila(i) + 120) { fila = i; break; }
            }
            return fila switch
            {
                0 => Game1.Arcade ? "OFF: manten Boton 2. ON: pulsa Boton 2 para alternar."
                    : "OFF: manten Shift. ON: pulsa Shift para alternar.",
                1 => "Silencia la musica y conserva su volumen.",
                2 when OpcionesJuego.SilenciarMusica => "Desactiva Silenciar musica para ajustar el volumen.",
                2 => "Arrastra para ajustar la musica de 0 a 100.",
                3 => "Arrastra para ajustar los efectos de 0 a 100.",
                4 => "Silencia los anuncios del locutor al alcanzar un nuevo rango.",
                5 => "Ajusta la visibilidad del sistema de rangos entre 25% y 100%.",
                6 => "Muestra el multiplicador total de la run: multiplicador del rango por el de los modificadores.",
                7 => "Muestra los fotogramas por segundo en la esquina superior derecha.",
                8 => "Alterna entre pantalla completa y ventana. Atajo: F11.",
                9 when _incluirFun => DescripcionFastTrack,
                _ => "Rueda para bajar. Flechas para elegir y ajustar."
            };
        }
    }

    public void Dispose()
    {
        foreach (var control in _interruptores.Values) control.Boton.Dispose();
        _pixel.Dispose();
        _recorte.Dispose();
    }
}
