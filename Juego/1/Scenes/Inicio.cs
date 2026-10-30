using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum;
using _1.UI;

namespace _1.Scenes;

public class Inicio : Escena
{
    private readonly Action<Escena> _cambiarEscena;
    private BotonAnimado _jugar;
    private BotonAnimado _opciones;
    private BotonInterruptor _alternarCorrer;
    private BotonAnimado _volver;
    private Panel _panelInicio;
    private Panel _panelOpciones;
    private SpriteFont _fuenteOpciones;
    private const float DuracionFase = 0.28f;
    private const float DesplazamientoTransicion = 90f;
    private bool _enTransicion;
    private bool _entrando;
    private bool _destinoOpciones;
    private float _tiempoTransicion;
    private float _opacidadInterfaz = 1f;
    private float _desplazamientoInterfaz;
    public bool OpcionesAbiertas => _panelOpciones?.IsVisible == true;
    public bool EnTransicion => _enTransicion;
    private AnimatedSprite _titulo;
    private SoundEffectInstance _musicaInicio;
    private Texture2D _hojaBoton;
    private Texture2D _hojaCreditos;
    private Texture2D _hojaMusica;
    private Texture2D _hojaMisc;
    private Texture2D _hojaInterruptor;
    private readonly List<BotonAnimado> _botones = new();

    public Inicio(Action<Escena> cambiarEscena)
    {
        _cambiarEscena = cambiarEscena;
    }

    protected override void LoadContent()
    {
        _fuenteOpciones = Content.Load<SpriteFont>("Fonts/PMingLiU Regular/Opciones");
        Core.Audio.SongVolume = 0.45f;
        Core.Audio.StopSong();
        _musicaInicio = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Inicio"), isLooped: true, isMusic: true);
        Core.Audio.PlaySoundEffect(_musicaInicio);
        _hojaBoton = Content.Load<Texture2D>("Sprites/UI/Buttons/Button-Sheet");
        _hojaInterruptor = Content.Load<Texture2D>("Sprites/UI/Buttons/ON-OFF-Sheet");
        _hojaCreditos = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Credits-Sheet");
        _hojaMusica = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Music-Sheet");
        _hojaMisc = Content.Load<Texture2D>("Sprites/UI/Buttons/Buttons_50x50/Button-Misc-Sheet");
        Texture2D hojaTitulo = Content.Load<Texture2D>("Sprites/UI/Titulo-Sheet");
        var cuadrosTitulo = new List<TextureRegion>();
        for (int i = 0; i < 16; i++)
        {
            cuadrosTitulo.Add(new TextureRegion(hojaTitulo, i * 800, 0, 800, 400));
        }
        _titulo = new AnimatedSprite(new Animation(cuadrosTitulo, TimeSpan.FromSeconds(0.12)))
        {
            Origin = new Vector2(400, 200)
        };
    }

    public override void Initialize()
    {
        base.Initialize();

        var panel = new Panel();
        _panelInicio = panel;
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _jugar = AgregarBoton(panel, "Jugar", -50);
        _jugar.Click += Jugar;
        _opciones = AgregarBoton(panel, "Opciones", 85);
        _opciones.Click += AbrirOpciones;
        AgregarBoton(panel, "Records", 220);

        // Musica, Misc y Creditos, de izquierda a derecha, con 12 px de separacion.
        AgregarBotonInferior(panel, _hojaMusica, -148);
        AgregarBotonInferior(panel, _hojaMisc, -86);
        AgregarBotonInferior(panel, _hojaCreditos, -24);

        _panelOpciones = new Panel();
        _panelOpciones.Dock(Dock.Fill);
        _panelOpciones.AddToRoot();
        _alternarCorrer = new BotonInterruptor(_hojaInterruptor, OpcionesJuego.AlternarCorrer);
        _alternarCorrer.Anchor(Anchor.Center);
        _alternarCorrer.Y = -10;
        _panelOpciones.AddChild(_alternarCorrer);
        _alternarCorrer.Click += AlternarCorrer;
        _volver = AgregarBoton(_panelOpciones, "Volver", 180);
        _volver.Click += Volver;
        _panelOpciones.IsVisible = false;
        _jugar.IsFocused = true;
    }

    private void AbrirOpciones(object sender, EventArgs args)
    {
        IniciarTransicion(true);
    }

    private void AlternarCorrer(object sender, EventArgs args)
    {
        if (_enTransicion || _alternarCorrer.Animando) return;
        OpcionesJuego.AlternarCorrer = !OpcionesJuego.AlternarCorrer;
        _alternarCorrer.CambiarEstado(OpcionesJuego.AlternarCorrer);
    }

    private void Volver(object sender, EventArgs args) => CerrarOpciones();

    public void CerrarOpciones()
    {
        IniciarTransicion(false);
    }

    private void IniciarTransicion(bool haciaOpciones)
    {
        if (_enTransicion || haciaOpciones == OpcionesAbiertas) return;
        _enTransicion = true;
        _entrando = false;
        _destinoOpciones = haciaOpciones;
        _tiempoTransicion = 0f;
        foreach (BotonAnimado boton in _botones) boton.IsFocused = false;
        _alternarCorrer.IsFocused = false;
    }

