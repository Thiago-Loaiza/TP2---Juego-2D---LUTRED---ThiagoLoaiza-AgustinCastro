using System;
using System.Collections.Generic;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public class Duck
{
    public const float RadioColision = 38f;
    public const float VelocidadPersecucion = 190f;
    public const float VelocidadRebote = 950f;
    public const float FrenadoRebote = 900f;

    private readonly Random _azar = new();
    private AnimatedSprite _sprite;
    private SoundEffectInstance _sonidoParry;
    private SoundEffectInstance _sonidoQuack;
    private Vector2 _direccionRebote;
    private float _rapidezRebote;
    private double _segundosHastaQuack;

    public Vector2 Position { get; set; }
    public bool Rebotando => _rapidezRebote > 0f;
    public CirculoColision Colision => new(Position, RadioColision);

    public void LoadContent(ContentManager content)
    {
        Texture2D textura = content.Load<Texture2D>("Sprites/Enemies/Duck/DuckSpriteSheet");
        List<TextureRegion> frames = new();
        // La hoja tiene cinco celdas de 355x261, pero solo las primeras tres tienen dibujo.
        for (int i = 0; i < 3; i++)
            frames.Add(new TextureRegion(textura, i * 355, 0, 355, 261));

        _sprite = new AnimatedSprite(new Animation(frames, TimeSpan.FromSeconds(0.15)));
        _sprite.Origin = new Vector2(177.5f, 130.5f);
        _sprite.Scale = new Vector2(0.4f);

        _sonidoParry = content.Load<SoundEffect>("SFX/Entities/Duck/DuckParry").CreateInstance();
        _sonidoQuack = content.Load<SoundEffect>("SFX/Entities/Duck/Dukquack").CreateInstance();
        _sonidoParry.Volume = 0.6f;
        _sonidoQuack.Volume = 0.5f;
        ProgramarQuack();
    }

    public void Update(GameTime gameTime, Vector2 objetivo)
    {
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 movimiento;
        if (Rebotando)
        {
            float tiempoRebote = Math.Min(segundos, _rapidezRebote / FrenadoRebote);
            float distancia = _rapidezRebote * tiempoRebote
                - 0.5f * FrenadoRebote * tiempoRebote * tiempoRebote;
            movimiento = _direccionRebote * distancia;
            _rapidezRebote = Math.Max(0f, _rapidezRebote - FrenadoRebote * tiempoRebote);
        }
        else
        {
            Vector2 diferencia = objetivo - Position;
            float distancia = diferencia.Length();
            movimiento = distancia > 0f
                ? diferencia / distancia * Math.Min(VelocidadPersecucion * segundos, distancia)
                : Vector2.Zero;
        }

        Position += movimiento;
        // El dibujo original de Duck mira hacia la izquierda.
        if (movimiento.X < 0f) _sprite.Effects = SpriteEffects.None;
        else if (movimiento.X > 0f) _sprite.Effects = SpriteEffects.FlipHorizontally;
        _sprite.Update(gameTime);

        _segundosHastaQuack -= segundos;
        if (_segundosHastaQuack <= 0)
        {
            _sonidoQuack.Stop();
            _sonidoQuack.Play();
            ProgramarQuack();
        }
    }

    public void Rebotar(Vector2 centroParry, float radioParry, Vector2 posicionAnterior)
    {
        if (Rebotando)
            return;

        // La posicion previa conserva el lado de llegada incluso si cruzo el centro.
        Vector2 direccion = posicionAnterior - centroParry;
        if (direccion.LengthSquared() == 0f) direccion = Position - centroParry;
        if (direccion.LengthSquared() == 0f) direccion = Vector2.UnitX;
        direccion.Normalize();
        _direccionRebote = direccion;
        _rapidezRebote = VelocidadRebote;
        Position = centroParry + direccion * (radioParry + RadioColision + 1f);
        _sonidoParry.Stop();
        _sonidoParry.Play();
    }

    private void ProgramarQuack()
    {
        _segundosHastaQuack = 5.0 + _azar.NextDouble() * 7.0;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _sprite.Position = Position;
        _sprite.Draw(spriteBatch);
    }

    public void DetenerSonidos()
    {
        _sonidoParry.Stop();
        _sonidoQuack.Stop();
    }

    public void UnloadContent()
    {
        _sonidoParry.Dispose();
        _sonidoQuack.Dispose();
    }
}
