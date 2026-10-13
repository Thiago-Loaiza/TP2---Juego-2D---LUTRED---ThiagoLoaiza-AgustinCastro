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
    private Escena _escenaActual;
    private Escena _escenaPendiente;

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

        // Cambiar despues de procesar la entrada evita modificar la UI en su Click.
        _escenaActual?.Dispose();
        GumService.Default.Root.Children.Clear();
        _escenaActual = _escenaPendiente;
        _escenaPendiente = null;
        _escenaActual.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        _escenaActual.Update(gameTime);
        GumService.Default.Update(gameTime);
        AplicarCambioDeEscena();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _escenaActual.Draw(gameTime);
        GumService.Default.Draw();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _escenaActual?.Dispose();
        _escenaPendiente?.Dispose();
        GumService.Default.Root.Children.Clear();
        base.UnloadContent();
    }
}
