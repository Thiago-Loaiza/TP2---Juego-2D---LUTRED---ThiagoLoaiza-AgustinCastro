using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace LutredLibrary.Scenes;

public abstract class Escena : IDisposable
{
    protected ContentManager Content { get; } = new ContentManager(
        Core.Content.ServiceProvider, Core.Content.RootDirectory);

    private bool _disposed;

    public virtual void Initialize() => LoadContent();
    protected virtual void LoadContent() { }
    public virtual void Update(GameTime gameTime) { }
    public abstract void Draw(GameTime gameTime);
    protected virtual void UnloadContent() { }

    public void Dispose()
    {
        if (_disposed)
            return;

        UnloadContent();
        Content.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
