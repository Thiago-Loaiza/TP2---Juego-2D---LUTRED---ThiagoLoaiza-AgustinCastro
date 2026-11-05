using System;
using System.Collections.Generic;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _1.UI;

// Interfaz independiente de la escena: sigue animandose mientras el juego esta pausado.
public sealed class ConfiguracionPausa : IDisposable
{
    private readonly Texture2D _configuracion;
    private readonly Texture2D _recuadro;
    private readonly Rectangle[] _recortesMarco;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _fuente;
    private readonly ListaOpciones _listaOpciones;
    private readonly Action _reanudar;
    private readonly Action _salir;
    private readonly AnimatedSprite _confirmar;
    private readonly AnimatedSprite _negar;
    private bool _confirmandoSalida;
    private float _tiempoConfirmacion;
    private int _opcionConfirmacion = 1;
    private MouseState _ratonAnterior;
    private KeyboardState _tecladoAnterior;
    private double _tiempo;
    private bool _pausoAudio;
    public bool Abierto { get; private set; }

    public ConfiguracionPausa(Action reanudar, Action salir = null)
    {
        _reanudar = reanudar;
        _salir = salir;
        if (_salir != null)
        {
            _confirmar = CargarRespuesta("Confirmar");
            _negar = CargarRespuesta("Negar");
        }
        _configuracion = Core.Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Configuration-Sheet");
        _recuadro = Core.Content.Load<Texture2D>("Sprites/UI/Cuadro-Sheet");
        _recortesMarco = CalcularRecortesMarco(_recuadro);
        _fuente = Core.Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _listaOpciones = new ListaOpciones();
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _ratonAnterior = Mouse.GetState();
        _tecladoAnterior = Keyboard.GetState();
    }

    private float Escala => Math.Min(Core.GraphicsDevice.Viewport.Width / 1280f,
        Core.GraphicsDevice.Viewport.Height / 720f);

    private Rectangle Area(float x, float y, float ancho, float alto)
    {
        var viewport = Core.GraphicsDevice.Viewport;
        return new Rectangle((int)(viewport.Width * 0.5f + x * Escala),
            (int)(viewport.Height * 0.5f + y * Escala),
            (int)(ancho * Escala), (int)(alto * Escala));
    }

    private Rectangle AreaBoton => new((int)(24 * Escala), (int)(24 * Escala),
        (int)(50 * Escala), (int)(50 * Escala));
    private Rectangle AreaLista => Area(-550, -165, 620, 340);
    private Rectangle AreaReanudar => Area(_salir != null ? -270 : -370, 195, 260, 55);
    private Rectangle AreaSalir => Area(-540, 195, 240, 55);
    private Rectangle AreaRespuesta(int opcion) => Area(opcion == 0 ? -185 : 95, 65, 90, 90);
    private Rectangle AreaAyuda => Area(150, -140, 450, 240);

    // Tambien bloquea el fotograma de apertura/cierre para que el clic no llegue a la escena.
    public bool Update(GameTime gameTime, bool escapePulsado)
    {
        KeyboardState teclado = Keyboard.GetState();
        bool pausaArcade = Game1.Arcade && Pulsada(teclado, Keys.Enter) && !_confirmandoSalida;
        _tiempo += gameTime.ElapsedGameTime.TotalSeconds;
        _listaOpciones.Update(gameTime, AreaLista, Abierto && !_confirmandoSalida && !pausaArcade,
            usarTab: true, incluirSalir: _salir != null);
        _confirmar?.Update(gameTime);
        _negar?.Update(gameTime);
        if (_confirmandoSalida) _tiempoConfirmacion += (float)gameTime.ElapsedGameTime.TotalSeconds;
        MouseState raton = Mouse.GetState();
        bool clic = raton.LeftButton == ButtonState.Pressed && _ratonAnterior.LeftButton == ButtonState.Released;
        bool bloquear = Abierto;
        if (Core.Instance.IsActive)
        {
            if (!Abierto)
            {
                if (pausaArcade || (escapePulsado && (!Game1.Arcade || !teclado.IsKeyDown(Keys.B)))
                    || (clic && AreaBoton.Contains(raton.Position)))
                {
                    Abierto = true;
                    _pausoAudio = !Core.Audio.IsPaused;
                    if (_pausoAudio) Core.Audio.PauseAudio();
                    bloquear = true;
                    _listaOpciones.SincronizarEntrada();
                }
            }
            else if (_confirmandoSalida)
            {
                ActualizarConfirmacion(teclado, raton, clic, escapePulsado);
            }
            else if (pausaArcade)
            {
                Cerrar();
            }
            else if (_salir != null && ((clic && AreaSalir.Contains(raton.Position))
                || (_listaOpciones.SalirSeleccionado && Aceptar(teclado))))
            {
                _confirmandoSalida = true;
                _tiempoConfirmacion = 0f;
                _opcionConfirmacion = 1;
                _listaOpciones.SincronizarEntrada();
            }
            else if (escapePulsado || (clic && AreaReanudar.Contains(raton.Position))
                || (_listaOpciones.ReanudarSeleccionado && ((teclado.IsKeyDown(Keys.Enter)
                && _tecladoAnterior.IsKeyUp(Keys.Enter)) || (teclado.IsKeyDown(Keys.Space)
                && _tecladoAnterior.IsKeyUp(Keys.Space)))))
            {
                Cerrar();
            }

        }
        _ratonAnterior = raton;
        _tecladoAnterior = teclado;
        return bloquear;
    }

