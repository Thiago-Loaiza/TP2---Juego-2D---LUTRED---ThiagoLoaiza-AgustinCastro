namespace _1;

public static class OpcionesJuego
{
    public static int VolumenMusica
    {
        get => (int)System.MathF.Round(LutredLibrary.Core.Audio.MusicVolume * 100);
        set => LutredLibrary.Core.Audio.MusicVolume = value / 100f;
    }
    public static int VolumenSfx
    {
        get => (int)System.MathF.Round(LutredLibrary.Core.Audio.EffectsVolume * 100);
        set => LutredLibrary.Core.Audio.EffectsVolume = value / 100f;
    }
    public static bool AlternarCorrer { get; set; }
    public static bool SilenciarMusica
    {
        get => LutredLibrary.Core.Audio.IsMusicMuted;
        set => LutredLibrary.Core.Audio.IsMusicMuted = value;
    }
}
