using System;
using System.Collections.Generic;
using _1.Colisiones;
using _1.Progression;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public enum ResultadoDupe { Nada, Parreado, Contacto }

public sealed class Dupe
{
    private readonly double _retraso;
    private readonly Dictionary<SinnerAnimation, Animation> _animaciones = new();
    private RastroDupe _rastro;
    private AnimatedSprite _sprite;
    private SinnerAnimation _animacion;
    private readonly EstadoDupe _estado = new();
    private bool _aparecio;
    private bool _visibleAnterior;
    private SoundEffectInstance _aparicion, _movimiento, _ataque, _parry;
    public Vector2 Position => _sprite.Position;
    public bool Visible => _rastro?.Preparado == true && _estado.OcultoRestante <= 0;
    private CirculoColision Colision => new(Position + new Vector2(0, 15), 24);

    public Dupe(int indice)
    {
        if (indice < 0) throw new ArgumentOutOfRangeException(nameof(indice));
        _retraso = 1.0 + indice * 0.5;
    }

    public void LoadContent(ContentManager content)
    {
        Animation Cargar(string nombre, double intervalo)
        {
            Texture2D textura = content.Load<Texture2D>($"Sprites/Enemies/Dupe/{nombre}-Sheet");
            var cuadros = new List<TextureRegion>();
            for (int x = 0; x < textura.Width; x += 300)
                cuadros.Add(new TextureRegion(textura, x, 0, 300, 300));
            return new Animation(cuadros, TimeSpan.FromSeconds(intervalo));
        }
        _animaciones[SinnerAnimation.Reposo] = Cargar("Dupe", 0.4);
        _animaciones[SinnerAnimation.Caminata] = Cargar("DupeWalk", 0.12);
        _animaciones[SinnerAnimation.CaminataArriba] = Cargar("DupeWalkUP", 0.12);
        _animaciones[SinnerAnimation.CaminataAbajo] = _animaciones[SinnerAnimation.CaminataArriba];
        _animaciones[SinnerAnimation.Correr] = Cargar("DupeRUN", 0.08);
        _animaciones[SinnerAnimation.CorrerArriba] = Cargar("DupeRUN_UP", 0.08);
        _animaciones[SinnerAnimation.CorrerAbajo] = Cargar("DupeRUN_DOWN", 0.08);
        _sprite = new AnimatedSprite(_animaciones[SinnerAnimation.Reposo])
        {
            Origin = new Vector2(150), Scale = new Vector2(0.5f)
        };
        _aparicion = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>("SFX/Entities/Dupe/Spawn"), volume: 0.45f);
        _movimiento = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>("SFX/Entities/Dupe/Moving"), volume: 0.12f, isLooped: true);
        _ataque = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>("SFX/Entities/Dupe/Attack"), volume: 0.55f);
        _parry = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>("SFX/Entities/Dupe/DupeParry"), volume: 0.55f);
    }

    public ResultadoDupe Update(GameTime gameTime, Sinner sinner, Vector2 inicioSinner, bool puedeTransmitir)
    {
        double segundos = gameTime.ElapsedGameTime.TotalSeconds;
        var actual = new PasoDupe(sinner.Position, sinner.AnimacionMovimiento, sinner.Orientacion);
        _rastro ??= new RastroDupe(_retraso, actual with { Posicion = inicioSinner - new Vector2(0, 15) });
        Vector2 anterior = Position;
        PasoDupe paso = _rastro.Actualizar(segundos, actual);
        _sprite.Position = paso.Posicion;
        _sprite.Effects = paso.Orientacion;
        if (_animacion != paso.Animacion)
        {
            _animacion = paso.Animacion;
            _sprite.Animation = _animaciones[_animacion];
        }
        _sprite.Update(gameTime);
        _estado.Actualizar(segundos);
        if (!Visible)
        {
            _visibleAnterior = false;
            Core.Audio.StopSoundEffect(_movimiento);
            return ResultadoDupe.Nada;
        }
        if (!_visibleAnterior)
        {
            anterior = Position; // No barrer desde el origen o desde una aparicion antigua.
            _aparecio = false;
        }
        _visibleAnterior = true;
        if (!_aparecio)
        {
            _aparecio = true;
            Core.Audio.PlaySoundEffect(_aparicion, restart: true);
        }
        if (_animacion != SinnerAnimation.Reposo)
        {
            if (_movimiento.State != SoundState.Playing) Core.Audio.PlaySoundEffect(_movimiento);
        }
        else Core.Audio.StopSoundEffect(_movimiento);
        if (!_estado.PuedeTocar || sinner.DashActivo) return ResultadoDupe.Nada;
        if (sinner.PuedeParrear && sinner.ColisionParry.SeCruzan(inicioSinner, Colision, anterior + new Vector2(0, 15)))
        {
            _estado.Parrear();
            Core.Audio.PlaySoundEffect(_parry, restart: true);
            return ResultadoDupe.Parreado;
        }
        if (puedeTransmitir && sinner.Colision.SeCruzan(inicioSinner, Colision, anterior + new Vector2(0, 15)))
        {
            _estado.Desaparecer();
            _visibleAnterior = false;
            Core.Audio.StopSoundEffect(_movimiento);
            Core.Audio.PlaySoundEffect(_ataque, restart: true);
            return ResultadoDupe.Contacto;
        }
        return ResultadoDupe.Nada;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!Visible) return;
        _sprite.Color = _estado.ParreadoRestante > 0
            ? new Color(85, 20, 115) * (0.3f + 0.7f * (float)(0.5 + 0.5 * Math.Cos(_estado.ParreadoRestante * Math.PI * 12)))
            : Color.White;
        _sprite.Draw(spriteBatch);
    }

    public void DetenerSonidos()
    {
        Core.Audio.StopSoundEffect(_aparicion);
        Core.Audio.StopSoundEffect(_movimiento);
        Core.Audio.StopSoundEffect(_ataque);
        Core.Audio.StopSoundEffect(_parry);
    }

    public void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_aparicion);
        Core.Audio.ReleaseSoundEffect(_movimiento);
        Core.Audio.ReleaseSoundEffect(_ataque);
        Core.Audio.ReleaseSoundEffect(_parry);
    }
}