    private bool Pulsada(KeyboardState teclado, Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
    private bool Aceptar(KeyboardState teclado) => Pulsada(teclado, Keys.Enter) || Pulsada(teclado, Keys.Space);

    private void ActualizarConfirmacion(KeyboardState teclado, MouseState raton, bool clic, bool escapePulsado)
    {
        if (Pulsada(teclado, Keys.A) || Pulsada(teclado, Keys.Left) || Pulsada(teclado, Keys.W) || Pulsada(teclado, Keys.Up))
            _opcionConfirmacion = 0;
        if (Pulsada(teclado, Keys.D) || Pulsada(teclado, Keys.Right) || Pulsada(teclado, Keys.S) || Pulsada(teclado, Keys.Down))
            _opcionConfirmacion = 1;
        if (Pulsada(teclado, Keys.Tab)) _opcionConfirmacion = 1 - _opcionConfirmacion;
        bool aceptar = Aceptar(teclado);
        for (int i = 0; i < 2; i++)
        {
            if (!AreaRespuesta(i).Contains(raton.Position)) continue;
            if (raton.Position != _ratonAnterior.Position || clic) _opcionConfirmacion = i;
            if (clic) aceptar = true;
        }
        if (escapePulsado || (aceptar && _opcionConfirmacion == 1))
        {
            _confirmandoSalida = false;
            _listaOpciones.SincronizarEntrada();
        }
        else if (aceptar)
        {
            Cerrar();
            _salir?.Invoke();
        }
    }

    private static AnimatedSprite CargarRespuesta(string nombre)
    {
        Texture2D hoja = Core.Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-" + nombre + "-Sheet");
        var cuadros = new List<TextureRegion>();
        for (int i = 0; i < hoja.Width / 50; i++) cuadros.Add(new TextureRegion(hoja, i * 50, 0, 50, 50));
        return new AnimatedSprite(new Animation(cuadros, TimeSpan.FromSeconds(0.12))) { Origin = new Vector2(25) };
    }

    private void Cerrar()
    {
        Abierto = false;
        _confirmandoSalida = false;
        if (_pausoAudio) Core.Audio.ResumeAudio();
        _pausoAudio = false;
        _reanudar?.Invoke();
    }

    public void Draw()
    {
        var batch = Core.SpriteBatch;
        var raton = Mouse.GetState();
        int cuadro = (int)(_tiempo / 0.12) % 9;
        batch.Begin(samplerState: SamplerState.PointClamp);
        if (!Abierto)
        {
            batch.Draw(_configuracion, AreaBoton, new Rectangle(cuadro * 50, 0, 50, 50),
                AreaBoton.Contains(raton.Position) ? Color.White : Color.White * 0.75f);
            batch.End();
            return;
        }
        batch.Draw(_pixel, Core.GraphicsDevice.Viewport.Bounds, Color.Black * 0.7f);
        Rectangle modal = Area(-600, -265, 720, 530);
        batch.Draw(_pixel, modal, Color.Black);
        DibujarMarco(modal, cuadro);

        batch.End();

        batch.Begin(samplerState: SamplerState.LinearClamp);
        Texto("CONFIGURACION", Area(-540, -235, 600, 50), 0.45f, Color.White);
        if (_salir != null)
            Texto("Salir", AreaSalir, 0.34f,
                AreaSalir.Contains(raton.Position) || _listaOpciones.SalirSeleccionado
                    ? new Color(255, 95, 95) : new Color(180, 100, 100));
        Texto("Reanudar", AreaReanudar, 0.34f,
            AreaReanudar.Contains(raton.Position) || _listaOpciones.ReanudarSeleccionado
                ? Color.White : new Color(180, 180, 180));
        batch.End();
        _listaOpciones.Draw(AreaLista);
        _listaOpciones.DibujarAyuda(AreaAyuda);
        if (_confirmandoSalida) DibujarConfirmacion();
    }

    private void DibujarConfirmacion()
    {
        float t = Math.Clamp(_tiempoConfirmacion / 0.25f, 0f, 1f);
        float opacidad = t * t * (3f - 2f * t);
        var batch = Core.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_pixel, Core.GraphicsDevice.Viewport.Bounds, Color.Black * (0.9f * opacidad));
        Rectangle modal = Area(-500, -190, 1000, 420);
        int borde = Math.Max(1, (int)(2 * Escala));
        batch.Draw(_pixel, modal, new Color(180, 180, 180) * opacidad);
        batch.Draw(_pixel, new Rectangle(modal.X + borde, modal.Y + borde,
            modal.Width - borde * 2, modal.Height - borde * 2), Color.Black * opacidad);
        for (int i = 0; i < 2; i++)
        {
            AnimatedSprite sprite = i == 0 ? _confirmar : _negar;
            Rectangle area = AreaRespuesta(i);
            sprite.Position = area.Center.ToVector2();
            sprite.Scale = new Vector2(area.Width / 50f);
            sprite.Color = (_opcionConfirmacion == i ? Color.White : new Color(140, 140, 140)) * opacidad;
            sprite.Draw(batch);
        }
        batch.End();
        batch.Begin(samplerState: SamplerState.LinearClamp);
        Texto("SALIR DE LA RUN", Area(-450, -145, 900, 55), 0.45f, Color.White * opacidad);
        Texto("Si sales de esta run,\ncontara como perdida.", Area(-450, -75, 900, 110), 0.34f,
            new Color(255, 95, 95) * opacidad);
        Texto("SI", Area(-215, 160, 150, 40), 0.3f, Color.White * opacidad);
        Texto("NO", Area(65, 160, 150, 40), 0.3f, Color.White * opacidad);
        batch.End();
    }

