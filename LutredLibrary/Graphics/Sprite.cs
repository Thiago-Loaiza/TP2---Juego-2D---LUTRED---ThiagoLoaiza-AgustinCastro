using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LutredLibrary.Graphics;

public class Sprite
{
    public TextureRegion Region { get; set; }
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Color Color { get; set; } = Color.White;
    public float Rotation { get; set; } = 0f;
    public Vector2 Origin { get; set; } = Vector2.Zero;
    public Vector2 Scale { get; set; } = Vector2.One;
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;
    public float LayerDepth { get; set; } = 0f;

    public Sprite(TextureRegion region)
    {
        Region = region;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            Region.Texture,
            Position,
            Region.Bounds,
            Color,
            Rotation,
            Origin,
            Scale,
            Effects,
            LayerDepth
        );
    }
}