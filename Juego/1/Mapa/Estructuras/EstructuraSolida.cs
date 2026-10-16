using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Mapa.Estructuras;

public abstract class EstructuraSolida
{
    protected Texture2D Textura { get; }
    public Rectangle Limites { get; }
    public IReadOnlyList<ColisionSolida> Colisiones { get; }

    protected EstructuraSolida(Rectangle limites, Texture2D textura,
        params ColisionSolida[] colisiones)
    {
        Limites = limites;
        Textura = textura;
        Colisiones = Array.AsReadOnly(colisiones);
    }

    public abstract void Draw(SpriteBatch spriteBatch);

    // compartir las coordenadas de textura mantiene los ladrillos alineados entre todas las partes.
    protected void DibujarRegion(SpriteBatch spriteBatch, Rectangle region)
    {
        int x = (region.Left - Limites.Left) * Textura.Width / Limites.Width;
        int y = (region.Top - Limites.Top) * Textura.Height / Limites.Height;
        int derecha = (region.Right - Limites.Left) * Textura.Width / Limites.Width;
        int abajo = (region.Bottom - Limites.Top) * Textura.Height / Limites.Height;
        var origen = new Rectangle(x, y, Math.Max(1, derecha - x), Math.Max(1, abajo - y));
        spriteBatch.Draw(Textura, region, origen, Color.White);
    }
}
