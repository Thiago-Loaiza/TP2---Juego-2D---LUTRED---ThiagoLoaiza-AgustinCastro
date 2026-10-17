using System.Collections.Generic;

namespace _1.Mapa;

public static class CatalogoEstructuras
{
    public static IReadOnlyList<PlantillaEstructura> Crear()
    {
        return new PlantillaEstructura[]
        {
            new("bloque pequeno", new[]
            {
                "#"
            }, peso: 6),

            new("bloque grande", new[]
            {
                "##",
                "##"
            }, peso: 3),

            new("pasillo horizontal", new[]
            {
                "########",
                "........",
                "########"
            }, peso: 2),

            new("pasillo vertical", new[]
            {
                "#.#",
                "#.#",
                "#.#",
                "#.#",
                "#.#",
                "#.#",
                "#.#",
                "#.#"
            }, peso: 2),

            PlantillaEstructura.EnL("esquina", ancho: 3, alto: 3, grosor: 1, peso: 2),
            PlantillaEstructura.Circular("columna circular", diametro: 2, peso: 1)
        };
    }
}
