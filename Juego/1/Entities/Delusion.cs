using System;
using System.Collections.Generic;
using _1.Colisiones;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public enum ResultadoDelusion { Nada, Parreado, Golpe }

public sealed class Delusion
{
    private const float EscalaDelusion = 0.6f;
    private const float EscalaTeletransporte = 1.2f;
    private const float EscalaBola = 1.5f;
    // Dispara mientras Sinner este dentro de este alcance; mas lejos lo persigue teletransportandose.
    private const float AlcanceDisparo = 600f;
    private const float DistanciaMinimaDestino = 160f;
    private const float DistanciaMaximaDestino = 280f;
    private const float IntervaloDisparo = 1.8f;
    private const float SegundosPorFramePreparacion = 0.12f;
    private const float VelocidadBola = 380f;
    private const float RadioBola = 18f;
    private const float VidaBola = 3f;
    private const float SegundosPorFrameRotura = 0.08f;
    private const float DuracionVuelo = 0.9f;
    private const float AlturaVuelo = 140f;
    private const float SegundosPorFrameCaida = 0.08f;
    private const float EsperaTrasTeletransporte = 1f;
    private const float IntervaloTeletransporte = 4f;
    // La bola azul espera junto a Delusion, del lado contrario a Sinner.
    private static readonly Vector2 SeparacionFuegoAzul = new(85f, -10f);
    private enum Estado { Acechando, Preparando, Volando, Cayendo }

    private sealed class Bola
    {
        public Vector2 Posicion;
        public Vector2 PosicionAnterior;
        public Vector2 Direccion;
        public float Tiempo;
        public bool Rota;
    }

    private Texture2D _cuerpo;
    private Texture2D _proyectil;
    private Texture2D _fuegoMovimiento;
    private Texture2D _fuegoCaido;
    private Texture2D _fuegoQuieto;
    private readonly List<Bola> _bolas = new();
    private Estado _estado;
    private float _tiempo;
    private float _reloj;
    private float _espera = 2f;
    private float _esperaTeletransporte;
    private Vector2 _origenVuelo;
    private Vector2 _destino;
    private bool _mirandoIzquierda;
    public Vector2 Position { get; private set; }
    public int ParrysEsteFrame { get; private set; }

    public void LoadContent(ContentManager content)
    {
        const string carpeta = "Sprites/Enemies/Delusion/";
        _cuerpo = content.Load<Texture2D>(carpeta + "Sprite-Delusion-Sheet");
        _proyectil = content.Load<Texture2D>(carpeta + "Sprite-Delusion-ProyectilAtaque-Sheet");
        _fuegoMovimiento = content.Load<Texture2D>(carpeta + "Sprite-Delusion-FuegoMovimientoTP-sheet");
        _fuegoCaido = content.Load<Texture2D>(carpeta + "Sprite-Delusion-FuegoCaidoTP-sheet");
        _fuegoQuieto = content.Load<Texture2D>(carpeta + "Sprite-Delusion-FuegoQuietoTP-sheet");
    }

    public void Ubicar(Vector2 centroSinner, IReadOnlyList<Vector2> posicionesLibres)
    {
        Position = ElegirDestino(centroSinner, posicionesLibres, out Vector2 destino)
            ? destino : centroSinner + new Vector2(DistanciaMaximaDestino, 0f);
        _bolas.Clear();
        _espera = 2f;
        CambiarEstado(Estado.Acechando);
    }

