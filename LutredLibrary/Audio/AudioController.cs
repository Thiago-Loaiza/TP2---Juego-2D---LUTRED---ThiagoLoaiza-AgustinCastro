using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace LutredLibrary.Audio;

public sealed class AudioController : IDisposable
{
    private sealed class ManagedSound
    {
        public SoundEffectInstance Instance;
        public float Volume;
        public bool IsMusic;
        public bool AutoDispose;
    }

    private readonly List<ManagedSound> _sounds = new();
    private float _songVolume = 1f;
    private float _soundEffectVolume = 1f;
    private bool _isMusicMuted;
    private float _musicVolume = 0.5f;
    private float _effectsVolume = 0.5f;
    public bool IsDisposed { get; private set; }
    public bool IsMuted { get; private set; }
    public bool IsPaused { get; private set; }

    public bool IsMusicMuted
    {
        get => _isMusicMuted;
        set { _isMusicMuted = value; ApplyVolumes(); }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set { _musicVolume = Math.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    public float EffectsVolume
    {
        get => _effectsVolume;
        set { _effectsVolume = Math.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    public float SongVolume
    {
        get => IsMuted || IsMusicMuted ? 0f : _songVolume * MusicVolume;
        set { _songVolume = Math.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    public float SoundEffectVolume
    {
        get => IsMuted ? 0f : _soundEffectVolume * EffectsVolume;
        set { _soundEffectVolume = Math.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    // las instancias reutilizables se conservan al detenerse, se liberan cuando la escena deja de necesitarlas.
    public SoundEffectInstance CreateSoundEffectInstance(SoundEffect soundEffect,
        float volume = 1f, bool isLooped = false, bool isMusic = false)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(soundEffect);
        var instance = soundEffect.CreateInstance();
        instance.IsLooped = isLooped;
        var sound = new ManagedSound
        {
            Instance = instance,
            Volume = Math.Clamp(volume, 0f, 1f),
            IsMusic = isMusic
        };
        _sounds.Add(sound);
        ApplyVolume(sound);
        return instance;
    }

    // los efectos de una sola reproduccion se liberan automaticamente cuando terminan.
    public SoundEffectInstance PlaySoundEffect(SoundEffect soundEffect,
        float volume = 1f, float pitch = 0f, float pan = 0f, bool isLooped = false)
    {
        var instance = CreateSoundEffectInstance(soundEffect, volume, isLooped);
        _sounds[^1].AutoDispose = true;
        instance.Pitch = Math.Clamp(pitch, -1f, 1f);
        instance.Pan = Math.Clamp(pan, -1f, 1f);
        PlaySoundEffect(instance);
        return instance;
    }

    public void PlaySoundEffect(SoundEffectInstance instance, bool restart = false)
    {
        GetSound(instance);
        if (restart) instance.Stop();
        instance.Play();
        if (IsPaused) instance.Pause();
    }

    public void StopSoundEffect(SoundEffectInstance instance)
    {
        if (instance != null && !instance.IsDisposed) instance.Stop();
    }

    public void SetVolume(SoundEffectInstance instance, float volume)
    {
        var sound = GetSound(instance);
        sound.Volume = Math.Clamp(volume, 0f, 1f);
        ApplyVolume(sound);
    }

    public void SetPitch(SoundEffectInstance instance, float pitch)
        => GetSound(instance).Instance.Pitch = Math.Clamp(pitch, -1f, 1f);

    public void ReleaseSoundEffect(SoundEffectInstance instance)
    {
        if (instance == null) return;
        int index = _sounds.FindIndex(sound => sound.Instance == instance);
        if (index < 0) return;
        if (!instance.IsDisposed) { instance.Stop(); instance.Dispose(); }
        _sounds.RemoveAt(index);
    }

    public void Update()
    {
        for (int i = _sounds.Count - 1; i >= 0; i--)
        {
            var sound = _sounds[i];
            if (sound.Instance.IsDisposed
                || (sound.AutoDispose && sound.Instance.State == SoundState.Stopped))
                ReleaseSoundEffect(sound.Instance);
        }
    }

    public void PlaySong(Song song, bool isRepeating = true)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        StopSong();
        MediaPlayer.Volume = SongVolume;
        MediaPlayer.IsRepeating = isRepeating;
        MediaPlayer.Play(song);
        if (IsPaused) MediaPlayer.Pause();
    }

    public void StopSong() => MediaPlayer.Stop();

    public void PauseAudio()
    {
        if (IsPaused) return;
        IsPaused = true;
        MediaPlayer.Pause();
        foreach (var sound in _sounds)
            if (!sound.Instance.IsDisposed && sound.Instance.State == SoundState.Playing)
                sound.Instance.Pause();
    }

    public void ResumeAudio()
    {
        if (!IsPaused) return;
        IsPaused = false;
        MediaPlayer.Resume();
        foreach (var sound in _sounds)
            if (!sound.Instance.IsDisposed && sound.Instance.State == SoundState.Paused)
                sound.Instance.Resume();
    }

    public void MuteAudio() { IsMuted = true; ApplyVolumes(); }
    public void UnmuteAudio() { IsMuted = false; ApplyVolumes(); }
    public void ToggleMute() { IsMuted = !IsMuted; ApplyVolumes(); }

    private ManagedSound GetSound(SoundEffectInstance instance)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return _sounds.Find(sound => sound.Instance == instance && !instance.IsDisposed)
            ?? throw new ArgumentException("The instance is not owned by this controller.", nameof(instance));
    }

    private void ApplyVolume(ManagedSound sound)
    {
        if (!sound.Instance.IsDisposed)
            sound.Instance.Volume = sound.Volume * (sound.IsMusic ? SongVolume : SoundEffectVolume);
    }

    private void ApplyVolumes()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        MediaPlayer.Volume = SongVolume;
        foreach (var sound in _sounds) ApplyVolume(sound);
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        StopSong();
        for (int i = _sounds.Count - 1; i >= 0; i--)
            ReleaseSoundEffect(_sounds[i].Instance);
        IsDisposed = true;
    }
}
