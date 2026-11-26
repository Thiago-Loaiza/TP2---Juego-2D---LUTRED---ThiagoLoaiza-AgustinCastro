using System;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Entities;
using _1.Progression;
using _1.UI;
using LutredLibrary;
using LutredLibrary.Graphics;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Scenes;

// Antes del primer layer: un recinto sin mapa procedural, reloj ni enemigos.
public sealed class Previa : Escena
{
    private const float RadioRecinto = 1020f;
    private const float RadioApertura = 170f;
    private const float DuracionEntrada = 1f;
    private const float DuracionPuerta = 0.65f;
    private const float DuracionSalida = 1f;
    private const float VolumenPrologo = 0.45f;
    private static readonly float[] DistanciasAcercamiento = { 280f, 220f, 155f, 85f };
    private static readonly Vector2 Centro = Vector2.Zero;
    private static readonly Vector2 Puerta = new(0f, -600f);
    private readonly Action<Escena> _cambiarEscena;
    private readonly EstadoMejoras _mejoras;
    private readonly Camara2D _camara = new();
    private readonly Sinner _sinner = new();
    private readonly Texture2D[] _nubes = new Texture2D[3];
    private readonly List<(Vector2 Posicion, float Radio, int Variante, int Cuadro, float Fase, float Vida, float Elevacion)> _borde = new();
    private readonly SoundEffectInstance[] _acercamientos = new SoundEffectInstance[4];
    private SoundEffectInstance _abrir, _cerrar, _musicaPrologo;
    private Texture2D _pixel;
    private AnimatedSprite _spritePuerta;
    private TextoLinterna _textoLinterna;
    private float _reloj, _edadEntrada, _apertura, _edadSalida;
    private int _nivelAcercamiento;
    private bool _cerca, _atravesada, _cerrandoTrasEntrada, _saliendo;

    public Previa(Action<Escena> cambiarEscena, EstadoMejoras mejoras)
    {
        _cambiarEscena = cambiarEscena;
        _mejoras = mejoras;
    }

