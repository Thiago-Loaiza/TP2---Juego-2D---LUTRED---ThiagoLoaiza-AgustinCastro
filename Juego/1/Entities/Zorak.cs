using System;
using System.Collections.Generic;
using LutredLibrary;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public enum ResultadoZorak { Nada, Parreado, Golpe }

public class Zorak
{
    public const float IntervaloAtaque = 10f;
    public const float VentanaParry = 1f;
    private const int TamanoFrame = 400;
    private const float DuracionAviso = 2f;
    private const float DuracionMirada = VentanaParry;
    private const float DuracionParticulas = 4f;
    private const float DuracionDispersion = 1f;
    private const float FrenadoParticulas = 5f;
    private enum Estado { Oculto, Avisando, Mirando, Atacando, Parreado, Explosion }

    private struct Particula
    {
        public Vector2 Posicion;
        public Vector2 Velocidad;
        public float Vida;
        public float Duracion;
        public float Rotacion;
        public float Giro;
        public float Tamano;
        public int FrameInicial;
    }

    private readonly List<Particula> _particulas = new();
    private readonly Random _azar = new();
    private AnimatedSprite _sprite;
    private Animation _aviso;
    private Animation _mirada;
    private Animation _ataque;
    private Animation _parry;
    private Texture2D _pixel;
    private readonly List<TextureRegion> _framesParticulas = new();
    private SoundEffectInstance _tick;
    private SoundEffectInstance _sonidoExplosion;
    private Estado _estado;
    private float _hastaAparicion = IntervaloAtaque - DuracionAviso;
    private float _tiempoMirada;
    private float _tiempoAviso;
    private bool _movimientoDetectado;
    private Vector2 _posicion;

    // La derrota se entrega al terminar la animacion para que el ataque sea visible.
    public bool BloqueandoJugador => _estado == Estado.Atacando;

    // Cada cuadro del ataque oscurece un paso mas, hasta negro en el ultimo.
    public float OpacidadOscurecimiento => !BloqueandoJugador ? 0f : MathHelper.Clamp(
        ((_sprite.Region.Bounds.X - _ataque.Frames[0].Bounds.X) / TamanoFrame + 1f)
        / _ataque.Frames.Count, 0f, 1f);

    public void LoadContent(ContentManager content)
    {
        Texture2D textura = content.Load<Texture2D>("Sprites/Enemies/Zorak/Zorak-Sheet");
        // Dos cuadros rapidos en bucle; la fase dura dos segundos independientemente del bucle.
        _aviso = new Animation(Cortar(textura, 1, 2), TimeSpan.FromSeconds(0.1));
        // El 10 es el primer cuadro despues de los ojos cerrados: se sostiene durante la piedad.
        _mirada = new Animation(Cortar(textura, 10, 10), TimeSpan.FromSeconds(VentanaParry));
        // El 14 vuelve a la cara redonda: terminar en el 13 conserva el penultimo gesto.
        _ataque = new Animation(Cortar(textura, 10, 13), TimeSpan.FromSeconds(0.09));
        // El recurso actual termina en 18; admite 19-20 cuando se amplie la hoja.
        int ultimoFrame = Math.Min(20, textura.Width / TamanoFrame);
        _parry = new Animation(Cortar(textura, 15, ultimoFrame), TimeSpan.FromSeconds(0.05));
        _sprite = new AnimatedSprite(_aviso)
        {
            IsLooping = false,
            Origin = new Vector2(TamanoFrame / 2f),
            Scale = new Vector2(0.6f)
        };
        _tick = Core.Audio.CreateSoundEffectInstance(
            content.Load<SoundEffect>("SFX/Entities/Zorak/Tick"), volume: 0.65f, isLooped: true);
        _sonidoExplosion = Core.Audio.CreateSoundEffectInstance(
            content.Load<SoundEffect>("SFX/Entities/Zorak/ParryZorak"), volume: 0.65f);
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        Texture2D particulas = content.Load<Texture2D>("Sprites/Enemies/Zorak/Particulas-Sheet");
        _framesParticulas.Clear();
        for (int x = 0; x < particulas.Width; x += particulas.Height)
            _framesParticulas.Add(new TextureRegion(particulas, x, 0, particulas.Height, particulas.Height));
    }

