using System;
using System.Collections.Generic;
using _1.UI;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace _1.Scenes;

public sealed class Seleccion : Escena
{
    private enum Estado { Neutral, Escapar, Continuar }
    private readonly Action<Escena> _cambiarEscena;
    private readonly int _numeroNivel;
    private readonly bool _transicionReproducida;
    private readonly int _cantidadDuke;
    private readonly int _cantidadJarJar;
    private readonly bool _sorakElegido;
    private readonly double _duracionNivel;
    private enum Enemigo { Sorak, JarJar, Duke }
    private Enemigo? _enemigoSeleccionado;
    private bool _entrando;
    private readonly List<Enemigo> _opcionesRivales = new();
    private readonly SoundEffectInstance[] _musicas = new SoundEffectInstance[3];
    private readonly Texture2D[] _animaciones = new Texture2D[3];
    private readonly int[] _cantidadFrames = { 11, 30, 24 };
    private readonly Texture2D[,] _fondosAnimados = new Texture2D[3, 2];
    private readonly int[] _cuadrosFondo = { 20, 20, 22 };
    private readonly List<(Button boton, ColoredRectangleRuntime fondo,
        TextRuntime texto)> _botones = new();
    private readonly float[] _mezcla = { 1f, 0f, 0f };
    private Estado _estado;
    private TextRuntime _titulo;
    private SpriteFont _fuenteRivales;
    private Texture2D _pixelPanel;
    private ColoredRectangleRuntime _destello;
    private const float DuracionDestello = 0.3f;
    private float _destelloRestante;
    private double _relojEstado;
    private double _relojFondo;
    private float _opacidadDuda;
    private Color _fondo = Color.White;
    private Color _tinta = Color.Black;
    private Color _sinner = Color.Black;

