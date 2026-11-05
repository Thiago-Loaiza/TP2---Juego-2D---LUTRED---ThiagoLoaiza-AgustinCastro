using System;
using _1.Entities;
using _1.Progression;
using _1.UI;
using LutredLibrary;
using LutredLibrary.Graphics;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace _1.Scenes;

public partial class Partida : Escena
{
    public void SincronizarEntrada() => _sinner?.SincronizarEntrada();
    private readonly Action<Escena> _cambiarEscena;
    private readonly EstadoMejoras _mejoras;
    private EstadoPuntaje Puntaje => _mejoras.Puntaje;
    private readonly int _numeroNivel;
    private readonly double _duracionNivel;
    private Sinner _sinner;
    private readonly Camara2D _camara = new();

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

    public override void Initialize()
    {
        base.Initialize();
        _sinner.Position = new Vector2(
            Core.GraphicsDevice.Viewport.Width,
            Core.GraphicsDevice.Viewport.Height
        ) * 0.5f;
        _relojNivel = new RelojNivel(_sinner.Colision.Centro, _duracionNivel);
        _camara.Seguir(_sinner.Position);
        UbicarEnemigosIniciales();
        CrearInterfazDerrota();
        GenerarMapa();
        IniciarEntradaNivel();
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Audio.IsPaused) { return; }
        if (!_derrotado && !_nivelCompletado) ActualizarMusicaFellDoor(gameTime);
        _bordePantalla.Update(gameTime);
        if (_nivelCompletado)
        {
            ActualizarSalidaNivel();
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
            _tiempoDesdeDerrota += gameTime.ElapsedGameTime.TotalSeconds;
            if (_tiempoDesdeDerrota >= EsperaBotonDerrota)
            {
                if (!_volverInicio.IsEnabled)
                {
                    _volverInicio.Visual.Visible = true;
                    _volverInicio.IsEnabled = true;
                    _volverInicio.IsFocused = true;
                }
                _volverInicio.Update(gameTime);
            }
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
            ActualizarEnemigos(gameTime, centroAnteriorSinner);
        }
    }

    public override void Draw(GameTime gameTime) => DibujarPartida(gameTime);
}
