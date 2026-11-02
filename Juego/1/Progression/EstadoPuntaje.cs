using System;
using System.Collections.Generic;
using System.Globalization;

namespace _1.Progression;

public enum TipoParry { Duck, Zorak, JarJar, Lanza, Shame, Delusion }

public sealed record DefinicionRango(string Nombre, string Audio, double Multiplicador, double Umbral);

public sealed class PremioPuntaje
{
    public string Nombre { get; init; }
    public int Puntos { get; init; }
    internal int Base { get; init; }
    internal int Credito { get; init; }
    internal int CreditoPiso { get; init; }
}

// Estado de la partida completa: no depende del dibujo, audio ni tiempo del menu.
public sealed class EstadoPuntaje
{
    public const int PuntajeMaximo = 99_999_999;
    public const double GraciaDecaimiento = 2;
    private const double MaximoEstilo = 14_000;
    public static IReadOnlyList<DefinicionRango> Rangos { get; } = Array.AsReadOnly(new[]
    {
        new DefinicionRango("DEFICIENTE", "Deficiente", 1, 0),
        new DefinicionRango("CAPAZ", "Capaz", 1, 300),
        new DefinicionRango("BUENO", "Bueno", 1.25, 800),
        new DefinicionRango("ASOMBROSO", "Asombroso", 1.5, 1500),
        new DefinicionRango("SUPERIOR", "Superior", 2, 2500),
        new DefinicionRango("SANTO SUPERIOR", "SantoSuperior", 2.5, 4000),
        new DefinicionRango("SÚPER SANTO SUPERIOR", "SuperSantoSuperior", 3, 6000),
        new DefinicionRango("PERFECTO", "Perfecto", 3.33, 8500),
        new DefinicionRango("LUTRED", "LUTRED", 5, 11500)
    });

    private readonly List<PremioPuntaje> _acciones = new();
    private readonly Queue<double> _parrys = new();
    private readonly List<PremioPuntaje> _premiosLanzas = new();
    private readonly Dictionary<object, Queue<double>> _ducks = new();
    private readonly List<(object Enemigo, double Tiempo, PremioPuntaje Premio)> _jarJars = new();
    private double _reloj;
    private double _ultimaAccion;
    public int Total { get; private set; }
    public int PuntosDelPiso { get; private set; }
    public double Estilo { get; private set; }
    public int Rango { get; private set; } = -1;
    public double Multiplicador => Rango < 0 ? 1 : Rangos[Rango].Multiplicador;
    public string TextoTotal => Total.ToString("D8", CultureInfo.InvariantCulture);
    public string TextoPiso => PuntosDelPiso.ToString("D8", CultureInfo.InvariantCulture);
    public IReadOnlyList<PremioPuntaje> Acciones => _acciones;
    public event Action<int> RangoSubido;
    public float Progreso
    {
        get
        {
            if (Rango < 0) return 0;
            double minimo = Rangos[Rango].Umbral;
            double maximo = Rango + 1 < Rangos.Count ? Rangos[Rango + 1].Umbral : MaximoEstilo;
            return (float)Math.Clamp((Estilo - minimo) / (maximo - minimo), 0, 1);
        }
    }

    public void ComenzarNivel()
    {
        PuntosDelPiso = 0;
        Estilo = 0;
        Rango = -1;
        _acciones.Clear();
        _parrys.Clear();
        _ducks.Clear();
        _jarJars.Clear();
        ComenzarTandaLanzas();
        _ultimaAccion = _reloj;
    }

    public void Actualizar(double segundos)
    {
        if (!double.IsFinite(segundos) || segundos < 0) throw new ArgumentOutOfRangeException(nameof(segundos));
        double anterior = _reloj;
        _reloj += segundos;
        double decaimiento = Math.Max(0, _reloj - Math.Max(anterior, _ultimaAccion + GraciaDecaimiento));
        // Decaimiento exponencial con una base de 60 puntos/s; tambien llega a cero.
        Estilo = Math.Max(0, (Estilo + 500) * Math.Exp(-0.12 * decaimiento) - 500);
        ActualizarRango();
        _jarJars.RemoveAll(j => _reloj - j.Tiempo >= 1);
    }

