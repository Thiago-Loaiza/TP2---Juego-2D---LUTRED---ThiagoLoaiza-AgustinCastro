using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.UI;

public sealed class MarcoNinePatchAnimado
{
    private readonly Texture2D _textura;
    private readonly Rectangle[] _cuadros;
    private const int Borde = 32;

    public MarcoNinePatchAnimado(Texture2D textura, int cantidadCuadros)
    {
        _textura = textura;
        _cuadros = new Rectangle[cantidadCuadros];
        var pixeles = new Color[textura.Width * textura.Height];
        textura.GetData(pixeles);
        int ancho = textura.Width / cantidadCuadros;
        for (int i = 0; i < cantidadCuadros; i++)
        {
            int inicio = i * ancho;
            int izquierda = inicio + ancho, derecha = inicio - 1;
            int arriba = textura.Height, abajo = -1;
            for (int y = 0; y < textura.Height; y++)
            for (int x = inicio; x < inicio + ancho; x++)
            {
                if (pixeles[y * textura.Width + x].A == 0) continue;
                izquierda = Math.Min(izquierda, x);
                derecha = Math.Max(derecha, x);
                arriba = Math.Min(arriba, y);
                abajo = Math.Max(abajo, y);
            }
            _cuadros[i] = derecha >= izquierda && abajo >= arriba
                ? new Rectangle(izquierda, arriba, derecha - izquierda + 1, abajo - arriba + 1)
                : new Rectangle(inicio, 0, ancho, textura.Height);
        }
    }

    public void Draw(SpriteBatch lote, Rectangle destino, float tiempo, float escala, float alfa)
    {
        Rectangle origen = _cuadros[(int)(tiempo / 0.12f) % _cuadros.Length];
        int borde = Math.Min(Borde, Math.Min(origen.Width, origen.Height) / 2);
        int margen = Math.Min(Math.Max(1, (int)(borde * escala)), Math.Min(destino.Width, destino.Height) / 2);
        int[] fx = { origen.Left, origen.Left + borde, origen.Right - borde, origen.Right };
        int[] fy = { origen.Top, origen.Top + borde, origen.Bottom - borde, origen.Bottom };
        int[] dx = { destino.Left, destino.Left + margen, destino.Right - margen, destino.Right };
        int[] dy = { destino.Top, destino.Top + margen, destino.Bottom - margen, destino.Bottom };
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
            lote.Draw(_textura,
                new Rectangle(dx[x], dy[y], dx[x + 1] - dx[x], dy[y + 1] - dy[y]),
                new Rectangle(fx[x], fy[y], fx[x + 1] - fx[x], fy[y + 1] - fy[y]), Color.White * alfa);
    }
}
