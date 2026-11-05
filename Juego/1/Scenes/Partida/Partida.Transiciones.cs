using System;
using LutredLibrary;
using Microsoft.Xna.Framework.Audio;

namespace _1.Scenes;

public partial class Partida
{
    private const int CantidadLayers = 8;
    private const int CantidadTransiciones = 9;
    private readonly bool _transicionReproducida;
    private bool _nivelCompletado;
    private enum FaseEntrada { Transicion, Aparicion, Jugando }
    private bool _derrotado;
    private FaseEntrada _faseEntrada;
    private SoundEffectInstance _sonidoEntrada;
    private SoundEffectInstance _sonidoSalida;
    private SoundEffectInstance _musicaNivel;
    private float _opacidadBlanca = 1f;

    private void IniciarEntradaNivel()
    {
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

    private void ActualizarSalidaNivel()
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
    }

    private void CompletarNivel()
    {
        _nivelCompletado = true;
        Core.Audio.StopSoundEffect(_musicaNivel);
        Core.Audio.StopSoundEffect(_musicaFellDoor);
        _sinner.DetenerSonidos();
        DetenerSonidosEnemigos();
        _zorak?.DetenerSonidos();
        Core.Audio.PlaySoundEffect(_sonidoSalida, restart: true);
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
}
