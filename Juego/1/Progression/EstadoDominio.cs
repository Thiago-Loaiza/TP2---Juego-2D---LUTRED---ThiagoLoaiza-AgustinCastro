using System;

namespace _1.Progression;

// Estado propio de Dominio, independiente de la animacion y de los impactos automaticos.
public sealed class EstadoDominio
{
    public int Cadena { get; private set; }
    public double TiempoActivo { get; private set; }
    public double RecargaRestante { get; private set; }
    public bool Activo => TiempoActivo > 0;
    private bool _manualEnCurso;
    private bool _manualExitoso;
    private bool _ignorarIntento;

    public void Actualizar(double segundos)
    {
        if (!double.IsFinite(segundos) || segundos < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segundos));
        }
        TiempoActivo = Math.Max(0, TiempoActivo - segundos);
        RecargaRestante = Math.Max(0, RecargaRestante - segundos);
    }

    public void ComenzarParryManual()
    {
        FinalizarParryManual();
        _manualEnCurso = true;
        _manualExitoso = false;
        _ignorarIntento = Activo || RecargaRestante > 0;
    }

    public void FinalizarParryManual()
    {
        if (_manualEnCurso && !_manualExitoso && !_ignorarIntento)
        {
            Cadena = 0;
        }
        _manualEnCurso = false;
    }

    public void RegistrarParryManualExitoso(int nivel)
    {
        if (!_manualEnCurso || _manualExitoso)
        {
            return;
        }
        _manualExitoso = true;
        if (nivel <= 0 || _ignorarIntento || Activo || RecargaRestante > 0)
        {
            return;
        }
        Cadena++;
        if (Cadena >= (nivel >= 2 ? 2 : 3))
        {
            Cadena = 0;
            TiempoActivo = nivel >= 2 ? 10.0 : 5.0;
            RecargaRestante = 15.0;
        }
    }
}
