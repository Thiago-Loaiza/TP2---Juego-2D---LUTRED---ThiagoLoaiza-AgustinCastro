using System;
using System.Collections.Generic;

namespace _1.Progression;

public sealed record DefinicionMejora(string Id, string Nombre, string Descripcion,
    int PisoMinimo, int Maximo, bool Implementada);

// Una instancia por partida; viaja entre preguntas, mejoras y pisos.
public sealed class EstadoMejoras
{
    public static readonly DefinicionMejora[] Catalogo =
    {
        new("PiezAgiles", "Pies agiles", "+30% de velocidad de movimiento.", 0, 2, true),
        new("TipoDuro", "Tipo duro", "+0,1 s de ventana de parry.\n+15% al radio del parry por acumulacion.", 0, 2, true),
        new("Dominio", "Dominio", "3 parrys manuales exitosos seguidos otorgan 5 s de autoparry.\nEl aura parpadea en los ultimos 2 s.\nRecarga: 15 segundos.", 0, 2, true),
        new("Precipitacion", "Precipitacion", "Un indicador avisa de una posible colision mortal.\nRecarga: 10 segundos.", 0, 1, false),
        new("GranJabalina", "Gran jabalina", "Lanza una jabalina dorada que parrea todo a su paso.\nRecarga: 30 segundos.", 8, 1, false),
        new("Paz", "Paz", "Zorak: +0,5 s de piedad al abrir los ojos.\nTooManyParries: bloquea al llegar a 5, en vez de 3.\nErosion Bird: 2 lanzas menos y ataques\nun 20% menos frecuentes.", 8, 1, true),
        new("FellDoor", "FellDoor", "Una puerta por piso: quedate quieto cerca 1,5 s.\nSobrevivi 10 s entre demonios parriables para volver.\nOtorga +20% de velocidad durante el piso y recoge\nel 50% de los orbes restantes, redondeando hacia arriba.\nEl reloj sigue corriendo y SHAME puede entrar.", 8, 1, true),
        new("AureolaDivina", "Aureola divina", "+15% de velocidad. Q: dash de 0,2 s hacia el movimiento\no la ultima direccion, a triple velocidad de correr.\nAtraviesa enemigos e ignora sus golpes y efectos.\nLas paredes lo bloquean. Recarga: 2 segundos.", 12, 1, true),
        new("TETUTRED", "TETUTRED", "+20% de velocidad, aceleracion instantanea y sin inercia.", 12, 1, true),
        new("Escudo", "Escudo", "Permite recibir un golpe mortal por piso.\nAl romperse otorga 2 s de invulnerabilidad.\nSe repone al ascender al siguiente piso.", 16, 1, true),
        new("Caliz", "Caliz", "+20% de velocidad. Al correr en una misma direccion,\ngana +10% cada 3 s, hasta +50% adicional.\nEl aumento se reinicia al cambiar de direccion.", 16, 1, true)
    };
    private readonly Dictionary<string, int> _acumulaciones = new();
    public EstadoDominio Dominio { get; } = new();
    public EstadoPuntaje Puntaje { get; } = new();
    public HashSet<string> DescripcionesVistas { get; } = new();
    public int Cantidad(string id) => _acumulaciones.TryGetValue(id, out int valor) ? valor : 0;
    public float MultiplicadorVelocidad => 1f + Cantidad("PiezAgiles") * 0.3f
        + Cantidad("TETUTRED") * 0.2f + Cantidad("Caliz") * 0.2f + Cantidad("AureolaDivina") * 0.15f;
    public double ExtraParry => Cantidad("TipoDuro") * 0.1;
    public float MultiplicadorRadioParry => 1f + Cantidad("TipoDuro") * 0.15f;
    public bool Disponible(DefinicionMejora mejora, int piso) => (Game1.IgnorarNivelMinimoMejoras || piso >= mejora.PisoMinimo)
        && mejora.Implementada
        && Cantidad(mejora.Id) < mejora.Maximo
        && (mejora.Id != "TETUTRED" || Cantidad("PiezAgiles") == 2);
    public string Describir(DefinicionMejora mejora) => mejora.Id == "Dominio" && Cantidad("Dominio") >= 1
        ? "2 parrys manuales exitosos seguidos otorgan 12 s de autoparry.\nEl aura parpadea en los ultimos 2 s.\nRecarga: 15 segundos."
        : mejora.Descripcion;

    public int[] ElegirOpciones(int piso)
    {
        var disponibles = new List<int>();
        for (int i = 0; i < Catalogo.Length; i++)
            if (Disponible(Catalogo[i], piso)) disponibles.Add(i);
        int[] opciones = { -1, -1, -1 };
        for (int i = 0; i < opciones.Length && disponibles.Count > 0; i++)
        {
            int indice = Random.Shared.Next(disponibles.Count);
            opciones[i] = disponibles[indice];
            disponibles.RemoveAt(indice);
        }
        return opciones;
    }

    public bool Aplicar(DefinicionMejora mejora, int piso)
    {
        if (!mejora.Implementada || !Disponible(mejora, piso))
        {
            return false;
        }
        _acumulaciones[mejora.Id] = Cantidad(mejora.Id) + 1;
        return true;
    }
}
