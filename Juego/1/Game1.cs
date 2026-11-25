using Gum.Forms;
using Gum.Forms.Controls;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using _1.Scenes;
using System.Linq;
using System.Diagnostics;

namespace _1;

public class Game1 : Core
{
    // true = ON: habilita H (correr), J (dash), B (volver), N (borrar) y Enter (pausa/salir del menu).
    // false = OFF: conserva solamente los controles habituales del teclado.
    public static bool Arcade = true;
    public static bool FastTrack { get; set; }

    // true = ON: Jugar abre la eleccion. false = OFF: entra directamente a la partida.
    public static bool EmpezarConEleccion = false;

    // Jugar abre mejoras y despues la pregunta; desactivar para el inicio habitual.
    public static bool EmpezarConMejoras = false;

    // Permite ofrecer mejoras sin exigir su piso minimo; conserva requisitos y limites.
    public static bool IgnorarNivelMinimoMejoras = false;

    private Escena _escenaActual;
    private RenderTarget2D _pantallaDupe;
    private RenderTarget2D _pantallaVirtual;
    public const int AnchoPartida = 1280;
    public const int AltoPartida = 720;
    public static Viewport VistaPartida => new(0, 0, AnchoPartida, AltoPartida);
    private static Matrix _ratonAVistaVirtual = Matrix.Identity;

    private static Rectangle AreaPresentacion(int ancho, int alto)
    {
        float escala = System.Math.Min(ancho / (float)AnchoPartida, alto / (float)AltoPartida);
        int anchoFinal = System.Math.Max(1, (int)System.Math.Round(AnchoPartida * escala));
        int altoFinal = System.Math.Max(1, (int)System.Math.Round(AltoPartida * escala));
        return new Rectangle((ancho - anchoFinal) / 2, (alto - altoFinal) / 2, anchoFinal, altoFinal);
    }

    private void ActualizarCoordenadasRaton()
    {
        var pp = GraphicsDevice.PresentationParameters;
        Rectangle area = AreaPresentacion(pp.BackBufferWidth, pp.BackBufferHeight);
        Rectangle ventana = Window.ClientBounds;
        float escalaX = pp.BackBufferWidth / (float)System.Math.Max(1, ventana.Width);
        float escalaY = pp.BackBufferHeight / (float)System.Math.Max(1, ventana.Height);
        _ratonAVistaVirtual = Matrix.CreateScale(escalaX, escalaY, 1f)
            * Matrix.CreateTranslation(-area.X, -area.Y, 0f)
            * Matrix.CreateScale(AnchoPartida / (float)area.Width, AltoPartida / (float)area.Height, 1f);
        GumService.Default.Cursor.TransformMatrix = _ratonAVistaVirtual;
    }

    public static MouseState RatonVirtual()
    {
        MouseState estado = Mouse.GetState();
        Vector2 posicion = Vector2.Transform(new Vector2(estado.X, estado.Y), _ratonAVistaVirtual);
        return new MouseState((int)System.Math.Floor(posicion.X), (int)System.Math.Floor(posicion.Y),
            estado.ScrollWheelValue, estado.LeftButton, estado.MiddleButton, estado.RightButton,
            estado.XButton1, estado.XButton2);
    }
    private Escena _escenaPendiente;
    private bool _salirPresionadoAnterior;
    private bool _enterPresionadoAnterior;
    private bool _f11PresionadoAnterior;
    private _1.UI.ConfiguracionPausa _configuracionPausa;
    private readonly _1.UI.MusicaMenu _musicaMenu = new();
    private SpriteFont _fuenteFps;
    private readonly Stopwatch _relojFps = new();
    private int _fotogramasFps;
    private int _fps;

