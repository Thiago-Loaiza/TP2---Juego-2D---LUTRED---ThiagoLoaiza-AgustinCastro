using System;
using Microsoft.Xna.Framework;

namespace _1.Colisiones;

public readonly struct ColisionSolida
{
    private readonly Rectangle _rectangulo;
    private readonly CirculoColision _circulo;
    private readonly bool _esCirculo;

    public ColisionSolida(Rectangle rectangulo)
    {
        _rectangulo = rectangulo;
        _circulo = default;
        _esCirculo = false;
    }

    public ColisionSolida(CirculoColision circulo)
    {
        _rectangulo = default;
        _circulo = circulo;
        _esCirculo = true;
    }

    public float LimitarEje(CirculoColision jugador, float movimiento, bool horizontal)
    {
        float posicion = horizontal ? jugador.Centro.X : jugador.Centro.Y;
        float perpendicular = horizontal ? jugador.Centro.Y : jugador.Centro.X;
        float minimo, maximo, distancia, radio;
        if (_esCirculo)
        {
            minimo = maximo = horizontal ? _circulo.Centro.X : _circulo.Centro.Y;
            distancia = Math.Abs(perpendicular
                - (horizontal ? _circulo.Centro.Y : _circulo.Centro.X));
            radio = jugador.Radio + _circulo.Radio;
        }
        else
        {
            minimo = horizontal ? _rectangulo.Left : _rectangulo.Top;
            maximo = horizontal ? _rectangulo.Right : _rectangulo.Bottom;
            float cercano = MathHelper.Clamp(perpendicular,
                horizontal ? _rectangulo.Top : _rectangulo.Left,
                horizontal ? _rectangulo.Bottom : _rectangulo.Right);
            distancia = Math.Abs(perpendicular - cercano);
            radio = jugador.Radio;
        }

        if (distancia >= radio)
            return movimiento;

        float alcance = MathF.Sqrt(radio * radio - distancia * distancia);
        float entrada = minimo - alcance;
        float salida = maximo + alcance;
        const float tolerancia = 0.001f;
        if (movimiento > 0f && posicion <= entrada + tolerancia)
            return Math.Min(movimiento, Math.Max(0f, entrada - posicion));
        if (movimiento < 0f && posicion >= salida - tolerancia)
            return Math.Max(movimiento, Math.Min(0f, salida - posicion));
        return movimiento;
    }
}
