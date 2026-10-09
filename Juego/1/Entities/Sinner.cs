using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace _1.Entities;

public enum SinnerAnimation
{
    Reposo,
    Caminata,
    CaminataArriba,
    Parry,
    Correr,
    CorrerAbajo
}

public class Sinner
{
    private SoundEffectInstance _sonidoCaminata;
    private SoundEffectInstance _sonidoParry;
    private AnimatedSprite _efectoParry;
    private KeyboardState _tecladoAnterior;
    private bool _haciendoParry;
    private const double SegundosPorFrameParry = 0.05;
    private const float Velocidad = 220f;
    private AnimatedSprite _sprite;

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

        _animaciones[SinnerAnimation.CorrerAbajo] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerRun-Sheet",
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
            "SFX/Sinner/Caminata"
        );

        _sonidoCaminata = sonido.CreateInstance();
        _sonidoCaminata.IsLooped = true;
        _sonidoCaminata.Volume = 0.4f;

        _sonidoParry = content.Load<SoundEffect>(
            "SFX/Sinner/Parry"
        ).CreateInstance();
        _sonidoParry.Volume = 0.6f;

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
        // Evita reiniciar la misma animación en cada Update.
        if (_animacionActual == animacion)
            return;

        _animacionActual = animacion;
        _sprite.Animation = _animaciones[animacion];
    }

    public void Update(GameTime gameTime)
    {
        KeyboardState teclado = Keyboard.GetState();

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
            _haciendoParry = true;
            _sonidoCaminata.Stop();
            _sprite.IsLooping = false;
            CambiarAnimacion(SinnerAnimation.Parry);
            _efectoParry.Animation = _efectoParry.Animation;
            _sonidoParry.Stop();
            _sonidoParry.Play();
        }

        if (_haciendoParry)
        {
            _sprite.Update(gameTime);
            _efectoParry.Update(gameTime);
            return;
        }
        Vector2 direccion = Vector2.Zero;

        if (teclado.IsKeyDown(Keys.A))
            direccion.X -= 1 ;

        if (teclado.IsKeyDown(Keys.D))
            direccion.X += 1 ;

        if (teclado.IsKeyDown(Keys.W))
            direccion.Y -= 1 ;

        if (teclado.IsKeyDown(Keys.S))
            direccion.Y += 1 ;

        float multiplicadorSprint = 1.0f;

        if (teclado.IsKeyDown(Keys.LeftShift))
        {
            if(teclado.IsKeyDown(Keys.A) || teclado.IsKeyDown(Keys.D))
            {
                CambiarAnimacion(SinnerAnimation.Correr);
            }
            if(teclado.IsKeyDown(Keys.W) || teclado.IsKeyDown(Keys.S))
            {
                CambiarAnimacion(SinnerAnimation.CorrerAbajo);
            }
            multiplicadorSprint = 2.5f;
        }

        
        if (direccion != Vector2.Zero)
        {
            if (_sonidoCaminata.State != SoundState.Playing)
                _sonidoCaminata.Play();
            direccion.Normalize();

            float segundos =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            Position += direccion * (Velocidad * multiplicadorSprint) * segundos;

            if (direccion.X < 0)
                _sprite.Effects = SpriteEffects.FlipHorizontally;
            else if (direccion.X > 0)
                _sprite.Effects = SpriteEffects.None;

            if (direccion.Y != 0)
            {
                CambiarAnimacion(SinnerAnimation.CaminataArriba);
            }
            else
            {
                CambiarAnimacion(SinnerAnimation.Caminata);
            }
        }
        else
        {
            _sonidoCaminata.Stop();
            CambiarAnimacion(SinnerAnimation.Reposo);
        }

        _sprite.Update(gameTime);
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
        _sonidoCaminata.Dispose();
        _sonidoParry.Dispose();
    }
}
