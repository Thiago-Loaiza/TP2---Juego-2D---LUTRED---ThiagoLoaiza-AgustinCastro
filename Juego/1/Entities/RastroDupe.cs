using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public readonly record struct PasoDupe(Vector2 Posicion, SinnerAnimation Animacion, SpriteEffects Orientacion);

// Cada copia conserva el mismo recorrido con su propio retraso, aun estando oculta.
public sealed class RastroDupe
{
    private readonly Queue<(double Tiempo, PasoDupe Paso)> _pasos = new();
    private (double Tiempo, PasoDupe Paso) _anterior;
    private double _tiempo;
    public double Retraso { get; }
    public bool Preparado => _tiempo >= Retraso;

    public RastroDupe(double retraso, PasoDupe inicial)
    {
        if (!double.IsFinite(retraso) || retraso <= 0) throw new ArgumentOutOfRangeException(nameof(retraso));
        Retraso = retraso;
        _anterior = (0, inicial);
    }

    public PasoDupe Actualizar(double segundos, PasoDupe actual)
    {
        _tiempo += segundos;
        _pasos.Enqueue((_tiempo, actual));
        double objetivo = _tiempo - Retraso;
        while (_pasos.Count > 0 && _pasos.Peek().Tiempo <= objetivo)
            _anterior = _pasos.Dequeue();
        if (_pasos.Count == 0 || objetivo <= _anterior.Tiempo) return _anterior.Paso;
        var siguiente = _pasos.Peek();
        float mezcla = (float)((objetivo - _anterior.Tiempo) / (siguiente.Tiempo - _anterior.Tiempo));
        return _anterior.Paso with { Posicion = Vector2.Lerp(_anterior.Paso.Posicion, siguiente.Paso.Posicion, mezcla) };
    }
}
