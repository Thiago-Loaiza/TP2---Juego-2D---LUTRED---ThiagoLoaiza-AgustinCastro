using LutredLibrary;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using _1.Colisiones;

namespace _1.Entities;

public enum SinnerAnimation
{
    Reposo,
    Caminata,
    CaminataArriba,
    Parry,
    Correr,
    CorrerArriba
}

public class Sinner
{
    private SoundEffectInstance _sonidoCaminata;
    private SoundEffectInstance _sonidoParry;
    private SoundEffectInstance _sonidoParryExitoso;
    private AnimatedSprite _efectoParry;
    private KeyboardState _tecladoAnterior;
    private bool _haciendoParry;
    private const double SegundosPorFrameParry = 0.05;
    private const float Velocidad = 220f;
    private const float MultiplicadorSprint = 2.5f;
    private const float Aceleracion = 1800f;
    private const float Frenado = 2200f;
    private Vector2 _velocidadActual;
    private AnimatedSprite _sprite;

    public bool UsarInercia { get; set; } = true;
    public bool HaciendoParry => _haciendoParry;
    public bool ParryIniciadoEsteFrame { get; private set; }
    public Vector2 VelocidadActual => _velocidadActual;
    public CirculoColision Colision => new(Position + new Vector2(0f, 15f), 24f);
    public CirculoColision ColisionParry => new(Colision.Centro, 28f);

    private readonly Dictionary<SinnerAnimation, Animation>
        _animaciones = new();

    private SinnerAnimation _animacionActual;

    public Vector2 Position
    {
        get => _sprite.Position;
        set => _sprite.Position = value;
    }

    public void LoadContent(ContentManager content)
    {
        _animaciones[SinnerAnimation.Reposo] = CrearAnimacion(
            content,
            "Sprites/Sinner/Sinner-Sheet",
            3,
            0.4
        );

        _animaciones[SinnerAnimation.Caminata] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerWalk-Sheet",
            5,
            0.12
        );