    protected override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _textoLinterna = new TextoLinterna(Content, _pixel);
        Texture2D hojaPuerta = Content.Load<Texture2D>("Sprites/Misc/Puerta-Sheet");
        var cuadrosPuerta = new List<TextureRegion>();
        // La hoja original va de abierta a cerrada; invertirla para animar la apertura.
        for (int i = 2; i >= 0; i--)
            cuadrosPuerta.Add(new TextureRegion(hojaPuerta, i * 300, 0, 300, 300));
        _spritePuerta = new AnimatedSprite(new Animation(cuadrosPuerta, TimeSpan.FromSeconds(DuracionPuerta / 3f)))
        {
            Position = Puerta,
            Origin = new Vector2(153f, 208f),
            Scale = new Vector2(1.4f),
            IsLooping = false
        };
        _nubes[0] = Content.Load<Texture2D>("Misc/Nubes/NubePequena-Sheet");
        _nubes[1] = Content.Load<Texture2D>("Misc/Nubes/NubeMediana-Sheet");
        _nubes[2] = Content.Load<Texture2D>("Misc/Nubes/NubeGrande-Sheet");
        CrearBordeNubes();
        for (int i = 0; i < _acercamientos.Length; i++)
            _acercamientos[i] = Core.Audio.CreateSoundEffectInstance(
                Content.Load<SoundEffect>($"SFX/Puerta/Acercamiento{i + 1}"), volume: 0.65f);
        _abrir = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>("SFX/Puerta/PuertaAbriendose"));
        _cerrar = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>("SFX/Puerta/PuertaCerrandose"));
        _sinner.LoadContent(Content);
        _sinner.Teletransportar(new Vector2(0f, 580f));
        _camara.Seguir(_sinner.Position);
        Core.Audio.SongVolume = 0f;
        _musicaPrologo = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Prologo"), isLooped: true, isMusic: true);
        Core.Audio.PlaySoundEffect(_musicaPrologo);
    }

    private void CrearBordeNubes()
    {
        // Cubrir todo lo que la camara puede ver fuera del recinto.
        // Las capas exteriores son densas y las interiores tienen contornos irregulares.
        for (int anillo = 5; anillo >= 0; anillo--)
        {
            float distancia = RadioRecinto + 100f + anillo * 145f;
            int cantidad = (int)MathF.Ceiling(MathHelper.TwoPi * distancia / (anillo == 0 ? 45f : 80f));
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = (i + anillo * 0.5f + Random.Shared.NextSingle() * 0.5f) * MathHelper.TwoPi / cantidad;
                float radio = distancia + MathHelper.Lerp(-32f, 32f, Random.Shared.NextSingle());
                int variante = Random.Shared.Next(3);
                float tamano = variante == 0 ? MathHelper.Lerp(35f, 65f, Random.Shared.NextSingle())
                    : variante == 1 ? MathHelper.Lerp(70f, 115f, Random.Shared.NextSingle())
                    : MathHelper.Lerp(120f, 190f, Random.Shared.NextSingle());
                if (anillo > 0) tamano = Math.Max(tamano, 100f);
                if (anillo == 0) radio = Math.Max(radio, RadioRecinto + tamano + 8f);
                Vector2 posicion = Centro + new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * radio;
                _borde.Add((posicion, tamano, variante, Random.Shared.Next(9),
                    Random.Shared.NextSingle() * MathHelper.TwoPi, 0f, 0f));
            }
        }
        // El humo que se desprende asciende y se desvanece sin desplazar la barrera fija.
        for (int i = 0; i < 160; i++)
        {
            float angulo = Random.Shared.NextSingle() * MathHelper.TwoPi;
            float distancia = RadioRecinto + MathHelper.Lerp(90f, 380f, Random.Shared.NextSingle());
            Vector2 posicion = Centro + new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * distancia;
            int variante = Random.Shared.Next(3);
            float tamano = MathHelper.Lerp(28f, 110f, Random.Shared.NextSingle());
            _borde.Add((posicion, tamano, variante, Random.Shared.Next(9),
                Random.Shared.NextSingle() * MathHelper.TwoPi,
                MathHelper.Lerp(3f, 7f, Random.Shared.NextSingle()),
                MathHelper.Lerp(80f, 230f, Random.Shared.NextSingle())));
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _reloj = (_reloj + dt) % 1000f;
        if (_edadEntrada < DuracionEntrada)
        {
            _edadEntrada += dt;
            Core.Audio.SongVolume = VolumenPrologo * MathHelper.Clamp(_edadEntrada / DuracionEntrada, 0f, 1f);
            _sinner.SincronizarEntrada();
            return;
        }
        if (_saliendo)
        {
            _edadSalida += dt;
            Core.Audio.SongVolume = VolumenPrologo * (1f - MathHelper.Clamp(_edadSalida / DuracionSalida, 0f, 1f));
            if (_edadSalida >= DuracionSalida) IniciarRun(_cambiarEscena, _mejoras);
            return;
        }
        if (_atravesada)
        {
            if (!_cerrandoTrasEntrada)
            {
                _apertura = Math.Min(1f, _apertura + dt / DuracionPuerta);
                if (_apertura >= 1f)
                {
                    _cerrandoTrasEntrada = true;
                    Core.Audio.PlaySoundEffect(_cerrar, restart: true);
                }
            }
            else
            {
                _apertura = Math.Max(0f, _apertura - dt / DuracionPuerta);
                if (_apertura <= 0f) _saliendo = true;
            }
            return;
        }

        Vector2 anterior = _sinner.Colision.Centro;
        _sinner.Update(gameTime);
        _sinner.LimitarAreaCircular(Centro, RadioRecinto);
        _camara.Seguir(_sinner.Position);
        float distanciaPuerta = Vector2.Distance(_sinner.Colision.Centro, Puerta);
        ActualizarAcercamiento(distanciaPuerta);
        bool cerca = distanciaPuerta <= RadioApertura;
        if (cerca != _cerca)
        {
            _cerca = cerca;
            if (cerca)
            {
                Core.Audio.PlaySoundEffect(_abrir, restart: true);
            }
            else Core.Audio.PlaySoundEffect(_cerrar, restart: true);
        }
        _apertura = Math.Clamp(_apertura + (cerca ? 1f : -1f) * dt / DuracionPuerta, 0f, 1f);
        var umbral = new CirculoColision(Puerta, 24f);
        if (_sinner.Colision.SeCruzan(anterior, umbral, Puerta))
        {
            _atravesada = true;
            _sinner.DetenerSonidos();
            if (_apertura >= 1f)
            {
                _cerrandoTrasEntrada = true;
                Core.Audio.PlaySoundEffect(_cerrar, restart: true);
            }
        }
    }

    private void ActualizarAcercamiento(float distancia)
    {
        int nivel = 0;
        while (nivel < DistanciasAcercamiento.Length && distancia <= DistanciasAcercamiento[nivel])
            nivel++;
        if (nivel > _nivelAcercamiento)
        {
            // Los sonidos se encadenan por proximidad y no vuelven a sonar estando quieto.
            foreach (SoundEffectInstance sonido in _acercamientos)
                Core.Audio.StopSoundEffect(sonido);
            Core.Audio.PlaySoundEffect(_acercamientos[nivel - 1], restart: true);
            _nivelAcercamiento = nivel;
        }
    }

    internal static void IniciarRun(Action<Escena> cambiarEscena, EstadoMejoras mejoras)
    {
        if (mejoras.FastTrack)
            cambiarEscena(new Partida(cambiarEscena, mejoras: mejoras));
        else if (Game1.EmpezarConMejoras)
            cambiarEscena(new Mejoras(() => cambiarEscena(new Seleccion(cambiarEscena, mejoras: mejoras)), mejoras));
        else
            cambiarEscena(Game1.EmpezarConEleccion
                ? new Seleccion(cambiarEscena, mejoras: mejoras)
                : new Partida(cambiarEscena, mejoras: mejoras));
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.White);
        var batch = Core.SpriteBatch;
        var viewport = Game1.VistaPartida;
        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camara.ObtenerTransformacion(viewport));
        foreach (var nube in _borde)
        {
            Vector2 posicion = nube.Posicion;
            float opacidad = 1f;
            float radio = nube.Radio + MathF.Sin(_reloj * 1.6f + nube.Fase) * 4f;
            if (nube.Vida > 0f)
            {
                float progreso = (_reloj / nube.Vida + nube.Fase / MathHelper.TwoPi) % 1f;
                posicion += new Vector2(MathF.Sin(progreso * MathHelper.TwoPi + nube.Fase) * 22f,
                    -progreso * nube.Elevacion);
                opacidad = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progreso / 0.15f, 0f, 1f))
                    * (1f - MathHelper.SmoothStep(0f, 1f, Math.Clamp((progreso - 0.55f) / 0.45f, 0f, 1f)));
                radio *= 1f + progreso * 0.2f;
            }
            // Ni las nubes que ascienden ni su borde pueden invadir la zona blanca.
            float distanciaMinima = RadioRecinto + radio * 1.05f;
            if (posicion.LengthSquared() < distanciaMinima * distanciaMinima)
                posicion = Vector2.Normalize(posicion) * distanciaMinima;
            // Evitar enviar al SpriteBatch cientos de nubes fuera de la vista.
            if (Math.Abs(posicion.X - _camara.Posicion.X) > viewport.Width * 0.5f + radio
                || Math.Abs(posicion.Y - _camara.Posicion.Y) > viewport.Height * 0.5f + radio) continue;
            Texture2D textura = _nubes[nube.Variante];
            int ancho = textura.Width / 9;
            int cuadro = (nube.Cuadro + (int)(_reloj / 0.12f)) % 9;
            batch.Draw(textura, posicion, new Rectangle(cuadro * ancho, 0, ancho, textura.Height),
                Color.White * opacidad, 0f, new Vector2(ancho, textura.Height) * 0.5f, radio * 2f / ancho, SpriteEffects.None, 0f);
        }
        batch.End();
        Matrix transformacion = _camara.ObtenerTransformacion(viewport);
        if (!_atravesada)
            _textoLinterna.Draw(Puerta + new Vector2(0f, 125f), _sinner.Colision.Centro, transformacion);
        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transformacion);
        if (!_atravesada && _sinner.Colision.Centro.Y < Puerta.Y) _sinner.Draw(batch);
        DibujarPuerta(batch);
        if (!_atravesada && _sinner.Colision.Centro.Y >= Puerta.Y) _sinner.Draw(batch);
        batch.End();

        float alfa = _saliendo
            ? MathHelper.SmoothStep(0f, 1f, Math.Clamp(_edadSalida / DuracionSalida, 0f, 1f))
            : 1f - MathHelper.SmoothStep(0f, 1f, Math.Clamp(_edadEntrada / DuracionEntrada, 0f, 1f));
        if (alfa <= 0f) return;
        batch.Begin(blendState: BlendState.AlphaBlend);
        batch.Draw(_pixel, viewport.Bounds, Color.Black * alfa);
        batch.End();
    }

    private void DibujarPuerta(SpriteBatch batch)
    {
        // El progreso puede invertirse en cualquier momento si Sinner se aleja.
        int cuadro = Math.Clamp((int)MathF.Round(_apertura * 2f), 0, 2);
        _spritePuerta.Region = _spritePuerta.Animation.Frames[cuadro];
        _spritePuerta.Color = _saliendo
            ? Color.Lerp(Color.White, Color.Black, Math.Clamp(_edadSalida / 0.5f, 0f, 1f))
            : Color.White;
        _spritePuerta.Draw(batch);
    }

    protected override void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_musicaPrologo);
        _sinner.UnloadContent();
        _textoLinterna?.Dispose();
        _pixel?.Dispose();
        foreach (var sonido in _acercamientos) Core.Audio.ReleaseSoundEffect(sonido);
        Core.Audio.ReleaseSoundEffect(_abrir);
        Core.Audio.ReleaseSoundEffect(_cerrar);
    }
}
