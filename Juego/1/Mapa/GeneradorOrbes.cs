using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _1.Colisiones;

namespace _1.Mapa;

public static class GeneradorOrbes
{
    private const int Paso = 32;
    // Incluye el cuadro completo del orbe y deja espacio para el cuerpo de Sinner.
    private const float Margen = 40f;
    private const float Separacion = 192f;

    public static List<Vector2> Generar(Rectangle limites, Vector2 inicio,
        IReadOnlyList<ColisionSolida> obstaculos, int cantidad, Random azar)
    {
        var candidatos = new List<Vector2>();
        List<Vector2> accesibles = PosicionesAccesibles(limites, inicio, obstaculos);
        for (int i = 0; i < accesibles.Count; i++)
        {
            if (Vector2.DistanceSquared(inicio, accesibles[i]) >= Separacion * Separacion)
            {
                candidatos.Add(accesibles[i]);
            }
        }

        // Mezclar antes de elegir evita favorecer una zona del recorrido.
        for (int i = candidatos.Count - 1; i > 0; i--)
        {
            int otro = azar.Next(i + 1);
            (candidatos[i], candidatos[otro]) = (candidatos[otro], candidatos[i]);
        }
        var posiciones = new List<Vector2>();
        for (int i = 0; i < candidatos.Count && posiciones.Count < cantidad; i++)
        {
            bool separado = true;
            for (int j = 0; j < posiciones.Count; j++)
            {
                if (Vector2.DistanceSquared(candidatos[i], posiciones[j]) < Separacion * Separacion)
                {
                    separado = false;
                    break;
                }
            }
            if (separado)
            {
                posiciones.Add(candidatos[i]);
            }
        }
        if (posiciones.Count != cantidad)
        {
            throw new InvalidOperationException("No hay suficiente espacio accesible para los orbes.");
        }
        return posiciones;
    }

    public static List<Vector2> PosicionesAccesibles(Rectangle limites, Vector2 inicio,
        IReadOnlyList<ColisionSolida> obstaculos)
    {
        var accesibles = new List<Vector2>();
        var visitados = new HashSet<Point>();
        var pendientes = new Queue<Point>();
        Point[] direcciones = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        pendientes.Enqueue(Point.Zero);
        visitados.Add(Point.Zero);

        // Solo se recorren posiciones conectadas con el inicio mediante pasos sin paredes.
        while (pendientes.Count > 0)
        {
            Point celda = pendientes.Dequeue();
            Vector2 posicion = inicio + new Vector2(celda.X * Paso, celda.Y * Paso);
            accesibles.Add(posicion);

            for (int d = 0; d < direcciones.Length; d++)
            {
                Point siguiente = celda + direcciones[d];
                if (visitados.Contains(siguiente))
                {
                    continue;
                }
                Vector2 movimiento = new(direcciones[d].X * Paso, direcciones[d].Y * Paso);
                Vector2 destino = posicion + movimiento;
                if (destino.X - Margen < limites.Left || destino.X + Margen > limites.Right
                    || destino.Y - Margen < limites.Top || destino.Y + Margen > limites.Bottom)
                {
                    continue;
                }
                bool libre = true;
                var cuerpo = new CirculoColision(posicion, Margen);
                var llegada = new CirculoColision(destino, Margen);
                bool horizontal = movimiento.X != 0f;
                float avance = horizontal ? movimiento.X : movimiento.Y;
                for (int i = 0; i < obstaculos.Count; i++)
                {
                    if (obstaculos[i].Toca(llegada)
                        || obstaculos[i].LimitarEje(cuerpo, avance, horizontal) != avance)
                    {
                        libre = false;
                        break;
                    }
                }
                if (libre)
                {
                    visitados.Add(siguiente);
                    pendientes.Enqueue(siguiente);
                }
            }
        }
        return accesibles;
    }
}
