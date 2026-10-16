using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace _1.Colisiones;

public static class ResolutorMovimiento
{
    public static Vector2 Limitar(CirculoColision jugador, Vector2 movimiento,
        IReadOnlyList<ColisionSolida> obstaculos)
    {
        // resolver cada eje contra todas las formas evita que una estructura altere un recorrido ya comprobado.
        float x = movimiento.X;
        foreach (ColisionSolida obstaculo in obstaculos)
            x = obstaculo.LimitarEje(jugador, x, horizontal: true);

        var despuesDeX = new CirculoColision(
            jugador.Centro + new Vector2(x, 0f), jugador.Radio);
        float y = movimiento.Y;
        foreach (ColisionSolida obstaculo in obstaculos)
            y = obstaculo.LimitarEje(despuesDeX, y, horizontal: false);
        return new Vector2(x, y);
    }
}