    private void AgregarBotonInferior(Panel panel, Texture2D hoja, float x)
    {
        var boton = new BotonAnimado(hoja, 50, 50, 9, TimeSpan.FromSeconds(0.12))
        {
            Text = "",
            Width = 50,
            Height = 50
        };
        boton.Anchor(Anchor.BottomRight);
        boton.X = x;
        boton.Y = -24;
        panel.AddChild(boton);
        _botones.Add(boton);
    }
    private BotonAnimado AgregarBoton(Panel panel, string texto, float y)
    {
        var boton = new BotonAnimado(_hojaBoton, 400, 200, 9,
            TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
        {
            Text = texto,
            Width = 320,
            Height = 130
        };
        boton.Anchor(Anchor.Center);
        boton.Y = y;
        panel.AddChild(boton);
        _botones.Add(boton);
        return boton;
    }

    private void Jugar(object sender, EventArgs args)
    {
        if (_enTransicion) return;
        var mejoras = new _1.Progression.EstadoMejoras();
        if (Game1.EmpezarConMejoras)
        {
            _cambiarEscena(new Mejoras(() => _cambiarEscena(new Seleccion(_cambiarEscena, mejoras: mejoras)), mejoras));
            return;
        }
        _cambiarEscena(Game1.EmpezarConEleccion
            ? new Seleccion(_cambiarEscena, mejoras: mejoras)
            : new Partida(_cambiarEscena, mejoras: mejoras));
    }

    public override void Update(GameTime gameTime)
    {
        _titulo.Update(gameTime);
        if (_enTransicion)
        {
            _tiempoTransicion += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_tiempoTransicion >= DuracionFase)
            {
                if (!_entrando)
                {
                    _panelInicio.IsVisible = !_destinoOpciones;
                    _panelOpciones.IsVisible = _destinoOpciones;
                    _panelInicio.Y = 0;
                    _panelOpciones.Y = 0;
                    _entrando = true;
                    _tiempoTransicion = 0f;
                }
                else
                {
                    _enTransicion = false;
                    if (_destinoOpciones) _alternarCorrer.IsFocused = true;
                    else _opciones.IsFocused = true;
                }
            }
            float t = Math.Clamp(_tiempoTransicion / DuracionFase, 0f, 1f);
            float suave = t * t * (3f - 2f * t);
            _opacidadInterfaz = _entrando ? suave : 1f - suave;
            _desplazamientoInterfaz = DesplazamientoTransicion * (_entrando ? 1f - suave : suave);
        }
        else
        {
            _opacidadInterfaz = 1f;
            _desplazamientoInterfaz = 0f;
        }
        (_panelOpciones.IsVisible ? _panelOpciones : _panelInicio).Y = _desplazamientoInterfaz;
        foreach (BotonAnimado boton in _botones) boton.Opacidad = _opacidadInterfaz;
        foreach (BotonAnimado boton in _botones) boton.Update(gameTime);
        _alternarCorrer.Opacidad = _opacidadInterfaz;
        _alternarCorrer.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);
        var viewport = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        _titulo.Position = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f - 210f * escala + _desplazamientoInterfaz);
        _titulo.Scale = new Vector2(0.85f * escala);
        _titulo.Color = Color.White * _opacidadInterfaz;
        // La fuente se genera grande y se reduce con filtrado lineal para suavizar sus bordes.
        Core.SpriteBatch.Begin(samplerState: OpcionesAbiertas ? SamplerState.LinearClamp : SamplerState.PointClamp);
        if (OpcionesAbiertas)
        {
            DibujarTextoOpciones("OPCIONES", -210f, 0.625f, 2f, escala);
            DibujarTextoOpciones("Alternar correr", -85f, 0.375f, 0f, escala);
            string ayuda = OpcionesJuego.AlternarCorrer
                ? "Pulsa Shift para correr. Pulsa otra vez para caminar."
                : "Manten Shift presionado para correr.";
            DibujarTextoOpciones(ayuda, 75f, 0.2875f, 0f, escala);
        }
        else
        {
            _titulo.Draw(Core.SpriteBatch);
        }
        Core.SpriteBatch.End();
    }

    private void DibujarTextoOpciones(string texto, float y, float escalaFuente, float borde, float escalaPantalla)
    {
        var viewport = Core.GraphicsDevice.Viewport;
        Vector2 posicion = new(viewport.Width * 0.5f,
            viewport.Height * 0.5f + y * escalaPantalla + _desplazamientoInterfaz);
        Vector2 origen = _fuenteOpciones.MeasureString(texto) * 0.5f;
        float escala = escalaFuente * escalaPantalla;
        void Dibujar(Vector2 desplazamiento, Color color) => Core.SpriteBatch.DrawString(
            _fuenteOpciones, texto, posicion + desplazamiento * escalaPantalla,
            color * _opacidadInterfaz, 0f, origen, escala, SpriteEffects.None, 0f);

        // Solo el titulo lleva contorno. El resto usa blanco para leerse sobre negro.
        for (int i = 0; borde > 0f && i < 16; i++)
        {
            float angulo = MathHelper.TwoPi * i / 16f;
            Dibujar(new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * (borde + 0.5f), Color.White);
        }
        Color relleno = borde > 0f ? Color.Black : Color.White;
        Dibujar(Vector2.Zero, relleno);
        Dibujar(new Vector2(0.5f, 0f), relleno);
    }

    protected override void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_musicaInicio);
        if (_jugar != null)
            _jugar.Click -= Jugar;
        if (_opciones != null) _opciones.Click -= AbrirOpciones;
        if (_alternarCorrer != null) _alternarCorrer.Click -= AlternarCorrer;
        _alternarCorrer?.Dispose();
        if (_volver != null) _volver.Click -= Volver;
        foreach (BotonAnimado boton in _botones) boton.Dispose();
        _botones.Clear();
    }
}
