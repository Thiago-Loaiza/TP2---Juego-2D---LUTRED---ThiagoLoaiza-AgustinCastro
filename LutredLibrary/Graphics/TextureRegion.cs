using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LutredLibrary.Graphics;

public class TextureRegion
{
    public Texture2D Texture { get; }
    public Rectangle Bounds { get; }

    public int Width => Bounds.Width;
    public int Height => Bounds.Height;

    public TextureRegion(
        Texture2D texture,
        int x,
        int y,
        int width,
        int height)
    {
        Texture = texture;
        Bounds = new Rectangle(x, y, width, height);
    }
}