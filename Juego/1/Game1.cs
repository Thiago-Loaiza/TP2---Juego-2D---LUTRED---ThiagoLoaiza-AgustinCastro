using Gum.Forms;
using Gum.Forms.Controls;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using _1.Scenes;

namespace _1;

public class Game1 : Core
{
    // true = ON: Jugar abre la eleccion. false = OFF: entra directamente a la partida.
    public static bool EmpezarConEleccion = true;

    // Jugar abre mejoras y despues la pregunta; desactivar para el inicio habitual.
    public static bool EmpezarConMejoras = true;

    // Permite ofrecer mejoras sin exigir su piso minimo; conserva requisitos y limites.
    public static bool IgnorarNivelMinimoMejoras = false;

    private Escena _escenaActual;
    private Escena _escenaPendiente;
    private bool _salirPresionadoAnterior;
    private _1.UI.ConfiguracionPausa _configuracionPausa;

    public Game1() : base("LUTRED", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();

        GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
        GumService.Default.ContentLoader.XnaContentManager = Content;
        GumService.Default.CanvasWidth = GraphicsDevice.Viewport.Width;
        GumService.Default.CanvasHeight = GraphicsDevice.Viewport.Height;
        FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.S });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.W });
        FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.F });
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
        GumService.Default.Root.Children.Clear();
        _escenaActual = _escenaPendiente;
        _escenaPendiente = null;
        _escenaActual.Initialize();
        if (_escenaActual is Partida partida)
            _configuracionPausa = new _1.UI.ConfiguracionPausa(partida.SincronizarEntrada);
        else if (_escenaActual is Mejoras mejoras)
            _configuracionPausa = new _1.UI.ConfiguracionPausa(mejoras.SincronizarEntrada);
    }

    protected override void Update(GameTime gameTime)
    {
        bool salirPresionado = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape);
        bool salirPulsado = salirPresionado && !_salirPresionadoAnterior;
        _salirPresionadoAnterior = salirPresionado;
        bool bloquearEscena = _configuracionPausa?.Update(gameTime, salirPulsado) == true;
        if (salirPulsado && !bloquearEscena)
        {
            if (_escenaActual is Inicio inicio && (inicio.OpcionesAbiertas || inicio.EnTransicion))
                inicio.CerrarOpciones();
            else if (_escenaActual is Creditos creditos)
                creditos.VolverAlInicio();
            else
            {
                Exit();
                return;
            }
        }

        if (!bloquearEscena)
        {
            _escenaActual.Update(gameTime);
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
        AplicarCambioDeEscena();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _escenaActual.Draw(gameTime);
        GumService.Default.Draw();
        _1.UI.BotonAnimado.DibujarTextosSuaves();
        _configuracionPausa?.Draw();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _configuracionPausa?.Dispose();
        _configuracionPausa = null;
        _escenaActual?.Dispose();
        _escenaPendiente?.Dispose();
        GumService.Default.Root.Children.Clear();
        base.UnloadContent();
    }
}