    public Game1() : base("LUTRED", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        OpcionesJuego.Cargar();
        AplicarPantallaCompleta();
        _fuenteFps = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _relojFps.Start();

        GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
        GumService.Default.ContentLoader.XnaContentManager = Content;
        GumService.Default.CanvasWidth = AnchoPartida;
        GumService.Default.CanvasHeight = AltoPartida;
        ActualizarCoordenadasRaton();
        FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
        foreach (Keys tecla in new[] { Keys.S, Keys.D, Keys.Down, Keys.Right })
            if (!FrameworkElement.TabKeyCombos.Any(combo => combo.PushedKey == tecla))
                FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = tecla });
        foreach (Keys tecla in new[] { Keys.W, Keys.A, Keys.Up, Keys.Left })
            if (!FrameworkElement.TabReverseKeyCombos.Any(combo => combo.PushedKey == tecla))
                FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = tecla });
        if (!FrameworkElement.ClickCombos.Any(combo => combo.PushedKey == Keys.Space))
            FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.Space });
        FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);

        CambiarEscena(new Inicio(CambiarEscena));
        AplicarCambioDeEscena();
    }

    private void AplicarPantallaCompleta()
    {
        if (Graphics.IsFullScreen == OpcionesJuego.PantallaCompleta) return;
        bool completa = OpcionesJuego.PantallaCompleta;
        var pantalla = GraphicsDevice.Adapter.CurrentDisplayMode;
        // Usar pantalla completa real a la resolucion actual del monitor, no una
        // ventana de 1280x720 maximizada por el controlador de escritorio.
        Graphics.HardwareModeSwitch = true;
        Graphics.PreferredBackBufferWidth = completa ? pantalla.Width : AnchoPartida;
        Graphics.PreferredBackBufferHeight = completa ? pantalla.Height : AltoPartida;
        Graphics.IsFullScreen = completa;
        Graphics.ApplyChanges();
        Window.IsBorderless = completa;
        GraphicsDevice.Viewport = new Viewport(0, 0,
            GraphicsDevice.PresentationParameters.BackBufferWidth,
            GraphicsDevice.PresentationParameters.BackBufferHeight);
    }

    public void CambiarEscena(Escena escena)
    {
        _escenaPendiente?.Dispose();
        _escenaPendiente = escena;
    }

    private void AplicarCambioDeEscena()
    {
        if (_escenaPendiente == null)
            return;

        // el cambio se aplica despues de procesar la entrada para evitar modificar la interfaz durante el clic.
        _configuracionPausa?.Dispose();
        _configuracionPausa = null;
        _escenaActual?.Dispose();
        _musicaMenu.CambiarEscena(_escenaPendiente is Inicio or Records or Creditos or Misc,
            _escenaPendiente is Creditos);
        GumService.Default.Root.Children.Clear();
        _escenaActual = _escenaPendiente;
        _escenaPendiente = null;
        _escenaActual.Initialize();
        if (_escenaActual is Partida partida)
            _configuracionPausa = new _1.UI.ConfiguracionPausa(partida.SincronizarEntrada, partida.AbandonarRun);
        else if (_escenaActual is Mejoras mejoras)
            _configuracionPausa = new _1.UI.ConfiguracionPausa(mejoras.SincronizarEntrada,
                () => mejoras.AbandonarRun(CambiarEscena));
    }

    protected override void Update(GameTime gameTime)
    {
        ActualizarCoordenadasRaton();
        bool f11Presionado = Keyboard.GetState().IsKeyDown(Keys.F11);
        if (IsActive && f11Presionado && !_f11PresionadoAnterior)
            OpcionesJuego.PantallaCompleta = !OpcionesJuego.PantallaCompleta;
        _f11PresionadoAnterior = f11Presionado;
        OpcionesJuego.ActualizarGuardado((float)gameTime.ElapsedGameTime.TotalSeconds);
        _musicaMenu.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        KeyboardState teclado = Keyboard.GetState();
        bool enterPresionado = teclado.IsKeyDown(Keys.Enter);
        bool enterPulsado = enterPresionado && !_enterPresionadoAnterior;
        _enterPresionadoAnterior = enterPresionado;
        bool retroceso = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || teclado.IsKeyDown(Keys.Escape);
        bool salirPresionado = retroceso || (Arcade && teclado.IsKeyDown(Keys.B));
        bool salirPulsado = salirPresionado && !_salirPresionadoAnterior;
        _salirPresionadoAnterior = salirPresionado;
        bool bloquearEscena = _configuracionPausa?.Update(gameTime, salirPulsado) == true;
        // Solo el menu principal, sin modales ni transiciones, puede cerrar la aplicacion.
        if (IsActive && _escenaActual is Inicio menuPrincipal && !menuPrincipal.ControlesAbiertos
            && !menuPrincipal.OpcionesAbiertas && !menuPrincipal.EnTransicion
            && (Arcade ? enterPulsado : salirPulsado && retroceso))
        {
            Exit();
            return;
        }
        if (IsActive && salirPulsado && !bloquearEscena)
        {
            if (_escenaActual is Inicio controles && controles.ControlesAbiertos)
                controles.CerrarControles();
            else if (_escenaActual is Inicio inicio && (inicio.OpcionesAbiertas || inicio.EnTransicion))
                inicio.CerrarOpciones();
            else if (_escenaActual is Creditos creditos)
                creditos.VolverAlInicio();
            else if (_escenaActual is Records records)
                records.VolverAlInicio();
            else if (_escenaActual is Misc misc)
                misc.VolverAlInicio();
            // En el resto de escenas, Escape/Back solo se procesa localmente.
        }

        if (!bloquearEscena)
        {
            bool controlesAbiertos = _escenaActual is Inicio controlesMenu && controlesMenu.ControlesAbiertos;
            _escenaActual.Update(gameTime);
            // El modal gestiona su entrada y bloquea los botones del menu hasta terminar de cerrar.
            if (!controlesAbiertos && !(_escenaActual is Inicio menuControles && menuControles.ControlesAbiertos))
            {
                // La lista desplazable gestiona su propio foco; Gum conserva el raton y el mando.
                bool opcionesAbiertas = _escenaActual is Inicio menu && menu.OpcionesAbiertas;
                if (opcionesAbiertas) FrameworkElement.KeyboardsForUiControl.Remove(GumService.Default.Keyboard);
                try
                {
                    GumService.Default.Update(gameTime);
                }
                finally
                {
                    if (opcionesAbiertas) FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
                }
            }
        }
        AplicarCambioDeEscena();
        if (Graphics.IsFullScreen != OpcionesJuego.PantallaCompleta)
        {
            AplicarPantallaCompleta();
            ActualizarCoordenadasRaton();
        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Effect filtroDupe = (_escenaActual as Partida)?.FiltroDupe;
        // Toda la escena, incluida Gum, se compone a la misma resolucion que la ventana.
        _pantallaVirtual ??= new RenderTarget2D(GraphicsDevice, AnchoPartida, AltoPartida);
        GraphicsDevice.SetRenderTarget(_pantallaVirtual);
        GraphicsDevice.Clear(Color.Black);
        _escenaActual.Draw(gameTime);
        GumService.Default.Draw();
        _1.UI.BotonAnimado.DibujarTextosSuaves();
        (_escenaActual as Inicio)?.DibujarControles();
        _configuracionPausa?.Draw();
        base.Draw(gameTime);

        RenderTarget2D imagenFinal = _pantallaVirtual;
        if (filtroDupe != null)
        {
            _pantallaDupe ??= new RenderTarget2D(GraphicsDevice, AnchoPartida, AltoPartida);
            GraphicsDevice.SetRenderTarget(_pantallaDupe);
            SpriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp,
                effect: filtroDupe);
            SpriteBatch.Draw(_pantallaVirtual, VistaPartida.Bounds, Color.White);
            SpriteBatch.End();
            imagenFinal = _pantallaDupe;
        }
        DibujarFps();
        GraphicsDevice.SetRenderTarget(null);
        var pp = GraphicsDevice.PresentationParameters;
        GraphicsDevice.Viewport = new Viewport(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp);
        SpriteBatch.Draw(imagenFinal, AreaPresentacion(pp.BackBufferWidth, pp.BackBufferHeight), Color.White);
        SpriteBatch.End();
    }

    private void DibujarFps()
    {
        _fotogramasFps++;
        double segundos = _relojFps.Elapsed.TotalSeconds;
        if (segundos >= 0.5)
        {
            _fps = (int)System.Math.Round(_fotogramasFps / segundos);
            _fotogramasFps = 0;
            _relojFps.Restart();
        }
        if (!OpcionesJuego.MostrarFps) return;
        var vista = GraphicsDevice.Viewport;
        float escala = System.Math.Min(vista.Width / 1280f, vista.Height / 720f);
        string texto = $"FPS: {_fps}";
        Vector2 posicion = new(vista.Width - 24f * escala - _fuenteFps.MeasureString(texto).X * escala, 24f * escala);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _1.UI.MarcadorPuntaje.DibujarTexto(_fuenteFps, texto, posicion, escala);
        SpriteBatch.End();
    }

    protected override void UnloadContent()
    {
        OpcionesJuego.GuardarAhora();
        _pantallaDupe?.Dispose();
        _pantallaDupe = null;
        _pantallaVirtual?.Dispose();
        _pantallaVirtual = null;
        _configuracionPausa?.Dispose();
        _configuracionPausa = null;
        _escenaActual?.Dispose();
        _escenaPendiente?.Dispose();
        _musicaMenu.Dispose();
        GumService.Default.Root.Children.Clear();
        base.UnloadContent();
    }
}
