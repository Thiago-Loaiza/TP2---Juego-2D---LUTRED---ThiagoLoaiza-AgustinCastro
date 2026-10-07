using System;
using System.Collections.Generic;

namespace LutredLibrary.Graphics;

public class Animation
{
    public List<TextureRegion> Frames { get; }
    public TimeSpan Delay { get; }

    public Animation(List<TextureRegion> frames, TimeSpan delay)
    {
        if (frames == null || frames.Count == 0)
            throw new ArgumentException(
                "La animación necesita al menos un fotograma.",
                nameof(frames)
            );

        if (delay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(delay),
                "La duración de cada fotograma debe ser positiva."
            );

        Frames = frames;
        Delay = delay;
    }
}