    private static List<TextureRegion> Cortar(Texture2D textura, int primero, int ultimo)
    {
        List<TextureRegion> frames = new();
        for (int numero = primero; numero <= ultimo; numero++)
            frames.Add(new TextureRegion(textura, (numero - 1) * TamanoFrame, 0,
                TamanoFrame, TamanoFrame));
        return frames;
    }

    public ResultadoZorak Update(GameTime gameTime, bool jugadorMoviendose, bool parryIniciado, Rectangle ocupado = default)
    {
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _hastaAparicion -= segundos;
        switch (_estado)
        {
            case Estado.Oculto:
                if (_hastaAparicion <= 0f)
                {
                    if (!ElegirPosicion(Core.GraphicsDevice.Viewport, ocupado)) { break; }
                    _hastaAparicion = IntervaloAtaque;
                    CambiarEstado(Estado.Avisando, _aviso);
                    Core.Audio.PlaySoundEffect(_tick, restart: true);
                }
                break;
            case Estado.Avisando:
                _sprite.Update(gameTime);
                _tiempoAviso += segundos;
                if (_tiempoAviso < DuracionAviso) break;
                DetenerSonidos();
                _tiempoMirada = 0f;
                _movimientoDetectado = false;
                CambiarEstado(Estado.Mirando, _mirada);
                // Resolver tambien el cuadro exacto en que termina de abrir los ojos.
                return ResolverMirada(jugadorMoviendose, parryIniciado);
            case Estado.Mirando:
                _sprite.Update(gameTime);
                _tiempoMirada += segundos;
                // Resolver antes de ocultar evita perder el ataque al cumplirse el segundo.
                ResultadoZorak resultado = ResolverMirada(jugadorMoviendose, parryIniciado);
                if (_estado == Estado.Mirando && _tiempoMirada >= DuracionMirada)
                    _estado = Estado.Oculto;
                return resultado;
            case Estado.Atacando:
                _sprite.Update(gameTime);
                if (_sprite.IsFinished) return ResultadoZorak.Golpe;
                break;
            case Estado.Parreado:
                _sprite.Update(gameTime);
                if (_sprite.IsFinished)
                {
                    Explotar();
                    _estado = Estado.Explosion;
                }
                break;
            case Estado.Explosion:
                ActualizarParticulas(segundos);
                if (_particulas.Count == 0) _estado = Estado.Oculto;
                break;
        }
        return ResultadoZorak.Nada;
    }

    private ResultadoZorak ResolverMirada(bool movimiento, bool parryIniciado)
    {
        if (parryIniciado && _tiempoMirada <= VentanaParry)
        {
            CambiarEstado(Estado.Parreado, _parry);
            return ResultadoZorak.Parreado;
        }
        // Se detecta desde la apertura, pero el parry tiene prioridad durante su ventana.
        _movimientoDetectado |= movimiento;
        if (_movimientoDetectado && _tiempoMirada >= VentanaParry)
            CambiarEstado(Estado.Atacando, _ataque);
        return ResultadoZorak.Nada;
    }

    private void CambiarEstado(Estado estado, Animation animacion)
    {
        _estado = estado;
        _sprite.IsLooping = estado == Estado.Avisando;
        _sprite.Animation = animacion;
        if (estado == Estado.Avisando) _tiempoAviso = 0f;
    }

