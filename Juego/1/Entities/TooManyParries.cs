using System;
using System.Collections.Generic;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

// Al aparecer cuenta nuevos intentos; solo bloquea el parry al alcanzar el limite.
public sealed class TooManyParries
{
    private const int TamanoFrame = 200;
    private const int CantidadFrames = 17;
    private const float SegundosPorFrame = 0.08f;
    // Pulsaciones de la tecla de parry, acertadas o no, que lo invocan dentro de la ventana.
    private const int PulsacionesParaAparecer = 5;
    private const float VentanaParrys = 3f;
    private const float DuracionPresencia = 5f;
    private const float DuracionBloqueo = 5f;
    public const float MultiplicadorVelocidad = 0.8f;
    private const float DuracionFundido = 0.3f;
    private const float Margen = 24f;
    private const float DuracionEntradaCadena = 0.32f;
    private const float IntervaloSonidoBloqueo = 0.09f;

    private sealed class Cadena
    {
        public int Esquina;
        public float ExtensionX, ExtensionY, Escala, Edad;
    }

    private Texture2D _textura;
    private Texture2D _texturaCadena;
    private Texture2D _vineta;
    private SpriteFont _fuente;
    private readonly List<Cadena> _cadenas = new();
    private readonly SoundEffectInstance[] _sonidosCadena = new SoundEffectInstance[5];
    private int _siguienteSonido;
    private int _sonidosPendientes;
    private float _esperaSonido;
    private float _intensidadVineta;
    private readonly Queue<float> _pulsaciones = new();
    private float _reloj;
    private float _tiempoActivo;
    private float _duracion;
    public bool Activo { get; private set; }
    public int ContadorParrys { get; private set; }
    public int LimiteParrys { get; private set; } = 3;
    public bool BloqueandoParrys => Activo && ContadorParrys >= LimiteParrys;

