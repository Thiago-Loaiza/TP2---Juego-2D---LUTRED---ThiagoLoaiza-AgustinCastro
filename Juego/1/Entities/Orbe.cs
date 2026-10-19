using System;
using System.Collections.Generic;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _1.Colisiones;

namespace _1.Entities;

public sealed class Orbe
{
    public const float Radio = 24f;
    private readonly AnimatedSprite _sprite;
    public CirculoColision Colision => new(_sprite.Position, Radio);

    public static Animation CrearAnimacion(Texture2D hoja)
    {
        var cuadros = new List<TextureRegion>();
        for (int i = 0; i < 4; i++)
        {
            cuadros.Add(new TextureRegion(hoja, i * 50, 0, 50, 50));
        }
        return new Animation(cuadros, TimeSpan.FromSeconds(0.12));
    }

    public Orbe(Animation animacion, Vector2 posicion)
    {
        _sprite = new AnimatedSprite(animacion)
        {
            Position = posicion,
            Origin = new Vector2(25f)
        };
    }

    public void Update(GameTime gameTime) => _sprite.Update(gameTime);
    public void Draw(SpriteBatch spriteBatch) => _sprite.Draw(spriteBatch);
}
