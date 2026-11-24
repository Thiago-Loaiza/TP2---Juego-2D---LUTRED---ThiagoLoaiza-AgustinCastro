using System;
using System.Collections.Generic;
using _1.Colisiones;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Mapa.Decoraciones;

public sealed class DecoracionesMapa
{
    private const int TamanoGrupo = 240;
    private const float Escala = 1.5f;
    private const float DuracionCuadro = 0.14f;

    private sealed record Tipo(Texture2D Textura, int Tamano, int Cuadros, bool Vertical, int BaseY, int Peso);
    private sealed record Decoracion(Tipo Tipo, Vector2 Posicion, Rectangle Limites, int Fase);
    private readonly Tipo[] _tipos;
    private readonly List<Decoracion> _piso = new();
    private readonly List<Decoracion> _verticales = new();
    private double _tiempo;

    public DecoracionesMapa(ContentManager content)
    {
        Tipo Cargar(string nombre, int tamano, bool vertical, int baseY, int peso)
        {
            Texture2D textura = content.Load<Texture2D>("Sprites/Misc/Decorations/" + nombre);
            return new Tipo(textura, tamano, textura.Width / tamano, vertical, baseY, peso);
        }
        _tipos = new[]
        {
            Cargar("Sprite-flor-1", 20, false, 10, 6),
            Cargar("Sprite-flor-2", 20, false, 10, 6),
            Cargar("Sprite-roca-1", 40, false, 20, 4),
            Cargar("Sprite-roca-2", 40, false, 20, 4),
            Cargar("Sprite-Pasto-Sheet", 40, true, 32, 10),
            Cargar("Sprite-cruz-Sheet", 60, true, 50, 1)
        };
    }

    public void Generar(MapaGenerado mapa)
    {
        _piso.Clear();
        _verticales.Clear();
        _tiempo = 0;
        var azar = new Random(mapa.Semilla);
        var posiciones = new List<Vector2>();
        Rectangle zona = mapa.Limites;
        for (int y = zona.Top; y < zona.Bottom; y += TamanoGrupo)
            for (int x = zona.Left; x < zona.Right; x += TamanoGrupo)
            {
                if (azar.NextDouble() > 0.35) continue;
                Vector2 centro = new(x + azar.Next(TamanoGrupo), y + azar.Next(TamanoGrupo));
                int cantidad = azar.Next(1, 4);
                for (int i = 0; i < cantidad; i++)
                {
                    Tipo tipo = ElegirTipo(azar);
                    Vector2 posicion = centro + new Vector2(azar.Next(-70, 71), azar.Next(-70, 71));
                    int lado = (int)(tipo.Tamano * Escala);
                    var limite = new Rectangle((int)posicion.X - lado / 2,
                        (int)posicion.Y - (int)(tipo.BaseY * Escala), lado, lado);
                    if (!zona.Contains(limite)) continue;

                    // El circulo envolvente evita que el sprite se superponga con muros rectos o circulares.
                    var huella = new CirculoColision(limite.Center.ToVector2(), lado * 0.707107f);
                    bool libre = true;
                    foreach (ColisionSolida obstaculo in mapa.Obstaculos)
                        if (obstaculo.Toca(huella)) { libre = false; break; }
                    if (!libre) continue;
                    foreach (Vector2 anterior in posiciones)
                        if (Vector2.DistanceSquared(anterior, posicion) < 26 * 26) { libre = false; break; }
                    if (!libre) continue;

                    posiciones.Add(posicion);
                    var decoracion = new Decoracion(tipo, posicion, limite, azar.Next(tipo.Cuadros));
                    (tipo.Vertical ? _verticales : _piso).Add(decoracion);
                }
            }
        _verticales.Sort((a, b) => a.Posicion.Y.CompareTo(b.Posicion.Y));
    }

    private Tipo ElegirTipo(Random azar)
    {
        int total = 0;
        foreach (Tipo tipo in _tipos) total += tipo.Peso;
        int eleccion = azar.Next(total);
        foreach (Tipo tipo in _tipos)
        {
            eleccion -= tipo.Peso;
            if (eleccion < 0) return tipo;
        }
        return _tipos[0];
    }

    public void Update(float segundos) => _tiempo = (_tiempo + segundos) % (42 * DuracionCuadro);

    public void DibujarPiso(SpriteBatch batch, Rectangle visible)
    {
        foreach (Decoracion decoracion in _piso)
            if (decoracion.Limites.Intersects(visible)) Dibujar(batch, decoracion);
    }

    public void DibujarVerticales(SpriteBatch batch, Rectangle visible, float ySinner, bool delante)
    {
        foreach (Decoracion decoracion in _verticales)
            if ((decoracion.Posicion.Y > ySinner) == delante && decoracion.Limites.Intersects(visible))
                Dibujar(batch, decoracion);
    }

    private void Dibujar(SpriteBatch batch, Decoracion decoracion)
    {
        Tipo tipo = decoracion.Tipo;
        int cuadro = ((int)(_tiempo / DuracionCuadro) + decoracion.Fase) % tipo.Cuadros;
        var origen = new Rectangle(cuadro * tipo.Tamano, 0, tipo.Tamano, tipo.Tamano);
        batch.Draw(tipo.Textura, decoracion.Limites, origen, Color.White);
    }
}
