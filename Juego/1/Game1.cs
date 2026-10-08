using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using LutredLibrary;
using _1.Entities;

namespace _1;

public class Game1 : Core
{
    private Sinner _sinner;

    public Game1() : base("LUTRED", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        _sinner.Position = new Vector2(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height
        ) * 0.5f;
    }

    protected override void LoadContent()
    {
        Song musica = Content.Load<Song>(
            "Music/And the rest was silence - Grace OST"
        );

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0.3f;
        MediaPlayer.Play(musica);

        _sinner = new Sinner();
        _sinner.LoadContent(Content);

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _sinner?.UnloadContent();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back
            == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        _sinner.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _sinner.Draw(SpriteBatch);

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
