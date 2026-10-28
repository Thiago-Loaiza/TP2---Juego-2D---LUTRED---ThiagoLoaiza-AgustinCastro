using System;
using System.Collections.Generic;
using _1.Colisiones;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public sealed class Shame
{
    public const float RadioColision = 38f;
    public const double VelocidadBase = Duck.VelocidadPersecucion * 2.0;
    private AnimatedSprite _sprite;
    private Texture2D _circuloEstela;
    private const double IntervaloEstela = 0.06;
    private const float DuracionEstela = 0.9f;
    private readonly Random _azar = new();
    private readonly List<(Vector2 Posicion, float Edad, float Radio, Vector2 Deriva)> _particulas = new();
    private Vector2 _direccionRebote;
    private double _rapidezRebote;
    private double _tiempoEstela;
    private SoundEffectInstance _ambiente;
    private SoundEffectInstance _risa;
    private SoundEffectInstance _parry;
    private double _espera;
    private double _tiempoAceleracion;

    public Vector2 Position { get; private set; }
    public bool Activo { get; private set; }
    public bool Rebotando => _rapidezRebote > 0;
    public bool PuedeMatar => Activo && _espera <= 0;
    public double Velocidad { get; private set; } = VelocidadBase;
    public CirculoColision Colision => new(Position, RadioColision);

    public void LoadContent(ContentManager content)
    {
        Texture2D textura = content.Load<Texture2D>("Sprites/Enemies/SHAME/Shame-Sheet");
        List<TextureRegion> frames = new();
        for (int i = 0; i < 6; i++)
        {
            frames.Add(new TextureRegion(textura, i * 350, 0, 350, 350));
        }
        _sprite = new AnimatedSprite(new Animation(frames, TimeSpan.FromSeconds(0.12)))
        {
            Origin = new Vector2(175f),
            Scale = Vector2.One
        };
        _circuloEstela = CrearTexturaHumo();
        _ambiente = Core.Audio.CreateSoundEffectInstance(
            content.Load<SoundEffect>("SFX/Entities/SHAME/ShameAmbience"), volume: 0.5f, isLooped: true);
        _risa = Core.Audio.CreateSoundEffectInstance(
            content.Load<SoundEffect>("SFX/Entities/SHAME/Shame_Laugh"), volume: 0.7f);
        _parry = Core.Audio.CreateSoundEffectInstance(
            content.Load<SoundEffect>("SFX/Entities/SHAME/Shameparry"), volume: 0.7f);
    }

    public void Aparecer(Vector2 posicion)
    {
        Position = posicion;
        Activo = true;
        _particulas.Clear();
        _tiempoEstela = 0;
        _rapidezRebote = 0;
        for (int i = 0; i < 6; i++)
        {
            EmitirParticula(Position, 0);
        }
        _espera = 1.0;
        Velocidad = VelocidadBase;
        _tiempoAceleracion = 0;
        if (_risa != null)
        {
            Core.Audio.PlaySoundEffect(_risa, restart: true);
            Core.Audio.PlaySoundEffect(_ambiente);
        }
    }

    public void Update(GameTime gameTime, Vector2 objetivo)
    {
        if (!Activo) { return; }
        _sprite?.Update(gameTime);
        if (_ambiente != null && _ambiente.State == SoundState.Stopped)
        {
            Core.Audio.PlaySoundEffect(_ambiente);
        }
        Vector2 posicionAnterior = Position;
        double segundosTotales = gameTime.ElapsedGameTime.TotalSeconds;
        double segundos = segundosTotales;
        double esperaConsumida = Math.Min(_espera, segundos);
        _espera -= esperaConsumida;
        segundos -= esperaConsumida;
        if (Rebotando)
        {
            double tiempoRebote = Math.Min(segundos, _rapidezRebote / Duck.FrenadoRebote);
            double distanciaRebote = _rapidezRebote * tiempoRebote
                - 0.5 * Duck.FrenadoRebote * tiempoRebote * tiempoRebote;
            Position += _direccionRebote * (float)distanciaRebote;
            _rapidezRebote = Math.Max(0, _rapidezRebote - Duck.FrenadoRebote * tiempoRebote);
            segundos -= tiempoRebote;
        }
        // La aceleracion solo avanza mientras persigue, despues del rebote.
        // Integrar por intervalos conserva los aumentos de velocidad con pocos FPS.
        double recorrido = 0;
        while (segundos > 0)
        {
            double tramo = Math.Min(segundos, 5.0 - _tiempoAceleracion);
            recorrido += Velocidad * tramo;
            segundos -= tramo;
            _tiempoAceleracion += tramo;
            if (_tiempoAceleracion >= 5.0)
            {
                Velocidad *= 1.2;
                _tiempoAceleracion = 0;
            }
        }
        Vector2 diferencia = objetivo - Position;
        float distancia = diferencia.Length();
        if (distancia > 0)
        {
            Position += diferencia / distancia * (float)Math.Min(recorrido, distancia);
        }
        ActualizarEstela(posicionAnterior, segundosTotales);
    }

    internal static Texture2D CrearTexturaHumo()
    {
        const int tamano = 256;
        const float radio = tamano * 0.5f;
        const float radioInterior = radio - 4f;
        var pixeles = new Color[tamano * tamano];
        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float dx = x + 0.5f - radio;
                float dy = y + 0.5f - radio;
                float distanciaCuadrada = dx * dx + dy * dy;
                pixeles[y * tamano + x] = distanciaCuadrada > radio * radio
                    ? Color.Transparent
                    : distanciaCuadrada >= radioInterior * radioInterior ? Color.Red : Color.Black;
            }
        }
        var textura = new Texture2D(Core.GraphicsDevice, tamano, tamano);
        textura.SetData(pixeles);
        return textura;
    }

    private void ActualizarEstela(Vector2 posicionAnterior, double segundos)
    {
        for (int i = _particulas.Count - 1; i >= 0; i--)
        {
            var particula = _particulas[i];
            float edad = particula.Edad + (float)segundos;
            if (edad >= DuracionEstela)
            {
                _particulas.RemoveAt(i);
            }
            else
            {
                _particulas[i] = (particula.Posicion, edad, particula.Radio, particula.Deriva);
            }
        }
        double emision = IntervaloEstela - _tiempoEstela;
        _tiempoEstela = (_tiempoEstela + segundos) % IntervaloEstela;
        // Emitir a lo largo del recorrido; omitir las particulas que ya habrian expirado.
        if (emision < segundos - DuracionEstela)
        {
            emision += Math.Ceiling((segundos - DuracionEstela - emision) / IntervaloEstela) * IntervaloEstela;
        }
        for (; emision <= segundos; emision += IntervaloEstela)
        {
            Vector2 posicion = Vector2.Lerp(posicionAnterior, Position, (float)(emision / segundos));
            EmitirParticula(posicion, (float)(segundos - emision));
        }
    }

    private void EmitirParticula(Vector2 centro, float edad)
    {
        float angulo = _azar.NextSingle() * MathHelper.TwoPi;
        Vector2 direccion = new(MathF.Cos(angulo), MathF.Sin(angulo));
        // Cada circulo nace en un lugar distinto alrededor del sprite y se dispersa.
        Vector2 posicion = centro + direccion * (25f + _azar.NextSingle() * 40f);
        float radio = 92f + _azar.NextSingle() * 24f;
        Vector2 deriva = direccion * (20f + _azar.NextSingle() * 30f) - new Vector2(0, 18f);
        _particulas.Add((posicion, edad, radio, deriva));
    }

    private void DibujarCirculo(SpriteBatch spriteBatch, Vector2 posicion, float radio, float opacidad)
    {
        spriteBatch.Draw(_circuloEstela, posicion, null, Color.White * opacidad,
            0f, new Vector2(_circuloEstela.Width * 0.5f),
            radio * 2f / _circuloEstela.Width, SpriteEffects.None, 0f);
    }

    public void Parrear(Vector2 centroParry, float radioParry, Vector2 posicionAnterior)
    {
        if (Rebotando) { return; }
        // Conservar la rapidez de llegada antes de restablecer la persecucion.
        _rapidezRebote = Duck.VelocidadRebote * (Velocidad / VelocidadBase);
        Velocidad = VelocidadBase;
        _tiempoAceleracion = 0;
        Vector2 direccion = posicionAnterior - centroParry;
        if (direccion.LengthSquared() == 0) { direccion = Position - centroParry; }
        if (direccion.LengthSquared() == 0) { direccion = Vector2.UnitX; }
        direccion.Normalize();
        _direccionRebote = direccion;
        Position = centroParry + direccion * (radioParry + RadioColision + 1f);
        if (_parry != null) { Core.Audio.PlaySoundEffect(_parry, restart: true); }
    }

    // Estimacion hasta el contacto si Sinner permaneciera en su posicion actual.
    public double TiempoHastaContacto(CirculoColision sinner)
    {
        double tiempoRebote = _rapidezRebote / Duck.FrenadoRebote;
        Vector2 finRebote = Position + _direccionRebote
            * (float)(_rapidezRebote * tiempoRebote * 0.5);
        double distancia = Math.Max(0, Vector2.Distance(finRebote, sinner.Centro)
            - RadioColision - sinner.Radio);
        return _espera + tiempoRebote + distancia / Velocidad;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!Activo) { return; }
        for (int i = 0; i < _particulas.Count; i++)
        {
            var particula = _particulas[i];
            float progreso = particula.Edad / DuracionEstela;
            float opacidad = MathHelper.Clamp((1f - progreso) / 0.45f, 0f, 1f);
            DibujarCirculo(spriteBatch, particula.Posicion + particula.Deriva * particula.Edad,
                particula.Radio + progreso * 20f, opacidad);
        }
        _sprite.Position = Position;
        _sprite.Draw(spriteBatch);
    }

    public void DetenerSonidos()
    {
        Core.Audio.StopSoundEffect(_ambiente);
        Core.Audio.StopSoundEffect(_risa);
        Core.Audio.StopSoundEffect(_parry);
    }

    public void UnloadContent()
    {
        _particulas.Clear();
        _circuloEstela?.Dispose();
        Core.Audio.ReleaseSoundEffect(_ambiente);
        Core.Audio.ReleaseSoundEffect(_risa);
        Core.Audio.ReleaseSoundEffect(_parry);
    }
}
