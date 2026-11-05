using System;
using System.Collections.Generic;
using _1.Entities;
using _1.Progression;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace _1.Scenes;

public partial class Partida
{
    private readonly int _cantidadDuke;
    private readonly int _cantidadJarJar;
    private readonly int _cantidadDupe;
    private readonly List<Dupe> _dupes = new();
    private readonly EstadoEfectoDupe _efectoDupe = new();
    private readonly bool _sorakElegido;
    private readonly bool _erosionBirdElegido;
    private readonly bool _delusionElegido;
    private readonly bool _tooManyParriesElegido;
    private Shame _shame;
    private SoundEffectInstance _avisoShame;
    private bool _shameParreadoEsteParry;
    private readonly List<Duck> _dukes = new();
    private readonly List<JarJar> _jarJars = new();
    private Zorak _zorak;
    private ErosionBird _erosionBird;
    private Delusion _delusion;
    private TooManyParries _tooManyParries;
    private int _tandaLanzas;

    private void UbicarEnemigosIniciales()
    {
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
    }

    private void ActualizarEnemigos(GameTime gameTime, Vector2 centroAnteriorSinner)
    {
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
}
