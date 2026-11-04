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
    CorrerArriba,
    CaminataAbajo,
    CorrerAbajo
}

public class Sinner
{
    private SoundEffectInstance _sonidoCaminata;
    private SoundEffectInstance _sonidoParry;
    private SoundEffectInstance _sonidoParryExitoso;
    private AnimatedSprite _efectoParry;
    private AnimatedSprite _auraDominio;
    private AnimatedSprite _aureolaDivina;
    private AnimatedSprite _efectoPaz;
    private AnimatedSprite _spriteDash;
    private Vector2 _ultimaDireccion = Vector2.UnitY;
    private Vector2 _direccionDash;
    private float _dashRestante;
    private float _recargaDash;
    private float _velocidadDash;
    public bool DashActivo { get; private set; }
    private AnimatedSprite _escudo;
    private AnimatedSprite _rupturaEscudo;
    private _1.Progression.EstadoEscudo _estadoEscudo;
    private KeyboardState _tecladoAnterior;
    private bool _correrAlternado;
    private bool _haciendoParry;
    private double _parryRestante;
    private const double SegundosPorFrameParry = 0.05;
    private const float Velocidad = 220f;
    public const float VelocidadCaminata = Velocidad;
    private const float MultiplicadorSprint = 2.5f;
    private const float Aceleracion = 1800f;
    private const float Frenado = 2200f;
    private Vector2 _velocidadActual;
    private AnimatedSprite _sprite;

    public _1.Progression.EstadoMejoras Mejoras { get; set; } = new();
    private Vector2 _direccionCaliz;
    private float _tiempoCaliz;

    public bool UsarInercia { get; set; } = true;
    public bool ControlesInvertidos { get; set; }
    public SinnerAnimation AnimacionMovimiento => _animacionActual == SinnerAnimation.Parry
        ? SinnerAnimation.Reposo : _animacionActual;
    public SpriteEffects Orientacion => _sprite.Effects;
    public bool HaciendoParry => _haciendoParry;
    // TooManyParries anula el parry manual y el autoparry mientras esta en pantalla.
    public bool ParryBloqueado { get; set; }
    public float MultiplicadorVelocidadExterno { get; set; } = 1f;
    public bool AutoparryActivo => Mejoras.Dominio.Activo && !ParryBloqueado;
    public bool PuedeParrear => !DashActivo && !ParryBloqueado && (HaciendoParry || AutoparryActivo);
    public bool ParryIniciadoEsteFrame { get; private set; }
    // Cada pulsacion de la tecla, aunque no llegue a iniciar un parry.
    public bool ParryPresionadoEsteFrame { get; private set; }
    public Vector2 VelocidadActual => _velocidadActual;
    public CirculoColision Colision => new(Position + new Vector2(0f, 15f), 24f);
    public CirculoColision ColisionParry => new(Colision.Centro, 35f * Mejoras.MultiplicadorRadioParry);

    private readonly Dictionary<SinnerAnimation, Animation>
        _animaciones = new();
    private readonly Dictionary<SinnerAnimation, Animation> _parryMovimiento = new();

    private SinnerAnimation _animacionActual;

    public Vector2 Position
    {
        get => _sprite.Position;
        set => _sprite.Position = value;
    }

    public void LoadContent(ContentManager content)
    {
        if (Mejoras.Cantidad("Paz") > 0)
        {
            _efectoPaz = new AnimatedSprite(CrearAnimacion(content,
                "Sprites/Upgrades/Efectos/Paz-Sheet", 19, 0.08))
            {
                Origin = new Vector2(150f), Scale = new Vector2(0.5f)
            };
        }
        if (Mejoras.Cantidad("AureolaDivina") > 0)
        {
            _aureolaDivina = new AnimatedSprite(CrearAnimacion(content,
                "Sprites/Upgrades/Efectos/AureolaDivina-Sheet", 6, 0.1))
            {
                Origin = new Vector2(150f), Scale = new Vector2(0.5f)
            };
            _spriteDash = new AnimatedSprite(CrearAnimacion(content,
                "Sprites/Sinner/SinnerDASH-Sheet", 9, 0.2 / 9))
            {
                Origin = new Vector2(150f), Scale = new Vector2(0.5f), IsLooping = false
            };
        }
        _estadoEscudo = new _1.Progression.EstadoEscudo(Mejoras.Cantidad("Escudo") > 0);
        if (_estadoEscudo.Disponible)
        {
            _escudo = new AnimatedSprite(CrearAnimacion(content,
                "Sprites/Upgrades/Efectos/Shield/Shield-Sheet", 14, 0.08, 50, 50))
            {
                Origin = new Vector2(25f), Scale = new Vector2(0.75f)
            };
            _rupturaEscudo = new AnimatedSprite(CrearAnimacion(content,
                "Sprites/Upgrades/Efectos/Shield/ShieldBroken-Sheet", 9, 0.06, 50, 50))
            {
                Origin = new Vector2(25f), Scale = new Vector2(0.75f), IsLooping = false
            };
        }
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
            SegundosPorFrameParry + Mejoras.ExtraParry / 5
        );

