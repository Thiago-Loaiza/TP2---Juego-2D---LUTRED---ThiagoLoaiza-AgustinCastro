using System;
using _1.Persistence;
using LutredLibrary;

namespace _1;

public static class OpcionesJuego
{
    private static readonly GuardadoOpciones Archivo = new();
    private static bool _alternarCorrer;
    private static bool _pendiente;
    private static float _esperaGuardado;
    public static string ErrorGuardado => Archivo.Error;

    public static int VolumenMusica
    {
        get => (int)System.MathF.Round(LutredLibrary.Core.Audio.MusicVolume * 100);
        set
        {
            int volumen = Math.Clamp(value, 0, 100);
            if (volumen == VolumenMusica) return;
            Core.Audio.MusicVolume = volumen / 100f;
            SolicitarGuardado();
        }
    }
    public static int VolumenSfx
    {
        get => (int)System.MathF.Round(LutredLibrary.Core.Audio.EffectsVolume * 100);
        set
        {
            int volumen = Math.Clamp(value, 0, 100);
            if (volumen == VolumenSfx) return;
            Core.Audio.EffectsVolume = volumen / 100f;
            SolicitarGuardado();
        }
    }
    public static bool AlternarCorrer
    {
        get => _alternarCorrer;
        set { if (_alternarCorrer == value) return; _alternarCorrer = value; SolicitarGuardado(); }
    }
    public static bool SilenciarMusica
    {
        get => LutredLibrary.Core.Audio.IsMusicMuted;
        set
        {
            if (SilenciarMusica == value) return;
            Core.Audio.IsMusicMuted = value;
            SolicitarGuardado();
        }
    }

    public static void Cargar()
    {
        DatosOpciones datos = Archivo.Cargar();
        _alternarCorrer = datos.AlternarCorrer;
        Core.Audio.MusicVolume = datos.VolumenMusica / 100f;
        Core.Audio.EffectsVolume = datos.VolumenSfx / 100f;
        Core.Audio.IsMusicMuted = datos.SilenciarMusica;
        _pendiente = false;
    }

    private static void SolicitarGuardado() { _pendiente = true; _esperaGuardado = 0.35f; }

    public static void ActualizarGuardado(float segundos)
    {
        if (!_pendiente) return;
        _esperaGuardado -= segundos;
        if (_esperaGuardado <= 0f) GuardarAhora();
    }

    public static void GuardarAhora()
    {
        if (!_pendiente) return;
        _pendiente = false;
        Archivo.Guardar(new DatosOpciones(AlternarCorrer, SilenciarMusica, VolumenMusica, VolumenSfx));
    }
}
