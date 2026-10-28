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
        new("TipoDuro", "Tipo duro", "+0,25 segundos de ventana de parry.", 0, 2, true),
        new("Dominio", "Dominio", "3 parrys perfectos seguidos otorgan 1,5 s de autoparry.\nRecarga: 15 segundos.", 0, 2, false),
        new("Precipitacion", "Precipitacion", "Un indicador avisa de una posible colision mortal.\nRecarga: 10 segundos.", 0, 1, false),
        new("GranJabalina", "Gran jabalina", "Lanza una jabalina dorada que parrea todo a su paso.\nRecarga: 30 segundos.", 8, 1, false),
        new("Guantelet", "Guantelet", "Potencia los parrys contra las entidades.\nDuck retrocede mas y JarJar queda aturdido mas tiempo.", 8, 1, false),
        new("Paz", "Paz", "Debilita a las entidades para facilitar el combate.\nZorak otorga +1 s de piedad; ToManyParrys dura menos.", 8, 1, false),
        new("FellDoor", "FellDoor", "Abre una dimension de bolsillo con demonios rojos.\nSobrevivir 10 s recoge el 40% de los orbes objetivo.\nEl reloj sigue corriendo y SHAME puede entrar.", 8, 1, false),
        new("AureolaDivina", "Aureola divina", "+15% de velocidad y un dash en la direccion de mirada.\nEl dash atraviesa enemigos. Recarga: 2 segundos.", 12, 1, false),
        new("TETUTRED", "TETUTRED", "+20% de velocidad, aceleracion instantanea y sin inercia.", 12, 1, true),
        new("Escudo", "Escudo", "Permite recibir un golpe mortal por piso.\nSe repone al ascender al siguiente piso.", 16, 1, false),
        new("Caliz", "Caliz", "+20% de velocidad. Al correr en una misma direccion,\ngana +10% cada 3 s, hasta +50% adicional.\nEl aumento se reinicia al cambiar de direccion.", 16, 1, true)
    };
    private readonly Dictionary<string, int> _acumulaciones = new();
    public HashSet<string> DescripcionesVistas { get; } = new();
    public int Cantidad(string id) => _acumulaciones.TryGetValue(id, out int valor) ? valor : 0;
    public float MultiplicadorVelocidad => 1f + Cantidad("PiezAgiles") * 0.3f
        + Cantidad("TETUTRED") * 0.2f + Cantidad("Caliz") * 0.2f;
    public double ExtraParry => Cantidad("TipoDuro") * 0.25;
    public bool Disponible(DefinicionMejora mejora, int piso) => piso >= mejora.PisoMinimo
        && Cantidad(mejora.Id) < mejora.Maximo
        && (mejora.Id != "TETUTRED" || Cantidad("PiezAgiles") == 2);
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
