using Microsoft.Xna.Framework;

namespace _1.Colisiones;

public readonly struct CirculoColision
{
    public Vector2 Centro { get; }
    public float Radio { get; }

    public CirculoColision(Vector2 centro, float radio)
    {
        Centro = centro;
        Radio = radio;
    }

    public bool Toca(CirculoColision otro)
    {
        float radios = Radio + otro.Radio;
        return Vector2.DistanceSquared(Centro, otro.Centro) <= radios * radios;
    }

    public bool SeCruzan(Vector2 centroAnterior,
        CirculoColision otro, Vector2 centroAnteriorOtro)
    {
        // Movimiento relativo: buscamos el punto del recorrido mas cercano a cero.
        Vector2 inicio = centroAnterior - centroAnteriorOtro;
        Vector2 fin = Centro - otro.Centro;
        Vector2 recorrido = fin - inicio;
        float longitudCuadrada = recorrido.LengthSquared();
        if (longitudCuadrada == 0f)
            return Toca(otro);

        float instante = MathHelper.Clamp(
            -Vector2.Dot(inicio, recorrido) / longitudCuadrada, 0f, 1f);
        Vector2 puntoCercano = inicio + recorrido * instante;
        float radios = Radio + otro.Radio;
        return puntoCercano.LengthSquared() <= radios * radios;
    }
}