    public ResultadoDelusion Update(GameTime gameTime, Sinner sinner, Vector2 centroAnterior,
        IReadOnlyList<ColisionSolida> obstaculos, IReadOnlyList<Vector2> posicionesLibres)
    {
        ParrysEsteFrame = 0;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _tiempo += dt;
        _reloj = (_reloj + dt) % 100f;
        _esperaTeletransporte = Math.Max(0f, _esperaTeletransporte - dt);
        Vector2 objetivo = sinner.Colision.Centro;
        switch (_estado)
        {
            case Estado.Acechando:
                _espera -= dt;
                _mirandoIzquierda = objetivo.X < Position.X;
                if (Vector2.DistanceSquared(Position, objetivo) > AlcanceDisparo * AlcanceDisparo)
                {
                    if (_esperaTeletransporte <= 0f
                        && ElegirDestino(objetivo, posicionesLibres, out _destino))
                    {
                        _origenVuelo = PosicionFuegoAzul;
                        CambiarEstado(Estado.Volando);
                    }
                }
                else if (_espera <= 0f)
                {
                    CambiarEstado(Estado.Preparando);
                }
                break;
            case Estado.Preparando:
                if (_tiempo >= SegundosPorFramePreparacion * 3f)
                {
                    Disparar(objetivo);
                    _espera = IntervaloDisparo;
                    CambiarEstado(Estado.Acechando);
                }
                break;
            case Estado.Volando:
                // Delusion sigue en su lugar hasta que la bola azul cae: recien ahi se teletransporta.
                if (_tiempo >= DuracionVuelo)
                {
                    Position = _destino;
                    CambiarEstado(Estado.Cayendo);
                }
                break;
            case Estado.Cayendo:
                if (_tiempo >= SegundosPorFrameCaida * 4f)
                {
                    _espera = EsperaTrasTeletransporte;
                    _esperaTeletransporte = IntervaloTeletransporte;
                    CambiarEstado(Estado.Acechando);
                }
                break;
        }

        bool parreado = false;
        bool golpe = false;
        for (int i = _bolas.Count - 1; i >= 0; i--)
        {
            Bola bola = _bolas[i];
            bola.Tiempo += dt;
            if (bola.Rota)
            {
                if (bola.Tiempo >= SegundosPorFrameRotura * 4f) { _bolas.RemoveAt(i); }
                continue;
            }
            bola.PosicionAnterior = bola.Posicion;
            bola.Posicion += bola.Direccion * VelocidadBola * dt;
            CirculoColision colision = new(bola.Posicion, RadioBola);
            if (sinner.PuedeParrear
                && sinner.ColisionParry.SeCruzan(centroAnterior, colision, bola.PosicionAnterior))
            {
                Romper(bola);
                parreado = true;
                ParrysEsteFrame++;
            }
            else if (sinner.Colision.SeCruzan(centroAnterior, colision, bola.PosicionAnterior))
            {
                golpe = true;
            }
            else if (bola.Tiempo >= VidaBola || ChocaConPared(colision, obstaculos))
            {
                Romper(bola);
            }
        }
        return golpe ? ResultadoDelusion.Golpe
            : parreado ? ResultadoDelusion.Parreado : ResultadoDelusion.Nada;
    }

    private void Disparar(Vector2 objetivo)
    {
        Vector2 direccion = objetivo - Position;
        if (direccion.LengthSquared() < 0.0001f) { direccion = _mirandoIzquierda ? -Vector2.UnitX : Vector2.UnitX; }
        direccion.Normalize();
        Vector2 inicio = Position + direccion * 30f;
        _bolas.Add(new Bola { Posicion = inicio, PosicionAnterior = inicio, Direccion = direccion });
    }

    private static void Romper(Bola bola)
    {
        bola.Rota = true;
        bola.Tiempo = 0f;
    }

    private static bool ChocaConPared(CirculoColision colision, IReadOnlyList<ColisionSolida> obstaculos)
    {
        for (int i = 0; i < obstaculos.Count; i++)
        {
            if (obstaculos[i].Toca(colision)) { return true; }
        }
        return false;
    }

    private static bool ElegirDestino(Vector2 centroSinner, IReadOnlyList<Vector2> posicionesLibres,
        out Vector2 destino)
    {
        destino = default;
        if (posicionesLibres == null || posicionesLibres.Count == 0) { return false; }
        // Cae en un anillo alrededor de Sinner: ni encima de el ni fuera de su alcance.
        List<Vector2> candidatas = new();
        float medio = (DistanciaMinimaDestino + DistanciaMaximaDestino) * 0.5f;
        float menorError = float.MaxValue;
        for (int i = 0; i < posicionesLibres.Count; i++)
        {
            float distancia = Vector2.Distance(posicionesLibres[i], centroSinner);
            if (distancia >= DistanciaMinimaDestino && distancia <= DistanciaMaximaDestino)
                candidatas.Add(posicionesLibres[i]);
            float error = Math.Abs(distancia - medio);
            if (error < menorError)
            {
                menorError = error;
                destino = posicionesLibres[i];
            }
        }
        if (candidatas.Count > 0) { destino = candidatas[Random.Shared.Next(candidatas.Count)]; }
        return true;
    }

