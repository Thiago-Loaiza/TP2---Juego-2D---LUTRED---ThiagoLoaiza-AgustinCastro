using System;
using System.Collections.Generic;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public enum ResultadoJarJar
{
    Nada,
    Parreado,
    Golpe
}

public class JarJar
{
    public const float RadioDeteccion = 260f;
    private const int TamanoFrame = 300;
    private const double SegundosPorFrameCarga = 0.25;
    private const double SegundosPorFrameRecarga = 0.2;
    // Margen tras aparecer encima de Sinner para que el parry no exija un frame exacto.
    private const float VentanaParry = 0.15f;
    private const float DuracionRecarga = 5f;

    private enum Estado
    {
        Esperando,
        Cargando,
        Atacando,
        Recargando
    }

    private AnimatedSprite _sprite;
    private Animation _carga;
    private Animation _recarga;
    private SoundEffectInstance _sonidoParry;
    private Estado _estado;
    private float _temporizador;

    // El rango acompana a JarJar: tras atacar se queda donde aparecio.
    public Vector2 Position { get; set; }

    public void LoadContent(ContentManager content)
    {
        List<TextureRegion> framesCarga = CortarFrames(content,
            "Sprites/Enemies/JarJar/Sprite-JarJar-Sheet", 6);
        _carga = new Animation(framesCarga, TimeSpan.FromSeconds(SegundosPorFrameCarga));

        // Espadas abajo, a un lado, abajo y al otro lado: 1-2-1-3.
        List<TextureRegion> framesRecarga = CortarFrames(content,
            "Sprites/Enemies/JarJar/Sprite-JarJar-Carga-Sheet", 3);
        _recarga = new Animation(
            new List<TextureRegion> { framesRecarga[0], framesRecarga[1], framesRecarga[0], framesRecarga[2] },
            TimeSpan.FromSeconds(SegundosPorFrameRecarga));

        _sprite = new AnimatedSprite(_carga);
        _sprite.Origin = new Vector2(TamanoFrame / 2f);
        _sprite.Scale = new Vector2(0.5f);
        ReiniciarCarga();

        _sonidoParry = content.Load<SoundEffect>("SFX/Entities/JarJar/JarJarParry").CreateInstance();
        _sonidoParry.Volume = 0.6f;
    }

    private static List<TextureRegion> CortarFrames(ContentManager content, string ruta, int cantidad)
    {
        Texture2D textura = content.Load<Texture2D>(ruta);
        List<TextureRegion> frames = new();
        for (int i = 0; i < cantidad; i++)
            frames.Add(new TextureRegion(textura, i * TamanoFrame, 0, TamanoFrame, TamanoFrame));
        return frames;
    }

    public void Ubicar(Vector2 posicion)
    {
        Position = posicion;
        ReiniciarCarga();
        _estado = Estado.Esperando;
    }

    public ResultadoJarJar Update(GameTime gameTime, Vector2 centroSinner, bool sinnerHaciendoParry)
    {
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool sinnerEnRango = Vector2.DistanceSquared(centroSinner, Position)
            <= RadioDeteccion * RadioDeteccion;

        switch (_estado)
        {
            case Estado.Esperando:
                if (sinnerEnRango)
                    _estado = Estado.Cargando;
                break;

            case Estado.Cargando:
                if (!sinnerEnRango)
                {
                    // Salir del rango cancela la carga y la proxima vez empieza de cero.
                    ReiniciarCarga();
                    _estado = Estado.Esperando;
                    break;
                }
                _sprite.Update(gameTime);
                if (_sprite.IsFinished)
                {
                    Position = centroSinner;
                    _temporizador = VentanaParry;
                    _estado = Estado.Atacando;
                }
                break;

            case Estado.Atacando:
                Position = centroSinner;
                if (sinnerHaciendoParry)
                {
                    _sonidoParry.Stop();
                    _sonidoParry.Play();
                    _sprite.IsLooping = true;
                    _sprite.Animation = _recarga;
                    _temporizador = DuracionRecarga;
                    _estado = Estado.Recargando;
                    return ResultadoJarJar.Parreado;
                }
                _temporizador -= segundos;
                if (_temporizador <= 0f)
                    return ResultadoJarJar.Golpe;
                break;

            case Estado.Recargando:
                _sprite.Update(gameTime);
                _temporizador -= segundos;
                if (_temporizador <= 0f)
                {
                    ReiniciarCarga();
                    _estado = Estado.Esperando;
                }
                break;
        }

        return ResultadoJarJar.Nada;
    }

    private void ReiniciarCarga()
    {
        // Reasignar la animacion vuelve al primer frame y limpia IsFinished.
        _sprite.IsLooping = false;
        _sprite.Animation = _carga;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _sprite.Position = Position;
        _sprite.Draw(spriteBatch);
    }

    public void DetenerSonidos()
    {
        _sonidoParry.Stop();
    }

    public void UnloadContent()
    {
        _sonidoParry.Dispose();
    }
}