        _animaciones[SinnerAnimation.CaminataArriba] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerWalkUP-Sheet",
            6,
            0.12
        );

        _animaciones[SinnerAnimation.Parry] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerParryFront-Sheet",
            5,
            SegundosPorFrameParry
        );

        _animaciones[SinnerAnimation.Correr] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerRun-Sheet",
            8,
            0.12
        );

        _animaciones[SinnerAnimation.CorrerArriba] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerRunUP-Sheet",
            10,
            0.12
        );

        _animacionActual = SinnerAnimation.Reposo;

        _sprite = new AnimatedSprite(
            _animaciones[_animacionActual]
        );
        _sprite.Origin = new Vector2(150,150);
        _sprite.Scale = new Vector2(0.5f);
        SoundEffect sonido = content.Load<SoundEffect>(
            "SFX/Entities/Sinner/Caminata"
        );

        _sonidoCaminata = Core.Audio.CreateSoundEffectInstance(sonido, isLooped: true);
        Core.Audio.SetVolume(_sonidoCaminata, 0.4f);

        _sonidoParry = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>(
            "SFX/Entities/Sinner/Parry"
        ));
        Core.Audio.SetVolume(_sonidoParry, 0.6f);

        _sonidoParryExitoso = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>(
            "SFX/Entities/Sinner/ParryExitoso"
        ));
        Core.Audio.SetVolume(_sonidoParryExitoso, 0.6f);

        _efectoParry = new AnimatedSprite(CrearAnimacion(
            content,
            "Effects/EstrellaParry-Sheet",
            4,
            SegundosPorFrameParry * 5 / 4,
            50,
            50
        ));
        _efectoParry.Position = Position;
        _efectoParry.IsLooping = false;
        _efectoParry.Origin = new Vector2(25, 25);
        _efectoParry.Scale = new Vector2(0.7f, 0.7f);
        _tecladoAnterior = Keyboard.GetState();
    }

    private static Animation CrearAnimacion(
        ContentManager content,
        string ruta,
        int cantidadFrames,
        double segundosPorFrame,
        int anchoFrame = 300,
        int altoFrame = 300)
    {
        Texture2D textura = content.Load<Texture2D>(ruta);

        List<TextureRegion> frames = new();

        for (int i = 0; i < cantidadFrames; i++)
        {
            frames.Add(new TextureRegion(
                textura,
                i * anchoFrame,
                0,
                anchoFrame,
                altoFrame
            ));
        }

        return new Animation(
            frames,
            TimeSpan.FromSeconds(segundosPorFrame)
        );
    }

    public void CambiarAnimacion(SinnerAnimation animacion)
    {
        if (_animacionActual == animacion)
            return;

        _animacionActual = animacion;
        _sprite.Animation = _animaciones[animacion];
    }

    public void Update(GameTime gameTime, IReadOnlyList<ColisionSolida> obstaculos = null)
    {
        KeyboardState teclado = Keyboard.GetState();
        ParryIniciadoEsteFrame = false;

        bool parryPresionado = teclado.IsKeyDown(Keys.F)
            && _tecladoAnterior.IsKeyUp(Keys.F);
        _tecladoAnterior = teclado;

        if (_haciendoParry && _sprite.IsFinished)
        {
            _haciendoParry = false;
            _sprite.IsLooping = true;
            CambiarAnimacion(SinnerAnimation.Reposo);
        }

        if (parryPresionado && !_haciendoParry)
        {
            ParryIniciadoEsteFrame = true;
            _haciendoParry = true;
            _sprite.IsLooping = false;
            CambiarAnimacion(SinnerAnimation.Parry);
            _efectoParry.Animation = _efectoParry.Animation;
            Core.Audio.StopSoundEffect(_sonidoParry);
            Core.Audio.PlaySoundEffect(_sonidoParry);
        }

        Vector2 direccion = Vector2.Zero;

        if (teclado.IsKeyDown(Keys.A)) direccion.X -= 1;
        if (teclado.IsKeyDown(Keys.D)) direccion.X += 1;
        if (teclado.IsKeyDown(Keys.W)) direccion.Y -= 1;
        if (teclado.IsKeyDown(Keys.S)) direccion.Y += 1;

        if (direccion != Vector2.Zero)
            direccion.Normalize();

        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool corriendo = teclado.IsKeyDown(Keys.LeftShift);
        Vector2 velocidadObjetivo = direccion * Velocidad
            * (corriendo ? MultiplicadorSprint : 1f);

        if (UsarInercia)
        {
            bool reduciendoVelocidad = velocidadObjetivo.LengthSquared()
                < _velocidadActual.LengthSquared();
            float cambioPorSegundo = reduciendoVelocidad ? Frenado : Aceleracion;
            _velocidadActual = AcercarVelocidad(
                _velocidadActual, velocidadObjetivo, cambioPorSegundo * segundos);
        }
        else
        {
            _velocidadActual = velocidadObjetivo;
        }

        Vector2 movimiento = _velocidadActual * segundos;
        if (obstaculos != null)
        {
            Vector2 permitido = ResolutorMovimiento.Limitar(Colision, movimiento, obstaculos);
            // quitar la velocidad contra la pared evita acumular inercia.
            if (permitido.X != movimiento.X)
                _velocidadActual.X = 0f;
            if (permitido.Y != movimiento.Y)
                _velocidadActual.Y = 0f;
            movimiento = permitido;
        }
        Position += movimiento;

        if (_velocidadActual != Vector2.Zero)
        {
            Core.Audio.SetPitch(_sonidoCaminata,
                _velocidadActual.LengthSquared() > Velocidad * Velocidad
                ? 0.5f
                : 0f);

            if (_sonidoCaminata.State != SoundState.Playing)
                Core.Audio.PlaySoundEffect(_sonidoCaminata);

            if (!_haciendoParry)
            {
                if (_velocidadActual.LengthSquared() > Velocidad * Velocidad)
                {
                    CambiarAnimacion(_velocidadActual.Y != 0
                        ? SinnerAnimation.CorrerArriba
                        : SinnerAnimation.Correr);
                }
                else
                {
                    CambiarAnimacion(_velocidadActual.Y != 0
                        ? SinnerAnimation.CaminataArriba
                        : SinnerAnimation.Caminata);
                }
            }

            if (_velocidadActual.X < 0)
                _sprite.Effects = SpriteEffects.FlipHorizontally;
            else if (_velocidadActual.X > 0)
                _sprite.Effects = SpriteEffects.None;
        }
        else
        {
            Core.Audio.StopSoundEffect(_sonidoCaminata);
            if (!_haciendoParry)
                CambiarAnimacion(SinnerAnimation.Reposo);
        }

        _sprite.Update(gameTime);
        if (_haciendoParry)
            _efectoParry.Update(gameTime);
    }

    private static Vector2 AcercarVelocidad(
        Vector2 actual, Vector2 objetivo, float cambioMaximo)
    {
        Vector2 diferencia = objetivo - actual;
        float distancia = diferencia.Length();

        // llegar exactamente al objetivo evita oscilar o deslizarse indefinidamente.
        if (distancia <= cambioMaximo || distancia == 0f)
            return objetivo;

        return actual + diferencia / distancia * cambioMaximo;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _sprite.Draw(spriteBatch);
        if (_haciendoParry && !_efectoParry.IsFinished)
        {
            _efectoParry.Position = Position;
            _efectoParry.Draw(spriteBatch);
        }

    }

    public void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_sonidoCaminata);
        Core.Audio.ReleaseSoundEffect(_sonidoParry);
        Core.Audio.ReleaseSoundEffect(_sonidoParryExitoso);
    }

    public void ConfirmarParry()
    {
        Core.Audio.StopSoundEffect(_sonidoParryExitoso);
        Core.Audio.PlaySoundEffect(_sonidoParryExitoso);
    }

    public void DetenerSonidos()
    {
        Core.Audio.StopSoundEffect(_sonidoCaminata);
        Core.Audio.StopSoundEffect(_sonidoParry);
        Core.Audio.StopSoundEffect(_sonidoParryExitoso);
    }
}
