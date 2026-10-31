using System;

namespace _1.Progression;

// Estado propio de Dominio, independiente de la animacion y de los impactos automaticos.
public sealed class EstadoDominio
{
    public int Cadena { get; private set; }
    public double TiempoActivo { get; private set; }
    public double RecargaRestante { get; private set; }
    public bool Activo => TiempoActivo > 0;
    public float OpacidadAura
    {
        get
        {
            if (!Activo) return 0f;
            if (TiempoActivo > 2.0) return 1f;
            double transcurrido = 2.0 - TiempoActivo;
            // El parpadeo se acelera al agotarse; el autoparry sigue funcionando durante el aviso.
            double fase = (2.0 * transcurrido + transcurrido * transcurrido) * Math.PI * 2.0;
            return (float)(0.2 + 0.8 * (0.5 + 0.5 * Math.Cos(fase)));
        }
    }
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
            TiempoActivo = nivel >= 2 ? 12.0 : 5.0;
            RecargaRestante = 15.0;
        }
    }
}
