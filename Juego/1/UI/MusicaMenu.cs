using System;
using LutredLibrary;
using Microsoft.Xna.Framework.Audio;

namespace _1.UI;

// Estas pistas pertenecen al menu completo, no a cada escena que lo muestra.
internal sealed class MusicaMenu : IDisposable
{
    private const float DuracionFundido = 0.6f;
    private SoundEffectInstance _inicio;
    private SoundEffectInstance _creditos;
    private bool _enCreditos;
    private float _mezcla;

    public void CambiarEscena(bool esMenu, bool esCreditos)
    {
        if (!esMenu)
        {
            Dispose();
            return;
        }
        if (_inicio == null)
        {
            Core.Audio.StopSong();
            Core.Audio.SongVolume = 0.45f;
            _inicio = Core.Audio.CreateSoundEffectInstance(Core.Content.Load<SoundEffect>("Music/Inicio"),
                isLooped: true, isMusic: true);
            Core.Audio.PlaySoundEffect(_inicio);
        }
        _enCreditos = esCreditos;
        if (esCreditos && _creditos == null)
        {
            _creditos = Core.Audio.CreateSoundEffectInstance(Core.Content.Load<SoundEffect>("Music/Misc/Creditos"),
                volume: 0f, isLooped: true, isMusic: true);
            Core.Audio.PlaySoundEffect(_creditos);
        }
    }

    public void Update(float segundos)
    {
        if (_inicio == null) return;
        _mezcla = Math.Clamp(_mezcla + (_enCreditos ? 1 : -1) * segundos / DuracionFundido, 0f, 1f);
        float suave = _mezcla * _mezcla * (3f - 2f * _mezcla);
        Core.Audio.SetVolume(_inicio, 1f - 0.9f * suave);
        if (_creditos != null)
        {
            Core.Audio.SetVolume(_creditos, suave);
            if (!_enCreditos && _mezcla == 0f)
            {
                Core.Audio.ReleaseSoundEffect(_creditos);
                _creditos = null;
            }
        }
    }

    public void Dispose()
    {
        Core.Audio.ReleaseSoundEffect(_inicio);
        Core.Audio.ReleaseSoundEffect(_creditos);
        _inicio = null;
        _creditos = null;
        _enCreditos = false;
        _mezcla = 0;
    }
}
