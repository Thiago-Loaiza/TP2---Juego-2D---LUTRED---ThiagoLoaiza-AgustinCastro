using System;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Mapa;
using _1.Mapa.Decoraciones;
using _1.Mapa.Estructuras;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Scenes;

public partial class Partida
{
    private readonly GeneradorMapa _generador;
    private IReadOnlyList<PlantillaEstructura> _catalogoEstructuras;
    private DecoracionesMapa _decoraciones;
    private readonly List<EstructuraSolida> _estructuras = new();
    private readonly List<ColisionSolida> _obstaculos = new();
    private List<Vector2> _posicionesLibres = new();
    private Rectangle _limitesExteriores;

    private void GenerarMapa()
    {
        Texture2D ladrillos = Content.Load<Texture2D>("Textures/LadrillosTextura");
        _catalogoEstructuras = CatalogoEstructuras.Crear(Content);
        int lado = _generador.ChunksPorLado;
        int baseLado = _1.Progression.EstadoExpansion.ChunksIniciales;
        int intentos = 1500 * lado * lado / (baseLado * baseLado);
        MapaGenerado mapa = _generador.Generar(_sinner.Position, ladrillos,
            _catalogoEstructuras, Random.Shared.Next(), intentos: intentos);
        _limitesExteriores = mapa.Limites;
        _limitesExteriores.Inflate(_generador.TamanoChunk, _generador.TamanoChunk);
        _estructuras.Clear();
        _obstaculos.Clear();
        _estructuras.AddRange(mapa.Estructuras);
        _obstaculos.AddRange(mapa.Obstaculos);
        _decoraciones.Generar(mapa);
        // JarJar solo se teletransporta a lugares sin paredes y conectados con el inicio.
        _posicionesLibres = GeneradorOrbes.PosicionesAccesibles(mapa.Limites,
            _sinner.Colision.Centro, _obstaculos);
        GenerarOrbes();
        _delusion?.Ubicar(_sinner.Colision.Centro, _posicionesLibres);
        PrepararFellDoor(mapa.Limites);
        _tintaAntigua.Parameters["TamanoTexel"].SetValue(
            new Vector2(1f / ladrillos.Width, 1f / ladrillos.Height));
    }

    private Rectangle AreaVisible()
    {
        var viewport = Game1.VistaPartida;
        return new Rectangle(
            (int)MathF.Floor(_camara.Posicion.X - viewport.Width * 0.5f),
            (int)MathF.Floor(_camara.Posicion.Y - viewport.Height * 0.5f),
            viewport.Width + 1, viewport.Height + 1);
    }
}
