using _1.Progression;
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
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;

namespace _1.Scenes;

public partial class Partida : Escena
{
    public void SincronizarEntrada() => _sinner?.SincronizarEntrada();
    private const int TotalOrbes = 10;
    private const int CantidadLayers = 8;
    private const int CantidadTransiciones = 9;
    private readonly Action<Escena> _cambiarEscena;
    private readonly EstadoMejoras _mejoras;
    private EstadoPuntaje Puntaje => _mejoras.Puntaje;
    private MarcadorPuntaje _marcador;
    private readonly int _numeroNivel;
    private readonly bool _transicionReproducida;
    private readonly int _cantidadDuke;
    private readonly int _cantidadJarJar;
    private readonly int _cantidadDupe;
    private readonly List<Dupe> _dupes = new();
    private readonly EstadoEfectoDupe _efectoDupe = new();
    private Effect _coloresInvertidos;
    public Effect FiltroDupe => !_derrotado && !_nivelCompletado && _efectoDupe.Activo ? _coloresInvertidos : null;
    private readonly bool _sorakElegido;
    private readonly bool _erosionBirdElegido;
    private readonly bool _delusionElegido;
    private readonly bool _tooManyParriesElegido;
    private readonly double _duracionNivel;
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
        bool transicionReproducida = false, int cantidadDuke = 0, int cantidadJarJar = 0, bool sorakElegido = false,
        double duracionNivel = RelojNivel.Duracion, bool erosionBirdElegido = false, EstadoMejoras mejoras = null,
        bool delusionElegido = false, bool tooManyParriesElegido = false, int cantidadDupe = 0)
    {
        _cambiarEscena = cambiarEscena ?? throw new ArgumentNullException(nameof(cambiarEscena));
        if (numeroNivel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroNivel));
        }
        _numeroNivel = numeroNivel;
        _mejoras = mejoras ?? new EstadoMejoras();
        _transicionReproducida = transicionReproducida;
        if (cantidadDuke < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadDuke));
        }
        if (cantidadJarJar < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadJarJar));
        }
        _cantidadDuke = cantidadDuke;
        _cantidadJarJar = cantidadJarJar;
        if (cantidadDupe < 0) throw new ArgumentOutOfRangeException(nameof(cantidadDupe));
        _cantidadDupe = cantidadDupe;
        _sorakElegido = sorakElegido;
        _erosionBirdElegido = erosionBirdElegido;
        _delusionElegido = delusionElegido;
        _tooManyParriesElegido = tooManyParriesElegido;
        if (!double.IsFinite(duracionNivel) || duracionNivel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duracionNivel));
        }
        _duracionNivel = duracionNivel;
    }

    private enum FaseEntrada { Transicion, Aparicion, Jugando }

    private Sinner _sinner;
    private Shame _shame;
    private RelojNivel _relojNivel;
    private SoundEffectInstance _avisoShame;
    private bool _shameParreadoEsteParry;
    private readonly List<Duck> _dukes = new();
    private readonly List<JarJar> _jarJars = new();
    private Zorak _zorak;
    private ErosionBird _erosionBird;
    private Delusion _delusion;
    private TooManyParries _tooManyParries;
    private int _tandaLanzas;
    private readonly Camara2D _camara = new();
    private readonly GeneradorMapa _generador = new();
    private readonly List<EstructuraSolida> _estructuras = new();
    private readonly List<ColisionSolida> _obstaculos = new();
    private List<Vector2> _posicionesLibres = new();
    private Rectangle _limitesExteriores;
    private Effect _tintaAntigua;
    private PantallaDerrota _pantallaDerrota;
    private BotonAnimado _volverInicio;
    private bool _derrotado;
    private FaseEntrada _faseEntrada;
    private SoundEffectInstance _sonidoEntrada;
    private SoundEffectInstance _sonidoSalida;
    private SoundEffectInstance _musicaNivel;
    private Texture2D _pixelTransicion;
    private Texture2D _degradadoFondo;
    private float _faseLatidoFondo;
    private float _opacidadBlanca = 1f;
    private BordeAnimado _bordePantalla;

    public override void Initialize()
    {
        base.Initialize();
        _sinner.Position = new Vector2(
            Core.GraphicsDevice.Viewport.Width,
            Core.GraphicsDevice.Viewport.Height
        ) * 0.5f;
        _relojNivel = new RelojNivel(_sinner.Colision.Centro, _duracionNivel);
        _camara.Seguir(_sinner.Position);
        // Radio fijo de 1000 px; rotacion aleatoria y un sector por Duck para dispersarlos.
        float rotacionDukes = Random.Shared.NextSingle() * MathHelper.TwoPi;
        float sectorDukes = MathHelper.TwoPi / Math.Max(1, _dukes.Count);
        for (int i = 0; i < _dukes.Count; i++)
        {
            float angulo = rotacionDukes + (i + Random.Shared.NextSingle() * 0.7f) * sectorDukes;
            _dukes[i].Position = _sinner.Position
                + new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * 1000f;
        }
        for (int i = 0; i < _jarJars.Count; i++)
        {
            float angulo = i * MathHelper.TwoPi / _jarJars.Count;
            _jarJars[i].Ubicar(_sinner.Position
                + Vector2.Transform(new Vector2(-300f, 280f), Matrix.CreateRotationZ(angulo)));
        }

        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();
        _volverInicio = new BotonAnimado(
            Content.Load<Texture2D>("Sprites/UI/Buttons/Button-Sheet"),
            400, 200, 9, TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
        {
            Text = "Volver a la pantalla de inicio",
            Width = 640,
            Height = 100,
            IsEnabled = false
        };
        _volverInicio.Anchor(Anchor.Center);
        _volverInicio.Y = 190;
        _volverInicio.Visual.Visible = false;
        _volverInicio.Click += VolverAlInicio;
        panel.AddChild(_volverInicio);

        Texture2D ladrillos = Content.Load<Texture2D>("Textures/LadrillosTextura");
        MapaGenerado mapa = _generador.Generar(_sinner.Position, ladrillos,
            CatalogoEstructuras.Crear(), Random.Shared.Next());
        _limitesExteriores = mapa.Limites;
        _limitesExteriores.Inflate(_generador.TamanoChunk, _generador.TamanoChunk);
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
        // JarJar solo se teletransporta a lugares sin paredes y conectados con el inicio.
        _posicionesLibres = GeneradorOrbes.PosicionesAccesibles(mapa.Limites,
            _sinner.Colision.Centro, _obstaculos);
        _delusion?.Ubicar(_sinner.Colision.Centro, _posicionesLibres);
        PrepararFellDoor(mapa.Limites);
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
        // La transparencia crece hacia abajo; el filtrado lineal suaviza el degradado.
        _degradadoFondo = new Texture2D(Core.GraphicsDevice, 1, 256);
        var coloresFondo = new Color[256];
        for (int y = 0; y < coloresFondo.Length; y++)
            coloresFondo[y] = Color.White * (y / 255f);
        _degradadoFondo.SetData(coloresFondo);
        _fuenteContador = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        Puntaje.ComenzarNivel();
        _marcador = new MarcadorPuntaje(Content, Puntaje);
        _indicadorOrbes = Content.Load<Texture2D>("Sprites/Misc/Orbs/IndicadorPuntos");
        _animacionOrbe = Orbe.CrearAnimacion(
            Content.Load<Texture2D>("Sprites/Misc/Orbs/Punto-Sheet"));
        _tintaAntigua = Content.Load<Effect>("Effects/TintaAntigua");
        _coloresInvertidos = Content.Load<Effect>("Effects/ColoresInvertidos");
        // aumentar el umbral produce mas negro, aumentar el contraste reduce los grises.
        _tintaAntigua.Parameters["Umbral"].SetValue(0.80f);
        _tintaAntigua.Parameters["Contraste"].SetValue(60f);
        _tintaAntigua.Parameters["Grano"].SetValue(0.10f);
        _tintaAntigua.Parameters["EscalaGrano"].SetValue(512f);
        _tintaAntigua.Parameters["GrosorTinta"].SetValue(3f);

        int layer = _numeroNivel <= CantidadLayers ? _numeroNivel : 4 + (_numeroNivel - 9) % 5;
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

        _sinner = new Sinner { Mejoras = _mejoras };
        _sinner.LoadContent(Content);
        for (int i = 0; i < _cantidadDupe; i++)
        {
            var dupe = new Dupe(i);
            dupe.LoadContent(Content);
            _dupes.Add(dupe);
        }
        _shame = new Shame();
        _shame.LoadContent(Content);
        _avisoShame = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("SFX/Entities/SHAME/AvisoShame"), volume: 0.8f);

        for (int i = 0; i < _cantidadDuke; i++)
        {
            var duke = new Duck();
            duke.LoadContent(Content);
            _dukes.Add(duke);
        }
        for (int i = 0; i < _cantidadJarJar; i++)
        {
            var jarJar = new JarJar();
            jarJar.LoadContent(Content);
            _jarJars.Add(jarJar);
        }
        if (_sorakElegido)
        {
            _zorak = new Zorak(_mejoras.Cantidad("Paz") > 0);
            _zorak.LoadContent(Content);
        }
        if (_erosionBirdElegido)
        {
            _erosionBird = new ErosionBird(_mejoras.Cantidad("Paz") > 0);
            _erosionBird.LoadContent(Content);
        }
        if (_delusionElegido)
        {
            _delusion = new Delusion();
            _delusion.LoadContent(Content);
        }
        if (_tooManyParriesElegido)
        {
            _tooManyParries = new TooManyParries();
            _tooManyParries.LoadContent(Content);
        }
        _pantallaDerrota = new PantallaDerrota();
        _pantallaDerrota.LoadContent(Content);

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _marcador?.Dispose();
        _sinner?.UnloadContent();
        foreach (Dupe dupe in _dupes) dupe.UnloadContent();
        _dupes.Clear();
        _shame?.UnloadContent();
        Core.Audio.ReleaseSoundEffect(_avisoShame);
        for (int i = 0; i < _dukes.Count; i++)
        {
            _dukes[i].UnloadContent();
        }
        for (int i = 0; i < _jarJars.Count; i++)
        {
            _jarJars[i].UnloadContent();
        }
        _dukes.Clear();
        _jarJars.Clear();
        if (_volverInicio != null)
        {
            _volverInicio.Click -= VolverAlInicio;
            _volverInicio.Dispose();
        }
        _zorak?.UnloadContent();
        _erosionBird?.UnloadContent();
        Core.Audio.StopSong();
        Core.Audio.ReleaseSoundEffect(_sonidoEntrada);
        Core.Audio.ReleaseSoundEffect(_sonidoSalida);
        Core.Audio.ReleaseSoundEffect(_musicaNivel);
        Core.Audio.ReleaseSoundEffect(_musicaFellDoor);
        _pixelTransicion?.Dispose();
        _degradadoFondo?.Dispose();
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Audio.IsPaused) { return; }
        if (!_derrotado && !_nivelCompletado) ActualizarMusicaFellDoor(gameTime);
        _bordePantalla.Update(gameTime);
        if (_nivelCompletado)
        {
            if (!Core.Audio.IsPaused && _sonidoSalida.State == SoundState.Stopped)
            {
                void AbrirPregunta() => _cambiarEscena(new Seleccion(_cambiarEscena, _numeroNivel + 1,
                    transicionReproducida: true,
                    cantidadDuke: _cantidadDuke, cantidadJarJar: _cantidadJarJar, sorakElegido: _sorakElegido,
                    duracionNivel: _duracionNivel, erosionBirdElegido: _erosionBirdElegido, mejoras: _mejoras,
                    delusionElegido: _delusionElegido, tooManyParriesElegido: _tooManyParriesElegido,
                    cantidadDupe: _cantidadDupe));
                if (_numeroNivel % 4 == 0)
                {
                    _cambiarEscena(new Mejoras(AbrirPregunta, _mejoras, _numeroNivel));
                }
                else
                {
                    AbrirPregunta();
                }
            }
            return;
        }
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (!_derrotado)
            _faseLatidoFondo = (_faseLatidoFondo + segundos * MathHelper.TwoPi / 2.4f) % MathHelper.TwoPi;
        if (!_derrotado && _faseEntrada == FaseEntrada.Jugando) Puntaje.Actualizar(segundos);
        _marcador.Update(segundos);
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
            _volverInicio.Update(gameTime);
        }
        else
        {
            _sinner.ActualizarEscudo(gameTime);
            _efectoDupe.Actualizar(segundos);
            _sinner.ControlesInvertidos = _efectoDupe.Activo;
            if (_enFellDoor)
            {
                ActualizarDimensionFellDoor(gameTime);
                return;
            }
            if (_zorak?.BloqueandoJugador == true)
            {
                if (_zorak.Update(gameTime, false, false) == ResultadoZorak.Golpe)
                    Perder();
                return;
            }
            Vector2 centroAnteriorSinner = _sinner.Colision.Centro;

            _tooManyParries?.Update(gameTime);
            AplicarTooManyParries();
            _sinner.Update(gameTime, _obstaculos);
            if (_sinner.ParryPresionadoEsteFrame)
            {
                _tooManyParries?.RegistrarPulsacion(_mejoras.Cantidad("Paz") > 0);
                // Aplicarlo antes de los enemigos: la pulsacion que lo invoca ya no parrea.
                AplicarTooManyParries();
            }
            _camara.Seguir(_sinner.Position);
            if (ActualizarPuertaFellDoor(gameTime, centroAnteriorSinner)) return;
            ResultadoZorak resultadoZorak = _zorak?.Update(gameTime,
                Vector2.DistanceSquared(centroAnteriorSinner, _sinner.Colision.Centro) > 0.000001f,
                !_sinner.DashActivo && (_sinner.ParryIniciadoEsteFrame || _sinner.AutoparryActivo),
                _erosionBird?.AreaOcupada ?? Rectangle.Empty, _sinner.DashActivo) ?? ResultadoZorak.Nada;
            if (resultadoZorak == ResultadoZorak.Parreado)
                RegistrarParry(TipoParry.Zorak, _zorak, !_sinner.ParryIniciadoEsteFrame,
                    _zorak.ParryUltimoInstante);
            if (_zorak?.BloqueandoJugador == true)
            {
                _sinner.DetenerSonidos();
                DetenerSonidosEnemigos();
                return;
            }
            for (int i = 0; i < _dukes.Count; i++)
            {
                Duck duke = _dukes[i];
                Vector2 posicionAnterior = duke.Position;
                bool estabaRebotando = duke.Rebotando;
                duke.Update(gameTime, _sinner.Colision.Centro);

                // Resolver cada parry antes del contacto, conservando el recorrido de cada Duke.
                if (_sinner.PuedeParrear
                    && _sinner.ColisionParry.SeCruzan(centroAnteriorSinner,
                        duke.Colision, posicionAnterior))
                {
                    if (!estabaRebotando)
                    {
                        duke.Rebotar(_sinner.ColisionParry.Centro,
                            _sinner.ColisionParry.Radio, posicionAnterior);
                        RegistrarParry(TipoParry.Duck, duke, !_sinner.HaciendoParry);
                    }
                }
                else if (_sinner.Colision.SeCruzan(centroAnteriorSinner,
                    duke.Colision, posicionAnterior))
                {
                    if (Perder()) return;
                }
            }

            ResultadoErosionBird resultadoBird = _erosionBird?.Update(gameTime, _sinner,
                centroAnteriorSinner, Core.GraphicsDevice.Viewport, _zorak?.AreaOcupada ?? Rectangle.Empty)
                ?? ResultadoErosionBird.Nada;
            if (_erosionBird != null && _tandaLanzas != _erosionBird.TandaActual)
            {
                _tandaLanzas = _erosionBird.TandaActual;
                Puntaje.ComenzarTandaLanzas();
            }
            if (resultadoBird == ResultadoErosionBird.Parreado)
            {
                bool automatico = !_sinner.HaciendoParry;
                for (int i = 0; i < _erosionBird.ParrysEsteFrame; i++)
                    RegistrarParry(TipoParry.Lanza, _erosionBird, automatico);
                if (_erosionBird.TandaParreadaEsteFrame) Puntaje.RegistrarBadBird();
            }
            else if (resultadoBird == ResultadoErosionBird.Golpe)
            {
                if (Perder()) return;
            }
            Rectangle areaVisible = AreaVisible();
            for (int i = 0; i < _jarJars.Count; i++)
            {
                ResultadoJarJar resultadoJarJar = _jarJars[i].Update(gameTime,
                    _sinner.Colision.Centro, _sinner.VelocidadActual, _sinner.PuedeParrear,
                    areaVisible, _posicionesLibres, _sinner.DashActivo);
                if (resultadoJarJar == ResultadoJarJar.Parreado)
                {
                    RegistrarParry(TipoParry.JarJar, _jarJars[i], !_sinner.HaciendoParry);
                }
                else if (resultadoJarJar == ResultadoJarJar.Golpe)
                {
                    if (Perder()) return;
                }
            }

            ResultadoDelusion resultadoDelusion = _delusion?.Update(gameTime, _sinner,
                centroAnteriorSinner, _obstaculos, _posicionesLibres) ?? ResultadoDelusion.Nada;
            if (resultadoDelusion == ResultadoDelusion.Parreado)
            {
                bool automatico = !_sinner.HaciendoParry;
                for (int i = 0; i < _delusion.ParrysEsteFrame; i++)
                    RegistrarParry(TipoParry.Delusion, _delusion, automatico);
            }
            else if (resultadoDelusion == ResultadoDelusion.Golpe)
            {
                if (Perder()) return;
            }

            if (!_derrotado && _faseEntrada == FaseEntrada.Jugando)
            {
                foreach (Dupe dupe in _dupes)
                {
                    ResultadoDupe resultado = dupe.Update(gameTime, _sinner, centroAnteriorSinner,
                        _efectoDupe.PuedeRecibirEfecto);
                    if (resultado == ResultadoDupe.Contacto)
                        _efectoDupe.IntentarAplicar();
                    else if (resultado == ResultadoDupe.Parreado)
                        RegistrarParry(TipoParry.Dupe, dupe, !_sinner.HaciendoParry);
                }
                _sinner.ControlesInvertidos = _efectoDupe.Activo;
                RecogerOrbes(centroAnteriorSinner);
                if (!_nivelCompletado)
                {
                    ActualizarShame(gameTime, centroAnteriorSinner);
                }
            }
        }
    }

    private void ActualizarShame(GameTime gameTime, Vector2 centroAnteriorSinner)
    {
        if (!_shame.Activo)
        {
            _relojNivel.Update(gameTime.ElapsedGameTime.TotalSeconds, _sinner.Colision.Centro);
            if (_relojNivel.AvisoPendiente)
            {
                Core.Audio.PlaySoundEffect(_avisoShame, restart: true);
            }
            if (_relojNivel.Terminado)
            {
                Core.Audio.StopSoundEffect(_avisoShame);
                _shame.Aparecer(_enFellDoor ? _centroFellDoor : _relojNivel.PosicionHaceDosSegundos,
                    _enFellDoor ? 3.0 : 1.0);
            }
            return;
        }

        if (_sinner.AutoparryActivo || !_sinner.HaciendoParry || _sinner.ParryIniciadoEsteFrame)
        {
            _shameParreadoEsteParry = false;
        }
        Vector2 posicionAnterior = _shame.Position;
        bool podiaMatar = _shame.PuedeMatar;
        bool estabaRebotando = _shame.Rebotando;
        _shame.Update(gameTime, _sinner.Colision.Centro);
        if (!_shame.PuedeMatar) { return; }
        // Si la espera termino este frame, ignorar el recorrido de Sinner durante la gracia.
        Vector2 inicioSinner = podiaMatar ? centroAnteriorSinner : _sinner.Colision.Centro;
        if (_sinner.PuedeParrear && _sinner.ColisionParry.SeCruzan(inicioSinner,
            _shame.Colision, posicionAnterior))
        {
            if (!_shameParreadoEsteParry && !estabaRebotando)
            {
                bool ultimoInstante = EsParryShameUltimoInstante(inicioSinner, posicionAnterior);
                _shame.Parrear(_sinner.ColisionParry.Centro,
                    _sinner.ColisionParry.Radio, posicionAnterior);
                RegistrarParry(TipoParry.Shame, _shame, !_sinner.HaciendoParry, ultimoInstante);
                _shameParreadoEsteParry = true;
            }
        }
        else if (_sinner.Colision.SeCruzan(inicioSinner, _shame.Colision, posicionAnterior))
        {
            Perder();
        }
    }

    private void RegistrarParry(TipoParry tipo, object enemigo, bool automatico, bool ultimoInstante = false)
    {
        _sinner.ConfirmarParry(automatico);
        Puntaje.RegistrarParry(tipo, enemigo, automatico, ultimoInstante);
    }

    private void AplicarTooManyParries()
    {
        bool activo = _tooManyParries?.Activo == true;
        _sinner.ParryBloqueado = _tooManyParries?.BloqueandoParrys == true;
        _sinner.MultiplicadorVelocidadExterno = (activo ? TooManyParries.MultiplicadorVelocidad : 1f)
            * (_recompensaFellDoor ? 1.2f : 1f);
    }

    private bool EsParryShameUltimoInstante(Vector2 inicioSinner, Vector2 inicioShame)
    {
        // Usa la distancia previa al movimiento: un SHAME rapido no convierte un parry temprano
        // en uno tardio por atravesar todo el margen durante un solo frame.
        float separacion = Vector2.Distance(inicioSinner, inicioShame)
            - _sinner.Colision.Radio - _shame.Colision.Radio;
        // Los ultimos 5 pixeles entre las colisiones, antes del contacto mortal.
        return separacion > 0f && separacion <= 5f;
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
                Puntaje.Sumar("BEACON", 200);
                _temblorContadorRestante = DuracionTemblorContador;
            }
        }
        if (_orbesRecogidos == TotalOrbes)
        {
            _nivelCompletado = true;
            Core.Audio.StopSoundEffect(_musicaNivel);
            Core.Audio.StopSoundEffect(_musicaFellDoor);
            _sinner.DetenerSonidos();
            DetenerSonidosEnemigos();
            _zorak?.DetenerSonidos();
            Core.Audio.PlaySoundEffect(_sonidoSalida, restart: true);
        }
    }

    private bool Perder()
    {
        if (_sinner.DashActivo) return false;
        if (_sinner.AbsorberGolpe()) return false;
        Puntaje.Perder();
        _derrotado = true;
        Core.Audio.StopSong();
        Core.Audio.StopSoundEffect(_musicaNivel);
        Core.Audio.StopSoundEffect(_musicaFellDoor);
        _sinner.DetenerSonidos();
        DetenerSonidosEnemigos();
        _zorak?.DetenerSonidos();
        _pantallaDerrota.Mostrar();
        _volverInicio.Visual.Visible = true;
        _volverInicio.IsEnabled = true;
        _volverInicio.IsFocused = true;
        return true;
    }

    private void VolverAlInicio(object sender, EventArgs args)
    {
        _volverInicio.IsEnabled = false;
        _cambiarEscena(new Inicio(_cambiarEscena));
    }

    public void AbandonarRun()
    {
        Puntaje.Perder();
        _cambiarEscena(new Inicio(_cambiarEscena, animarEntrada: true));
    }

    private void DetenerSonidosEnemigos()
    {
        foreach (Dupe dupe in _dupes) dupe.DetenerSonidos();
        _shame?.DetenerSonidos();
        _erosionBird?.DetenerSonidos();
        Core.Audio.StopSoundEffect(_avisoShame);
        for (int i = 0; i < _dukes.Count; i++)
        {
            _dukes[i].DetenerSonidos();
        }
        for (int i = 0; i < _jarJars.Count; i++)
        {
            _jarJars[i].DetenerSonidos();
        }
    }

    private Rectangle AreaVisible()
    {
        var viewport = Core.GraphicsDevice.Viewport;
        return new Rectangle(
            (int)MathF.Floor(_camara.Posicion.X - viewport.Width * 0.5f),
            (int)MathF.Floor(_camara.Posicion.Y - viewport.Height * 0.5f),
            viewport.Width + 1, viewport.Height + 1);
    }

    private void DibujarExterior()
    {
        Rectangle visible = AreaVisible();
        Rectangle mapa = _limitesExteriores;
        if (visible.Top < mapa.Top)
            DibujarNegro(new Rectangle(visible.Left, visible.Top, visible.Width, mapa.Top - visible.Top));
        if (visible.Bottom > mapa.Bottom)
            DibujarNegro(new Rectangle(visible.Left, mapa.Bottom, visible.Width, visible.Bottom - mapa.Bottom));
        if (visible.Left < mapa.Left)
            DibujarNegro(new Rectangle(visible.Left, visible.Top, mapa.Left - visible.Left, visible.Height));
        if (visible.Right > mapa.Right)
            DibujarNegro(new Rectangle(mapa.Right, visible.Top, visible.Right - mapa.Right, visible.Height));
    }

    private void DibujarNegro(Rectangle region)
    {
        Core.SpriteBatch.Draw(_pixelTransicion, region, Color.Black);
    }

    private void DibujarDegradadoFondo()
    {
        float intensidad = _numeroNivel < 4 ? 0f : _numeroNivel < 8 ? 0.3f : _numeroNivel < 12 ? 0.6f : 1f;
        if (intensidad == 0f) return;

        float latido = (MathF.Sin(_faseLatidoFondo) + 1f) * 0.5f;
        var viewport = Core.GraphicsDevice.Viewport;
        // Incluso en el pico del latido, el fondo oscuro no supera la mitad inferior.
        int altura = Math.Max(1, (int)(viewport.Height * MathHelper.Lerp(0.44f, 0.5f, latido)));
        Color color = _enFellDoor ? new Color(8, 0, 2) : Color.Black;
        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: BlendState.AlphaBlend);
        Core.SpriteBatch.Draw(_degradadoFondo,
            new Rectangle(0, viewport.Height - altura, viewport.Width, altura),
            color * (intensidad * MathHelper.Lerp(0.94f, 1f, latido)));
        Core.SpriteBatch.End();
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

        Core.GraphicsDevice.Clear(_derrotado ? Color.Black : _enFellDoor ? new Color(48, 3, 8) : Color.White);
        if (!_derrotado) DibujarDegradadoFondo();
        Matrix transformacion = _camara.ObtenerTransformacion(Core.GraphicsDevice.Viewport);

        if (!_derrotado)
        {
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointWrap,
                effect: _enFellDoor ? null : _tintaAntigua,
                transformMatrix: transformacion);
            Rectangle visible = AreaVisible();
            foreach (EstructuraSolida estructura in _estructuras)
                if (estructura.Limites.Intersects(visible)) estructura.Draw(Core.SpriteBatch);
            if (_enFellDoor)
                Core.SpriteBatch.Draw(_pixelTransicion, visible, new Color(100, 0, 12) * 0.65f);
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
            DibujarExterior();
            if (_enFellDoor)
            {
                foreach (DemonioFellDoor demonio in _demoniosFellDoor) demonio.Draw(Core.SpriteBatch);
                _shame.Draw(Core.SpriteBatch);
                _sinner.Draw(Core.SpriteBatch);
            }
            else
            {
                DibujarPuertaFellDoor();
                for (int i = 0; i < _jarJars.Count; i++)
                {
                    _jarJars[i].DrawAura(Core.SpriteBatch);
                }
                for (int i = 0; i < _orbes.Count; i++)
                {
                    _orbes[i].Draw(Core.SpriteBatch);
                }
                for (int i = 0; i < _jarJars.Count; i++)
                {
                    _jarJars[i].Draw(Core.SpriteBatch);
                }
                _delusion?.Draw(Core.SpriteBatch);
                _erosionBird?.DrawLanzas(Core.SpriteBatch);
                _shame.Draw(Core.SpriteBatch);
                foreach (Dupe dupe in _dupes) dupe.Draw(Core.SpriteBatch);
                _sinner.Draw(Core.SpriteBatch);
                _delusion?.DrawBolas(Core.SpriteBatch);
                for (int i = 0; i < _dukes.Count; i++)
                {
                    _dukes[i].Draw(Core.SpriteBatch);
                }
            }
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
            DibujarTemporizador();
            _marcador.Draw(Core.GraphicsDevice.Viewport);
            Core.SpriteBatch.End();

            // La capa negra cubre tambien el borde y Zorak queda encima de todo.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            if (!_enFellDoor)
            {
                _erosionBird?.Draw(Core.SpriteBatch);
                _tooManyParries?.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
                _zorak?.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
            }
            Core.SpriteBatch.End();
        }
    }

    private void DibujarBorde()
    {
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        _bordePantalla.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        Core.SpriteBatch.End();
    }

    private void DibujarTemporizador()
    {
        double tiempo = _shame.Activo
            ? _shame.TiempoHastaContacto(_sinner.Colision) : _relojNivel.Restante;
        string texto = RelojNivel.Formatear(tiempo);
        Vector2 posicion = new(
            MathF.Round((Core.GraphicsDevice.Viewport.Width - _fuenteContador.MeasureString(texto).X) * 0.5f),
            24f);
        Color color = _shame.Activo || _relojNivel.Rojo ? Color.Red : Color.White;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    Core.SpriteBatch.DrawString(_fuenteContador, texto,
                        posicion + new Vector2(x, y) * 2f, Color.Black);
                }
            }
        }
        Core.SpriteBatch.DrawString(_fuenteContador, texto, posicion, color);
    }

    private void DibujarContadorOrbes()
    {
        string texto = _enFellDoor ? $"{(int)Math.Ceiling(_supervivenciaFellDoor)}" : $"{_orbesRecogidos}/{TotalOrbes}";
        Vector2 tamano = _fuenteContador.MeasureString(texto);
        Vector2 origen = tamano * 0.5f;
        Vector2 posicion = new(Core.GraphicsDevice.Viewport.Width * 0.5f, 24f + _fuenteContador.LineSpacing + 8f + origen.Y);

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
        if (_enFellDoor || _orbes.Count == 0)
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

        Viewport viewport = Core.GraphicsDevice.Viewport;
        Vector2 origen = Vector2.Transform(centroSinner, transformacion);
        Vector2 objetivoPantalla = Vector2.Transform(objetivo, transformacion);
        Vector2 direccion = objetivoPantalla - origen;
        float distanciaObjetivo = direccion.Length();
        if (distanciaObjetivo < 0.001f)
        {
            return;
        }
        direccion /= distanciaObjetivo;
        Vector2 pivote = new(_indicadorOrbes.Width * 0.5f, _indicadorOrbes.Height * 0.5f);
        // La diagonal deja el icono completo dentro de la pantalla incluso al girar.
        float margen = Math.Min(pivote.Length() + 16f,
            Math.Min(viewport.Width, viewport.Height) * 0.5f - 1f);
        float hastaX = direccion.X == 0f ? float.PositiveInfinity
            : ((direccion.X > 0f ? viewport.Width - margen : margen) - origen.X) / direccion.X;
        float hastaY = direccion.Y == 0f ? float.PositiveInfinity
            : ((direccion.Y > 0f ? viewport.Height - margen : margen) - origen.Y) / direccion.Y;
        float distanciaBorde = Math.Max(0f, Math.Min(hastaX, hastaY));
        // Apuntar antes del orbe: cerca sigue al objetivo, lejos queda limitado al borde.
        float separacionOrbe = Orbe.Radio + pivote.Length() + 8f;
        float distanciaIndicador = MathHelper.Clamp(distanciaObjetivo - separacionOrbe,
            Math.Min(64f, distanciaBorde), distanciaBorde);
        Vector2 posicion = origen + direccion * distanciaIndicador;
        // El dibujo original apunta hacia arriba.
        float rotacion = MathF.Atan2(direccion.Y, direccion.X) + MathHelper.PiOver2;
        Core.SpriteBatch.Draw(_indicadorOrbes,
            new Vector2(MathF.Round(posicion.X), MathF.Round(posicion.Y)),
            null, Color.White, rotacion, pivote, 1f, SpriteEffects.None, 0f);
    }
}
