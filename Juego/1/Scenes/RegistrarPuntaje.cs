using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using _1.Progression;
using _1.UI;
using LutredLibrary;
using LutredLibrary.Graphics;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _1.Scenes;

public sealed class RegistrarPuntaje : Escena
{
    private const float DuracionTransicion = 0.3f;
    private sealed record Tecla(string Texto, int Fila, int Columna, int Ancho = 1);
    private static readonly Tecla[] Teclas = CrearTeclas();
    private readonly Action<Escena> _cambiarEscena;
    private readonly int _puntaje;
    private readonly int? _nivelLlegado;
    private readonly DateTimeOffset _fecha;
    private readonly StringBuilder _nombre = new();
    private readonly HistorialRecords _historial = new();
    private SpriteFont _fuente;
    private SpriteFont _fuenteFrases;
    private Texture2D _pixel;
    private AnimatedSprite _confirmar, _negar;
    private SoundEffectInstance _musica;
    private KeyboardState _tecladoAnterior = Keyboard.GetState();
    private MouseState _ratonAnterior = Mouse.GetState();
    private float _tiempo, _tiempoSalida, _tiempoConfirmacion, _repeticion;
    private bool _saliendo, _confirmando;
    private int _seleccion, _filaConfirmar = 2, _opcionConfirmacion = 1;
    private int _direccionX, _direccionY;
    private string _error;
    private float Opacidad => _saliendo ? 1f - Math.Clamp(_tiempoSalida / DuracionTransicion, 0f, 1f)
        : Math.Clamp(_tiempo / DuracionTransicion, 0f, 1f);
    private float Escala => Math.Min(Core.GraphicsDevice.Viewport.Width / 1280f, Core.GraphicsDevice.Viewport.Height / 720f);
    private Vector2 Centro => new(Core.GraphicsDevice.Viewport.Width * 0.5f, Core.GraphicsDevice.Viewport.Height * 0.5f);

    public RegistrarPuntaje(Action<Escena> cambiarEscena, int puntaje, DateTimeOffset fecha, int? nivelLlegado = null)
    {
        _cambiarEscena = cambiarEscena;
        _puntaje = puntaje;
        _nivelLlegado = nivelLlegado;
        _fecha = fecha;
    }

    private static Tecla[] CrearTeclas()
    {
        var teclas = new List<Tecla>();
        string[] filas = { "QWERTYUIOP", "ASDFGHJKLÑ", "ZXCVBNM012", "3456789-._" };
        for (int fila = 0; fila < filas.Length; fila++)
            for (int columna = 0; columna < filas[fila].Length; columna++)
                teclas.Add(new Tecla(filas[fila][columna].ToString(), fila, columna));
        teclas.Add(new Tecla("ESPACIO", 4, 0, 7));
        teclas.Add(new Tecla("BORRAR", 4, 7, 3));
        return teclas.ToArray();
    }

