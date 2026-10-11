using System;
using System.Collections.Generic;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1;

public class PantallaDerrota
{
    private readonly Random _azar = new();
    private readonly List<Animation> _animaciones = new();
    private AnimatedSprite _sprite;

    public void LoadContent(ContentManager content)
    {
        foreach (string nombre in new[] { "Eye", "Flower", "Line" })
        {
            Texture2D textura = content.Load<Texture2D>(
                $"Sprites/UI/DeatchScreen/DeathScreen-{nombre}-Sheet");
            List<TextureRegion> frames = new();
            for (int x = 0; x < textura.Width; x += 100)
                frames.Add(new TextureRegion(textura, x, 0, 100, 100));
            _animaciones.Add(new Animation(frames, TimeSpan.FromSeconds(0.1)));
        }
    }

    public void Mostrar()
    {
        _sprite = new AnimatedSprite(_animaciones[_azar.Next(_animaciones.Count)]);
        _sprite.Origin = new Vector2(50f);
        _sprite.Scale = new Vector2(2f);
    }

    public void Update(GameTime gameTime)
    {
        _sprite.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        _sprite.Position = new Vector2(viewport.Width, viewport.Height) * 0.5f;
        _sprite.Draw(spriteBatch);
    }
}
