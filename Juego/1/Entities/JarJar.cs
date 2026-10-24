using LutredLibrary;
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
    // este margen permite realizar el parry sin exigir un cuadro exacto al aparecer el enemigo.
    private const float VentanaParry = 0.15f;
    private const float DuracionRecarga = 5f;
    // radio en pixeles del circulo dibujado dentro de la textura del aura (500x500).
    private const float RadioAuraEnTextura = 152f;
    // tiempo en el que no puede volver a teletransportarse, para dar tiempo a que Sinner llegue.
    private const float DuracionRecienTeletransportado = 3f;
    // radio alrededor del punto hacia donde va Sinner en el que se elige el destino.
    private const float RadioBusquedaDestino = 300f;

    private enum Estado
    {
        Esperando,
        Cargando,
        Atacando,
        Recargando,
        RecienTeletransportado
    }

    private AnimatedSprite _sprite;
    private Sprite _aura;
    private Animation _carga;
    private Animation _recarga;
    private SoundEffectInstance _sonidoParry;
    private Estado _estado;
    private float _temporizador;

    public Vector2 Position { get; set; }

    public void LoadContent(ContentManager content)
    {
        List<TextureRegion> framesCarga = CortarFrames(content,
            "Sprites/Enemies/JarJar/Sprite-JarJar-Sheet", 6);
        _carga = new Animation(framesCarga, TimeSpan.FromSeconds(SegundosPorFrameCarga));

        // la recarga alterna los cuadros 1, 2, 1, 3 para volver al centro entre ambos lados.
        List<TextureRegion> framesRecarga = CortarFrames(content,
            "Sprites/Enemies/JarJar/Sprite-JarJar-Carga-Sheet", 3);
        _recarga = new Animation(
            new List<TextureRegion> { framesRecarga[0], framesRecarga[1], framesRecarga[0], framesRecarga[2] },
            TimeSpan.FromSeconds(SegundosPorFrameRecarga));

        _sprite = new AnimatedSprite(_carga);
        _sprite.Origin = new Vector2(TamanoFrame / 2f);
        _sprite.Scale = new Vector2(0.5f);
        ReiniciarCarga();

        Texture2D texturaAura = content.Load<Texture2D>("Sprites/Enemies/JarJar/Sprite-JarJar-Aura");
        _aura = new Sprite(new TextureRegion(texturaAura, 0, 0, texturaAura.Width, texturaAura.Height));
        _aura.Origin = new Vector2(texturaAura.Width / 2f, texturaAura.Height / 2f);
        _aura.Scale = new Vector2(RadioDeteccion / RadioAuraEnTextura);

        _sonidoParry = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>("SFX/Entities/JarJar/JarJarParry"));
        Core.Audio.SetVolume(_sonidoParry, 0.6f);
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

    public ResultadoJarJar Update(GameTime gameTime, Vector2 centroSinner, Vector2 velocidadSinner,
        bool sinnerHaciendoParry, Rectangle areaVisible, IReadOnlyList<Vector2> posicionesLibres)
    {
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool sinnerEnRango = Vector2.DistanceSquared(centroSinner, Position)
            <= RadioDeteccion * RadioDeteccion;

        switch (_estado)
        {
            case Estado.Esperando:
                if (sinnerEnRango)
                    _estado = Estado.Cargando;
                else if (!EsVisible(Position, areaVisible)
                    && Teletransportar(centroSinner, velocidadSinner, areaVisible, posicionesLibres))
                {
                    _temporizador = DuracionRecienTeletransportado;
                    _estado = Estado.RecienTeletransportado;
                }
                break;

            case Estado.RecienTeletransportado:
                if (sinnerEnRango)
                {
                    _estado = Estado.Cargando;
                    break;
                }
                _temporizador -= segundos;
                if (_temporizador <= 0f)
                    _estado = Estado.Esperando;
                break;

            case Estado.Cargando:
                if (!sinnerEnRango)
                {
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
                    Core.Audio.StopSoundEffect(_sonidoParry);
                    Core.Audio.PlaySoundEffect(_sonidoParry);
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

    private static bool EsVisible(Vector2 posicion, Rectangle areaVisible)
    {
        // se cuenta el aura para que no aparezca ni desaparezca un circulo de golpe en pantalla.
        Rectangle area = areaVisible;
        area.Inflate((int)RadioDeteccion, (int)RadioDeteccion);
        return area.Contains(posicion);
    }

    private bool Teletransportar(Vector2 centroSinner, Vector2 velocidadSinner,
        Rectangle areaVisible, IReadOnlyList<Vector2> posicionesLibres)
    {
        if (posicionesLibres == null || posicionesLibres.Count == 0)
            return false;

        Vector2 direccion = velocidadSinner;
        if (direccion.LengthSquared() < 1f)
        {
            // si Sinner esta quieto no hay hacia donde anticiparse, se elige un lado al azar.
            float angulo = Random.Shared.NextSingle() * MathHelper.TwoPi;
            direccion = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo));
        }
        direccion.Normalize();

        // el objetivo queda justo fuera de la pantalla en la direccion en la que va Sinner.
        float distanciaFueraDePantalla = new Vector2(areaVisible.Width, areaVisible.Height).Length() * 0.5f
            + RadioDeteccion;
        Vector2 objetivo = centroSinner + direccion * distanciaFueraDePantalla;

        List<Vector2> cercanas = new();
        Vector2? masCercana = null;
        float distanciaMasCercana = float.MaxValue;
        for (int i = 0; i < posicionesLibres.Count; i++)
        {
            Vector2 candidata = posicionesLibres[i];
            if (EsVisible(candidata, areaVisible))
                continue;
            float distancia = Vector2.DistanceSquared(candidata, objetivo);
            if (distancia <= RadioBusquedaDestino * RadioBusquedaDestino)
                cercanas.Add(candidata);
            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                masCercana = candidata;
            }
        }

        // si cerca del objetivo hay paredes en todos lados, se usa la posicion libre mas cercana.
        Vector2? destino = cercanas.Count > 0 ? cercanas[Random.Shared.Next(cercanas.Count)] : masCercana;
        if (destino == null)
            return false;
        Position = destino.Value;
        return true;
    }

    private void ReiniciarCarga()
    {
        // reasignar la animacion vuelve al primer cuadro y reinicia su estado de finalizacion.
        _sprite.IsLooping = false;
        _sprite.Animation = _carga;
    }

    public void DrawAura(SpriteBatch spriteBatch)
    {
        // solo se muestra mientras la zona de deteccion puede activar un ataque.
        if (_estado != Estado.Esperando && _estado != Estado.Cargando
            && _estado != Estado.RecienTeletransportado)
            return;
        _aura.Position = Position;
        _aura.Draw(spriteBatch);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _sprite.Position = Position;
        _sprite.Draw(spriteBatch);
    }

    public void DetenerSonidos()
    {
        Core.Audio.StopSoundEffect(_sonidoParry);
    }

    public void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_sonidoParry);
    }
}
