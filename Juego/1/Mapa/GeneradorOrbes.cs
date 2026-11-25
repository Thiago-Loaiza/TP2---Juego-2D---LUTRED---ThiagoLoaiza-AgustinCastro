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
    private const int TamanoSector = 128;

    private static Point Sector(Vector2 posicion) => new(
        (int)MathF.Floor(posicion.X / TamanoSector), (int)MathF.Floor(posicion.Y / TamanoSector));

    private static Dictionary<Point, List<ColisionSolida>> IndexarObstaculos(IReadOnlyList<ColisionSolida> obstaculos)
    {
        var sectores = new Dictionary<Point, List<ColisionSolida>>();
        foreach (ColisionSolida obstaculo in obstaculos)
        {
            Rectangle region = obstaculo.Limites;
            int margen = (int)MathF.Ceiling(Margen) + Paso;
            region.Inflate(margen, margen);
            Point primero = Sector(new Vector2(region.Left, region.Top));
            Point ultimo = Sector(new Vector2(region.Right, region.Bottom));
            for (int y = primero.Y; y <= ultimo.Y; y++)
                for (int x = primero.X; x <= ultimo.X; x++)
                {
                    Point clave = new(x, y);
                    if (!sectores.TryGetValue(clave, out var lista)) sectores[clave] = lista = new();
                    lista.Add(obstaculo);
                }
        }
        return sectores;
    }

    public static List<Vector2> Generar(Rectangle limites, Vector2 inicio,
        IReadOnlyList<ColisionSolida> obstaculos, int cantidad, Random azar)
        => Generar(PosicionesAccesibles(limites, inicio, obstaculos), inicio, cantidad, azar);

    public static List<Vector2> Generar(IReadOnlyList<Vector2> accesibles, Vector2 inicio, int cantidad, Random azar)
    {
        var candidatos = new List<Vector2>();
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
        var sectores = IndexarObstaculos(obstaculos);
        Point[] direcciones = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        pendientes.Enqueue(Point.Zero);
        visitados.Add(Point.Zero);

        // Solo se recorren posiciones conectadas con el inicio mediante pasos sin paredes.
        while (pendientes.Count > 0)
        {
            Point celda = pendientes.Dequeue();
            Vector2 posicion = inicio + new Vector2(celda.X * Paso, celda.Y * Paso);
            sectores.TryGetValue(Sector(posicion), out var cercanos);
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
                for (int i = 0; cercanos != null && i < cercanos.Count; i++)
                {
                    if (cercanos[i].Toca(llegada)
                        || cercanos[i].LimitarEje(cuerpo, avance, horizontal) != avance)
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
