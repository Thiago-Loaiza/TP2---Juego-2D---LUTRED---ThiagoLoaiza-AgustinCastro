using System;
using System.Collections.Generic;
using _1.Colisiones;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public enum ResultadoErosionBird { Nada, Parreado, Golpe }

public sealed class ErosionBird
{
    private const float EscalaLanza = 1.5f;
    private const float RadioPortales = 240f;
    private const float EscalaAve = 1.8f;
    // Las dos poses sumaban 4 s: ahora suman 1.5 s (2.5 s menos).
    private const float DuracionPreparacion = 0.75f;
    // Mantiene la pose final medio segundo y deja margen para salir por el lado libre.
    private const float DuracionAtaque = 0.5f;
    private const float SeguimientoTrasAtaque = 0.25f;
    private const float SegundosPorFrameReversa = 0.05f;
    private const float DuracionReversa = 15 * SegundosPorFrameReversa;
    private const float DuracionRetirada = 0.32f;
    private const float RecorridoEstocada = 100f;
    private const float InicioEstocada = 0.1f;
    private const float DuracionRotura = 0.64f;
    private const float DuracionRestos = 1f;
    private const float DuracionDesvanecimiento = 0.45f;
    private enum Estado { Oculto, Mirando, Preparando, Cargando, Atacando, Revirtiendo, Restos, Desvaneciendo }
    private enum EstadoLanza { Portal, Ataque, Retirando, Rota, Restos, Desvaneciendo }
    private sealed class Lanza
    {
        public Vector2 Posicion;
        public Vector2 PosicionAnterior;
        public Vector2 Direccion;
        public float Desplazamiento;
        public EstadoLanza Estado;
        public float Tiempo;
    }

    private Texture2D _ave;
    private Texture2D _ataque;
    private readonly List<Lanza> _lanzas = new();
    private readonly Dictionary<string, SoundEffectInstance> _sonidos = new();
    private readonly float[] _inicioLanza = new float[22];
    private readonly float[] _finLanza = new float[22];
    private Estado _estado;
    private float _tiempo;
    private float _espera = 2f;
    private Vector2 _posicion;
    private Vector2 _centroPortales;
    private float _seguimientoRestante = float.PositiveInfinity;

    // El ave vive en coordenadas de pantalla; las lanzas viven en el mapa.
    public Rectangle AreaOcupada => _estado == Estado.Oculto ? Rectangle.Empty
        : new Rectangle((int)(_posicion.X - 100f * EscalaAve), (int)(_posicion.Y - 100f * EscalaAve),
            (int)(200f * EscalaAve), (int)(200f * EscalaAve));

