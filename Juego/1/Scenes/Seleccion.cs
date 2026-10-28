using _1.Progression;
using System;
using System.Collections.Generic;
using _1.UI;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using LutredLibrary;
using LutredLibrary.Scenes;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace _1.Scenes;

public sealed class Seleccion : Escena
{
    private enum Estado { Neutral, Escapar, Continuar }
    private const float PrimeraOpcionY = -225f;
    private const float SeparacionOpciones = 65f;
    private const float OpcionTiempoY = -95f;
    private const float EscalaOpciones = 1.7f;
    private readonly Action<Escena> _cambiarEscena;
    private readonly EstadoMejoras _mejoras;
    private readonly int _numeroNivel;
    private readonly bool _transicionReproducida;
    private readonly int _cantidadDuke;
    private readonly int _cantidadJarJar;
    private readonly bool _sorakElegido;
    private readonly bool _erosionBirdElegido;
    private readonly double _duracionNivel;
    private enum Enemigo { Sorak, JarJar, Duke, ErosionBird }
    private Enemigo? _enemigoSeleccionado;
    private bool _entrando;
    private int _opcionBajoCursor = -1;
    private readonly float[] _brilloOpciones = new float[3];
    private readonly List<Enemigo> _opcionesRivales = new();
    private readonly SoundEffectInstance[] _musicas = new SoundEffectInstance[3];
    private readonly Texture2D[] _animaciones = new Texture2D[3];
    private readonly int[] _cantidadFrames = { 11, 30, 24 };
    private readonly Texture2D[,] _fondosAnimados = new Texture2D[3, 2];
    private readonly int[] _cuadrosFondo = { 20, 20, 22 };
    private readonly List<(Button boton,
        TextRuntime texto)> _botones = new();
    private readonly float[] _mezcla = { 1f, 0f, 0f };
    private Estado _estado;
    private readonly AnimatedSprite[] _frases = new AnimatedSprite[3];
    private readonly List<BotonAnimado> _accionesAnimadas = new();
    private int _varianteEleccion = Random.Shared.Next(2);
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
        double duracionNivel = RelojNivel.Duracion, bool erosionBirdElegido = false, EstadoMejoras mejoras = null)
    {
        _cambiarEscena = cambiarEscena;
        _numeroNivel = numeroNivel;
        _mejoras = mejoras ?? new EstadoMejoras();
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
        _erosionBirdElegido = erosionBirdElegido;
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
        if (!erosionBirdElegido)
        {
            pool.Add(Enemigo.ErosionBird);
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
        string[] frases = { "Elejir_Frase1", "Elejir_Frase2", "Duda_Frase" };
        for (int i = 0; i < frases.Length; i++)
        {
            Texture2D hoja = Content.Load<Texture2D>($"Pharses/Selection/{frases[i]}-Sheet");
            int ancho = hoja.Width / 9;
            var cuadros = new List<TextureRegion>();
            for (int cuadro = 0; cuadro < 9; cuadro++)
            {
                cuadros.Add(new TextureRegion(hoja, cuadro * ancho, 0, ancho, hoja.Height));
            }
            _frases[i] = new AnimatedSprite(new Animation(cuadros, TimeSpan.FromSeconds(0.12)))
            {
                Origin = new Vector2(ancho, hoja.Height) * 0.5f
            };
        }
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

        AgregarAccionAnimada(panel, "Eso es todo", "EsoesTodo", -390,
            () => CambiarEstado(Estado.Escapar));
        AgregarAccionAnimada(panel, "Si...", "Si", 390,
            () => CambiarEstado(Estado.Continuar));
        AgregarAccionAnimada(panel, "Si, estoy seguro", "SiEstoySeguro", -390,
            () => _cambiarEscena(new Inicio(_cambiarEscena)));
        AgregarAccionAnimada(panel, "Espera...", "Espera", 390,
            () => CambiarEstado(Estado.Neutral));
        for (int i = 0; i < _opcionesRivales.Count; i++)
        {
            Enemigo rival = _opcionesRivales[i];
            AgregarBoton(panel, NombreRival(rival), 0, PrimeraOpcionY + i * SeparacionOpciones,
                () => ElegirEnemigo(rival));
            var boton = _botones[i + 4].boton;
            boton.Width = 400;
            boton.Height = 62;
            _botones[i + 4].texto.Visible = false;

        }
        AgregarBoton(panel, "Menos tiempo", 0, OpcionTiempoY, () => EntrarAPrueba(menosTiempo: true));
        var opcionTiempo = _botones[_botones.Count - 1];
        opcionTiempo.boton.Width = 450;
        opcionTiempo.boton.Height = 62;
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

        var etiqueta = visual.TextInstance;
        etiqueta.UseCustomFont = true;
        etiqueta.CustomFontFile = "Fonts/PMingLiU Regular/Font.fnt";
        etiqueta.FontScale = 1.4f;
        boton.Click += (_, _) => accion();
        panel.AddChild(boton);
        int indiceOpcion = _botones.Count - 4;
        boton.Visual.RollOn += (_, _) => _opcionBajoCursor = indiceOpcion;
        boton.Visual.RollOff += (_, _) =>
        {
            if (_opcionBajoCursor == indiceOpcion)
            {
                _opcionBajoCursor = -1;
            }
        };
        _botones.Add((boton, etiqueta));
    }

    private void AgregarAccionAnimada(Panel panel, string nombre, string archivo, float x, Action accion)
    {
        Texture2D hoja = Content.Load<Texture2D>($"Pharses/Selection/Acciones/{archivo}-Sheet");
        var boton = new BotonAnimado(hoja, hoja.Width / 9, hoja.Height, 9, TimeSpan.FromSeconds(0.12))
        {
            // La etiqueta conserva el nombre para la navegacion; el dibujo viene de la hoja.
            Text = nombre,
            Width = 340,
            Height = 170,
            ColorNormal = new Color(195, 195, 195),
            ColorIluminado = Color.White
        };
        boton.Anchor(Anchor.Center);
        boton.X = x;
        var etiqueta = ((ButtonVisual)boton.Visual).TextInstance;
        etiqueta.Visible = false;
        boton.Click += (_, _) => accion();
        panel.AddChild(boton);
        _accionesAnimadas.Add(boton);
        _botones.Add((boton, etiqueta));
    }
    private void CambiarEstado(Estado estado)
    {
        // Continuar compromete la eleccion: ya no se puede regresar a Neutral.
        if (_estado == Estado.Continuar)
        {
            return;
        }
        if (_estado == estado)
        {
            return;
        }
        if (estado == Estado.Escapar)
        {
            // Nuevo sorteo por visita a duda; al volver se conserva esta variante de eleccion.
            _varianteEleccion = Random.Shared.Next(2);
        }
        _estado = estado;
        int indiceFrase = estado == Estado.Escapar ? 2 : _varianteEleccion;
        _frases[indiceFrase].Animation = _frases[indiceFrase].Animation;
        if (estado == Estado.Continuar)
        {
            _destelloRestante = DuracionDestello;
        }
        _sinner = estado == Estado.Escapar ? Color.White : Color.Black;
        _relojEstado = 0;
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
            || (enemigo == Enemigo.Sorak && _sorakElegido)
            || (enemigo == Enemigo.ErosionBird && _erosionBirdElegido))
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
            duracionNivel: menosTiempo ? Math.Max(double.Epsilon, _duracionNivel * 0.67) : _duracionNivel,
            erosionBirdElegido: _erosionBirdElegido || _enemigoSeleccionado == Enemigo.ErosionBird, mejoras: _mejoras));
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
        for (int i = 0; i < _brilloOpciones.Length; i++)
        {
            Button opcion = _botones[i + 4].boton;
            bool resaltada = _opcionBajoCursor >= 0 ? _opcionBajoCursor == i : opcion.IsFocused;
            float objetivo = continuar && opcion.IsEnabled && resaltada ? 1f : 0f;
            _brilloOpciones[i] = MathHelper.Lerp(_brilloOpciones[i], objetivo, suavidad);
        }
        _opacidadDuda = MathHelper.Clamp(_opacidadDuda
            + (escapar ? 1f : -1f) * (float)dt / 0.45f, 0f, 1f);
        Color rojo = new Color(165, 12, 24);
        _fondo = Color.Lerp(_fondo, continuar ? rojo : escapar ? Color.Black : Color.White, suavidad);
        _tinta = Color.Lerp(_tinta, escapar ? Color.White : Color.Black, suavidad);
        if (_estado != Estado.Continuar)
        {
            _frases[_estado == Estado.Escapar ? 2 : _varianteEleccion].Update(gameTime);
        }
        for (int i = 0; i < _accionesAnimadas.Count; i++)
        {
            _accionesAnimadas[i].Update(gameTime);
        }
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
        else
        {
            AnimatedSprite frase = _frases[_estado == Estado.Escapar ? 2 : _varianteEleccion];
            float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
            frase.Position = new Vector2(viewport.Width * 0.5f, 110f * escala);
            frase.Scale = new Vector2(1.2f * escala);
            frase.Draw(Core.SpriteBatch);
        }
        Core.SpriteBatch.End();
    }

    private string NombreRival(Enemigo rival) => rival switch
    {
        Enemigo.Sorak => "Zorak",
        Enemigo.JarJar => _cantidadJarJar > 0 ? "Otro Jarjar" : "Jarjar",
        Enemigo.ErosionBird => "Erosion Bird",
        _ => _cantidadDuke > 0 ? "Otro Duck" : "Duck"
    };

    private void DibujarRivales(Viewport viewport)
    {
        // El velo cubre las opciones y se disuelve antes de la parte inferior de Sinner.
        int inicioFundido = (int)(viewport.Height * 0.36f);
        int finFundido = (int)(viewport.Height * 0.48f);
        Color velo = new Color(65, 0, 8) * 0.82f;
        Core.SpriteBatch.Draw(_pixelPanel, new Rectangle(0, 0, viewport.Width, inicioFundido), velo);
        for (int y = inicioFundido; y < finFundido; y++)
        {
            float opacidad = 1f - (y - inicioFundido) / (float)(finFundido - inicioFundido);
            Core.SpriteBatch.Draw(_pixelPanel, new Rectangle(0, y, viewport.Width, 1), velo * opacidad);
        }
        const string titulo = "ELIGE";
        Vector2 centro = new Vector2(viewport.Width, viewport.Height) * 0.5f;
        Vector2 posicionTitulo = centro + new Vector2(0, -310f);
        // Mismo pulso continuo y halo de doce muestras que el contador de orbes.
        float pulso = 0.5f - 0.5f * MathF.Cos((float)_relojEstado * MathHelper.TwoPi * 1.35f);
        float escalaTitulo = 2f * (1f + pulso * 0.025f);
        Color rojoTitulo = Color.Lerp(new Color(230, 20, 35), new Color(255, 65, 65), pulso);
        Vector2 origenTitulo = _fuenteRivales.MeasureString(titulo) * 0.5f;
        float radioHalo = 3f + pulso * 3f;
        Color brillo = new Color(230, 20, 35) * (0.04f + pulso * 0.18f);
        for (int i = 0; i < 12; i++)
        {
            float angulo = i * MathHelper.TwoPi / 12f;
            Vector2 desplazamiento = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * radioHalo;
            Core.SpriteBatch.DrawString(_fuenteRivales, titulo, posicionTitulo + desplazamiento,
                brillo, 0f, origenTitulo, escalaTitulo, SpriteEffects.None, 0f);
        }
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }
                Core.SpriteBatch.DrawString(_fuenteRivales, titulo,
                    posicionTitulo + new Vector2(x, y) * 3f, Color.Black,
                    0f, origenTitulo, escalaTitulo, SpriteEffects.None, 0f);
            }
        }
        Core.SpriteBatch.DrawString(_fuenteRivales, titulo,
            posicionTitulo, rojoTitulo,
            0f, origenTitulo, escalaTitulo, SpriteEffects.None, 0f);
        for (int i = 0; i < _opcionesRivales.Count; i++)
        {
            string nombre = NombreRival(_opcionesRivales[i]);
            Vector2 posicion = centro + new Vector2(0, PrimeraOpcionY + i * SeparacionOpciones);
            DibujarOpcion(nombre, posicion, _brilloOpciones[i]);
        }
        const string opcionTiempo = "Menos tiempo";
        Vector2 posicionTiempo = centro + new Vector2(0, OpcionTiempoY);
        DibujarOpcion(opcionTiempo, posicionTiempo, _brilloOpciones[2]);
    }

    private void DibujarOpcion(string texto, Vector2 posicion, float brillo)
    {
        Vector2 origen = _fuenteRivales.MeasureString(texto) * 0.5f;
        float escala = EscalaOpciones * (1f + brillo * 0.025f);
        if (brillo > 0.01f)
        {
            for (int i = 0; i < 12; i++)
            {
                float angulo = i * MathHelper.TwoPi / 12f;
                Vector2 halo = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * 3f;
                Core.SpriteBatch.DrawString(_fuenteRivales, texto, posicion + halo,
                    new Color(255, 65, 80) * (brillo * 0.13f),
                    0f, origen, escala, SpriteEffects.None, 0f);
            }
        }
        Color color = Color.Lerp(new Color(255, 65, 80), new Color(255, 175, 170), brillo);
        Core.SpriteBatch.DrawString(_fuenteRivales, texto, posicion, color,
            0f, origen, escala, SpriteEffects.None, 0f);
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
        for (int i = 0; i < _accionesAnimadas.Count; i++)
        {
            _accionesAnimadas[i].Dispose();
        }
        _pixelPanel?.Dispose();
        foreach (SoundEffectInstance pista in _musicas) Core.Audio.ReleaseSoundEffect(pista);
        foreach (var item in _botones) item.boton.IsFocused = false;
    }
}
