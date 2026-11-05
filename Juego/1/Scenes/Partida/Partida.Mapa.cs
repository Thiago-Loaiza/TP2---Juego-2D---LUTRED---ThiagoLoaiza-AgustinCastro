using System;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Mapa;
using _1.Mapa.Estructuras;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Scenes;

public partial class Partida
{
    private readonly GeneradorMapa _generador = new();
    private readonly List<EstructuraSolida> _estructuras = new();
    private readonly List<ColisionSolida> _obstaculos = new();
    private List<Vector2> _posicionesLibres = new();
    private Rectangle _limitesExteriores;

    private void GenerarMapa()
    {
        Texture2D ladrillos = Content.Load<Texture2D>("Textures/LadrillosTextura");
        MapaGenerado mapa = _generador.Generar(_sinner.Position, ladrillos,
            CatalogoEstructuras.Crear(), Random.Shared.Next());
        _limitesExteriores = mapa.Limites;
        _limitesExteriores.Inflate(_generador.TamanoChunk, _generador.TamanoChunk);
        _estructuras.Clear();
        _obstaculos.Clear();
        _estructuras.AddRange(mapa.Estructuras);
        _obstaculos.AddRange(mapa.Obstaculos);
        GenerarOrbes(mapa.Limites);
        // JarJar solo se teletransporta a lugares sin paredes y conectados con el inicio.
        _posicionesLibres = GeneradorOrbes.PosicionesAccesibles(mapa.Limites,
            _sinner.Colision.Centro, _obstaculos);
        _delusion?.Ubicar(_sinner.Colision.Centro, _posicionesLibres);
        PrepararFellDoor(mapa.Limites);
        _tintaAntigua.Parameters["TamanoTexel"].SetValue(
            new Vector2(1f / ladrillos.Width, 1f / ladrillos.Height));
    }

    private Rectangle AreaVisible()
    {
        var viewport = Core.GraphicsDevice.Viewport;
        return new Rectangle(
            (int)MathF.Floor(_camara.Posicion.X - viewport.Width * 0.5f),
            (int)MathF.Floor(_camara.Posicion.Y - viewport.Height * 0.5f),
            viewport.Width + 1, viewport.Height + 1);
    }
}
