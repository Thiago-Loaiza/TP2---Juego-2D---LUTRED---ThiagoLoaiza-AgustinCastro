using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Mapa.Estructuras;

public sealed class EstructuraCompuesta : EstructuraSolida
{
    private readonly Rectangle[] _bloques;

    public EstructuraCompuesta(Rectangle limites, Rectangle[] bloques, Texture2D textura)
        : base(limites, textura, Array.ConvertAll(bloques, bloque => new ColisionSolida(bloque)))
    {
        _bloques = (Rectangle[])bloques.Clone();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        foreach (Rectangle bloque in _bloques)
            DibujarRegion(spriteBatch, bloque);
    }
}
