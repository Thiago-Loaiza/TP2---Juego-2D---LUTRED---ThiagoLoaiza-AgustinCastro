using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Mapa.Estructuras;

public class EstructuraL : EstructuraSolida
{
    private readonly Rectangle _vertical;
    private readonly Rectangle _horizontal;

    public EstructuraL(Rectangle limites, int grosor, Texture2D textura)
        : base(limites, textura, CrearColisiones(limites, grosor))
    {
        _vertical = new Rectangle(limites.X, limites.Y, grosor, limites.Height);
        _horizontal = new Rectangle(limites.X + grosor, limites.Bottom - grosor,
            limites.Width - grosor, grosor);
    }

    private static ColisionSolida[] CrearColisiones(Rectangle limites, int grosor)
    {
        if (grosor <= 0 || grosor >= Math.Min(limites.Width, limites.Height))
            throw new ArgumentOutOfRangeException(nameof(grosor));
        return new[]
        {
            new ColisionSolida(new Rectangle(limites.X, limites.Y, grosor, limites.Height)),
            new ColisionSolida(new Rectangle(limites.X + grosor, limites.Bottom - grosor,
                limites.Width - grosor, grosor))
        };
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DibujarRegion(spriteBatch, _vertical);
        DibujarRegion(spriteBatch, _horizontal);
    }
}
