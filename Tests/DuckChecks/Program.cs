using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using _1;
using _1.Entities;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Game1 = _1.Game1;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("OK: " + message);
}

static T Field<T>(object instance, string name) =>
    (T)(instance is Game1 ? typeof(Game1) : instance.GetType())
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);

var circle = new CirculoColision(Vector2.Zero, 10f);
Check(circle.Toca(new CirculoColision(new Vector2(20f, 0f), 10f)), "El contacto exacto cuenta.");
Check(!circle.Toca(new CirculoColision(new Vector2(21f, 0f), 10f)), "Los circulos separados no chocan.");
Check(circle.SeCruzan(Vector2.Zero, new CirculoColision(new Vector2(-100f, 0f), 10f),
    new Vector2(100f, 0f)), "Se detecta un cruce rapido entre actualizaciones.");
Check(!circle.SeCruzan(Vector2.Zero, new CirculoColision(new Vector2(-100f, 30f), 10f),
    new Vector2(100f, 30f)), "Pasar cerca sin tocar no produce colision.");

using var game = new ControlledGame();
Core.Content.RootDirectory = Path.GetFullPath(args.Length > 0
    ? args[0] : "Juego/1/Content/bin/DesktopGL/Content");
game.IsFixedTimeStep = false;
game.RunOneFrame();
var sinner = Field<Sinner>(game, "_sinner");
var duck = Field<Duck>(game, "_duck");
Vector2 center = sinner.Colision.Centro;
string output = Path.GetFullPath(args.Length > 1
    ? args[1] : Path.Combine(AppContext.BaseDirectory, "Capturas"));
Directory.CreateDirectory(output);
using (var target = new RenderTarget2D(Core.GraphicsDevice, 1280, 720))
{
    Core.GraphicsDevice.SetRenderTarget(target);
    game.Render();
    Core.GraphicsDevice.SetRenderTarget(null);
    var pixels = new Color[1280 * 720];
    target.GetData(pixels);
    int sinnerPixels = 0;
    int duckPixels = 0;
    for (int y = 280; y < 440; y++)
    {
        for (int x = 560; x < 720; x++)
            if (pixels[y * 1280 + x].R < 200) sinnerPixels++;
        for (int x = 920; x < 1080; x++)
            if (pixels[y * 1280 + x].R < 200) duckPixels++;
    }
    Check(sinnerPixels > 50 && duckPixels > 50, "Sinner y Duck se dibujan en el mundo.");
    using var image = File.Create(Path.Combine(output, "Juego-Duck.png"));
    target.SaveAsPng(image, target.Width, target.Height);
}
duck.Position = center + new Vector2(300f, 0f);
game.Step(1f / 60f);
Check(Vector2.Distance(duck.Position, center) < 300f, "Duck se acerca a Sinner.");
double quack = Field<double>(duck, "_segundosHastaQuack");
Check(quack > 4.9 && quack <= 12.0, "El primer quack espera entre 5 y 12 segundos.");
typeof(Duck).GetField("_segundosHastaQuack", BindingFlags.NonPublic | BindingFlags.Instance)
    .SetValue(duck, 0.0);
game.Step(1f / 60f);
quack = Field<double>(duck, "_segundosHastaQuack");
Check(quack >= 5.0 && quack <= 12.0, "Cada quack programa un intervalo nuevo.");

var sprite = Field<AnimatedSprite>(sinner, "_sprite");
sprite.IsLooping = false;
sinner.CambiarAnimacion(SinnerAnimation.Parry);
typeof(Sinner).GetField("_haciendoParry", BindingFlags.NonPublic | BindingFlags.Instance)
    .SetValue(sinner, true);
duck.Position = center + new Vector2(61f, 0f);
game.Step(1f / 60f);
Check(duck.Rebotando && !Field<bool>(game, "_derrotado"), "El parry tiene prioridad sobre la derrota.");
Check(Field<SoundEffectInstance>(duck, "_sonidoParry").State == SoundState.Playing
    && Field<SoundEffectInstance>(sinner, "_sonidoParryExitoso").State == SoundState.Playing,
    "Se reproducen ambos sonidos del parry exitoso.");
for (int i = 0; i < 60; i++) game.Step(1f / 60f);
Check(Vector2.Distance(duck.Position, center) > 500f, "El rebote aleja mucho a Duck.");
for (int i = 0; i < 10; i++) game.Step(1f / 60f);
Check(!duck.Rebotando, "El rebote termina y permite volver a perseguir.");
float distance = Vector2.Distance(duck.Position, center);
game.Step(1f / 60f);
Check(Vector2.Distance(duck.Position, center) < distance, "Duck retoma la persecucion.");
Check(!sinner.HaciendoParry, "La proteccion termina con la animacion del parry.");

duck.Position = center + new Vector2(61f, 0f);
game.Step(1f / 60f);
Check(Field<bool>(game, "_derrotado"), "Tocar a Sinner sin parry provoca la derrota.");
Check(MediaPlayer.State == MediaState.Stopped, "La musica se detiene al perder.");
Check(Field<SoundEffectInstance>(duck, "_sonidoQuack").State == SoundState.Stopped,
    "Los sonidos de Duck se detienen al perder.");
Vector2 stoppedDuck = duck.Position;
game.Step(0.1f);
Check(duck.Position == stoppedDuck, "Duck deja de actualizarse durante la derrota.");

var defeat = Field<PantallaDerrota>(game, "_pantallaDerrota");
var animations = Field<List<Animation>>(defeat, "_animaciones");
var defeatSprite = Field<AnimatedSprite>(defeat, "_sprite");
Check(animations.Contains(defeatSprite.Animation), "Se elige una de las tres animaciones.");
int[] frameCounts = { 14, 19, 6 };
string[] names = { "Eye", "Flower", "Line" };
for (int i = 0; i < animations.Count; i++)
{
    Check(animations[i].Frames.Count == frameCounts[i], "Cantidad de fotogramas: " + names[i]);
    defeatSprite.Animation = animations[i];
    using var target = new RenderTarget2D(Core.GraphicsDevice, 1280, 720);
    Core.GraphicsDevice.SetRenderTarget(target);
    game.Render();
    Core.GraphicsDevice.SetRenderTarget(null);
    var pixels = new Color[1280 * 720];
    target.GetData(pixels);
    Check(pixels.Count(p => p.R == 0 && p.G == 0 && p.B == 0) > pixels.Length * 0.9,
        "Fondo negro: " + names[i]);
    Check(pixels[360 * 1280 + 640].R > 0
        || pixels.Skip(310 * 1280).Take(100 * 1280).Any(p => p.R > 0),
        "Animacion visible en el centro: " + names[i]);
    Check(defeatSprite.Position == new Vector2(640f, 360f), "Sprite centrado: " + names[i]);
    using var image = File.Create(Path.Combine(output, "Derrota-" + names[i] + ".png"));
    target.SaveAsPng(image, target.Width, target.Height);
    var firstFrame = defeatSprite.Region;
    game.Step(0.11f);
    Check(defeatSprite.Region != firstFrame, "La animacion avanza: " + names[i]);
}
Console.WriteLine("Verificacion de Duck completada.");

class ControlledGame : Game1
{
    public void Step(float seconds) => base.Update(new GameTime(
        TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds)));
    public void Render() => base.Draw(new GameTime());
}