    public Seleccion(Action<Escena> cambiarEscena, int numeroNivel = 1,
        bool transicionReproducida = false, int cantidadDuke = 0, int cantidadJarJar = 0, bool sorakElegido = false,
        double duracionNivel = RelojNivel.Duracion)
    {
        _cambiarEscena = cambiarEscena;
        _numeroNivel = numeroNivel;
        _transicionReproducida = transicionReproducida;
        if (cantidadDuke < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadDuke));
        }
        if (cantidadJarJar < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadJarJar));
        }
        _cantidadDuke = cantidadDuke;
        _cantidadJarJar = cantidadJarJar;
        _sorakElegido = sorakElegido;
        if (!double.IsFinite(duracionNivel) || duracionNivel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duracionNivel));
        }
        _duracionNivel = duracionNivel;
        var pool = new List<Enemigo> { Enemigo.JarJar, Enemigo.Duke };
        if (!sorakElegido)
        {
            pool.Add(Enemigo.Sorak);
        }
        // Dos rivales distintos por pantalla. Zorak sale de la pool una vez elegido.
        for (int i = 0; i < 2; i++)
        {
            int indice = Random.Shared.Next(pool.Count);
            _opcionesRivales.Add(pool[indice]);
            pool.RemoveAt(indice);
        }
    }

    protected override void LoadContent()
    {
        Core.Audio.StopSong();
        Core.Audio.SongVolume = 1f;
        _fuenteRivales = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _pixelPanel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixelPanel.SetData(new[] { Color.White });
        string[] fondos = { "ElegirV2", "DudaV2", "ContinuarV2" };
        string[] arte = { "Neutral", "Escapar", "Continuar" };
        string[] musica = { "Neutral", "Duda", "Continuar" };
        for (int i = 0; i < 3; i++)
        {
            for (int pagina = 0; pagina < 2; pagina++)
                _fondosAnimados[i, pagina] = Content.Load<Texture2D>(
                    $"Backgrounds/Selection/Atlas/{fondos[i]}-{pagina}");
            _animaciones[i] = Content.Load<Texture2D>($"Sprites/Sinner/PantallaEleccion/Atlas/{arte[i]}");
            // la base blanca premultiplicada permite aplicar un tono uniforme sin depender del negro original.
            var pixeles = new Color[_animaciones[i].Width * _animaciones[i].Height];
            _animaciones[i].GetData(pixeles);
            for (int p = 0; p < pixeles.Length; p++)
            {
                byte alfa = pixeles[p].A;
                pixeles[p] = new Color(alfa, alfa, alfa, alfa);
            }
            _animaciones[i].SetData(pixeles);
            _musicas[i] = Core.Audio.CreateSoundEffectInstance(Content.Load<SoundEffect>($"Music/Eleccion/Eleccion_{musica[i]}"), isLooped: true, isMusic: true);
            Core.Audio.SetVolume(_musicas[i], i == 0 ? 0.45f : 0f);
        }
        // las pistas silenciadas siguen avanzando para conservar la sincronizacion de la mezcla.
        foreach (SoundEffectInstance pista in _musicas) Core.Audio.PlaySoundEffect(pista);
    }

    public override void Initialize()
    {
        base.Initialize();
        var panel = new Panel();
        panel.Dock(Dock.Fill);
        panel.AddToRoot();

        _titulo = new TextRuntime();
        _titulo.UseCustomFont = true;
        _titulo.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        _titulo.FontScale = 2f;
        _titulo.Anchor(Anchor.Top);
        _titulo.Y = 70;
        _titulo.Text = "Y ahora que?";
        _titulo.Color = Color.Black;
        panel.Visual.Children.Add(_titulo);

        AgregarBoton(panel, "Escapar", -350, 0, () => CambiarEstado(Estado.Escapar));
        AgregarBoton(panel, "Continuar", 350, 0, () => CambiarEstado(Estado.Continuar));
        AgregarBoton(panel, "Confirmar", -350, 0, () => _cambiarEscena(new Inicio(_cambiarEscena)));
        AgregarBoton(panel, "Volver", 350, 0, () => CambiarEstado(Estado.Neutral));
        for (int i = 0; i < _opcionesRivales.Count; i++)
        {
            Enemigo rival = _opcionesRivales[i];
            AgregarBoton(panel, NombreRival(rival), 0, -80 + i * 140,
                () => ElegirEnemigo(rival));
            var boton = _botones[i + 4].boton;
            boton.Width = 560;
            boton.Height = 110;
            _botones[i + 4].texto.Visible = false;

        }
        AgregarBoton(panel, "Menos tiempo", 0, 200, () => EntrarAPrueba(menosTiempo: true));
        var opcionTiempo = _botones[_botones.Count - 1];
        opcionTiempo.boton.Width = 680;
        opcionTiempo.boton.Height = 110;
        opcionTiempo.texto.Visible = false;
        _destello = new ColoredRectangleRuntime();
        _destello.Dock(Dock.Fill);
        _destello.Visible = false;
        panel.Visual.Children.Add(_destello);
        ActualizarOpciones();
    }

    private void AgregarBoton(Panel panel, string texto, float x, float y, Action accion)
    {
        var boton = new Button { Text = texto, Width = 240, Height = 64 };
        boton.Anchor(Anchor.Center);
        boton.X = x;
        boton.Y = y;
        var visual = (ButtonVisual)boton.Visual;
        visual.ButtonCategory.ResetAllStates();
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;
        var fondo = new ColoredRectangleRuntime();
        fondo.Dock(Dock.Fill);
        fondo.Visible = false;
        visual.Children.Insert(0, fondo);
        var etiqueta = visual.TextInstance;
        etiqueta.UseCustomFont = true;
        etiqueta.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        etiqueta.FontScale = 1.4f;
        boton.Click += (_, _) => accion();
        panel.AddChild(boton);
        _botones.Add((boton, fondo, etiqueta));
    }

    private void CambiarEstado(Estado estado)
    {
        // Continuar compromete la eleccion: ya no se puede regresar a Neutral.
        if (_estado == Estado.Continuar)
        {
            return;
        }
        _estado = estado;
        if (estado == Estado.Continuar)
        {
            _destelloRestante = DuracionDestello;
        }
        _sinner = estado == Estado.Escapar ? Color.White : Color.Black;
        _relojEstado = 0;
        _titulo.Text = estado switch
        {
            Estado.Neutral => "Y ahora que?",
            Estado.Escapar => "Estas seguro?",
            _ => ""
        };
        ActualizarOpciones();
    }

    private void ActualizarOpciones()
    {
        for (int i = 0; i < _botones.Count; i++)
        {
            _botones[i].boton.IsFocused = false;
            _botones[i].boton.Visual.Visible = _estado switch
            {
                Estado.Neutral => i < 2,
                Estado.Escapar => i == 2 || i == 3,
                _ => i >= 4
            };
            _botones[i].boton.IsEnabled = _botones[i].boton.Visual.Visible
                && !_entrando && _destelloRestante <= 0f;
        }
        int foco = _estado == Estado.Neutral ? 0 : _estado == Estado.Escapar ? 3 : 4;
        _botones[foco].boton.IsFocused = true;
    }

    private void ElegirEnemigo(Enemigo enemigo)
    {
        if (_entrando || _estado != Estado.Continuar || _destelloRestante > 0f
            || !_opcionesRivales.Contains(enemigo)
            || (enemigo == Enemigo.Sorak && _sorakElegido))
        {
            return;
        }
        _enemigoSeleccionado = enemigo;
        EntrarAPrueba();
    }

    private void EntrarAPrueba(bool menosTiempo = false)
    {
        if (_entrando || _estado != Estado.Continuar || _destelloRestante > 0f
            || (!menosTiempo && !_enemigoSeleccionado.HasValue))
        {
            return;
        }
        _entrando = true;
        ActualizarOpciones();
        _cambiarEscena(new Partida(_cambiarEscena, _numeroNivel, _transicionReproducida,
            _cantidadDuke + (_enemigoSeleccionado == Enemigo.Duke ? 1 : 0),
            _cantidadJarJar + (_enemigoSeleccionado == Enemigo.JarJar ? 1 : 0),
            _sorakElegido || _enemigoSeleccionado == Enemigo.Sorak,
            duracionNivel: menosTiempo ? Math.Max(double.Epsilon, _duracionNivel * 0.67) : _duracionNivel));
    }

    public override void Update(GameTime gameTime)
    {
        double dt = gameTime.ElapsedGameTime.TotalSeconds;
        if (_destelloRestante > 0f)
        {
            _destelloRestante = Math.Max(0f, _destelloRestante - (float)dt);
            if (_destelloRestante == 0f)
            {
                ActualizarOpciones();
            }
        }
        _destello.Visible = _destelloRestante > 0f;
        _destello.Color = new Color(255, 185, 190) * (_destelloRestante / DuracionDestello);
        _relojEstado += dt;
        _relojFondo += dt;
        float suavidad = 1f - MathF.Exp(-8f * (float)dt);
        bool escapar = _estado == Estado.Escapar;
        bool continuar = _estado == Estado.Continuar;
        _opacidadDuda = MathHelper.Clamp(_opacidadDuda
            + (escapar ? 1f : -1f) * (float)dt / 0.45f, 0f, 1f);
        Color rojo = new Color(165, 12, 24);
        _fondo = Color.Lerp(_fondo, continuar ? rojo : escapar ? Color.Black : Color.White, suavidad);
        _tinta = Color.Lerp(_tinta, escapar ? Color.White : Color.Black, suavidad);
        _titulo.Color = _tinta;
        for (int i = 0; i < 3; i++)
        {
            _mezcla[i] = MathHelper.Lerp(_mezcla[i], i == (int)_estado ? 1f : 0f, suavidad);
            Core.Audio.SetVolume(_musicas[i], _mezcla[i] * 0.45f);
        }
        for (int i = 0; i < _botones.Count; i++)
        {
            var item = _botones[i];
            // El foco no cambia la tinta a blanco sobre el fondo blanco.
            item.texto.Color = item.boton.IsEnabled ? _tinta : Color.Lerp(_tinta, _fondo, 0.65f);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(_fondo);
        var viewport = Core.GraphicsDevice.Viewport;
        var area = new Rectangle(0, 0, viewport.Width, viewport.Height);
        int estado = (int)_estado;
        double fps = estado == 0 ? 8 : 24;
        int cantidad = _cantidadFrames[estado];
        double avance = Math.Floor(_relojEstado * fps);
        int frame;
        if (estado == 0)
            frame = (int)(avance % cantidad);
        else if (avance < cantidad)
            frame = (int)avance;
        else
        {
            // despues de la primera pasada, solo se repiten los seis cuadros finales.
            int framesLoop = Math.Min(6, cantidad);
            frame = cantidad - framesLoop + (int)((avance - cantidad) % framesLoop);
        }
        var origen = new Rectangle(frame % 6 * 640, frame / 6 * 360, 640, 360);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_estado == Estado.Continuar)
            DibujarFondo(2, area, 1f);
        else
        {
            // el fondo opaco debajo evita un oscurecimiento durante el fundido.
            DibujarFondo(0, area, 1f);
            if (_opacidadDuda > 0f) DibujarFondo(1, area, _opacidadDuda);
        }
        Core.SpriteBatch.Draw(_animaciones[estado], area, origen, _sinner);
        if (_estado == Estado.Continuar)
        {
            DibujarRivales(viewport);
        }
        Core.SpriteBatch.End();
    }

    private static string NombreRival(Enemigo rival) => rival switch
    {
        Enemigo.Sorak => "Zorak",
        Enemigo.JarJar => "Jarjar",
        _ => "Duck"
    };

    private void DibujarRivales(Viewport viewport)
    {
        // Velo rojo oscuro sobre el arte y debajo de los textos para mantener el contraste.
        Core.SpriteBatch.Draw(_pixelPanel, new Rectangle(0, 0, viewport.Width, viewport.Height),
            new Color(65, 0, 8) * 0.82f);
        const string titulo = "Elige una prueba";
        Vector2 centro = new Vector2(viewport.Width, viewport.Height) * 0.5f;
        Core.SpriteBatch.DrawString(_fuenteRivales, titulo,
            centro + new Vector2(0, -230f), new Color(255, 85, 95),
            0f, _fuenteRivales.MeasureString(titulo) * 0.5f, 2f, SpriteEffects.None, 0f);
        for (int i = 0; i < _opcionesRivales.Count; i++)
        {
            string nombre = NombreRival(_opcionesRivales[i]);
            Color color = new Color(255, 65, 80);
            const float escala = 2.6f;
            Vector2 posicion = centro + new Vector2(0, -80 + i * 140);
            Core.SpriteBatch.DrawString(_fuenteRivales, nombre, posicion, color,
                0f, _fuenteRivales.MeasureString(nombre) * 0.5f, escala, SpriteEffects.None, 0f);
        }
        const string opcionTiempo = "Menos tiempo";
        Vector2 posicionTiempo = centro + new Vector2(0, 200);
        Core.SpriteBatch.DrawString(_fuenteRivales, opcionTiempo, posicionTiempo, new Color(255, 65, 80),
            0f, _fuenteRivales.MeasureString(opcionTiempo) * 0.5f, 2.6f, SpriteEffects.None, 0f);
        string detalle = "-33% de tiempo: " + RelojNivel.Formatear(_duracionNivel * 0.67);
        Core.SpriteBatch.DrawString(_fuenteRivales, detalle, posicionTiempo + new Vector2(0, 48),
            new Color(255, 175, 180), 0f, _fuenteRivales.MeasureString(detalle) * 0.5f,
            1f, SpriteEffects.None, 0f);
    }

    private void DibujarFondo(int estado, Rectangle destino, float opacidad)
    {
        int cuadro = (int)(_relojFondo * 8) % _cuadrosFondo[estado];
        int pagina = cuadro / 12;
        int celda = cuadro % 12;
        var origen = new Rectangle(celda % 3 * 1280, celda / 3 * 720, 1280, 720);
        Core.SpriteBatch.Draw(_fondosAnimados[estado, pagina], destino, origen, Color.White * opacidad);
    }

    protected override void UnloadContent()
    {
        _pixelPanel?.Dispose();
        foreach (SoundEffectInstance pista in _musicas) Core.Audio.ReleaseSoundEffect(pista);
        foreach (var item in _botones) item.boton.IsFocused = false;
    }
}
