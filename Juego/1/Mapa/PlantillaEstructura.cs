using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Mapa.Estructuras;

namespace _1.Mapa;

public sealed class PlantillaEstructura
{
    private readonly Func<Rectangle, Texture2D, EstructuraSolida> _crear;

    public string Nombre { get; }
    public int Ancho { get; }
    public int Alto { get; }
    public int Peso { get; }
    public float ProbabilidadAparicion { get; private init; } = 1f;

    public PlantillaEstructura(string nombre, string[] plano, int peso = 1, int subdivisiones = 1)
    {
        ArgumentNullException.ThrowIfNull(plano);
        if (plano.Length == 0 || string.IsNullOrEmpty(plano[0]))
            throw new ArgumentException("El plano no puede estar vacio.", nameof(plano));
        if (subdivisiones <= 0 || plano[0].Length % subdivisiones != 0 || plano.Length % subdivisiones != 0)
            throw new ArgumentException("El plano debe ocupar chunks completos.", nameof(subdivisiones));
        Validar(nombre, plano[0].Length / subdivisiones, plano.Length / subdivisiones, peso);
        Nombre = nombre;
        Ancho = plano[0].Length / subdivisiones;
        Alto = plano.Length / subdivisiones;
        Peso = peso;
        var bloques = new List<Rectangle>();
        for (int y = 0; y < plano.Length; y++)
        {
            if (plano[y] == null || plano[y].Length != Ancho * subdivisiones)
                throw new ArgumentException("Todas las filas deben tener el mismo ancho.", nameof(plano));
            int x = 0;
            while (x < Ancho * subdivisiones)
            {
                if (plano[y][x] == '.') { x++; continue; }
                if (plano[y][x] != '#')
                    throw new ArgumentException("El plano solo admite # y .", nameof(plano));
                int inicio = x++;
                while (x < Ancho * subdivisiones && plano[y][x] == '#') x++;
                int ancho = x - inicio;
                int anterior = bloques.FindIndex(b => b.X == inicio && b.Width == ancho && b.Bottom == y);
                if (anterior < 0)
                    bloques.Add(new Rectangle(inicio, y, ancho, 1));
                else
                {
                    Rectangle bloque = bloques[anterior];
                    bloque.Height++;
                    bloques[anterior] = bloque;
                }
            }
        }
        if (bloques.Count == 0)
            throw new ArgumentException("La estructura necesita al menos una pared.", nameof(plano));

        _crear = (limites, textura) =>
        {
            int chunk = limites.Width / Ancho;
            if (chunk % subdivisiones != 0)
                throw new ArgumentException("El tamano del chunk no admite estas subdivisiones.");
            chunk /= subdivisiones;
            var regiones = new Rectangle[bloques.Count];
            for (int i = 0; i < bloques.Count; i++)
            {
                Rectangle bloque = bloques[i];
                regiones[i] = new Rectangle(limites.X + bloque.X * chunk,
                    limites.Y + bloque.Y * chunk, bloque.Width * chunk, bloque.Height * chunk);
            }
            return new EstructuraCompuesta(limites, regiones, textura);
        };
    }

    private PlantillaEstructura(string nombre, int ancho, int alto, int peso,
        Func<Rectangle, Texture2D, EstructuraSolida> crear)
    {
        Validar(nombre, ancho, alto, peso);
        Nombre = nombre;
        Ancho = ancho;
        Alto = alto;
        Peso = peso;
        _crear = crear;
    }

    public static PlantillaEstructura EnL(string nombre, int ancho, int alto, int grosor, int peso = 1)
    {
        if (grosor <= 0 || grosor >= Math.Min(ancho, alto))
            throw new ArgumentOutOfRangeException(nameof(grosor));
        return new PlantillaEstructura(nombre, ancho, alto, peso,
            (limites, textura) => new EstructuraL(limites, grosor * (limites.Width / ancho), textura));
    }

    public static PlantillaEstructura Circular(string nombre, int diametro, int peso = 1)
    {
        return new PlantillaEstructura(nombre, diametro, diametro, peso,
            (limites, textura) => new EstructuraCircular(limites.Center, limites.Width / 2, textura));
    }

    public static PlantillaEstructura DesdeImagen(string nombre, int ancho, int alto, Texture2D imagen, int peso = 1,
        float probabilidadAparicion = 1f)
    {
        ArgumentNullException.ThrowIfNull(imagen);
        if (probabilidadAparicion < 0f || probabilidadAparicion > 1f)
            throw new ArgumentOutOfRangeException(nameof(probabilidadAparicion));
        const int rejilla = 24;
        var pixeles = new Color[imagen.Width * imagen.Height];
        imagen.GetData(pixeles);
        var bloques = new List<Rectangle>();
        bool Solido(int x, int y)
        {
            int px = (int)((x + 0.5f) * imagen.Width / rejilla);
            int py = (int)((y + 0.5f) * imagen.Height / rejilla);
            return pixeles[py * imagen.Width + px].A >= 128;
        }
        for (int y = 0; y < rejilla; y++)
            for (int x = 0; x < rejilla;)
            {
                if (!Solido(x, y)) { x++; continue; }
                int inicio = x++;
                while (x < rejilla && Solido(x, y)) x++;
                int largo = x - inicio;
                int anterior = bloques.FindIndex(b => b.X == inicio && b.Width == largo && b.Bottom == y);
                if (anterior < 0) bloques.Add(new Rectangle(inicio, y, largo, 1));
                else
                {
                    Rectangle bloque = bloques[anterior];
                    bloque.Height++;
                    bloques[anterior] = bloque;
                }
            }
        Rectangle[] mascara = bloques.ToArray();
        return new PlantillaEstructura(nombre, ancho, alto, peso,
            (limites, _) => new EstructuraImagen(limites, imagen, mascara, rejilla))
            { ProbabilidadAparicion = probabilidadAparicion };
    }

    public EstructuraSolida Crear(Point origen, int tamanoChunk, Texture2D textura)
    {
        if (tamanoChunk < 2 || tamanoChunk % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(tamanoChunk));
        return _crear(new Rectangle(origen.X, origen.Y, Ancho * tamanoChunk, Alto * tamanoChunk), textura);
    }

    private static void Validar(string nombre, int ancho, int alto, int peso)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Falta el nombre.", nameof(nombre));
        if (ancho <= 0) throw new ArgumentOutOfRangeException(nameof(ancho));
        if (alto <= 0) throw new ArgumentOutOfRangeException(nameof(alto));
        if (peso <= 0) throw new ArgumentOutOfRangeException(nameof(peso));
    }
}
