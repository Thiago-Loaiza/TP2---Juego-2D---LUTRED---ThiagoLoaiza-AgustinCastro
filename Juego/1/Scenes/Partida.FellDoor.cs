using System;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Entities;
using _1.Mapa;
using _1.Mapa.Estructuras;
using _1.Progression;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace _1.Scenes;

public partial class Partida
{
    private Texture2D _texturaPuerta, _texturaDemonio;
    private SoundEffectInstance _musicaFellDoor;
    private float _mezclaMusicaFellDoor;
    private Vector2 _posicionPuerta, _regresoFellDoor, _centroFellDoor;
    private bool _puertaConsumida, _enFellDoor, _recompensaFellDoor;
    private float _quietoPuerta, _animacionPuerta = -1f;
    private float _supervivenciaFellDoor, _spawnDemonio, _recalculoCamino;
    private readonly List<DemonioFellDoor> _demoniosFellDoor = new();
    private readonly List<EstructuraSolida> _estructurasOriginales = new();
    private readonly List<ColisionSolida> _obstaculosOriginales = new();
    private Rectangle _limitesOriginales;
    private readonly Dictionary<Point, Vector2> _celdasFellDoor = new();
    private readonly Dictionary<Point, Point> _caminoFellDoor = new();
    private readonly List<Vector2> _spawnsFellDoor = new();
    private static readonly Point[] VecinosFellDoor = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    private void PrepararFellDoor(Rectangle limites)
    {
        if (_mejoras.Cantidad("FellDoor") == 0) return;
        _texturaPuerta = Content.Load<Texture2D>("Sprites/Upgrades/Efectos/FellDoor/Felldoor-Sheet");
        _texturaDemonio = Content.Load<Texture2D>("Sprites/Upgrades/Efectos/FellDoor/Enemies/Demonio-Sheet");
        _musicaFellDoor = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Misc/Felldoor/Ambience4"),
            volume: 0f, isLooped: true, isMusic: true);
        // La misma busqueda de espacio conectado que utilizan los puntos.
        var candidatos = GeneradorOrbes.Generar(limites, _sinner.Colision.Centro,
            _obstaculos, TotalOrbes + 1, Random.Shared);
        _posicionPuerta = candidatos[0];
        foreach (Vector2 candidato in candidatos)
        {
            bool separado = true;
            foreach (Orbe orbe in _orbes)
                if (Vector2.DistanceSquared(candidato, orbe.Colision.Centro) < 100f * 100f)
                    separado = false;
            if (separado) { _posicionPuerta = candidato; break; }
        }
    }

    private bool ActualizarPuertaFellDoor(GameTime tiempo, Vector2 centroAnterior)
    {
        if (_texturaPuerta == null || _puertaConsumida || _faseEntrada != FaseEntrada.Jugando) return false;
        float segundos = (float)tiempo.ElapsedGameTime.TotalSeconds;
        if (_animacionPuerta >= 0f)
        {
            _animacionPuerta += segundos;
            if (_animacionPuerta >= 1f)
            {
                EntrarFellDoor();
                return true;
            }
            return false;
        }
        bool cerca = Vector2.DistanceSquared(_sinner.Colision.Centro, _posicionPuerta) <= 110f * 110f;
        bool quieto = Vector2.DistanceSquared(centroAnterior, _sinner.Colision.Centro) < 0.01f
            && _sinner.VelocidadActual.LengthSquared() < 1f && !_sinner.DashActivo;
        _quietoPuerta = cerca && quieto ? _quietoPuerta + segundos : 0f;
        if (_quietoPuerta >= 1.5f) _animacionPuerta = 0f;
        return false;
    }

    private void EntrarFellDoor()
    {
        _puertaConsumida = true;
        _regresoFellDoor = _sinner.Position;
        _centroFellDoor = new Vector2(Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height) * 0.5f;
        _estructurasOriginales.AddRange(_estructuras);
        _obstaculosOriginales.AddRange(_obstaculos);
        _limitesOriginales = _limitesExteriores;
        // 48 px por celda: 25% menos que los 64 anteriores; paredes de 128 px (antes 64).
        var generador = new GeneradorMapa(tamanoChunk: _generador.TamanoChunk * 3 / 8,
            chunksPorLado: _generador.ChunksPorLado, separacionChunks: 2);
        var mapa = generador.Generar(_centroFellDoor, Content.Load<Texture2D>("Textures/Granito"),
            CatalogoEstructuras.Crear(), Random.Shared.Next(), tamanoEstructuras: _generador.TamanoChunk);
        _estructuras.Clear();
        _estructuras.AddRange(mapa.Estructuras);
        _obstaculos.Clear();
        _obstaculos.AddRange(mapa.Obstaculos);
        _limitesExteriores = mapa.Limites;
        _limitesExteriores.Inflate(generador.TamanoChunk, generador.TamanoChunk);
        foreach (Vector2 posicion in GeneradorOrbes.PosicionesAccesibles(mapa.Limites, _centroFellDoor, _obstaculos))
        {
            _celdasFellDoor[CeldaFellDoor(posicion)] = posicion;
            if (Vector2.DistanceSquared(posicion, _centroFellDoor) >= 450f * 450f)
                _spawnsFellDoor.Add(posicion);
        }
        DetenerSonidosEnemigos();
        _enFellDoor = true;
        Core.Audio.PlaySoundEffect(_musicaFellDoor);
        _supervivenciaFellDoor = 10f;
        _spawnDemonio = 1.5f;
        _sinner.Teletransportar(_centroFellDoor - new Vector2(0, 15));
        _relojNivel.ReiniciarHistorial(_sinner.Colision.Centro);
        _camara.Seguir(_sinner.Position);
        if (_shame.Activo || _relojNivel.Terminado) _shame.Aparecer(_centroFellDoor, 3.0);
        CrearDemonioFellDoor();
        CrearDemonioFellDoor();
    }

    private Point CeldaFellDoor(Vector2 posicion) => new(
        (int)MathF.Round((posicion.X - _centroFellDoor.X) / 32f),
        (int)MathF.Round((posicion.Y - _centroFellDoor.Y) / 32f));

    private void CrearDemonioFellDoor()
    {
        var candidatos = _spawnsFellDoor.FindAll(posicion =>
            Vector2.DistanceSquared(posicion, _sinner.Colision.Centro) >= 350f * 350f
            && !_demoniosFellDoor.Exists(d => Vector2.DistanceSquared(posicion, d.Position) < 80f * 80f));
        if (candidatos.Count == 0) return;
        _demoniosFellDoor.Add(new DemonioFellDoor(_texturaDemonio, candidatos[Random.Shared.Next(candidatos.Count)]));
    }

    private void ActualizarCaminosFellDoor()
    {
        // Un campo de caminos compartido permite rodear el granito sin atravesar paredes.
        Point origen = CeldaFellDoor(_sinner.Colision.Centro);
        if (!_celdasFellDoor.ContainsKey(origen))
        {
            float menor = float.MaxValue;
            foreach (var celda in _celdasFellDoor)
            {
                float distancia = Vector2.DistanceSquared(celda.Value, _sinner.Colision.Centro);
                if (distancia < menor) { menor = distancia; origen = celda.Key; }
            }
        }
        _caminoFellDoor.Clear();
        var pendientes = new Queue<Point>();
        pendientes.Enqueue(origen);
        _caminoFellDoor[origen] = origen;
        while (pendientes.Count > 0)
        {
            Point actual = pendientes.Dequeue();
            foreach (Point vecino in VecinosFellDoor)
            {
                Point siguiente = actual + vecino;
                if (!_celdasFellDoor.ContainsKey(siguiente) || _caminoFellDoor.ContainsKey(siguiente)) continue;
                // Las celdas accesibles ya reservan 40 px de margen para un paso de 32 px.
                _caminoFellDoor[siguiente] = actual;
                pendientes.Enqueue(siguiente);
            }
        }
    }

    private void ActualizarDimensionFellDoor(GameTime tiempo)
    {
        float segundos = (float)tiempo.ElapsedGameTime.TotalSeconds;
        Vector2 anterior = _sinner.Colision.Centro;
        _sinner.ParryBloqueado = false;
        _sinner.MultiplicadorVelocidadExterno = _recompensaFellDoor ? 1.2f : 1f;
        _sinner.Update(tiempo, _obstaculos);
        _camara.Seguir(_sinner.Position);
        ActualizarShame(tiempo, anterior);
        if (_derrotado) return;
        _supervivenciaFellDoor = Math.Max(0, _supervivenciaFellDoor - segundos);
        _spawnDemonio -= segundos;
        while (_spawnDemonio <= 0) { CrearDemonioFellDoor(); _spawnDemonio += 1.5f; }
        _recalculoCamino -= segundos;
        if (_recalculoCamino <= 0) { ActualizarCaminosFellDoor(); _recalculoCamino = 0.3f; }
        for (int i = _demoniosFellDoor.Count - 1; i >= 0; i--)
        {
            DemonioFellDoor demonio = _demoniosFellDoor[i];
            Vector2 antes = demonio.Position;
            Vector2 objetivo = _sinner.Colision.Centro;
            Point celda = CeldaFellDoor(antes);
            if (Vector2.DistanceSquared(antes, objetivo) > 64f * 64f
                && _caminoFellDoor.TryGetValue(celda, out Point paso) && paso != celda)
                objetivo = _celdasFellDoor[paso];
            demonio.Update(segundos, objetivo, Sinner.VelocidadCaminata, _obstaculos);
            if (_sinner.PuedeParrear && _sinner.ColisionParry.SeCruzan(anterior, demonio.Colision, antes))
            {
                RegistrarParry(TipoParry.Demonio, demonio, !_sinner.HaciendoParry);
                _demoniosFellDoor.RemoveAt(i);
            }
            else if (_sinner.Colision.SeCruzan(anterior, demonio.Colision, antes) && Perder()) return;
        }
        if (_supervivenciaFellDoor <= 0) SalirFellDoor();
    }

    private void SalirFellDoor()
    {
        Vector2 desplazamiento = _regresoFellDoor + new Vector2(0, 15) - _centroFellDoor;
        if (_shame.Activo) _shame.Trasladar(desplazamiento);
        _estructuras.Clear();
        _estructuras.AddRange(_estructurasOriginales);
        _obstaculos.Clear();
        _obstaculos.AddRange(_obstaculosOriginales);
        _limitesExteriores = _limitesOriginales;
        _estructurasOriginales.Clear();
        _obstaculosOriginales.Clear();
        _demoniosFellDoor.Clear();
        _celdasFellDoor.Clear();
        _caminoFellDoor.Clear();
        _spawnsFellDoor.Clear();
        _sinner.Teletransportar(_regresoFellDoor);
        _relojNivel.ReiniciarHistorial(_sinner.Colision.Centro);
        _camara.Seguir(_sinner.Position);
        _enFellDoor = false;
        _recompensaFellDoor = true;
        _sinner.AuraFellDoorActiva = true;
        AplicarTooManyParries();
        int cantidad = (_orbes.Count + 1) / 2;
        for (int i = 0; i < cantidad; i++)
        {
            _orbes.RemoveAt(_orbes.Count - 1);
            _orbesRecogidos++;
            Puntaje.Sumar("BEACON", 200);
        }
        _temblorContadorRestante = DuracionTemblorContador;
        RecogerOrbes(_sinner.Colision.Centro);
    }

    private void DibujarPuertaFellDoor()
    {
        if (_texturaPuerta == null || _puertaConsumida) return;
        int ancho = _texturaPuerta.Width / 30;
        int cuadro = _animacionPuerta < 0 ? 0 : Math.Min(29, (int)(_animacionPuerta * 30));
        Core.SpriteBatch.Draw(_texturaPuerta, _posicionPuerta - new Vector2(0, 48),
            new Rectangle(cuadro * ancho, 0, ancho, _texturaPuerta.Height), Color.White,
            0f, new Vector2(ancho, _texturaPuerta.Height) * 0.5f, 0.6f, SpriteEffects.None, 0f);
    }

    private void ActualizarMusicaFellDoor(GameTime tiempo)
    {
        if (_musicaFellDoor == null) return;
        float paso = (float)tiempo.ElapsedGameTime.TotalSeconds / 0.8f;
        _mezclaMusicaFellDoor = _enFellDoor
            ? Math.Min(1f, _mezclaMusicaFellDoor + paso)
            : Math.Max(0f, _mezclaMusicaFellDoor - paso);
        Core.Audio.SetVolume(_musicaNivel, 1f - _mezclaMusicaFellDoor);
        Core.Audio.SetVolume(_musicaFellDoor, _mezclaMusicaFellDoor);
        // El layer sigue avanzando silenciado para regresar al mismo momento musical.
        if (!_enFellDoor && _mezclaMusicaFellDoor == 0f)
            Core.Audio.StopSoundEffect(_musicaFellDoor);
    }
}
