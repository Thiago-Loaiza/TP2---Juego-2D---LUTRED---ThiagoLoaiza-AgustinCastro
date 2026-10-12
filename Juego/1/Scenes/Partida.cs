using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework.Media;
using LutredLibrary;
using _1.Entities;

namespace _1.Scenes;

public class Partida : Escena
{
    private Sinner _sinner;
    private Duck _duck;
    private PantallaDerrota _pantallaDerrota;
    private bool _derrotado;

    public override void Initialize()
    {
        base.Initialize();
        _sinner.Position = new Vector2(
            Core.GraphicsDevice.Viewport.Width,
            Core.GraphicsDevice.Viewport.Height
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

    public override void Update(GameTime gameTime)
    {
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
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(_derrotado ? Color.Black : Color.White);

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        if (_derrotado)
        {
            _pantallaDerrota.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        }
        else
        {
            _sinner.Draw(Core.SpriteBatch);
            _duck.Draw(Core.SpriteBatch);
        }

        Core.SpriteBatch.End();
    }
}
