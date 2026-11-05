using System;
using _1.Entities;
using _1.UI;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Scenes;

public partial class Partida
{
    protected override void LoadContent()
    {
        // La transparencia crece hacia abajo; el filtrado lineal suaviza el degradado.
        _degradadoFondo = new Texture2D(Core.GraphicsDevice, 1, 256);
        var coloresFondo = new Color[256];
        for (int y = 0; y < coloresFondo.Length; y++)
            coloresFondo[y] = Color.White * (y / 255f);
        _degradadoFondo.SetData(coloresFondo);
        _fuenteContador = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        Puntaje.ComenzarNivel();
        _marcador = new MarcadorPuntaje(Content, Puntaje);
        _indicadorOrbes = Content.Load<Texture2D>("Sprites/Misc/Orbs/IndicadorPuntos");
        _animacionOrbe = Orbe.CrearAnimacion(
            Content.Load<Texture2D>("Sprites/Misc/Orbs/Punto-Sheet"));
        _tintaAntigua = Content.Load<Effect>("Effects/TintaAntigua");
        _coloresInvertidos = Content.Load<Effect>("Effects/ColoresInvertidos");
        // aumentar el umbral produce mas negro, aumentar el contraste reduce los grises.
        _tintaAntigua.Parameters["Umbral"].SetValue(0.80f);
        _tintaAntigua.Parameters["Contraste"].SetValue(60f);
        _tintaAntigua.Parameters["Grano"].SetValue(0.10f);
        _tintaAntigua.Parameters["EscalaGrano"].SetValue(512f);
        _tintaAntigua.Parameters["GrosorTinta"].SetValue(3f);

        int layer = _numeroNivel <= CantidadLayers ? _numeroNivel : 4 + (_numeroNivel - 9) % 5;
        int transicionEntrada = (_numeroNivel - 1) % CantidadTransiciones + 1;
        int transicionSalida = _numeroNivel % CantidadTransiciones + 1;
        _sonidoEntrada = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>($"Music/Layers/Transicciones/Iniciador{transicionEntrada}"), isMusic: true);
        _sonidoSalida = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>($"Music/Layers/Transicciones/Iniciador{transicionSalida}"), isMusic: true);
        _musicaNivel = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>($"Music/Layers/Layers/Layer{layer}"), isLooped: true, isMusic: true);
        _pixelTransicion = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixelTransicion.SetData(new[] { Color.White });
        _bordePantalla = new BordeAnimado(
            Content.Load<Texture2D>("Sprites/UI/BorderSreen/EfectoBorde-Sheet"), TimeSpan.FromSeconds(0.12));

        _sinner = new Sinner { Mejoras = _mejoras };
        _sinner.LoadContent(Content);
        for (int i = 0; i < _cantidadDupe; i++)
        {
            var dupe = new Dupe(i);
            dupe.LoadContent(Content);
            _dupes.Add(dupe);
        }
        _shame = new Shame();
        _shame.LoadContent(Content);
        _avisoShame = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("SFX/Entities/SHAME/AvisoShame"), volume: 0.8f);

        for (int i = 0; i < _cantidadDuke; i++)
        {
            var duke = new Duck();
            duke.LoadContent(Content);
            _dukes.Add(duke);
        }
        for (int i = 0; i < _cantidadJarJar; i++)
        {
            var jarJar = new JarJar();
            jarJar.LoadContent(Content);
            _jarJars.Add(jarJar);
        }
        if (_sorakElegido)
        {
            _zorak = new Zorak(_mejoras.Cantidad("Paz") > 0);
            _zorak.LoadContent(Content);
        }
        if (_erosionBirdElegido)
        {
            _erosionBird = new ErosionBird(_mejoras.Cantidad("Paz") > 0);
            _erosionBird.LoadContent(Content);
        }
        if (_delusionElegido)
        {
            _delusion = new Delusion();
            _delusion.LoadContent(Content);
        }
        if (_tooManyParriesElegido)
        {
            _tooManyParries = new TooManyParries();
            _tooManyParries.LoadContent(Content);
        }
        _pantallaDerrota = new PantallaDerrota();
        _pantallaDerrota.LoadContent(Content);

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _marcador?.Dispose();
        _sinner?.UnloadContent();
        foreach (Dupe dupe in _dupes) dupe.UnloadContent();
        _dupes.Clear();
        _shame?.UnloadContent();
        Core.Audio.ReleaseSoundEffect(_avisoShame);
        for (int i = 0; i < _dukes.Count; i++)
        {
            _dukes[i].UnloadContent();
        }
        for (int i = 0; i < _jarJars.Count; i++)
        {
            _jarJars[i].UnloadContent();
        }
        _dukes.Clear();
        _jarJars.Clear();
        if (_volverInicio != null)
        {
            _volverInicio.Click -= VolverAlInicio;
            _volverInicio.Dispose();
        }
        _zorak?.UnloadContent();
        _erosionBird?.UnloadContent();
        Core.Audio.StopSong();
        Core.Audio.ReleaseSoundEffect(_sonidoEntrada);
        Core.Audio.ReleaseSoundEffect(_sonidoSalida);
        Core.Audio.ReleaseSoundEffect(_musicaNivel);
        Core.Audio.ReleaseSoundEffect(_musicaFellDoor);
        _pixelTransicion?.Dispose();
        _degradadoFondo?.Dispose();
        base.UnloadContent();
    }
}