        _animaciones[SinnerAnimation.Correr] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerRUN-Sheet",
            8,
            0.08
        );

        _animaciones[SinnerAnimation.CorrerArriba] = CrearAnimacion(
            content,
            "Sprites/Sinner/SinnerRUN_UP-Sheet",
            7,
            0.08
        );

        _animaciones[SinnerAnimation.CaminataAbajo] = _animaciones[SinnerAnimation.CaminataArriba];
        _animaciones[SinnerAnimation.CorrerAbajo] = CrearAnimacion(
            content, "Sprites/Sinner/SinnerRUN_DOWN-Sheet", 9, 0.08);

        _parryMovimiento[SinnerAnimation.Caminata] = _animaciones[SinnerAnimation.Parry];
        _parryMovimiento[SinnerAnimation.CaminataArriba] = _animaciones[SinnerAnimation.Parry];
        _parryMovimiento[SinnerAnimation.CaminataAbajo] = _animaciones[SinnerAnimation.Parry];
        _parryMovimiento[SinnerAnimation.Correr] = CrearAnimacion(
            content, "Sprites/Sinner/SinnerRUN-PARRY-Sheet", 8, 0.08);
        _parryMovimiento[SinnerAnimation.CorrerArriba] = CrearAnimacion(
            content, "Sprites/Sinner/SinnerRUN_UP-PARRY-Sheet", 7, 0.08);
        _parryMovimiento[SinnerAnimation.CorrerAbajo] = CrearAnimacion(
            content, "Sprites/Sinner/SinnerRUN_DOWN-PARRY-Sheet", 9, 0.08);

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
            "Effects/EfectoParry-Sheet",
            5,
            (SegundosPorFrameParry * 5 + Mejoras.ExtraParry) / 5,
            50,
            50
        ));
        if (Mejoras.Cantidad("Dominio") > 0)
        {
            _auraDominio = new AnimatedSprite(CrearAnimacion(content,
                "Sprites/Upgrades/Efectos/DominioV2-Sheet", 14, 0.07, 300, 300))
            {
                Origin = new Vector2(150f),
                Scale = new Vector2(0.6f)
            };
        }
        _efectoParry.Position = Position;
        _efectoParry.IsLooping = false;
        _efectoParry.Origin = new Vector2(25, 25);
        _efectoParry.Scale = new Vector2(0.93f, 0.93f);
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
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _recargaDash = Math.Max(0f, _recargaDash - segundos);
        DashActivo = _dashRestante > 0f;
        _aureolaDivina?.Update(gameTime);
        _efectoPaz?.Update(gameTime);
        Mejoras.Dominio.Actualizar(gameTime.ElapsedGameTime.TotalSeconds);
        if (AutoparryActivo)
        {
            _auraDominio?.Update(gameTime);
        }
        KeyboardState teclado = Keyboard.GetState();
        bool dashPresionado = teclado.IsKeyDown(Keys.Q) && _tecladoAnterior.IsKeyUp(Keys.Q);
        ParryIniciadoEsteFrame = false;

        bool parryPresionado = teclado.IsKeyDown(Keys.Space)
            && _tecladoAnterior.IsKeyUp(Keys.Space);
        ParryPresionadoEsteFrame = parryPresionado;
        bool shiftPresionado = (teclado.IsKeyDown(Keys.LeftShift) && _tecladoAnterior.IsKeyUp(Keys.LeftShift))
            || (teclado.IsKeyDown(Keys.RightShift) && _tecladoAnterior.IsKeyUp(Keys.RightShift));
        if (OpcionesJuego.AlternarCorrer && shiftPresionado)
            _correrAlternado = !_correrAlternado;
        if (!OpcionesJuego.AlternarCorrer)
            _correrAlternado = false;
        _tecladoAnterior = teclado;

        // El parry conserva su duracion aunque cambie la animacion o la direccion.
        if (_haciendoParry)
        {
            _parryRestante -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_parryRestante <= 0)
            {
                Mejoras.Dominio.FinalizarParryManual();
                _haciendoParry = false;
            }
        }

        if (parryPresionado && !_haciendoParry && !ParryBloqueado)
        {
            Mejoras.Dominio.ComenzarParryManual();
            ParryIniciadoEsteFrame = true;
            _haciendoParry = true;
            _parryRestante = SegundosPorFrameParry * 5 + Mejoras.ExtraParry;
            _efectoParry.Animation = _efectoParry.Animation;
            Core.Audio.StopSoundEffect(_sonidoParry);
            Core.Audio.PlaySoundEffect(_sonidoParry);
        }

        Vector2 direccion = Vector2.Zero;

        if (teclado.IsKeyDown(Keys.A)) direccion.X -= 1;
        if (teclado.IsKeyDown(Keys.D)) direccion.X += 1;
        if (teclado.IsKeyDown(Keys.W)) direccion.Y -= 1;
        if (teclado.IsKeyDown(Keys.S)) direccion.Y += 1;

        if (ControlesInvertidos) direccion = -direccion;

        if (direccion != Vector2.Zero)
        {
            direccion.Normalize();
            _ultimaDireccion = direccion;
        }

        bool corriendo = OpcionesJuego.AlternarCorrer
            ? _correrAlternado
            : teclado.IsKeyDown(Keys.LeftShift) || teclado.IsKeyDown(Keys.RightShift);
        if (direccion != _direccionCaliz || !corriendo || direccion == Vector2.Zero)
        {
            _tiempoCaliz = 0f;
        }
        else if (Mejoras.Cantidad("Caliz") > 0)
        {
            _tiempoCaliz = Math.Min(15f, _tiempoCaliz + segundos);
        }
        _direccionCaliz = direccion;
        float velocidadCaminata = Velocidad * Mejoras.MultiplicadorVelocidad;
        float bonusCaliz = Mejoras.Cantidad("Caliz") > 0 ? Math.Min(0.5f, MathF.Floor(_tiempoCaliz / 3f) * 0.1f) : 0f;
        Vector2 velocidadObjetivo = direccion * Velocidad * (Mejoras.MultiplicadorVelocidad + bonusCaliz)
            * (corriendo ? MultiplicadorSprint : 1f) * MultiplicadorVelocidadExterno;

        if (dashPresionado && _aureolaDivina != null && _recargaDash <= 0f && !DashActivo)
        {
            _direccionDash = _ultimaDireccion;
            _dashRestante = 0.2f;
            _recargaDash = 2f;
            _velocidadDash = Velocidad * (Mejoras.MultiplicadorVelocidad + bonusCaliz)
                * MultiplicadorSprint * MultiplicadorVelocidadExterno * 3f;
            DashActivo = true;
            _spriteDash.Animation = _spriteDash.Animation;
        }

        if (DashActivo)
        {
            _velocidadActual = _direccionDash * _velocidadDash;
        }
        else if (UsarInercia && Mejoras.Cantidad("TETUTRED") == 0)
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

        // Limitar el tramo rapido a 0,2 s, incluso si un cuadro tarda mas que el dash.
        float segundosDash = DashActivo ? Math.Min(segundos, _dashRestante) : 0f;
        Vector2 movimiento = DashActivo
            ? _velocidadActual * segundosDash + velocidadObjetivo * (segundos - segundosDash)
            : _velocidadActual * segundos;
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
                _velocidadActual.LengthSquared() > velocidadCaminata * velocidadCaminata
                ? 0.5f
                : 0f);

            if (_sonidoCaminata.State != SoundState.Playing)
                Core.Audio.PlaySoundEffect(_sonidoCaminata);

            _sprite.IsLooping = true;
            bool vertical = Math.Abs(_velocidadActual.Y) > Math.Abs(_velocidadActual.X);
            bool sprint = _velocidadActual.LengthSquared() > velocidadCaminata * velocidadCaminata;
            CambiarAnimacion(vertical
                ? (_velocidadActual.Y < 0
                    ? (sprint ? SinnerAnimation.CorrerArriba : SinnerAnimation.CaminataArriba)
                    : (sprint ? SinnerAnimation.CorrerAbajo : SinnerAnimation.CaminataAbajo))
                : (sprint ? SinnerAnimation.Correr : SinnerAnimation.Caminata));
            _sprite.Effects = !vertical && _velocidadActual.X < 0
                ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }
        else
        {
            Core.Audio.StopSoundEffect(_sonidoCaminata);
            _sprite.IsLooping = !_haciendoParry;
            CambiarAnimacion(_haciendoParry ? SinnerAnimation.Parry : SinnerAnimation.Reposo);
        }

        _sprite.Update(gameTime);
        if (DashActivo)
        {
            _spriteDash.Update(gameTime);
            _dashRestante = Math.Max(0f, _dashRestante - segundos);
            if (_dashRestante <= 0f) _velocidadActual = velocidadObjetivo;
        }
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
        if (_efectoPaz != null)
        {
            _efectoPaz.Position = Position;
            _efectoPaz.Draw(spriteBatch);
        }
        _sprite.Color = _estadoEscudo.Invulnerable
            ? Color.White * (0.4f + 0.6f * (float)(0.5 + 0.5 * Math.Cos(
                _estadoEscudo.InvulnerabilidadRestante * Math.PI * 12)))
            : Color.White;
        if (AutoparryActivo && _auraDominio != null)
        {
            _auraDominio.Position = Colision.Centro;
            _auraDominio.Color = Color.White * Mejoras.Dominio.OpacidadAura;
            _auraDominio.Draw(spriteBatch);
        }
        if (DashActivo && _spriteDash != null)
        {
            _spriteDash.Position = Position;
            _spriteDash.Effects = _sprite.Effects;
            _spriteDash.Draw(spriteBatch);
        }
        else if (_haciendoParry && _parryMovimiento.TryGetValue(_animacionActual, out Animation parry))
        {
            // Mostrar el gesto completo sin alargar la ventana de defensa.
            double duracion = SegundosPorFrameParry * 5 + Mejoras.ExtraParry;
            double progreso = Math.Clamp(1 - _parryRestante / duracion, 0, 1);
            int indice = Math.Min(parry.Frames.Count - 1, (int)(progreso * parry.Frames.Count));
            TextureRegion cuadro = parry.Frames[indice];
            spriteBatch.Draw(cuadro.Texture, _sprite.Position, cuadro.Bounds, _sprite.Color,
                _sprite.Rotation, _sprite.Origin, _sprite.Scale, _sprite.Effects, _sprite.LayerDepth);
        }
        else
            _sprite.Draw(spriteBatch);
        if (!DashActivo && _haciendoParry && !_efectoParry.IsFinished)
        {
            _efectoParry.Position = Position;
            _efectoParry.Draw(spriteBatch);
        }

        AnimatedSprite proteccion = _estadoEscudo.Disponible ? _escudo
            : _estadoEscudo.Invulnerable && _rupturaEscudo?.IsFinished == false ? _rupturaEscudo : null;
        if (proteccion != null)
        {
            proteccion.Position = Position;
            proteccion.Draw(spriteBatch);
        }

        if (_aureolaDivina != null)
        {
            _aureolaDivina.Position = Position - new Vector2(0f, 30f);
            _aureolaDivina.Draw(spriteBatch);
        }

    }

    public void ActualizarEscudo(GameTime gameTime)
    {
        _estadoEscudo.Actualizar(gameTime.ElapsedGameTime.TotalSeconds);
        if (_estadoEscudo.Disponible) _escudo?.Update(gameTime);
        else if (_estadoEscudo.Invulnerable) _rupturaEscudo?.Update(gameTime);
    }

    public bool AbsorberGolpe() => _estadoEscudo.AbsorberGolpe();

    public void UnloadContent()
    {
        Mejoras.Dominio.FinalizarParryManual();
        Core.Audio.ReleaseSoundEffect(_sonidoCaminata);
        Core.Audio.ReleaseSoundEffect(_sonidoParry);
        Core.Audio.ReleaseSoundEffect(_sonidoParryExitoso);
    }

    public void ConfirmarParry(bool automatico = false)
    {
        bool auraActiva = AutoparryActivo;
        if (!automatico)
        {
            Mejoras.Dominio.RegistrarParryManualExitoso(Mejoras.Cantidad("Dominio"));
        }
        if (!auraActiva && AutoparryActivo && _auraDominio != null)
        {
            _auraDominio.Animation = _auraDominio.Animation;
        }
        Core.Audio.StopSoundEffect(_sonidoParryExitoso);
        Core.Audio.PlaySoundEffect(_sonidoParryExitoso);
    }

    public void DetenerSonidos()
    {
        Core.Audio.StopSoundEffect(_sonidoCaminata);
        Core.Audio.StopSoundEffect(_sonidoParry);
        Core.Audio.StopSoundEffect(_sonidoParryExitoso);
    }

    public void Teletransportar(Vector2 posicion)
    {
        Position = posicion;
        _velocidadActual = Vector2.Zero;
        DashActivo = false;
        _dashRestante = 0f;
        _tiempoCaliz = 0f;
        DetenerSonidos();
        SincronizarEntrada();
    }

    public void SincronizarEntrada() => _tecladoAnterior = Keyboard.GetState();
}
