using System;

namespace _1.Progression;

// Diez segundos de efecto y dos de inmunidad; otro contacto nunca lo prolonga.
public sealed class EstadoEfectoDupe
{
    private double _proteccionRestante;
    public bool Activo => _proteccionRestante > 2;
    public bool PuedeRecibirEfecto => _proteccionRestante <= 0;

    public void Actualizar(double segundos)
    {
        _proteccionRestante = Math.Max(0, _proteccionRestante - segundos);
    }

    public bool IntentarAplicar()
    {
        if (!PuedeRecibirEfecto) return false;
        _proteccionRestante = 12;
        return true;
    }
}
