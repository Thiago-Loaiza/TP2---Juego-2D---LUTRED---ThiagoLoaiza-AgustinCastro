using System;
using System.Collections.Generic;
using _1.Colisiones;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public sealed class DemonioFellDoor
{
    private readonly Texture2D _textura;
    private float _tiempo;
    private bool _caminando;
    private bool _izquierda;
    public Vector2 Position { get; private set; }
    public CirculoColision Colision => new(Position, 22f);

    public DemonioFellDoor(Texture2D textura, Vector2 posicion)
    {
        _textura = textura;
        Position = posicion;
    }

    public void Update(float segundos, Vector2 objetivo, float velocidad,
        IReadOnlyList<ColisionSolida> obstaculos)
    {
        _tiempo += segundos;
        Vector2 diferencia = objetivo - Position;
        float distancia = diferencia.Length();
        Vector2 movimiento = distancia > 0.01f
            ? diferencia / distancia * Math.Min(distancia, velocidad * segundos) : Vector2.Zero;
        movimiento = ResolutorMovimiento.Limitar(Colision, movimiento, obstaculos);
        _caminando = movimiento.LengthSquared() > 0.001f;
        if (movimiento.X != 0) _izquierda = movimiento.X < 0;
        Position += movimiento;
    }

    public void Draw(SpriteBatch batch)
    {
        int ancho = _textura.Width / 3;
        int cuadro = _caminando ? 1 + (int)(_tiempo / 0.15f) % 2 : 0;
        batch.Draw(_textura, Position - new Vector2(0, 15),
            new Rectangle(cuadro * ancho, 0, ancho, _textura.Height), Color.White,
            0f, new Vector2(ancho / 2f, _textura.Height / 2f), 0.4f,
            _izquierda ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }
}
