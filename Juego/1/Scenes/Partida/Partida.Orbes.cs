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
    private int TotalOrbes => _mejoras.OrbesNivel(_numeroNivel);
    private readonly List<Orbe> _orbes = new();
    private Animation _animacionOrbe;
    private int _orbesRecogidos;
    private readonly List<float> _impulsosOrbes = new();
    private bool OrbesEfectivos => _mejoras.Cantidad("OrbesEfectivos") > 0;
    private float MultiplicadorOrbes
    {
        get
        {
            float extra = 0f;
            foreach (float restante in _impulsosOrbes) extra += restante * 0.04f;
            return 1f + Math.Min(0.6f, extra);
        }
    }

    private void ActualizarImpulsosOrbes(float segundos)
    {
        for (int i = _impulsosOrbes.Count - 1; i >= 0; i--)
        {
            _impulsosOrbes[i] -= segundos;
            if (_impulsosOrbes[i] <= 0f) _impulsosOrbes.RemoveAt(i);
        }
    }
    private bool PocosOrbes => _orbes.Count > 0 && _orbes.Count * 4 <= TotalOrbes;

    private void GenerarOrbes()
    {
        List<Vector2> posicionesOrbes = GeneradorOrbes.Generar(_posicionesLibres,
            _sinner.Colision.Centro, TotalOrbes, Random.Shared);
        _orbes.Clear();
        for (int i = 0; i < posicionesOrbes.Count; i++)
        {
            _orbes.Add(new Orbe(_animacionOrbe, posicionesOrbes[i], OrbesEfectivos ? 1.2f : 1f));
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
                if (OrbesEfectivos) _impulsosOrbes.Add(5f);
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
