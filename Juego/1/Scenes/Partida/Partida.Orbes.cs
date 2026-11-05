using System;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Entities;
using _1.Mapa;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;

namespace _1.Scenes;

public partial class Partida
{
    private const int TotalOrbes = 10;
    private readonly List<Orbe> _orbes = new();
    private Animation _animacionOrbe;
    private int _orbesRecogidos;
    private bool PocosOrbes => _orbes.Count > 0 && _orbes.Count * 4 <= TotalOrbes;

    private void GenerarOrbes(Rectangle limites)
    {
        List<Vector2> posicionesOrbes = GeneradorOrbes.Generar(limites,
            _sinner.Colision.Centro, _obstaculos, TotalOrbes, Random.Shared);
        _orbes.Clear();
        for (int i = 0; i < posicionesOrbes.Count; i++)
        {
            _orbes.Add(new Orbe(_animacionOrbe, posicionesOrbes[i]));
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
                Puntaje.Sumar("BEACON", 200);
                _temblorContadorRestante = DuracionTemblorContador;
            }
        }
        if (_orbesRecogidos == TotalOrbes)
        {
            CompletarNivel();
        }
    }
}
