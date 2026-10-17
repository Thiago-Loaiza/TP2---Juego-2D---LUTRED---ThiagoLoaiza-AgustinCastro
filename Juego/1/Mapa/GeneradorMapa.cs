using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;
using _1.Mapa.Estructuras;

namespace _1.Mapa;

public sealed class MapaGenerado
{
    public int Semilla { get; }
    public Rectangle Limites { get; }
    public IReadOnlyList<EstructuraSolida> Estructuras { get; }
    public IReadOnlyList<ColisionSolida> Obstaculos { get; }
    public IReadOnlyList<Rectangle> HuellasChunks { get; }

    internal MapaGenerado(int semilla, Rectangle limites, List<EstructuraSolida> estructuras,
        List<ColisionSolida> obstaculos, List<Rectangle> huellas)
    {
        Semilla = semilla;
        Limites = limites;
        Estructuras = estructuras.AsReadOnly();
        Obstaculos = obstaculos.AsReadOnly();
        HuellasChunks = huellas.AsReadOnly();
    }
}

public sealed class GeneradorMapa
{
    public int TamanoChunk { get; }
    public int ChunksPorLado { get; }
    public int SeparacionChunks { get; }
    public int RadioZonaInicial { get; }

    public GeneradorMapa(int tamanoChunk = 128, int chunksPorLado = 41,
        int separacionChunks = 1, int radioZonaInicial = 3)
    {
        if (tamanoChunk < 2 || tamanoChunk % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(tamanoChunk));
        if (chunksPorLado < 3 || chunksPorLado % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(chunksPorLado));
        if (separacionChunks < 1) throw new ArgumentOutOfRangeException(nameof(separacionChunks));
        if (radioZonaInicial < 0 || radioZonaInicial >= chunksPorLado / 2)
            throw new ArgumentOutOfRangeException(nameof(radioZonaInicial));
        TamanoChunk = tamanoChunk;
        ChunksPorLado = chunksPorLado;
        SeparacionChunks = separacionChunks;
        RadioZonaInicial = radioZonaInicial;
    }

    public MapaGenerado Generar(Vector2 inicio, Texture2D textura,
        IReadOnlyList<PlantillaEstructura> catalogo, int semilla, int intentos = 1500)
    {
        ArgumentNullException.ThrowIfNull(catalogo);
        if (catalogo.Count == 0) throw new ArgumentException("El catalogo esta vacio.", nameof(catalogo));
        if (intentos <= 0) throw new ArgumentOutOfRangeException(nameof(intentos));
        int pesoTotal = 0;
        foreach (PlantillaEstructura plantilla in catalogo)
        {
            ArgumentNullException.ThrowIfNull(plantilla);
            if (plantilla.Ancho > ChunksPorLado || plantilla.Alto > ChunksPorLado)
                throw new ArgumentException("Una plantilla es mas grande que el mapa.", nameof(catalogo));
            pesoTotal = checked(pesoTotal + plantilla.Peso);
        }

        var azar = new Random(semilla);
        var ocupados = new bool[ChunksPorLado, ChunksPorLado];
        var estructuras = new List<EstructuraSolida>();
        var obstaculos = new List<ColisionSolida>();
        var huellas = new List<Rectangle>();
        int centro = ChunksPorLado / 2;
        var origen = new Point((int)MathF.Floor(inicio.X) - centro * TamanoChunk - TamanoChunk / 2,
            (int)MathF.Floor(inicio.Y) - centro * TamanoChunk - TamanoChunk / 2);
        for (int y = centro - RadioZonaInicial; y <= centro + RadioZonaInicial; y++)
            for (int x = centro - RadioZonaInicial; x <= centro + RadioZonaInicial; x++)
                ocupados[x, y] = true;

        for (int intento = 0; intento < intentos; intento++)
        {
            int eleccion = azar.Next(pesoTotal);
            PlantillaEstructura elegida = catalogo[0];
            foreach (PlantillaEstructura plantilla in catalogo)
            {
                eleccion -= plantilla.Peso;
                if (eleccion < 0) { elegida = plantilla; break; }
            }
            var huella = new Rectangle(azar.Next(ChunksPorLado - elegida.Ancho + 1),
                azar.Next(ChunksPorLado - elegida.Alto + 1), elegida.Ancho, elegida.Alto);
            if (!EstaLibre(ocupados, huella)) continue;

            // se reserva la huella completa, incluidos los espacios vacios del plano.
            for (int y = huella.Top; y < huella.Bottom; y++)
                for (int x = huella.Left; x < huella.Right; x++)
                    ocupados[x, y] = true;

            EstructuraSolida estructura = elegida.Crear(new Point(
                origen.X + huella.X * TamanoChunk, origen.Y + huella.Y * TamanoChunk), TamanoChunk, textura);
            estructuras.Add(estructura);
            obstaculos.AddRange(estructura.Colisiones);
            huellas.Add(huella);
        }

        return new MapaGenerado(semilla, new Rectangle(origen.X, origen.Y,
            ChunksPorLado * TamanoChunk, ChunksPorLado * TamanoChunk), estructuras, obstaculos, huellas);
    }

    private bool EstaLibre(bool[,] ocupados, Rectangle huella)
    {
        for (int y = Math.Max(0, huella.Top - SeparacionChunks);
            y < Math.Min(ChunksPorLado, huella.Bottom + SeparacionChunks); y++)
            for (int x = Math.Max(0, huella.Left - SeparacionChunks);
                x < Math.Min(ChunksPorLado, huella.Right + SeparacionChunks); x++)
                if (ocupados[x, y]) return false;
        return true;
    }
}
