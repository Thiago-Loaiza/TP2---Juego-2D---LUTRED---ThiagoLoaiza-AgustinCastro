using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Mapa;

public static class CatalogoEstructuras
{
    public static IReadOnlyList<PlantillaEstructura> Crear(ContentManager content)
    {
        return new PlantillaEstructura[]
        {
            // ESTRUCTURAS COMUNES
            new("bloque pequeno", new[]
            {
                "#"
            }, peso: 12),

            new("bloque grande", new[]
            {
                "##",
                "##"
            }, peso: 6),

            new("pasillo horizontal", new[]
            {
                "########",
                "........",
                "########"
            }, peso: 4),

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
            }, peso: 4),

            PlantillaEstructura.EnL("esquina", ancho: 3, alto: 3, grosor: 1, peso: 4),
            PlantillaEstructura.Circular("columna circular", diametro: 2, peso: 2),

            // ESTRUCTURAS POCO COMUNES
            new("cruz", new[]
            {
                ".#.",
                "###",
                "###",
                ".#."
            }, peso: 3),

            // Dos arcos enfrentados, con accesos arriba y abajo.
            new("circulo hueco abierto", new[]
            {
                "..##..##..",
                ".##....##.",
                "##......##",
                "##......##",
                "##......##",
                "##......##",
                "##......##",
                "##......##",
                ".##....##.",
                "..##..##.."
            }, peso: 3, subdivisiones: 2),

            new("arco hacia abajo", new[]
            {
                ".####.",
                "##..##",
                "#....#",
                "#....#"
            }, peso: 3, subdivisiones: 2),

            new("castillo", new[]
            {
                "#.#.#",
                "#####",
                "#####",
                "#####",
                "#####"
            }, peso: 2),

            // ESTRUCTURAS RARAS
            new("mini laberinto", new[]
            {
                "#.########",
                "#........#",
                "########.#",
                "#........#",
                "#.########",
                "#........#",
                "########.#",
                "#........#",
                "#........#",
                "########.#"
            }, peso: 1),

            new("rayo", new[]
            {
                "##..", "##..", ".##.", ".##.",
                "..##", "..##", ".##.", ".##.",
                "##..", "##..", ".##.", ".##.",
                "..##", "..##", ".##.", ".##."
            }, peso: 1, subdivisiones: 2),

            // ESTRUCTURAS RARAS DE IMAGEN
            PlantillaEstructura.DesdeImagen("Doge", 3, 3, content.Load<Texture2D>("Sprites/Misc/Structures/Doge"), peso: 1, probabilidadAparicion: 0.0005f),
            PlantillaEstructura.DesdeImagen("SUS", 3, 3, content.Load<Texture2D>("Sprites/Misc/Structures/Sus"), peso: 1, probabilidadAparicion: 0.0005f),
            PlantillaEstructura.DesdeImagen("Creeper", 2, 2, content.Load<Texture2D>("Sprites/Misc/Structures/Creeper"), peso: 1, probabilidadAparicion: 0.0005f)
        };
    }
}
