using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using LutredLibrary.Scenes;
using LutredLibrary;
using LutredLibrary.Graphics;
using _1.Entities;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Mapa.Estructuras;
using _1.Mapa;
using System;
using _1.UI;

namespace _1.Scenes;

public class Partida : Escena
{
    private const int TotalOrbes = 10;
    private readonly Action<Escena> _cambiarEscena;
    private readonly List<Orbe> _orbes = new();
    private Animation _animacionOrbe;
    private SpriteFont _fuenteContador;
    private int _orbesRecogidos;
    private bool _nivelCompletado;
    private float _tiempoSalida;

    public Partida(Action<Escena> cambiarEscena)
    {
        _cambiarEscena = cambiarEscena ?? throw new ArgumentNullException(nameof(cambiarEscena));
    }

    private enum FaseEntrada { Transicion, Aparicion, Jugando }

    private Sinner _sinner;
    private Duck _duck;
    private JarJar _jarJar;
    private Zorak _zorak;
    private readonly Camara2D _camara = new();
    private readonly GeneradorMapa _generador = new();
    private readonly List<EstructuraSolida> _estructuras = new();
    private readonly List<ColisionSolida> _obstaculos = new();
    private Effect _tintaAntigua;
    private PantallaDerrota _pantallaDerrota;
    private bool _derrotado;
    private FaseEntrada _faseEntrada;
    private SoundEffectInstance _sonidoEntrada;
    private SoundEffectInstance _musicaNivel;
    private Texture2D _pixelTransicion;
    private float _opacidadBlanca = 1f;
    private BordeAnimado _bordePantalla;

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
        List<Vector2> posicionesOrbes = GeneradorOrbes.Generar(mapa.Limites,
            _sinner.Colision.Centro, _obstaculos, TotalOrbes, Random.Shared);
        _orbes.Clear();
        for (int i = 0; i < posicionesOrbes.Count; i++)
        {
            _orbes.Add(new Orbe(_animacionOrbe, posicionesOrbes[i]));
        }
        _tintaAntigua.Parameters["TamanoTexel"].SetValue(
            new Vector2(1f / ladrillos.Width, 1f / ladrillos.Height));
        Core.Audio.SongVolume = 0.3f;
        Core.Audio.StopSong();
        Core.Audio.PlaySoundEffect(_sonidoEntrada);
    }

    protected override void LoadContent()
    {
        _fuenteContador = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _animacionOrbe = Orbe.CrearAnimacion(
            Content.Load<Texture2D>("Sprites/Misc/Orbs/Punto-Sheet"));
        _tintaAntigua = Content.Load<Effect>("Effects/TintaAntigua");
        // aumentar el umbral produce mas negro, aumentar el contraste reduce los grises.
        _tintaAntigua.Parameters["Umbral"].SetValue(0.80f);
        _tintaAntigua.Parameters["Contraste"].SetValue(60f);
        _tintaAntigua.Parameters["Grano"].SetValue(0.10f);
        _tintaAntigua.Parameters["EscalaGrano"].SetValue(512f);
        _tintaAntigua.Parameters["GrosorTinta"].SetValue(3f);

        _sonidoEntrada = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Layers/Transicciones/Iniciador1"), isMusic: true);
        _musicaNivel = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Layers/Layers/Layer1"), isLooped: true, isMusic: true);
        _pixelTransicion = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixelTransicion.SetData(new[] { Color.White });
        _bordePantalla = new BordeAnimado(
            Content.Load<Texture2D>("Sprites/UI/BorderSreen/EfectoBorde-Sheet"), TimeSpan.FromSeconds(0.12));

        _sinner = new Sinner();
        _sinner.LoadContent(Content);

        _duck = new Duck();
        _duck.LoadContent(Content);

        _jarJar = new JarJar();
        _jarJar.LoadContent(Content);
        _zorak = new Zorak();
        _zorak.LoadContent(Content);
        _pantallaDerrota = new PantallaDerrota();
        _pantallaDerrota.LoadContent(Content);

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _sinner?.UnloadContent();
        _duck?.UnloadContent();
        _jarJar?.UnloadContent();
        _zorak?.UnloadContent();
        Core.Audio.StopSong();
        Core.Audio.ReleaseSoundEffect(_sonidoEntrada);
        Core.Audio.ReleaseSoundEffect(_musicaNivel);
        _pixelTransicion?.Dispose();
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        _bordePantalla.Update(gameTime);
        if (_nivelCompletado)
        {
            _tiempoSalida += (float)gameTime.ElapsedGameTime.TotalSeconds;
            // Un segundo de fundido y una pausa breve completamente a negro.
            if (_tiempoSalida >= 1.35f)
            {
                _cambiarEscena(new Seleccion(_cambiarEscena));
            }
            return;
        }
        for (int i = 0; i < _orbes.Count; i++)
        {
            _orbes[i].Update(gameTime);
        }
        if (_faseEntrada == FaseEntrada.Transicion)
        {
            if (!Core.Audio.IsPaused && _sonidoEntrada.State == SoundState.Stopped)
            {
                Core.Audio.PlaySoundEffect(_musicaNivel);
                _faseEntrada = FaseEntrada.Aparicion;
            }
            return;
        }

        if (_faseEntrada == FaseEntrada.Aparicion)
        {
            _opacidadBlanca = Math.Max(0f, _opacidadBlanca - (float)gameTime.ElapsedGameTime.TotalSeconds);
            if (_opacidadBlanca == 0f) _faseEntrada = FaseEntrada.Jugando;
        }

        if (_derrotado)
        {
            _pantallaDerrota.Update(gameTime);
        }
        else
        {
            if (_zorak.BloqueandoJugador)
            {
                if (_zorak.Update(gameTime, false, false) == ResultadoZorak.Golpe)
                    Perder();
                return;
            }
            Vector2 centroAnteriorSinner = _sinner.Colision.Centro;
            Vector2 posicionAnteriorDuck = _duck.Position;
            bool estabaRebotando = _duck.Rebotando;
            _sinner.Update(gameTime, _obstaculos);
            _camara.Seguir(_sinner.Position);
            ResultadoZorak resultadoZorak = _zorak.Update(gameTime,
                Vector2.DistanceSquared(centroAnteriorSinner, _sinner.Colision.Centro) > 0.000001f,
                _sinner.ParryIniciadoEsteFrame);
            if (resultadoZorak == ResultadoZorak.Parreado)
                _sinner.ConfirmarParry();
            if (_zorak.BloqueandoJugador)
            {
                _sinner.DetenerSonidos();
                _duck.DetenerSonidos();
                _jarJar.DetenerSonidos();
                return;
            }
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

            if (!_derrotado && _faseEntrada == FaseEntrada.Jugando)
            {
                RecogerOrbes(centroAnteriorSinner);
            }
        }
    }

    private void RecogerOrbes(Vector2 centroAnterior)
    {
        for (int i = _orbes.Count - 1; i >= 0; i--)
        {
            CirculoColision colision = _orbes[i].Colision;
            // Comprobar el recorrido evita saltarse un orbe al correr con pocos FPS.
            if (_sinner.Colision.SeCruzan(centroAnterior, colision, colision.Centro))
            {
                _orbes.RemoveAt(i);
                _orbesRecogidos++;
            }
        }
        if (_orbesRecogidos == TotalOrbes)
        {
            _nivelCompletado = true;
            Core.Audio.StopSoundEffect(_musicaNivel);
            _sinner.DetenerSonidos();
            _duck.DetenerSonidos();
            _jarJar.DetenerSonidos();
            _zorak.DetenerSonidos();
        }
    }

    private void Perder()
    {
        _derrotado = true;
        Core.Audio.StopSong();
        Core.Audio.StopSoundEffect(_musicaNivel);
        _sinner.DetenerSonidos();
        _duck.DetenerSonidos();
        _jarJar.DetenerSonidos();
        _zorak.DetenerSonidos();
        _pantallaDerrota.Mostrar();
    }

    public override void Draw(GameTime gameTime)
    {
        if (_faseEntrada == FaseEntrada.Transicion)
        {
            Core.GraphicsDevice.Clear(Color.Black);
            DibujarBorde();
            return;
        }

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
            for (int i = 0; i < _orbes.Count; i++)
            {
                _orbes[i].Draw(Core.SpriteBatch);
            }
            _jarJar.Draw(Core.SpriteBatch);
            _sinner.Draw(Core.SpriteBatch);
            _duck.Draw(Core.SpriteBatch);
        }

        Core.SpriteBatch.End();

        if (_faseEntrada == FaseEntrada.Aparicion)
        {
            var viewport = Core.GraphicsDevice.Viewport;
            Core.SpriteBatch.Begin(blendState: BlendState.AlphaBlend);
            Core.SpriteBatch.Draw(_pixelTransicion,
                new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White * _opacidadBlanca);
            Core.SpriteBatch.End();
        }
        DibujarBorde();

        if (!_derrotado)
        {
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            string contador = $"{_orbesRecogidos}/{TotalOrbes}";
            Vector2 tamano = _fuenteContador.MeasureString(contador);
            Vector2 posicion = new(
                MathF.Round((Core.GraphicsDevice.Viewport.Width - tamano.X) * 0.5f), 24f);
            Core.SpriteBatch.DrawString(_fuenteContador, contador,
                posicion + new Vector2(2f), Color.White);
            Core.SpriteBatch.DrawString(_fuenteContador, contador, posicion, Color.Black);
            Core.SpriteBatch.End();

            // La capa negra cubre tambien el borde y Zorak queda encima de todo.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            _zorak.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
            Core.SpriteBatch.End();
        }
        if (_nivelCompletado)
        {
            var viewport = Core.GraphicsDevice.Viewport;
            Core.SpriteBatch.Begin(blendState: BlendState.AlphaBlend);
            Core.SpriteBatch.Draw(_pixelTransicion,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * MathHelper.Clamp(_tiempoSalida, 0f, 1f));
            Core.SpriteBatch.End();
        }
    }

    private void DibujarBorde()
    {
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        _bordePantalla.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        Core.SpriteBatch.End();
    }
}