    private void CambiarEstado(Estado estado) { _estado = estado; _tiempo = 0f; }

    private Vector2 PosicionFuegoAzul => Position + new Vector2(
        _mirandoIzquierda ? SeparacionFuegoAzul.X : -SeparacionFuegoAzul.X, SeparacionFuegoAzul.Y);

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_estado == Estado.Cayendo)
        {
            int frame = Math.Min(3, (int)(_tiempo / SegundosPorFrameCaida));
            DibujarFuego(spriteBatch, _fuegoCaido, frame, Position, 0f);
            return;
        }
        // Los cuadros 1-2 son el reposo; 3-5 inclinan la llama antes de disparar.
        int cuadro = _estado == Estado.Preparando
            ? 2 + Math.Min(2, (int)(_tiempo / SegundosPorFramePreparacion))
            : (int)(_reloj / 0.15f) % 2;
        spriteBatch.Draw(_cuerpo, Position, new Rectangle(cuadro * 200, 0, 200, 200), Color.White, 0f,
            new Vector2(100f, 130f), EscalaDelusion,
            _mirandoIzquierda ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

        if (_estado != Estado.Volando)
        {
            // Mientras Sinner sigue en rango, la bola azul queda quieta al lado de Delusion.
            DibujarFuego(spriteBatch, _fuegoQuieto, (int)(_reloj / 0.1f) % 4, PosicionFuegoAzul, 0f);
        }
        else
        {
            // Trayectoria en arco: la altura simula la caida en la vista cenital.
            float t = MathHelper.Clamp(_tiempo / DuracionVuelo, 0f, 1f);
            Vector2 posicion = Vector2.Lerp(_origenVuelo, _destino, t)
                - new Vector2(0f, AlturaVuelo * 4f * t * (1f - t));
            Vector2 velocidad = (_destino - _origenVuelo) / DuracionVuelo
                - new Vector2(0f, AlturaVuelo * 4f * (1f - 2f * t) / DuracionVuelo);
            // El dibujo original avanza hacia la izquierda.
            float rotacion = MathF.Atan2(velocidad.Y, velocidad.X) + MathHelper.Pi;
            DibujarFuego(spriteBatch, _fuegoMovimiento, (int)(_reloj / 0.1f) % 3, posicion, rotacion);
        }
    }

    private static void DibujarFuego(SpriteBatch spriteBatch, Texture2D textura, int frame,
        Vector2 posicion, float rotacion)
    {
        spriteBatch.Draw(textura, posicion, new Rectangle(frame * 100, 0, 100, 100), Color.White,
            rotacion, new Vector2(50f, 60f), EscalaTeletransporte, SpriteEffects.None, 0f);
    }

    public void DrawBolas(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _bolas.Count; i++)
        {
            Bola bola = _bolas[i];
            // Los cuadros 4-7 son el humo que queda al romperse.
            int frame = bola.Rota ? 3 + Math.Min(3, (int)(bola.Tiempo / SegundosPorFrameRotura))
                : (int)(bola.Tiempo / 0.06f) % 3;
            float rotacion = bola.Rota ? 0f : MathF.Atan2(bola.Direccion.Y, bola.Direccion.X) + MathHelper.Pi;
            // La cabeza de la bola esta a la izquierda del cuadro: ahi se ubica la colision.
            Vector2 origen = bola.Rota ? new Vector2(25f) : new Vector2(14f, 25f);
            spriteBatch.Draw(_proyectil, bola.Posicion, new Rectangle(frame * 50, 0, 50, 50), Color.White,
                rotacion, origen, EscalaBola, SpriteEffects.None, 0f);
        }
    }
}
