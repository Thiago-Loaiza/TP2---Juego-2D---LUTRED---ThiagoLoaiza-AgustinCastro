using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework.Media;
using LutredLibrary;
using LutredLibrary.Graphics;
using _1.Entities;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Mapa.Estructuras;
using _1.Mapa;
using System;

namespace _1.Scenes;

public class Partida : Escena
{
    private Sinner _sinner;
    private Duck _duck;
    private JarJar _jarJar;
    private readonly Camara2D _camara = new();
    private readonly GeneradorMapa _generador = new();
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
        _camara.Seguir(_sinner.Position);
        _duck.Position = _sinner.Position + new Vector2(360f, 0f);
        _jarJar.Ubicar(_sinner.Position + new Vector2(-300f, 280f));

        Texture2D ladrillos = Content.Load<Texture2D>("Textures/LadrillosTextura");
        MapaGenerado mapa = _generador.Generar(_sinner.Position, ladrillos,
            CatalogoEstructuras.Crear(), Random.Shared.Next());
        _estructuras.Clear();
        _obstaculos.Clear();
        _estructuras.AddRange(mapa.Estructuras);
        _obstaculos.AddRange(mapa.Obstaculos);
        _tintaAntigua.Parameters["TamanoTexel"].SetValue(
            new Vector2(1f / ladrillos.Width, 1f / ladrillos.Height));
    }

    protected override void LoadContent()
    {
        _tintaAntigua = Content.Load<Effect>("Effects/TintaAntigua");
        // aumentar el umbral produce mas negro, aumentar el contraste reduce los grises.
        _tintaAntigua.Parameters["Umbral"].SetValue(0.80f);
        _tintaAntigua.Parameters["Contraste"].SetValue(60f);
        _tintaAntigua.Parameters["Grano"].SetValue(0.10f);
        _tintaAntigua.Parameters["EscalaGrano"].SetValue(512f);
        _tintaAntigua.Parameters["GrosorTinta"].SetValue(3f);

        Song musica = Content.Load<Song>(
            "Music/And the rest was silence - Grace OST"
        );

        Core.Audio.SongVolume = 0.3f;
        Core.Audio.PlaySong(musica);

        _sinner = new Sinner();
        _sinner.LoadContent(Content);

        _duck = new Duck();
        _duck.LoadContent(Content);

        _jarJar = new JarJar();
        _jarJar.LoadContent(Content);
        _pantallaDerrota = new PantallaDerrota();
        _pantallaDerrota.LoadContent(Content);

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _sinner?.UnloadContent();
        _duck?.UnloadContent();
        _jarJar?.UnloadContent();
        Core.Audio.StopSong();
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
            _camara.Seguir(_sinner.Position);
            _duck.Update(gameTime, _sinner.Colision.Centro);

            // el parry se resuelve antes del contacto para evitar una derrota cuando ambos ocurren juntos.
            if (_sinner.HaciendoParry
                && _sinner.ColisionParry.SeCruzan(centroAnteriorSinner,
                    _duck.Colision, posicionAnteriorDuck))
            {
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
                Perder();
                return;
            }

            ResultadoJarJar resultadoJarJar = _jarJar.Update(gameTime,
                _sinner.Colision.Centro, _sinner.HaciendoParry);
            if (resultadoJarJar == ResultadoJarJar.Parreado)
                _sinner.ConfirmarParry();
            else if (resultadoJarJar == ResultadoJarJar.Golpe)
                Perder();
        }
    }

    private void Perder()
    {
        _derrotado = true;
        Core.Audio.StopSong();
        _sinner.DetenerSonidos();
        _duck.DetenerSonidos();
        _jarJar.DetenerSonidos();
        _pantallaDerrota.Mostrar();
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(_derrotado ? Color.Black : Color.White);
        Matrix transformacion = _camara.ObtenerTransformacion(Core.GraphicsDevice.Viewport);

        if (!_derrotado)
        {
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointWrap,
                effect: _tintaAntigua,
                transformMatrix: transformacion);
            var viewport = Core.GraphicsDevice.Viewport;
            var visible = new Rectangle(
                (int)MathF.Floor(_camara.Posicion.X - viewport.Width * 0.5f),
                (int)MathF.Floor(_camara.Posicion.Y - viewport.Height * 0.5f),
                viewport.Width + 1, viewport.Height + 1);
            foreach (EstructuraSolida estructura in _estructuras)
                if (estructura.Limites.Intersects(visible)) estructura.Draw(Core.SpriteBatch);
            Core.SpriteBatch.End();
        }

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _derrotado ? Matrix.Identity : transformacion
        );

        if (_derrotado)
        {
            _pantallaDerrota.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        }
        else
        {
            _jarJar.Draw(Core.SpriteBatch);
            _sinner.Draw(Core.SpriteBatch);
            _duck.Draw(Core.SpriteBatch);
        }

        Core.SpriteBatch.End();
    }
}
