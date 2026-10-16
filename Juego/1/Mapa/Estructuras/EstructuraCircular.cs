using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Mapa.Estructuras;

public class EstructuraCircular : EstructuraSolida
{
    private readonly Rectangle[] _filas;

    public EstructuraCircular(Point centro, int radio, Texture2D textura)
        : base(CrearLimites(centro, radio), textura,
            new ColisionSolida(new CirculoColision(centro.ToVector2(), radio)))
    {
        _filas = new Rectangle[Limites.Height];
        for (int y = 0; y < Limites.Height; y++)
        {
            float dy = y + 0.5f - radio;
            float alcance = MathF.Sqrt(radio * radio - dy * dy);
            int izquierda = (int)MathF.Ceiling(radio - alcance - 0.5f);
            int derecha = (int)MathF.Floor(radio + alcance - 0.5f) + 1;
            _filas[y] = new Rectangle(Limites.X + izquierda, Limites.Y + y,
                derecha - izquierda, 1);
        }
    }

    private static Rectangle CrearLimites(Point centro, int radio)
    {
        if (radio <= 0)
            throw new ArgumentOutOfRangeException(nameof(radio));
        return new Rectangle(centro.X - radio, centro.Y - radio, radio * 2, radio * 2);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        foreach (Rectangle fila in _filas)
            DibujarRegion(spriteBatch, fila);
    }
}