    protected override void LoadContent()
    {
        _fuente = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _fuenteFrases = Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _confirmar = CargarBoton("Confirmar");
        _negar = CargarBoton("Negar");
        Core.Audio.StopSong();
        Core.Audio.SongVolume = 0.45f;
        _musica = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>("Music/Misc/Creditos"),
            isLooped: true, isMusic: true);
        Core.Audio.PlaySoundEffect(_musica);
    }

    private AnimatedSprite CargarBoton(string nombre)
    {
        Texture2D hoja = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-" + nombre + "-Sheet");
        var cuadros = new List<TextureRegion>();
        for (int i = 0; i < hoja.Width / 50; i++) cuadros.Add(new TextureRegion(hoja, i * 50, 0, 50, 50));
        return new AnimatedSprite(new Animation(cuadros, TimeSpan.FromSeconds(0.12))) { Origin = new Vector2(25) };
    }

    private Rectangle Area(float x, float y, float ancho, float alto) => new(
        (int)(Centro.X + x * Escala), (int)(Centro.Y + y * Escala),
        Math.Max(1, (int)(ancho * Escala)), Math.Max(1, (int)(alto * Escala)));
    private Rectangle AreaTecla(Tecla tecla) => Area(-400 + tecla.Columna * 58, -70 + tecla.Fila * 58, tecla.Ancho * 58 - 8, 50);
    private Rectangle AreaConfirmar => Area(285, 10, 110, 110);
    private Rectangle AreaRespuesta(int opcion) => Area(opcion == 0 ? -185 : 95, 65, 90, 90);

    private void MoverSeleccion(int dx, int dy)
    {
        if (_seleccion == Teclas.Length)
        {
            if (dx < 0 || dy != 0)
            {
                if (dy != 0) _filaConfirmar = (_filaConfirmar + dy + 5) % 5;
                for (int i = Teclas.Length - 1; i >= 0; i--)
                    if (Teclas[i].Fila == _filaConfirmar) { _seleccion = i; break; }
            }
            return;
        }
        Tecla actual = Teclas[_seleccion];
        if (dy != 0)
        {
            int fila = (actual.Fila + dy + 5) % 5;
            float columna = actual.Columna + (actual.Ancho - 1) * 0.5f;
            float distancia = float.MaxValue;
            for (int i = 0; i < Teclas.Length; i++)
            {
                Tecla candidata = Teclas[i];
                if (candidata.Fila != fila) continue;
                float separacion = columna < candidata.Columna ? candidata.Columna - columna
                    : columna > candidata.Columna + candidata.Ancho - 1 ? columna - candidata.Columna - candidata.Ancho + 1 : 0f;
                if (separacion < distancia) { distancia = separacion; _seleccion = i; }
            }
        }
        else if (dx != 0)
        {
            int vecino = _seleccion + dx;
            if (vecino >= 0 && vecino < Teclas.Length && Teclas[vecino].Fila == actual.Fila) _seleccion = vecino;
            else if (dx > 0) { _filaConfirmar = actual.Fila; _seleccion = Teclas.Length; }
            else
                for (int i = Teclas.Length - 1; i >= 0; i--)
                    if (Teclas[i].Fila == actual.Fila) { _seleccion = i; break; }
        }
    }

    private void ActivarSeleccion()
    {
        if (_seleccion == Teclas.Length)
        {
            if (string.IsNullOrWhiteSpace(_nombre.ToString())) { _error = "ELIGE UN NOMBRE PARA ESTA ALMA"; return; }
            _confirmando = true;
            _opcionConfirmacion = 1;
            _tiempoConfirmacion = 0f;
            _error = null;
            return;
        }
        string tecla = Teclas[_seleccion].Texto;
        if (tecla == "BORRAR") { if (_nombre.Length > 0) _nombre.Length--; }
        else if (_nombre.Length < HistorialRecords.MaximoCaracteresNombre) _nombre.Append(tecla == "ESPACIO" ? " " : tecla);
        _error = null;
    }

    private void Guardar()
    {
        if (_historial.Guardar(_puntaje, _nombre.ToString(), _fecha, _nivelLlegado) == ResultadoRegistro.Error)
        {
            _error = "NO SE PUDO GUARDAR. INTENTÁ OTRA VEZ.";
            return;
        }
        _saliendo = true;
        _tiempoSalida = 0f;
    }

    public override void Update(GameTime gameTime)
    {
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _tiempo += segundos;
        _confirmar.Update(gameTime);
        _negar.Update(gameTime);
        if (_confirmando) _tiempoConfirmacion += segundos;
        if (_saliendo)
        {
            _tiempoSalida += segundos;
            Core.Audio.SetVolume(_musica, Opacidad);
            if (_tiempoSalida >= DuracionTransicion) _cambiarEscena(new Inicio(_cambiarEscena, animarEntrada: true));
            return;
        }
        var teclado = Keyboard.GetState();
        var raton = Mouse.GetState();
        bool Pulsada(Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
        if (_tiempo >= DuracionTransicion && Core.Instance.IsActive)
        {
            int dx = (teclado.IsKeyDown(Keys.D) || teclado.IsKeyDown(Keys.Right) ? 1 : 0)
                - (teclado.IsKeyDown(Keys.A) || teclado.IsKeyDown(Keys.Left) ? 1 : 0);
            int dy = (teclado.IsKeyDown(Keys.S) || teclado.IsKeyDown(Keys.Down) ? 1 : 0)
                - (teclado.IsKeyDown(Keys.W) || teclado.IsKeyDown(Keys.Up) ? 1 : 0);
            if (dx != 0) dy = 0;
            _repeticion -= segundos;
            if (dx != 0 || dy != 0)
            {
                bool nuevaDireccion = dx != _direccionX || dy != _direccionY;
                if (nuevaDireccion || _repeticion <= 0f)
                {
                    if (_confirmando) _opcionConfirmacion = dx < 0 || dy < 0 ? 0 : 1;
                    else MoverSeleccion(dx, dy);
                    _repeticion = nuevaDireccion ? 0.32f : 0.09f;
                }
            }
            _direccionX = dx; _direccionY = dy;
            bool clic = raton.LeftButton == ButtonState.Pressed && _ratonAnterior.LeftButton == ButtonState.Released;
            bool aceptar = Pulsada(Keys.Space) || Pulsada(Keys.Enter);
            if (_confirmando)
            {
                for (int i = 0; i < 2; i++)
                    if (AreaRespuesta(i).Contains(raton.Position))
                    {
                        if (raton.Position != _ratonAnterior.Position || clic) _opcionConfirmacion = i;
                        if (clic) aceptar = true;
                    }
                if (Pulsada(Keys.Escape)) _confirmando = false;
                else if (aceptar)
                {
                    if (_opcionConfirmacion == 0) Guardar();
                    else { _confirmando = false; _error = null; }
                }
            }
            else
            {
                for (int i = 0; i <= Teclas.Length; i++)
                {
                    Rectangle area = i == Teclas.Length ? AreaConfirmar : AreaTecla(Teclas[i]);
                    if (!area.Contains(raton.Position)) continue;
                    if (raton.Position != _ratonAnterior.Position || clic) _seleccion = i;
                    if (clic) aceptar = true;
                    break;
                }
                if (Pulsada(Keys.Back) && _nombre.Length > 0) { _nombre.Length--; _error = null; }
                if (aceptar) ActivarSeleccion();
            }
        }
        _tecladoAnterior = teclado; _ratonAnterior = raton;
    }

    private void Texto(string texto, Vector2 centro, float tamano, float anchoMaximo, Color? color = null,
        float opacidad = 1f, SpriteFont fuente = null)
    {
        fuente ??= _fuente;
        Vector2 medida = fuente.MeasureString(texto);
        float factor = Math.Min(tamano * Escala * _fuente.LineSpacing / fuente.LineSpacing,
            anchoMaximo * Escala / Math.Max(1f, medida.X));
        MarcadorPuntaje.DibujarTexto(fuente, texto, centro - medida * factor * 0.5f, factor, Opacidad * opacidad, color);
    }

    private void Boton(AnimatedSprite sprite, Rectangle area, bool seleccionado, float opacidad = 1f)
    {
        sprite.Position = area.Center.ToVector2();
        sprite.Scale = new Vector2(area.Width / 50f);
        sprite.Color = (seleccionado ? Color.White : new Color(140, 140, 140)) * (Opacidad * opacidad);
        sprite.Draw(Core.SpriteBatch);
    }

    private void Recuadro(Rectangle area, Color borde, Color relleno, float opacidad = 1f)
    {
        int grosor = Math.Max(1, (int)(2 * Escala));
        Core.SpriteBatch.Draw(_pixel, area, borde * (Opacidad * opacidad));
        Core.SpriteBatch.Draw(_pixel, new Rectangle(area.X + grosor, area.Y + grosor,
            Math.Max(1, area.Width - grosor * 2), Math.Max(1, area.Height - grosor * 2)), relleno * (Opacidad * opacidad));
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);
        Texto(_puntaje.ToString("D8", CultureInfo.InvariantCulture), Centro + new Vector2(0, -245) * Escala, 0.9f, 800, Color.Gray);
        Rectangle campo = Area(-400, -205, 800, 66);
        Recuadro(campo, Color.White, new Color(20, 10, 10));
        string nombre = _nombre.ToString();
        Texto(nombre.Length == 0 ? "TU NOMBRE" : nombre, campo.Center.ToVector2(), 1.5f, 750, nombre.Length == 0 ? Color.Gray : Color.White);
        Texto($"{_nombre.Length}/{HistorialRecords.MaximoCaracteresNombre}", Centro + new Vector2(0, -115) * Escala, 0.75f, 300, Color.Gray);
        for (int i = 0; i < Teclas.Length; i++)
        {
            Rectangle area = AreaTecla(Teclas[i]);
            bool seleccionada = !_confirmando && i == _seleccion;
            Recuadro(area, seleccionada ? new Color(255, 90, 100) : new Color(120, 120, 120),
                seleccionada ? new Color(75, 15, 25) : new Color(20, 20, 20));
            Texto(Teclas[i].Texto, area.Center.ToVector2(), Teclas[i].Ancho > 1 ? 0.85f : 1.1f, Teclas[i].Ancho * 58 - 20);
        }
        Boton(_confirmar, AreaConfirmar, !_confirmando && _seleccion == Teclas.Length);
        Texto("CONFIRMAR", Centro + new Vector2(340, 145) * Escala, 0.8f, 200);
        if (_error != null && !_confirmando) Texto(_error, Centro + new Vector2(0, 275) * Escala, 0.8f, 1050, new Color(255, 95, 95));
        if (_confirmando) DibujarConfirmacion();
        batch.End();
        batch.Begin(samplerState: SamplerState.LinearClamp);
        float entradaConfirmacion = Math.Clamp(_tiempoConfirmacion / 0.25f, 0f, 1f);
        float suaveConfirmacion = entradaConfirmacion * entradaConfirmacion * (3f - 2f * entradaConfirmacion);
        Texto("¿Cómo quieres llamar esta alma?", Centro + new Vector2(0, -285) * Escala, 1.5f, 1050,
            opacidad: _confirmando ? 1f - 0.9f * suaveConfirmacion : 1f, fuente: _fuenteFrases);
        if (_confirmando)
            Texto("¿REALMENTE QUIERES LLAMARLA ASÍ?", Centro + new Vector2(0, -125) * Escala, 1.1f, 900,
                opacidad: suaveConfirmacion, fuente: _fuenteFrases);
        batch.End();
    }

    private void DibujarConfirmacion()
    {
        float entrada = Math.Clamp(_tiempoConfirmacion / 0.25f, 0f, 1f);
        float suave = entrada * entrada * (3f - 2f * entrada);
        Core.SpriteBatch.Draw(_pixel, Core.GraphicsDevice.Viewport.Bounds, Color.Black * (0.9f * Opacidad * suave));
        Recuadro(Area(-500, -190, 1000, 420), new Color(180, 180, 180), Color.Black, suave);
        Texto(_nombre.ToString().Trim(), Centro + new Vector2(0, -30) * Escala, 1.5f + 1.5f * suave, 900, opacidad: suave);
        Boton(_confirmar, AreaRespuesta(0), _opcionConfirmacion == 0, suave);
        Boton(_negar, AreaRespuesta(1), _opcionConfirmacion == 1, suave);
        Texto("SÍ", Centro + new Vector2(-140, 180) * Escala, 0.85f, 150, opacidad: suave);
        Texto("NO", Centro + new Vector2(140, 180) * Escala, 0.85f, 150, opacidad: suave);
        if (_error != null) Texto(_error, Centro + new Vector2(0, 260) * Escala, 0.8f, 1050, new Color(255, 95, 95));
    }

    protected override void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_musica);
        _pixel?.Dispose();
    }
}
