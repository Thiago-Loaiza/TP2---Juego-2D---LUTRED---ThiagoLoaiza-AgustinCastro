using System;
using System.Collections.Generic;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.UI;

public sealed class BordeAnimado
{
    private const int AnchoCuadro = 1280;
    private const int AltoCuadro = 720;
    private readonly AnimatedSprite _sprite;

    public BordeAnimado(Texture2D hoja, TimeSpan demora)
    {
        ArgumentNullException.ThrowIfNull(hoja);
        if (hoja.Width % AnchoCuadro != 0 || hoja.Height % AltoCuadro != 0)
            throw new ArgumentException("La hoja debe contener cuadros completos de 1280 por 720.", nameof(hoja));
        var cuadros = new List<TextureRegion>();
        for (int y = 0; y < hoja.Height; y += AltoCuadro)
            for (int x = 0; x < hoja.Width; x += AnchoCuadro)
                cuadros.Add(new TextureRegion(hoja, x, y, AnchoCuadro, AltoCuadro));
        _sprite = new AnimatedSprite(new Animation(cuadros, demora))
        {
            Color = Color.White * 0.5f
        };
    }

    public void Update(GameTime gameTime) => _sprite.Update(gameTime);

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        _sprite.Scale = new Vector2(viewport.Width / (float)AnchoCuadro, viewport.Height / (float)AltoCuadro);
        _sprite.Draw(spriteBatch);
    }
}
