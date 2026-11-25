using System;

namespace _1.Progression;

public sealed class EstadoExpansion
{
    public const int ChunksIniciales = 41;
    public const int ExpansionMaxima = 50;
    public const int OrbesIniciales = 10;
    public const int OrbesMaximos = 25;
    private int _tramosAplicados;
    public int ObjetivoOrbes { get; private set; } = OrbesIniciales;
    public int ChunksPorLado => ChunksIniciales + Math.Min(ExpansionMaxima, _tramosAplicados * 2);

    public void AvanzarHasta(int nivel)
    {
        int tramos = Math.Min(ExpansionMaxima / 2, nivel / 4);
        while (_tramosAplicados < tramos)
        {
            _tramosAplicados++;
            if (ObjetivoOrbes < OrbesMaximos)
                ObjetivoOrbes = Math.Min(OrbesMaximos, ObjetivoOrbes + Random.Shared.Next(2, 4));
        }
    }
}
