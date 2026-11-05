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

namespace _1;

public class Game1 : Core
{
    // true = ON: habilita H (correr), J (dash), B (volver), N (borrar) y Enter (pausa).
    // false = OFF: conserva solamente los controles habituales del teclado.
    public static bool Arcade = true;

    // true = ON: Jugar abre la eleccion. false = OFF: entra directamente a la partida.
    public static bool EmpezarConEleccion = true;

    // Jugar abre mejoras y despues la pregunta; desactivar para el inicio habitual.
    public static bool EmpezarConMejoras = true;

    // Permite ofrecer mejoras sin exigir su piso minimo; conserva requisitos y limites.
    public static bool IgnorarNivelMinimoMejoras = true;

    private Escena _escenaActual;
    private RenderTarget2D _pantallaDupe;
    private Escena _escenaPendiente;
    private bool _salirPresionadoAnterior;
    private _1.UI.ConfiguracionPausa _configuracionPausa;
    private readonly _1.UI.MusicaMenu _musicaMenu = new();

    public Game1() : base("LUTRED", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        OpcionesJuego.Cargar();

        GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
        GumService.Default.ContentLoader.XnaContentManager = Content;
        GumService.Default.CanvasWidth = GraphicsDevice.Viewport.Width;
        GumService.Default.CanvasHeight = GraphicsDevice.Viewport.Height;
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
            _configuracionPausa = new _1.UI.ConfiguracionPausa(mejoras.SincronizarEntrada);
    }

    protected override void Update(GameTime gameTime)
    {
        OpcionesJuego.ActualizarGuardado((float)gameTime.ElapsedGameTime.TotalSeconds);
        _musicaMenu.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        bool salirPresionado = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
            || (Arcade && Keyboard.GetState().IsKeyDown(Keys.B));
        bool salirPulsado = salirPresionado && !_salirPresionadoAnterior;
        _salirPresionadoAnterior = salirPresionado;
        bool bloquearEscena = _configuracionPausa?.Update(gameTime, salirPulsado) == true;
        if (salirPulsado && !bloquearEscena && _escenaActual is not Salvacion && _escenaActual is not RegistrarPuntaje)
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
            else
            {
                // B permite volver sin cerrar el juego desde la botonera.
                if (!Arcade || !Keyboard.GetState().IsKeyDown(Keys.B))
                {
                    Exit();
                    return;
                }
            }
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
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Effect filtroDupe = (_escenaActual as Partida)?.FiltroDupe;
        RenderTargetBinding[] destinoAnterior = null;
        Viewport viewportAnterior = GraphicsDevice.Viewport;
        if (filtroDupe != null)
        {
            if (_pantallaDupe == null || _pantallaDupe.Width != viewportAnterior.Width ||
                _pantallaDupe.Height != viewportAnterior.Height)
            {
                _pantallaDupe?.Dispose();
                _pantallaDupe = new RenderTarget2D(GraphicsDevice, viewportAnterior.Width, viewportAnterior.Height);
            }
            destinoAnterior = GraphicsDevice.GetRenderTargets();
            GraphicsDevice.SetRenderTarget(_pantallaDupe);
        }

        _escenaActual.Draw(gameTime);
        GumService.Default.Draw();
        _1.UI.BotonAnimado.DibujarTextosSuaves();
        (_escenaActual as Inicio)?.DibujarControles();
        _configuracionPausa?.Draw();
        base.Draw(gameTime);

        if (filtroDupe != null)
        {
            GraphicsDevice.SetRenderTargets(destinoAnterior);
            GraphicsDevice.Viewport = viewportAnterior;
            SpriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp,
                effect: filtroDupe);
            SpriteBatch.Draw(_pantallaDupe, new Rectangle(0, 0, viewportAnterior.Width, viewportAnterior.Height), Color.White);
            SpriteBatch.End();
        }
    }

    protected override void UnloadContent()
    {
        OpcionesJuego.GuardarAhora();
        _pantallaDupe?.Dispose();
        _pantallaDupe = null;
        _configuracionPausa?.Dispose();
        _configuracionPausa = null;
        _escenaActual?.Dispose();
        _escenaPendiente?.Dispose();
        _musicaMenu.Dispose();
        GumService.Default.Root.Children.Clear();
        base.UnloadContent();
    }
}
