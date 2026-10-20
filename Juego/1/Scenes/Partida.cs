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
    private const int CantidadLayers = 8;
    private const int CantidadTransiciones = 9;
    private readonly Action<Escena> _cambiarEscena;
    private readonly int _numeroNivel;
    private readonly bool _transicionReproducida;
    private readonly List<Orbe> _orbes = new();
    private Animation _animacionOrbe;
    private SpriteFont _fuenteContador;
    private Texture2D _indicadorOrbes;
    private int _orbesRecogidos;
    private bool _nivelCompletado;
    private const float DuracionTemblorContador = 0.28f;
    private float _temblorContadorRestante;
    private float _relojContador;

    private bool PocosOrbes => _orbes.Count > 0 && _orbes.Count * 4 <= TotalOrbes;

    public Partida(Action<Escena> cambiarEscena, int numeroNivel = 1,
        bool transicionReproducida = false)
    {
        _cambiarEscena = cambiarEscena ?? throw new ArgumentNullException(nameof(cambiarEscena));
        if (numeroNivel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroNivel));
        }
        _numeroNivel = numeroNivel;
        _transicionReproducida = transicionReproducida;
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
    private SoundEffectInstance _sonidoSalida;
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
        if (_transicionReproducida)
        {
            // La transicion ya sono al completar el nivel anterior.
            _faseEntrada = FaseEntrada.Aparicion;
            Core.Audio.PlaySoundEffect(_musicaNivel);
        }
        else
        {
            Core.Audio.PlaySoundEffect(_sonidoEntrada);
        }
    }

    protected override void LoadContent()
    {
        _fuenteContador = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _indicadorOrbes = Content.Load<Texture2D>("Sprites/Misc/Orbs/IndicadorPuntos");
        _animacionOrbe = Orbe.CrearAnimacion(
            Content.Load<Texture2D>("Sprites/Misc/Orbs/Punto-Sheet"));
        _tintaAntigua = Content.Load<Effect>("Effects/TintaAntigua");
        // aumentar el umbral produce mas negro, aumentar el contraste reduce los grises.
        _tintaAntigua.Parameters["Umbral"].SetValue(0.80f);
        _tintaAntigua.Parameters["Contraste"].SetValue(60f);
        _tintaAntigua.Parameters["Grano"].SetValue(0.10f);
        _tintaAntigua.Parameters["EscalaGrano"].SetValue(512f);
        _tintaAntigua.Parameters["GrosorTinta"].SetValue(3f);

        int layer = (_numeroNivel - 1) % CantidadLayers + 1;
        int transicionEntrada = (_numeroNivel - 1) % CantidadTransiciones + 1;
        int transicionSalida = _numeroNivel % CantidadTransiciones + 1;
        _sonidoEntrada = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>($"Music/Layers/Transicciones/Iniciador{transicionEntrada}"), isMusic: true);
        _sonidoSalida = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>($"Music/Layers/Transicciones/Iniciador{transicionSalida}"), isMusic: true);
        _musicaNivel = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>($"Music/Layers/Layers/Layer{layer}"), isLooped: true, isMusic: true);
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
        Core.Audio.ReleaseSoundEffect(_sonidoSalida);
        Core.Audio.ReleaseSoundEffect(_musicaNivel);
        _pixelTransicion?.Dispose();
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        _bordePantalla.Update(gameTime);
        if (_nivelCompletado)
        {
            if (!Core.Audio.IsPaused && _sonidoSalida.State == SoundState.Stopped)
            {
                _cambiarEscena(new Seleccion(_cambiarEscena, _numeroNivel + 1,
                    transicionReproducida: true));
            }
            return;
        }
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _relojContador = (_relojContador + segundos) % 100f;
        _temblorContadorRestante = Math.Max(0f, _temblorContadorRestante - segundos);
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
                _temblorContadorRestante = DuracionTemblorContador;
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
            Core.Audio.PlaySoundEffect(_sonidoSalida, restart: true);
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
        if (_nivelCompletado)
        {
            Core.GraphicsDevice.Clear(Color.Black);
            return;
        }

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
            DibujarIndicadorOrbes(transformacion);
            DibujarContadorOrbes();
            Core.SpriteBatch.End();

            // La capa negra cubre tambien el borde y Zorak queda encima de todo.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            _zorak.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
            Core.SpriteBatch.End();
        }
    }

    private void DibujarBorde()
    {
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        _bordePantalla.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        Core.SpriteBatch.End();
    }

    private void DibujarContadorOrbes()
    {
        string texto = $"{_orbesRecogidos}/{TotalOrbes}";
        Vector2 tamano = _fuenteContador.MeasureString(texto);
        Vector2 origen = tamano * 0.5f;
        Vector2 posicion = new(Core.GraphicsDevice.Viewport.Width * 0.5f, 24f + origen.Y);

        float intensidad = _temblorContadorRestante / DuracionTemblorContador;
        float amplitud = (PocosOrbes ? 3f : 2f) * intensidad * intensidad;
        posicion += new Vector2(
            MathF.Sin(_relojContador * 83f), MathF.Cos(_relojContador * 107f)) * amplitud;
        posicion = new Vector2(MathF.Round(posicion.X), MathF.Round(posicion.Y));

        float pulso = PocosOrbes
            ? 0.5f - 0.5f * MathF.Cos(_relojContador * MathHelper.TwoPi * 1.35f)
            : 0f;
        float rebote = 0.1f * MathF.Sin(intensidad * MathHelper.Pi);
        float escala = 1f + rebote + pulso * 0.025f;
        Color rojo = new(230, 20, 35);

        if (PocosOrbes)
        {
            // El halo respira alrededor del borde; el relleno siempre se dibuja negro encima.
            float radioHalo = 3f + pulso * 3f;
            Color brillo = rojo * (0.04f + pulso * 0.18f);
            for (int i = 0; i < 12; i++)
            {
                float angulo = i * MathHelper.TwoPi / 12f;
                Vector2 desplazamiento = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * radioHalo;
                Core.SpriteBatch.DrawString(_fuenteContador, texto, posicion + desplazamiento,
                    brillo, 0f, origen, escala, SpriteEffects.None, 0f);
            }
        }

        Color borde = Color.Lerp(rojo, new Color(255, 65, 65), pulso);
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }
                Core.SpriteBatch.DrawString(_fuenteContador, texto,
                    posicion + new Vector2(x, y) * 2f, borde,
                    0f, origen, escala, SpriteEffects.None, 0f);
            }
        }
        Core.SpriteBatch.DrawString(_fuenteContador, texto, posicion, Color.Black,
            0f, origen, escala, SpriteEffects.None, 0f);
    }

    private void DibujarIndicadorOrbes(Matrix transformacion)
    {
        if (_orbes.Count == 0)
        {
            return;
        }

        Vector2 centroSinner = _sinner.Colision.Centro;
        Vector2 objetivo = _orbes[0].Colision.Centro;
        float distanciaMinima = Vector2.DistanceSquared(centroSinner, objetivo);
        for (int i = 1; i < _orbes.Count; i++)
        {
            Vector2 candidato = _orbes[i].Colision.Centro;
            float distancia = Vector2.DistanceSquared(centroSinner, candidato);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                objetivo = candidato;
            }
        }

        Vector2 direccion = objetivo - centroSinner;
        if (direccion.LengthSquared() < 0.000001f)
        {
            direccion = -Vector2.UnitY;
        }
        Viewport viewport = Core.GraphicsDevice.Viewport;
        Vector2 origen = Vector2.Transform(centroSinner, transformacion);
        Vector2 pivote = new(_indicadorOrbes.Width * 0.5f, _indicadorOrbes.Height * 0.5f);
        // La diagonal deja el icono completo dentro de la pantalla incluso al girar.
        float margen = Math.Min(pivote.Length() + 16f,
            Math.Min(viewport.Width, viewport.Height) * 0.5f - 1f);
        float hastaX = direccion.X == 0f ? float.PositiveInfinity
            : ((direccion.X > 0f ? viewport.Width - margen : margen) - origen.X) / direccion.X;
        float hastaY = direccion.Y == 0f ? float.PositiveInfinity
            : ((direccion.Y > 0f ? viewport.Height - margen : margen) - origen.Y) / direccion.Y;
        Vector2 posicion = origen + direccion * Math.Min(hastaX, hastaY);

        // El dibujo original apunta hacia arriba.
        float rotacion = MathF.Atan2(direccion.Y, direccion.X) + MathHelper.PiOver2;
        Core.SpriteBatch.Draw(_indicadorOrbes,
            new Vector2(MathF.Round(posicion.X), MathF.Round(posicion.Y)),
            null, Color.White, rotacion, pivote, 1f, SpriteEffects.None, 0f);
    }
}
