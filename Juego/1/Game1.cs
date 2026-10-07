using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework.Media;

namespace _1;

public class Game1 : Core
{
    private AnimatedSprite _sinner;

    public Game1() : base("LUTRED", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        Song musica = Content.Load<Song>(
            "Music/And the rest was silence - Grace OST"
        );

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0.3f;
        MediaPlayer.Play(musica);
        
        Texture2D texturaSinner = Content.Load<Texture2D>(
            "Sprites/Sinner/Sinner-Sheet"
        );

        List<TextureRegion> frames = new List<TextureRegion>
        {
            new TextureRegion(texturaSinner,   0, 0, 300, 300),
            new TextureRegion(texturaSinner, 300, 0, 300, 300),
            new TextureRegion(texturaSinner, 600, 0, 300, 300)
        };

        Animation animacion = new Animation(
            frames,
            TimeSpan.FromSeconds(0.4)
        );

        _sinner = new AnimatedSprite(animacion)
        {
            Position = new Vector2(490, 210)
        };

        base.LoadContent();
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
        GraphicsDevice.Clear(Color.White); //Pone el color blanco

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _sinner.Draw(SpriteBatch);

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}