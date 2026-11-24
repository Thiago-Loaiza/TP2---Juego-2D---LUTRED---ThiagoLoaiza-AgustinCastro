using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Mapa.Estructuras;

// La silueta del PNG y sus colisiones comparten el mismo rectangulo de destino.
public sealed class EstructuraImagen : EstructuraSolida
{
    public Texture2D Imagen { get; }
    private readonly Rectangle _destino;

    public EstructuraImagen(Rectangle limites, Texture2D imagen, Rectangle[] bloques, int rejilla)
        : this(limites, imagen, Ajustar(limites, imagen), bloques, rejilla) { }

    private EstructuraImagen(Rectangle limites, Texture2D imagen, Rectangle destino, Rectangle[] bloques, int rejilla)
        : base(limites, imagen, Array.ConvertAll(bloques, bloque =>
            new ColisionSolida(new Rectangle(
                destino.X + bloque.Left * destino.Width / rejilla,
                destino.Y + bloque.Top * destino.Height / rejilla,
                bloque.Right * destino.Width / rejilla - bloque.Left * destino.Width / rejilla,
                bloque.Bottom * destino.Height / rejilla - bloque.Top * destino.Height / rejilla))))
    {
        Imagen = imagen;
        _destino = destino;
    }

    private static Rectangle Ajustar(Rectangle limites, Texture2D imagen)
    {
        float escala = Math.Min((float)limites.Width / imagen.Width, (float)limites.Height / imagen.Height);
        int ancho = (int)(imagen.Width * escala), alto = (int)(imagen.Height * escala);
        return new Rectangle(limites.Center.X - ancho / 2, limites.Center.Y - alto / 2, ancho, alto);
    }

    public override void Draw(SpriteBatch spriteBatch) => spriteBatch.Draw(Imagen, _destino, Color.White);
}
