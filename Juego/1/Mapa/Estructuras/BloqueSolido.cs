using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Mapa.Estructuras;

public class BloqueSolido : EstructuraSolida
{
    public BloqueSolido(Rectangle limites, Texture2D textura)
        : base(limites, textura, new ColisionSolida(limites))
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DibujarRegion(spriteBatch, Limites);
    }
}
