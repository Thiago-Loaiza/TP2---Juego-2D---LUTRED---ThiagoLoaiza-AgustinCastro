using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace _1.UI;

public sealed class RelojNivel
{
    public const double Duracion = 60;
    private readonly Queue<(double Tiempo, Vector2 Posicion)> _historial = new();
    private (double Tiempo, Vector2 Posicion) _anterior;
    private double _transcurrido;
    private readonly double _duracion;
    private double _faseParpadeo;
    public double Restante => Math.Max(0, _duracion - _transcurrido);
    public bool Terminado => Restante <= 0;
    public bool AvisoPendiente { get; private set; }
    public bool Rojo => Restante <= 8 && _faseParpadeo % 1.0 < 0.5;
    public Vector2 PosicionHaceDosSegundos { get; private set; }

    public RelojNivel(Vector2 posicionInicial, double duracion = Duracion)
    {
        if (!double.IsFinite(duracion) || duracion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duracion));
        }
        _duracion = duracion;
        _anterior = (0, posicionInicial);
        PosicionHaceDosSegundos = posicionInicial;
    }

    public void Update(double segundos, Vector2 posicion)
    {
        double restanteAnterior = Restante;
        _transcurrido += segundos;
        AvisoPendiente = (restanteAnterior > 8 || _transcurrido == segundos) && Restante <= 8;
        // El primer destello dura aproximadamente un segundo; cerca de cero llega a 12 Hz.
        if (Restante <= 8)
        {
            double progreso = 1.0 - Restante / 8.0;
            _faseParpadeo += segundos * (0.5 + 11.5 * progreso * progreso * progreso);
        }
        _historial.Enqueue((_transcurrido, posicion));
        double objetivo = _transcurrido - 2.0;
        while (_historial.Count > 0 && _historial.Peek().Tiempo <= objetivo)
        {
            _anterior = _historial.Dequeue();
        }
        PosicionHaceDosSegundos = _anterior.Posicion;
        if (objetivo > _anterior.Tiempo && _historial.Count > 0)
        {
            var siguiente = _historial.Peek();
            float mezcla = (float)((objetivo - _anterior.Tiempo) / (siguiente.Tiempo - _anterior.Tiempo));
            PosicionHaceDosSegundos = Vector2.Lerp(_anterior.Posicion, siguiente.Posicion, mezcla);
        }
    }

    public static string Formatear(double segundos)
    {
        int centesimas = (int)Math.Floor(Math.Clamp(segundos, 0, 5999.99) * 100 + 0.000001);
        return $"{centesimas / 6000}:{centesimas / 100 % 60:00}.{centesimas % 100:00}";
    }
}