    public void LoadContent(ContentManager content)
    {
        _textura = content.Load<Texture2D>("Sprites/TooManyParries/modelo-TooManyParrys-Sheet");
        _texturaCadena = content.Load<Texture2D>("Sprites/Enemies/TooManyParries/Chain");
        _fuente = content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        var sonido = content.Load<SoundEffect>("SFX/Entities/ToManyParries/Encadenado");
        for (int i = 0; i < _sonidosCadena.Length; i++)
            _sonidosCadena[i] = Core.Audio.CreateSoundEffectInstance(sonido, volume: 0.65f);

        // Viñeta eliptica: el centro permanece limpio incluso durante el bloqueo.
        const int ancho = 320, alto = 180;
        _vineta = new Texture2D(Core.GraphicsDevice, ancho, alto);
        var pixeles = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
        for (int x = 0; x < ancho; x++)
        {
            float nx = 2f * x / (ancho - 1) - 1f;
            float ny = 2f * y / (alto - 1) - 1f;
            float distancia = MathF.Sqrt(nx * nx + ny * ny);
            float alfa = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((distancia - 0.42f) / 0.92f, 0f, 1f));
            pixeles[y * ancho + x] = Color.Black * alfa;
        }
        _vineta.SetData(pixeles);
    }

    public void RegistrarPulsacion(bool pazActiva)
    {
        if (Activo)
        {
            if (BloqueandoParrys) return;
            ContadorParrys++;
            if (ContadorParrys >= LimiteParrys)
            {
                _duracion = _tiempoActivo + DuracionBloqueo;
                CompletarCadenas();
            }
            else
            {
                AgregarCadena(Random.Shared.Next(4));
                SonarCadena();
            }
            return;
        }
        _pulsaciones.Enqueue(_reloj);
        while (_pulsaciones.Count > 0 && _reloj - _pulsaciones.Peek() >= VentanaParrys) { _pulsaciones.Dequeue(); }
        if (_pulsaciones.Count < PulsacionesParaAparecer) { return; }
        _pulsaciones.Clear();
        _tiempoActivo = 0f;
        _duracion = DuracionPresencia;
        ContadorParrys = 0;
        _cadenas.Clear();
        _intensidadVineta = 0.3f;
        LimiteParrys = pazActiva ? 5 : 3;
        Activo = true;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _reloj += dt;
        if (!Activo) { return; }
        _tiempoActivo += dt;
        if (_tiempoActivo >= _duracion)
        {
            Activo = false;
            _cadenas.Clear();
            _sonidosPendientes = 0;
            return;
        }
        float objetivo = MathHelper.Lerp(0.3f, 0.85f, (float)ContadorParrys / LimiteParrys);
        _intensidadVineta = MathHelper.Lerp(_intensidadVineta, objetivo, Math.Min(1f, dt * 8f));
        foreach (Cadena cadena in _cadenas) cadena.Edad += dt;
        if (_sonidosPendientes > 0)
        {
            _esperaSonido -= dt;
            while (_sonidosPendientes > 0 && _esperaSonido <= 0f)
            {
                SonarCadena();
                _sonidosPendientes--;
                _esperaSonido += IntervaloSonidoBloqueo;
            }
        }
    }

    private void AgregarCadena(int esquina, float demora = 0f)
    {
        // Cada tramo cruza diagonalmente entre los dos bordes que forman su esquina.
        _cadenas.Add(new Cadena
        {
            Esquina = esquina,
            ExtensionX = 130f + Random.Shared.NextSingle() * 300f,
            ExtensionY = 85f + Random.Shared.NextSingle() * 185f,
            Escala = 0.24f + Random.Shared.NextSingle() * 0.12f,
            Edad = -demora
        });
    }

    private void CompletarCadenas()
    {
        int total = Random.Shared.Next(12, 15);
        int agregadas = 0;
        int primera = Random.Shared.Next(4);
        // Garantizar que las cuatro esquinas tengan cadenas, manteniendo su reparto aleatorio.
        for (int i = 0; i < 4; i++)
        {
            int esquina = (primera + i) % 4;
            if (!_cadenas.Exists(c => c.Esquina == esquina))
                AgregarCadena(esquina, agregadas++ * 0.025f);
        }
        while (_cadenas.Count < total)
        {
            int esquina = Random.Shared.Next(4);
            if (_cadenas.FindAll(c => c.Esquina == esquina).Count >= 5) continue;
            AgregarCadena(esquina, agregadas++ * 0.025f);
        }
        // La oleada completa reproduce exactamente cinco sonidos, incluyendo el del parry limite.
        SonarCadena();
        _sonidosPendientes = 4;
        _esperaSonido = IntervaloSonidoBloqueo;
    }

    private void SonarCadena()
    {
        Core.Audio.PlaySoundEffect(_sonidosCadena[_siguienteSonido], restart: true);
        _siguienteSonido = (_siguienteSonido + 1) % _sonidosCadena.Length;
    }

    private float AlfaPresencia => MathHelper.Clamp(Math.Min(_tiempoActivo, _duracion - _tiempoActivo) / DuracionFundido, 0f, 1f);

    public void DrawVineta(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (Activo) spriteBatch.Draw(_vineta, viewport.Bounds, Color.White * (AlfaPresencia * _intensidadVineta));
    }

    public void DrawCadenas(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (!Activo) return;
        float escalaPantalla = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        foreach (Cadena cadena in _cadenas)
        {
            if (cadena.Edad < 0f) continue;
            bool derecha = (cadena.Esquina & 1) != 0;
            bool abajo = (cadena.Esquina & 2) != 0;
            Vector2 esquina = new(derecha ? viewport.Width : 0f, abajo ? viewport.Height : 0f);
            float signoX = derecha ? -1f : 1f;
            float signoY = abajo ? -1f : 1f;
            float entrada = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(cadena.Edad / DuracionEntradaCadena, 0f, 1f));
            Vector2 desplazamiento = new Vector2(-signoX, -signoY) * (1f - entrada) * 250f * escalaPantalla;
            Vector2 inicio = esquina + new Vector2(signoX * cadena.ExtensionX * escalaPantalla, 0f) + desplazamiento;
            Vector2 fin = esquina + new Vector2(0f, signoY * cadena.ExtensionY * escalaPantalla) + desplazamiento;
            Vector2 tramo = fin - inicio;
            float longitud = tramo.Length();
            if (longitud <= 0f) continue;
            Vector2 direccion = tramo / longitud;
            float rotacion = MathF.Atan2(direccion.Y, direccion.X) - MathHelper.PiOver2;
            float escala = cadena.Escala * escalaPantalla;
            float largoSprite = _texturaCadena.Height * escala;
            Color color = Color.White * (entrada * AlfaPresencia);
            // Prolongar ambos extremos fuera de pantalla para no cortar los eslabones
            // en diagonal en los bordes. Dibujar siempre la textura completa.
            float margenCadena = _texturaCadena.Width * escala * 0.5f;
            float extension = margenCadena / Math.Min(Math.Abs(direccion.X), Math.Abs(direccion.Y)) + largoSprite;
            for (float distancia = -extension; distancia < longitud + extension; distancia += largoSprite)
            {
                spriteBatch.Draw(_texturaCadena, inicio + direccion * distancia,
                    null, color, rotacion,
                    new Vector2(_texturaCadena.Width * 0.5f, 0f), escala, SpriteEffects.None, 0f);
            }
        }
    }

    public void DetenerSonidos()
    {
        _sonidosPendientes = 0;
        foreach (var sonido in _sonidosCadena) Core.Audio.StopSoundEffect(sonido);
    }

    public void UnloadContent()
    {
        DetenerSonidos();
        foreach (var sonido in _sonidosCadena) Core.Audio.ReleaseSoundEffect(sonido);
        _vineta?.Dispose();
        _cadenas.Clear();
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (!Activo) { return; }
        float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        float alfa = AlfaPresencia;
        int frame = (int)(_tiempoActivo / SegundosPorFrame) % CantidadFrames;
        // Anclado a la esquina superior derecha, sin importar el tamano de la ventana.
        Vector2 posicion = new(viewport.Width - (Margen + TamanoFrame) * escala, Margen * escala);
        spriteBatch.Draw(_textura, posicion, new Rectangle(frame * TamanoFrame, 0, TamanoFrame, TamanoFrame),
            Color.White * alfa, 0f, Vector2.Zero, escala, SpriteEffects.None, 0f);
        string numero = ContadorParrys.ToString();
        float escalaTexto = escala;
        Vector2 tamano = _fuente.MeasureString(numero) * escalaTexto;
        Vector2 posicionNumero = posicion + new Vector2((TamanoFrame * escala - tamano.X) * 0.5f,
            TamanoFrame * escala + 4f * escala);
        Color borde = Color.Lerp(new Color(200, 200, 200), Color.Black,
            (float)ContadorParrys / LimiteParrys);
        _1.UI.MarcadorPuntaje.DibujarTexto(_fuente, numero, posicionNumero, escalaTexto,
            opacidad: alfa, color: Color.Black, colorBorde: borde, grosorBorde: 2f * escala);
    }
}
