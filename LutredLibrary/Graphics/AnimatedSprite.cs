using System;
using Microsoft.Xna.Framework;

namespace LutredLibrary.Graphics;

public class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation _animation;

    public bool IsLooping { get; set; } = true;
    public bool IsFinished { get; private set; }

    public Animation Animation
    {
        get => _animation;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _animation = value;
            _currentFrame = 0;
            _elapsed = TimeSpan.Zero;
            IsFinished = false;

            Region = _animation.Frames[0];
        }
    }

    public AnimatedSprite(Animation animation)
        : base(animation.Frames[0])
    {
        Animation = animation;
    }

    public void Update(GameTime gameTime)
    {
        if (IsFinished)
            return;

        _elapsed += gameTime.ElapsedGameTime;

        while (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;

            if (!IsLooping && _currentFrame == _animation.Frames.Count - 1)
            {
                IsFinished = true;
                _elapsed = TimeSpan.Zero;
                return;
            }

            _currentFrame =
                (_currentFrame + 1) % _animation.Frames.Count;

            Region = _animation.Frames[_currentFrame];
        }
    }
}
