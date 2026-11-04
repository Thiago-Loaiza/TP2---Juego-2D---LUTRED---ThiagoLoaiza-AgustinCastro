using System;

namespace _1.Progression;

public sealed class EstadoDupe
{
    public double OcultoRestante { get; private set; }
    public double ParreadoRestante { get; private set; }
    public bool PuedeTocar => OcultoRestante <= 0 && ParreadoRestante <= 0;

    public void Actualizar(double segundos)
    {
        OcultoRestante = Math.Max(0, OcultoRestante - segundos);
        ParreadoRestante = Math.Max(0, ParreadoRestante - segundos);
    }

    public void Parrear() => ParreadoRestante = 3;
    public void Desaparecer() => OcultoRestante = 15;
}
