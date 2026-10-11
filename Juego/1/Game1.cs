using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using LutredLibrary;
using _1.Entities;

namespace _1;

public class Game1 : Core
{
    private Sinner _sinner;
    private Duck _duck;
    private PantallaDerrota _pantallaDerrota;
    private bool _derrotado;

    public Game1() : base("LUTRED", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        _sinner.Position = new Vector2(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height
        ) * 0.5f;
        _duck.Position = _sinner.Position + new Vector2(360f, 0f);
    }

    protected override void LoadContent()
    {
        Song musica = Content.Load<Song>(
            "Music/And the rest was silence - Grace OST"
        );

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0.3f;
        MediaPlayer.Play(musica);

        _sinner = new Sinner();
        _sinner.LoadContent(Content);

        _duck = new Duck();
        _duck.LoadContent(Content);
        _pantallaDerrota = new PantallaDerrota();
        _pantallaDerrota.LoadContent(Content);

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _sinner?.UnloadContent();
        _duck?.UnloadContent();
        MediaPlayer.Stop();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back
            == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (_derrotado)
        {
            _pantallaDerrota.Update(gameTime);
        }
        else
        {
            Vector2 centroAnteriorSinner = _sinner.Colision.Centro;
            Vector2 posicionAnteriorDuck = _duck.Position;
            bool estabaRebotando = _duck.Rebotando;
            _sinner.Update(gameTime);
            _duck.Update(gameTime, _sinner.Colision.Centro);

            // El parry se resuelve antes del contacto que provoca la derrota.
            if (_sinner.HaciendoParry
                && _sinner.ColisionParry.SeCruzan(centroAnteriorSinner,
                    _duck.Colision, posicionAnteriorDuck))
            {
                // Un mismo rebote no debe repetir el sonido en cada actualizacion.
                if (!estabaRebotando)
                {
                    _duck.Rebotar(_sinner.ColisionParry.Centro,
                        _sinner.ColisionParry.Radio, posicionAnteriorDuck);
                    _sinner.ConfirmarParry();
                }
            }
            else if (_sinner.Colision.SeCruzan(centroAnteriorSinner,
                    _duck.Colision, posicionAnteriorDuck))
            {
                _derrotado = true;
                MediaPlayer.Stop();
                _sinner.DetenerSonidos();
                _duck.DetenerSonidos();
                _pantallaDerrota.Mostrar();
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_derrotado ? Color.Black : Color.White);

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        if (_derrotado)
        {
            _pantallaDerrota.Draw(SpriteBatch, GraphicsDevice.Viewport);
        }
        else
        {
            _sinner.Draw(SpriteBatch);
            _duck.Draw(SpriteBatch);
        }

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
