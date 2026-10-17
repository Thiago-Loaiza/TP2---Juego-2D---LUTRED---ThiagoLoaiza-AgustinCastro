using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LutredLibrary.Graphics;

public sealed class Camara2D
{
    public Vector2 Posicion { get; private set; }

    public void Seguir(Vector2 objetivo)
    {
        Posicion = objetivo;
    }

    public Matrix ObtenerTransformacion(Viewport viewport)
    {
        return Matrix.CreateTranslation(-Posicion.X, -Posicion.Y, 0f)
            * Matrix.CreateTranslation(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f);
    }
}
