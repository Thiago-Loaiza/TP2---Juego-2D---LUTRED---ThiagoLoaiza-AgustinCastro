using System;

namespace _1.Progression;

// Las restricciones pertenecen a la run y no a una pantalla de seleccion.
public sealed class EstadoModificadores
{
    private int _eleccionesHastaMenosTiempo = Random.Shared.Next(2, 4);
    private int _aparicionesLoMismo;
    private bool _loMismoEnEleccionAnterior;
    public bool MenosTiempoObligatorio => _eleccionesHastaMenosTiempo == 1;
    public bool LoMismoDisponible => _aparicionesLoMismo < 3 && !_loMismoEnEleccionAnterior;

    public void RegistrarEleccion(bool mostroLoMismo)
    {
        if (mostroLoMismo) _aparicionesLoMismo++;
        _loMismoEnEleccionAnterior = mostroLoMismo;
        _eleccionesHastaMenosTiempo--;
        if (_eleccionesHastaMenosTiempo <= 0)
            _eleccionesHastaMenosTiempo = Random.Shared.Next(2, 4);
    }
}