    public void LoadContent(ContentManager content)
    {
        _ave = content.Load<Texture2D>("Sprites/Enemies/ErosionBird/Erosion Bird-Sheet");
        _ataque = content.Load<Texture2D>("Sprites/Enemies/ErosionBird/EB_Attack-Sheet");
        string[] nombres = { "Aparicion", "SpawnerLanzas", "AtaqueLanza", "MuerteLanza", "DespawnLanzas", "Salida" };
        for (int i = 0; i < nombres.Length; i++)
        {
            _sonidos.Add(nombres[i], Core.Audio.CreateSoundEffectInstance(
                content.Load<SoundEffect>("SFX/Entities/ErosionBird/" + nombres[i]), volume: 0.7f));
        }
        // Medir solo el rojo de la lanza: el humo gris del portal no hace dano.
        Color[] pixeles = new Color[_ataque.Width * _ataque.Height];
        _ataque.GetData(pixeles);
        for (int frame = 4; frame < 12; frame++)
        {
            float inicio = 200f;
            float fin = 0f;
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 200; x++)
                {
                    Color color = pixeles[y * _ataque.Width + frame * 200 + x];
                    if (color.A > 0 && color.R > color.G * 1.5f && color.R > color.B * 1.5f)
                    {
                        inicio = Math.Min(inicio, x);
                        fin = Math.Max(fin, x);
                    }
                }
            }
            _inicioLanza[frame] = inicio;
            _finLanza[frame] = fin;
        }
    }

    public ResultadoErosionBird Update(GameTime gameTime, Sinner sinner,
        Vector2 centroAnterior, Viewport viewport, Rectangle ocupado)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _tiempo += dt;
        switch (_estado)
        {
            case Estado.Oculto:
                _espera -= dt;
                if (_espera <= 0f && _lanzas.Count == 0 && ElegirPosicion(viewport, ocupado))
                {
                    CambiarEstado(Estado.Mirando);
                    Sonar("Aparicion");
                }
                break;
            case Estado.Mirando:
                if (_tiempo >= 3.5f)
                {
                    _centroPortales = sinner.Colision.Centro;
                    _seguimientoRestante = float.PositiveInfinity;
                    // Tres de cuatro lados: el cuarto queda libre durante toda la invocacion.
                    int ladoLibre = Random.Shared.Next(4);
                    float orientacion = Random.Shared.Next(2) * MathHelper.PiOver4;
                    for (int i = 0; i < 4; i++)
                    {
                        if (i == ladoLibre) { continue; }
                        float angulo = orientacion + i * MathHelper.PiOver2;
                        Vector2 radial = new(MathF.Cos(angulo), MathF.Sin(angulo));
                        _lanzas.Add(new Lanza { Posicion = _centroPortales + radial * RadioPortales,
                            PosicionAnterior = _centroPortales + radial * RadioPortales,
                            Direccion = -radial });
                    }
                    Sonar("SpawnerLanzas");
                    CambiarEstado(Estado.Preparando);
                }
                break;
            case Estado.Preparando:
                if (_tiempo >= DuracionPreparacion) { CambiarEstado(Estado.Cargando); }
                break;
            case Estado.Cargando:
                if (_tiempo >= DuracionPreparacion)
                {
                    _centroPortales = sinner.Colision.Centro;
                    for (int i = 0; i < _lanzas.Count; i++)
                    {
                        _lanzas[i].Estado = EstadoLanza.Ataque;
                        _lanzas[i].Tiempo = 0f;
                    }
                    _seguimientoRestante = SeguimientoTrasAtaque;
                    Sonar("AtaqueLanza");
                    CambiarEstado(Estado.Atacando);
                }
                break;
            case Estado.Atacando:
                if (_tiempo >= DuracionAtaque) { CambiarEstado(Estado.Revirtiendo); }
                break;
            case Estado.Revirtiendo:
                if (_tiempo >= DuracionReversa) { CambiarEstado(Estado.Restos); }
                break;
            case Estado.Restos:
                if (_tiempo >= DuracionRestos)
                {
                    Sonar("Salida");
                    CambiarEstado(Estado.Desvaneciendo);
                }
                break;
            case Estado.Desvaneciendo:
                if (_tiempo >= DuracionDesvanecimiento)
                {
                    CambiarEstado(Estado.Oculto);
                    _espera = 4f;
                }
                break;
        }
        // Sigue un cuarto de segundo desde el lanzamiento y luego fija el centro.
        // Si el corte cae entre dos frames, conservar el punto exacto del recorrido.
        if (_seguimientoRestante > 0f)
        {
            float fraccion = dt > 0f ? Math.Min(1f, _seguimientoRestante / dt) : 1f;
            _centroPortales = Vector2.Lerp(centroAnterior, sinner.Colision.Centro, fraccion);
            _seguimientoRestante = Math.Max(0f, _seguimientoRestante - dt);
        }
        bool parreado = false;
        bool golpe = false;
        bool cerrar = false;
        for (int i = _lanzas.Count - 1; i >= 0; i--)
        {
            Lanza lanza = _lanzas[i];
            lanza.PosicionAnterior = lanza.Posicion;
            int anterior = Frame(lanza);
            lanza.Tiempo += dt;
            MoverLanza(lanza);
            if (lanza.Estado == EstadoLanza.Ataque)
            {
                int actual = Frame(lanza);
                if (sinner.PuedeParrear && Toca(lanza, anterior, actual, sinner.ColisionParry, centroAnterior))
                {
                    lanza.Estado = EstadoLanza.Rota;
                    lanza.Tiempo = 0f;
                    parreado = true;
                }
                else if (Toca(lanza, anterior, actual, sinner.Colision, centroAnterior))
                {
                    golpe = true;
                }
                if (lanza.Estado == EstadoLanza.Ataque && lanza.Tiempo >= DuracionAtaque)
                {
                    lanza.Estado = EstadoLanza.Retirando;
                    lanza.Tiempo = 0f;
                }
            }
            else if ((lanza.Estado == EstadoLanza.Rota && lanza.Tiempo >= DuracionRotura)
                || (lanza.Estado == EstadoLanza.Retirando && lanza.Tiempo >= DuracionRetirada))
            {
                lanza.Estado = EstadoLanza.Restos;
                lanza.Tiempo = 0f;
            }
            else if (lanza.Estado == EstadoLanza.Restos && lanza.Tiempo >= DuracionRestos)
            {
                lanza.Estado = EstadoLanza.Desvaneciendo;
                lanza.Tiempo = 0f;
                cerrar = true;
            }
            else if (lanza.Estado == EstadoLanza.Desvaneciendo && lanza.Tiempo >= DuracionDesvanecimiento)
            {
                _lanzas.RemoveAt(i);
            }
        }
        if (parreado) { Sonar("MuerteLanza"); }
        if (cerrar) { Sonar("DespawnLanzas"); }
        // El parry solo rompe las lanzas que alcanza.
        return golpe ? ResultadoErosionBird.Golpe
            : parreado ? ResultadoErosionBird.Parreado : ResultadoErosionBird.Nada;
    }

    private void MoverLanza(Lanza lanza)
    {
        if (lanza.Estado == EstadoLanza.Portal)
        {
            lanza.Desplazamiento = 0f;
        }
        else if (lanza.Estado == EstadoLanza.Ataque)
        {
            float avance = MathHelper.Clamp((lanza.Tiempo - InicioEstocada)
                / (DuracionAtaque - InicioEstocada), 0f, 1f);
            lanza.Desplazamiento = RecorridoEstocada * avance * avance;
        }
        else if (lanza.Estado == EstadoLanza.Retirando)
        {
            float vuelta = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp(lanza.Tiempo / DuracionRetirada, 0f, 1f));
            lanza.Desplazamiento = RecorridoEstocada * (1f - vuelta);
        }
        // El avance es local al centro, que queda fijo al agotarse el seguimiento.
        lanza.Posicion = _centroPortales
            - lanza.Direccion * (RadioPortales - lanza.Desplazamiento);
    }
    private bool Toca(Lanza lanza, int anterior, int actual, CirculoColision jugador, Vector2 centroAnterior)
    {
        float inicio = _inicioLanza[actual];
        float fin = _finLanza[actual];
        if (inicio > fin) { return false; }
        // Circulos solapados cubren el asta y la punta, incluyendo su extension entre frames.
        for (float x = inicio; x <= fin + 3f; x += 3f)
        {
            float proporcion = fin == inicio ? 0f : MathHelper.Clamp((x - inicio) / (fin - inicio), 0f, 1f);
            float previo = MathHelper.Lerp(_inicioLanza[anterior], _finLanza[anterior], proporcion);
            if (_inicioLanza[anterior] > _finLanza[anterior]) { previo = inicio; }
            Vector2 lateral = new Vector2(-lanza.Direccion.Y, lanza.Direccion.X) * (4f * EscalaLanza);
            Vector2 posicion = lanza.Posicion + lateral + lanza.Direccion * ((Math.Min(x, fin) - 50f) * EscalaLanza);
            Vector2 posicionAnterior = lanza.PosicionAnterior + lateral + lanza.Direccion * ((previo - 50f) * EscalaLanza);
            if (jugador.SeCruzan(centroAnterior, new CirculoColision(posicion, 7f), posicionAnterior))
            {
                return true;
            }
        }
        return false;
    }

    private static int Frame(Lanza lanza) => lanza.Estado switch
    {
        EstadoLanza.Portal => (int)(lanza.Tiempo / 0.1f) % 4,
        EstadoLanza.Ataque => 4 + Math.Min(7, (int)(lanza.Tiempo / (DuracionAtaque / 8f))),
        EstadoLanza.Retirando => 11 - Math.Min(7, (int)(lanza.Tiempo / (DuracionRetirada / 8f))),
        EstadoLanza.Rota => 12 + Math.Min(7, (int)(lanza.Tiempo / (DuracionRotura / 8f))),
        // El frame 20 aun tiene un trozo rojo de lanza: solo se reproduce al romperla.
        _ => 20 + (int)(lanza.Tiempo / 0.1f) % 2
    };

    private bool ElegirPosicion(Viewport viewport, Rectangle ocupado)
    {
        int margen = (int)(100f * EscalaAve) + 6;
        if (viewport.Width < margen * 2 || viewport.Height < margen * 2) { return false; }
        float alcanceX = Math.Min(viewport.Width * 0.04f, viewport.Width * 0.5f - margen);
        float alcanceY = Math.Min(viewport.Height * 0.04f, viewport.Height * 0.5f - margen);
        int primeraEsquina = Random.Shared.Next(4);
        for (int intento = 0; intento < 80; intento++)
        {
            int esquina = (primeraEsquina + intento) % 4;
            float x = margen + Random.Shared.NextSingle() * alcanceX;
            float y = margen + Random.Shared.NextSingle() * alcanceY;
            Vector2 candidata = new((esquina & 1) == 0 ? x : viewport.Width - x,
                (esquina & 2) == 0 ? y : viewport.Height - y);
            Rectangle area = new((int)candidata.X - margen, (int)candidata.Y - margen, margen * 2, margen * 2);
            if (!ocupado.IsEmpty && area.Intersects(ocupado)) { continue; }
            _posicion = candidata;
            return true;
        }
        return false;
    }

    private void CambiarEstado(Estado estado) { _estado = estado; _tiempo = 0f; }
    private void Sonar(string nombre) { Core.Audio.PlaySoundEffect(_sonidos[nombre], restart: true); }

    public void DrawLanzas(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _lanzas.Count; i++)
        {
            Lanza lanza = _lanzas[i];
            float alfa = lanza.Estado == EstadoLanza.Desvaneciendo
                ? Math.Max(0f, 1f - lanza.Tiempo / DuracionDesvanecimiento) : 1f;
            spriteBatch.Draw(_ataque, lanza.Posicion, new Rectangle(Frame(lanza) * 200, 0, 200, 100),
                Color.White * alfa, MathF.Atan2(lanza.Direccion.Y, lanza.Direccion.X),
                new Vector2(50, 50), EscalaLanza, SpriteEffects.None, 0f);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_estado == Estado.Oculto) { return; }
        int frame = FrameAve();
        float alfa = _estado == Estado.Desvaneciendo
            ? Math.Max(0f, 1f - _tiempo / DuracionDesvanecimiento) : 1f;
        spriteBatch.Draw(_ave, _posicion, new Rectangle(frame * 200, 0, 200, 200),
            Color.White * alfa, 0f, new Vector2(100), EscalaAve, SpriteEffects.None, 0f);
    }

    private int FrameAve() => _estado switch
    {
        Estado.Atacando => 15,
        Estado.Revirtiendo => 14 - Math.Min(14, (int)(_tiempo / SegundosPorFrameReversa)),
        Estado.Preparando => 5 + (int)(_tiempo / 0.1f) % 5,
        Estado.Cargando => 10 + (int)(_tiempo / 0.1f) % 5,
        // Mirada, descanso y desvanecimiento conservan el idle.
        _ => (int)(_tiempo / 0.1f) % 5
    };
    public void DetenerSonidos()
    {
        foreach (SoundEffectInstance sonido in _sonidos.Values) { Core.Audio.StopSoundEffect(sonido); }
    }

    public void UnloadContent()
    {
        foreach (SoundEffectInstance sonido in _sonidos.Values) { Core.Audio.ReleaseSoundEffect(sonido); }
        _sonidos.Clear();
        _lanzas.Clear();
    }
}