    private void Explotar()
    {
        // El sonido comienza junto con las cruces, al finalizar el parry exitoso.
        Core.Audio.PlaySoundEffect(_sonidoExplosion, restart: true);
        for (int i = 0; i < 90; i++)
        {
            float angulo = _azar.NextSingle() * MathHelper.TwoPi;
            _particulas.Add(new Particula
            {
                Posicion = _posicion,
                Velocidad = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo))
                    * (320f + _azar.NextSingle() * 400f),
                Vida = DuracionParticulas,
                Duracion = DuracionParticulas,
                Rotacion = angulo,
                Giro = (_azar.NextSingle() - 0.5f) * 16f,
                Tamano = 18f + _azar.NextSingle() * 21f,
                FrameInicial = _azar.Next(2)
            });
        }
    }

    private void ActualizarParticulas(float segundos)
    {
        for (int i = _particulas.Count - 1; i >= 0; i--)
        {
            Particula p = _particulas[i];
            float edadAnterior = p.Duracion - p.Vida;
            p.Vida -= segundos;
            if (p.Vida <= 0f) { _particulas.RemoveAt(i); continue; }
            // Dispersion radial sin gravedad, con frenado independiente de la tasa de cuadros.
            float tiempoMovimiento = Math.Clamp(DuracionDispersion - edadAnterior, 0f, segundos);
            float amortiguacion = MathF.Exp(-FrenadoParticulas * tiempoMovimiento);
            p.Posicion += p.Velocidad * ((1f - amortiguacion) / FrenadoParticulas);
            p.Velocidad *= amortiguacion;
            if (p.Duracion - p.Vida >= DuracionDispersion) p.Velocidad = Vector2.Zero;
            p.Rotacion += p.Giro * segundos;
            _particulas[i] = p;
        }
    }

    public Rectangle AreaOcupada => _estado == Estado.Oculto || _estado == Estado.Explosion
        ? Rectangle.Empty : new Rectangle((int)_posicion.X - 120, (int)_posicion.Y - 120, 240, 240);

    private bool ElegirPosicion(Viewport viewport, Rectangle ocupado)
    {
        float margenX = Math.Min(TamanoFrame * _sprite.Scale.X * 0.5f + 16f, viewport.Width * 0.5f);
        float margenY = Math.Min(TamanoFrame * _sprite.Scale.Y * 0.5f + 16f, viewport.Height * 0.5f);
        for (int intento = 0; intento < 80; intento++)
        {
            Vector2 candidata = new(
                margenX + _azar.NextSingle() * Math.Max(0f, viewport.Width - 2f * margenX),
                margenY + _azar.NextSingle() * Math.Max(0f, viewport.Height - 2f * margenY));
            Rectangle area = new((int)candidata.X - 136, (int)candidata.Y - 136, 272, 272);
            if (!ocupado.IsEmpty && area.Intersects(ocupado)) { continue; }
            _posicion = candidata;
            return true;
        }
        return false;
    }
    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (BloqueandoJugador)
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * OpacidadOscurecimiento);
        if (_estado == Estado.Explosion)
        {
            foreach (Particula p in _particulas)
            {
                float vida = p.Vida / p.Duracion;
                int frame = (p.FrameInicial + (int)((p.Duracion - p.Vida) / 0.1f)) % _framesParticulas.Count;
                TextureRegion region = _framesParticulas[frame];
                spriteBatch.Draw(region.Texture, p.Posicion, region.Bounds, Color.White * vida,
                    p.Rotacion, new Vector2(region.Width, region.Height) * 0.5f, p.Tamano / region.Width,
                    SpriteEffects.None, 0f);
            }
        }
        else if (_estado != Estado.Oculto)
        {
            _sprite.Position = _posicion;
            _sprite.Draw(spriteBatch);
        }
    }

    public void DetenerSonidos()
    {
        Core.Audio.StopSoundEffect(_tick);
        Core.Audio.StopSoundEffect(_sonidoExplosion);
    }

    public void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_tick);
        Core.Audio.ReleaseSoundEffect(_sonidoExplosion);
        _pixel?.Dispose();
        _particulas.Clear();
    }
}
