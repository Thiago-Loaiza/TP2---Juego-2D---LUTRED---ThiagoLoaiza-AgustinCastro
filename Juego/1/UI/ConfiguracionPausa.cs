using System;
using LutredLibrary;
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
    private readonly BotonInterruptor _interruptor;
    private readonly Action _reanudar;
    private MouseState _ratonAnterior;
    private KeyboardState _tecladoAnterior;
    private double _tiempo;
    private bool _pausoAudio;
    private bool _focoControl;
    public bool Abierto { get; private set; }

    public ConfiguracionPausa(Action reanudar)
    {
        _reanudar = reanudar;
        _configuracion = Core.Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Configuration-Sheet");
        _recuadro = Core.Content.Load<Texture2D>("Sprites/UI/Cuadro-Sheet");
        _recortesMarco = CalcularRecortesMarco(_recuadro);
        _fuente = Core.Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        _interruptor = new BotonInterruptor(Core.Content.Load<Texture2D>("Sprites/UI/Buttons/ON-OFF-Sheet"),
            OpcionesJuego.AlternarCorrer);
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
    private Rectangle AreaNombre => Area(-290, -65, 350, 80);
    private Rectangle AreaInterruptor => Area(100, -70, 200, 100);
    private Rectangle AreaReanudar => Area(-130, 65, 260, 60);
    private Rectangle AreaAyuda => Area(-300, 204, 600, 100);

    // Tambien bloquea el fotograma de apertura/cierre para que el clic no llegue a la escena.
    public bool Update(GameTime gameTime, bool escapePulsado)
    {
        _tiempo += gameTime.ElapsedGameTime.TotalSeconds;
        _interruptor.Update(gameTime);
        MouseState raton = Mouse.GetState();
        KeyboardState teclado = Keyboard.GetState();
        bool clic = raton.LeftButton == ButtonState.Pressed && _ratonAnterior.LeftButton == ButtonState.Released;
        bool Pulsado(Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
        bool bloquear = Abierto;
        if (Core.Instance.IsActive)
        {
            if (!Abierto)
            {
                if (escapePulsado || (clic && AreaBoton.Contains(raton.Position)))
                {
                    Abierto = true;
                    _pausoAudio = !Core.Audio.IsPaused;
                    if (_pausoAudio) Core.Audio.PauseAudio();
                    bloquear = true;
                    _focoControl = false;
                }
            }
            else if (escapePulsado || (clic && AreaReanudar.Contains(raton.Position)))
            {
                Cerrar();
            }
            else
            {
                if (Pulsado(Keys.Tab)) _focoControl = !_focoControl;
                bool confirmar = Pulsado(Keys.Enter) || Pulsado(Keys.F);
                if ((clic && AreaInterruptor.Contains(raton.Position)) || (confirmar && _focoControl))
                {
                    if (!_interruptor.Animando)
                    {
                        OpcionesJuego.AlternarCorrer = !OpcionesJuego.AlternarCorrer;
                        _interruptor.CambiarEstado(OpcionesJuego.AlternarCorrer);
                    }
                }
                else if (confirmar && !_focoControl) Cerrar();
            }
        }
        _ratonAnterior = raton;
        _tecladoAnterior = teclado;
        return bloquear;
    }

    private void Cerrar()
    {
        Abierto = false;
        if (_pausoAudio) Core.Audio.ResumeAudio();
        _pausoAudio = false;
        _reanudar?.Invoke();
    }

    public void Draw()
    {
        var batch = Core.SpriteBatch;
        var raton = Mouse.GetState();
        bool ayudaVisible = AreaNombre.Contains(raton.Position) || _focoControl;
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
        Rectangle modal = Area(-360, -190, 720, 380);
        batch.Draw(_pixel, modal, Color.Black);
        DibujarMarco(modal, cuadro);
        _interruptor.Draw(batch, AreaInterruptor);
        if (ayudaVisible)
        {
            batch.Draw(_pixel, AreaAyuda, Color.Black);
            DibujarMarco(AreaAyuda, cuadro);
        }
        batch.End();

        batch.Begin(samplerState: SamplerState.LinearClamp);
        Texto("CONFIGURACION", Area(-300, -145, 600, 50), 0.45f, Color.White);
        Texto("Alternar correr", AreaNombre, 0.34f, ayudaVisible ? Color.White : new Color(210, 210, 210));
        if (ayudaVisible)
            Texto("OFF: manten Shift para correr.\nON: pulsa Shift para alternar correr y caminar.",
                Area(-280, 220, 560, 68), 0.24f, Color.White);
        Texto("Reanudar", AreaReanudar, 0.34f,
            AreaReanudar.Contains(raton.Position) || !_focoControl ? Color.White : new Color(180, 180, 180));
        Texto("Esc para volver", Area(-160, 132, 320, 24), 0.18f, new Color(150, 150, 150));
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
        _interruptor.Dispose();
        _pixel.Dispose();
    }
}
