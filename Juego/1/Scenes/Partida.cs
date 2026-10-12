using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework.Media;
using LutredLibrary;
using _1.Entities;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Mapa.Estructuras;

namespace _1.Scenes;

public class Partida : Escena
{
    private Sinner _sinner;
    private Duck _duck;
    private readonly List<EstructuraSolida> _estructuras = new();
    private readonly List<ColisionSolida> _obstaculos = new();
    private Effect _tintaAntigua;
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

        // Tres piezas fijas para probar; luego las ubicara el generador del piso.
        const int lado = 192;
        Texture2D ladrillos = Content.Load<Texture2D>("Textures/LadrillosTextura");
        _estructuras.Clear();
        _obstaculos.Clear();
        _estructuras.Add(new BloqueSolido(new Rectangle(
            (int)_sinner.Colision.Centro.X - 320,
            (int)_sinner.Colision.Centro.Y - lado / 2,
            lado, lado), ladrillos));
        _estructuras.Add(new EstructuraCircular(new Point(
            (int)_sinner.Colision.Centro.X,
            (int)_sinner.Colision.Centro.Y - 200), 88, ladrillos));
        _estructuras.Add(new EstructuraL(new Rectangle(
            (int)_sinner.Colision.Centro.X + 200,
            (int)_sinner.Colision.Centro.Y + 40,
            224, 224), 64, ladrillos));
        foreach (EstructuraSolida estructura in _estructuras)
            _obstaculos.AddRange(estructura.Colisiones);
        _tintaAntigua.Parameters["TamanoTexel"].SetValue(
            new Vector2(1f / ladrillos.Width, 1f / ladrillos.Height));
    }

    protected override void LoadContent()
    {
        _tintaAntigua = Content.Load<Effect>("Effects/TintaAntigua");
        // Ajustes del estilo: mas umbral = mas negro; mas contraste = menos grises.
        _tintaAntigua.Parameters["Umbral"].SetValue(0.80f);
        _tintaAntigua.Parameters["Contraste"].SetValue(60f);
        _tintaAntigua.Parameters["Grano"].SetValue(0.10f);
        _tintaAntigua.Parameters["EscalaGrano"].SetValue(512f);
        _tintaAntigua.Parameters["GrosorTinta"].SetValue(3f);

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
            _sinner.Update(gameTime, _obstaculos);
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

        if (!_derrotado)
        {
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                effect: _tintaAntigua);
            foreach (EstructuraSolida estructura in _estructuras)
                estructura.Draw(Core.SpriteBatch);
            Core.SpriteBatch.End();
        }

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