    public PremioPuntaje Sumar(string nombre, int puntosBase)
    {
        if (puntosBase <= 0) throw new ArgumentOutOfRangeException(nameof(puntosBase));
        int puntos = (int)Math.Min(PuntajeMaximo, Math.Round(puntosBase * Multiplicador, MidpointRounding.AwayFromZero));
        int credito = Math.Min(puntos, PuntajeMaximo - Total);
        int creditoPiso = Math.Min(puntos, PuntajeMaximo - PuntosDelPiso);
        Total += credito;
        PuntosDelPiso += creditoPiso;
        var premio = new PremioPuntaje
        {
            Nombre = nombre.ToUpperInvariant(), Puntos = puntos, Base = puntosBase,
            Credito = credito, CreditoPiso = creditoPiso
        };
        _acciones.Add(premio);
        if (_acciones.Count > 6) _acciones.RemoveAt(0);
        Estilo = Math.Min(MaximoEstilo, Estilo + puntosBase);
        _ultimaAccion = _reloj;
        ActualizarRango();
        return premio;
    }

    public void RegistrarParry(TipoParry tipo, object enemigo, bool automatico, bool ultimoInstante = false)
    {
        switch (tipo)
        {
            case TipoParry.Duck:
                Sumar("PARRY DUCK", 200);
                if (!_ducks.TryGetValue(enemigo, out var golpes)) _ducks[enemigo] = golpes = new();
                golpes.Enqueue(_reloj);
                while (golpes.Count > 0 && _reloj - golpes.Peek() >= 3) golpes.Dequeue();
                if (golpes.Count >= 3) { Sumar("PATITO FEO", 450); golpes.Clear(); }
                break;
            case TipoParry.Zorak:
                Sumar("PARRY ZORAK", 350);
                if (ultimoInstante) Sumar("PROVOCACION", 500);
                break;
            case TipoParry.JarJar:
                RegistrarJarJar(enemigo);
                break;
            case TipoParry.Lanza:
                _premiosLanzas.Add(Sumar("PARRY LANZA", 300));
                break;
            case TipoParry.Shame:
                Sumar("PARRY SHAME", 700);
                if (ultimoInstante) Sumar("VEN POR EL", 1000);
                break;
            case TipoParry.Delusion:
                Sumar("PARRY DELUSION", 250);
                break;
        }
        if (automatico) Sumar("AUTO-PARRY", 25);
        _parrys.Enqueue(_reloj);
        while (_parrys.Count > 0 && _reloj - _parrys.Peek() >= 1) _parrys.Dequeue();
        if (_parrys.Count == 2) Sumar("DOBLE PARRY", 250);
        else if (_parrys.Count == 3) Sumar("TRIPLE PARRY", 500);
        else if (_parrys.Count > 3) Sumar("MASTER PARRY", 800);
    }

    private void RegistrarJarJar(object enemigo)
    {
        _jarJars.RemoveAll(j => _reloj - j.Tiempo >= 1);
        int pareja = _jarJars.FindIndex(j => !ReferenceEquals(j.Enemigo, enemigo));
        if (pareja < 0)
        {
            _jarJars.Add((enemigo, _reloj, Sumar("PARRY JARJAR", 300)));
            return;
        }
        // Sustituir el premio real anterior, incluido su multiplicador, sin tocar otros bonus.
        PremioPuntaje previo = _jarJars[pareja].Premio;
        _jarJars.RemoveAt(pareja);
        DescontarPremio(previo);
        ActualizarRango();
        Sumar("2X1 JARJAR", 750);
    }

    public void ComenzarTandaLanzas() => _premiosLanzas.Clear();

    public void RegistrarBadBird()
    {
        if (_premiosLanzas.Count == 0) return;
        // Reemplaza todos los premios de lanza de esta tanda, incluso los de frames anteriores.
        foreach (PremioPuntaje premio in _premiosLanzas) DescontarPremio(premio);
        _premiosLanzas.Clear();
        ActualizarRango();
        Sumar("BAD BIRD", 800);
    }

    private void DescontarPremio(PremioPuntaje premio)
    {
        Total = Math.Max(0, Total - premio.Credito);
        PuntosDelPiso = Math.Max(0, PuntosDelPiso - premio.CreditoPiso);
        Estilo = Math.Max(0, Estilo - premio.Base);
        _acciones.Remove(premio);
    }

    private void ActualizarRango()
    {
        int anterior = Rango;
        Rango = Estilo <= 0 ? -1 : Rangos.Count - 1;
        while (Rango > 0 && Estilo < Rangos[Rango].Umbral) Rango--;
        if (Rango > anterior) RangoSubido?.Invoke(Rango);
    }

    public void Perder()
    {
        Total = 0;
        Estilo = 0;
        Rango = -1;
        _acciones.Clear();
        ComenzarNivel();
    }
}
