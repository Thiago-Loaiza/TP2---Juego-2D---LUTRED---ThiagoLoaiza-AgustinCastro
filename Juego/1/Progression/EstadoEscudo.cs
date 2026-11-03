using System;

namespace _1.Progression;

// Se crea por piso: la mejora persiste, pero la carga y la gracia se renuevan.
public sealed class EstadoEscudo
{
    public bool Disponible { get; private set; }
    public double InvulnerabilidadRestante { get; private set; }
    public bool Invulnerable => InvulnerabilidadRestante > 0;

    public EstadoEscudo(bool adquirido) => Disponible = adquirido;

    public void Actualizar(double segundos)
    {
        InvulnerabilidadRestante = Math.Max(0, InvulnerabilidadRestante - segundos);
    }

    public bool AbsorberGolpe()
    {
        if (Invulnerable) return true;
        if (!Disponible) return false;
        Disponible = false;
        InvulnerabilidadRestante = 2;
        return true;
    }
}