    private void Texto(string texto, Rectangle area, float escala, Color color)
    {
        Core.SpriteBatch.DrawString(_fuente, texto, area.Center.ToVector2(), color, 0f,
            _fuente.MeasureString(texto) * 0.5f, escala * Escala, SpriteEffects.None, 0f);
    }

    private void DibujarMarco(Rectangle destino, int cuadro)
    {
        // El recorte ajustado elimina el negro exterior al contorno dibujado.
        const int borde = 32;
        Rectangle recorte = _recortesMarco[cuadro];
        int[] fuenteX = { recorte.Left, recorte.Left + borde, recorte.Right - borde, recorte.Right };
        int[] fuenteY = { recorte.Top, recorte.Top + borde, recorte.Bottom - borde, recorte.Bottom };
        int bordePantalla = Math.Max(1, (int)(borde * Escala));
        int[] destinoX = { destino.Left, destino.Left + bordePantalla, destino.Right - bordePantalla, destino.Right };
        int[] destinoY = { destino.Top, destino.Top + bordePantalla, destino.Bottom - bordePantalla, destino.Bottom };
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                Core.SpriteBatch.Draw(_recuadro,
                    new Rectangle(destinoX[x], destinoY[y], destinoX[x + 1] - destinoX[x], destinoY[y + 1] - destinoY[y]),
                    new Rectangle(fuenteX[x], fuenteY[y],
                        fuenteX[x + 1] - fuenteX[x], fuenteY[y + 1] - fuenteY[y]), Color.White);
    }

    private static Rectangle[] CalcularRecortesMarco(Texture2D textura)
    {
        var pixeles = new Color[textura.Width * textura.Height];
        textura.GetData(pixeles);
        var recortes = new Rectangle[9];
        int anchoCuadro = textura.Width / recortes.Length;
        for (int cuadro = 0; cuadro < recortes.Length; cuadro++)
        {
            int inicioX = cuadro * anchoCuadro;
            int izquierda = inicioX + anchoCuadro, arriba = textura.Height;
            int derecha = inicioX - 1, abajo = -1;
            for (int y = 0; y < textura.Height; y++)
                for (int x = inicioX; x < inicioX + anchoCuadro; x++)
                {
                    Color color = pixeles[y * textura.Width + x];
                    if (color.A == 0 || Math.Max(color.R, Math.Max(color.G, color.B)) < 128) continue;
                    izquierda = Math.Min(izquierda, x);
                    arriba = Math.Min(arriba, y);
                    derecha = Math.Max(derecha, x);
                    abajo = Math.Max(abajo, y);
                }
            recortes[cuadro] = derecha >= izquierda && abajo >= arriba
                ? new Rectangle(izquierda, arriba, derecha - izquierda + 1, abajo - arriba + 1)
                : new Rectangle(inicioX, 0, anchoCuadro, textura.Height);
        }
        return recortes;
    }

    public void Dispose()
    {
        if (Abierto) Cerrar();
        _listaOpciones.Dispose();
        _pixel.Dispose();
    }
}
