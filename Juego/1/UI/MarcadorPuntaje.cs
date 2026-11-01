using System;
using System.Collections.Generic;
using _1.Progression;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace _1.UI;

public sealed class MarcadorPuntaje : IDisposable
{
    private readonly EstadoPuntaje _estado;
    private readonly SpriteFont _fuente;
    private readonly Texture2D _pixel;
    private readonly Texture2D _texturaHumo;
    private readonly SoundEffectInstance[] _voces = new SoundEffectInstance[EstadoPuntaje.Rangos.Count];
    private readonly Texture2D[] _imagenes = new Texture2D[EstadoPuntaje.Rangos.Count];
    // Tonos del relleno de los sprites, en el mismo orden que los rangos.
    private static readonly Color[] ColoresMultiplicador =
    {
        new(235, 190, 65), new(185, 235, 90), new(15, 210, 165),
        new(65, 145, 255), new(255, 35, 110), new(255, 35, 110),
        new(255, 35, 110), new(180, 85, 245), new(255, 165, 20)
    };
    private readonly Dictionary<PremioPuntaje, float> _posiciones = new();
    private readonly List<ParticulaHumo> _humo = new();
    private readonly Random _azarHumo = new();
    private float _relojEfectos;
    private float _hastaHumo;
    private float _pulso;
    private float _barra;
    private int _rango;

    private readonly record struct EstiloAccion(Color Relleno, Color Borde, float Grosor,
        Color Luz = default, float Brillo = 0f, bool Palpitante = false);

    private sealed class ParticulaHumo
    {
        public PremioPuntaje Accion;
        public Vector2 Posicion;
        public Vector2 Velocidad;
        public float Edad;
        public float Radio;
    }

    private static EstiloAccion Estilo(string nombre) => nombre switch
    {
        "BEACON" => new(Color.Black, Color.Red, 1.3f),
        "AUTO-PARRY" => new(new Color(255, 230, 45), new Color(255, 252, 195), 1.3f),
        "DOBLE PARRY" => new(Color.Black, new Color(145, 145, 145), 1.2f),
        "TRIPLE PARRY" => new(Color.Black, new Color(210, 210, 210), 1.4f, new Color(215, 215, 215), 0.18f),
        "MASTER PARRY" => new(Color.Black, new Color(235, 235, 235), 1.6f, new Color(240, 240, 240), 0.45f, true),
        "PARRY DUCK" => new(new Color(165, 165, 165), Color.Black, 1.2f),
        "PATITO FEO" => new(new Color(255, 225, 45), new Color(135, 135, 135), 1.3f),
        "PARRY ZORAK" => new(new Color(155, 155, 155), new Color(225, 225, 225), 1.3f),
        "PROVOCACION" => new(new Color(155, 155, 155), new Color(155, 155, 155), 0f),
        "PARRY JARJAR" => new(new Color(175, 175, 175), new Color(230, 35, 50), 1.2f),
        "2X1 JARJAR" => new(new Color(175, 175, 175), new Color(230, 35, 50), 2f),
        "PARRY LANZA" => new(Color.White, Color.Black, 1.2f),
        "BAD BIRD" => new(new Color(255, 35, 50), new Color(155, 155, 155), 1.6f, new Color(255, 35, 50), 0.22f),
        "PARRY SHAME" => new(new Color(255, 25, 40), Color.Black, 1.4f, new Color(255, 25, 40), 0.3f),
        "VEN POR EL" => new(new Color(255, 65, 75), Color.Black, 1.6f, new Color(255, 30, 45), 0.5f),
        _ => new(Color.White, Color.Black, 1.2f)
    };

    public MarcadorPuntaje(ContentManager content, EstadoPuntaje estado)
    {
        _estado = estado;
        _fuente = content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _texturaHumo = _1.Entities.Shame.CrearTexturaHumo();
        _barra = estado.Progreso;
        _rango = estado.Rango;
        for (int i = 0; i < _voces.Length; i++)
        {
            _voces[i] = Core.Audio.CreateSoundEffectInstance(content.Load<SoundEffect>("SFX/Ranks/" + EstadoPuntaje.Rangos[i].Audio));
            _imagenes[i] = content.Load<Texture2D>("Sprites/Ranks/" + EstadoPuntaje.Rangos[i].Audio);
        }
        _estado.RangoSubido += SubirRango;
    }

    private void SubirRango(int rango)
    {
        foreach (var voz in _voces) Core.Audio.StopSoundEffect(voz);
        Core.Audio.PlaySoundEffect(_voces[rango], restart: true);
        _pulso = 0.35f;
    }

    public void Update(float segundos)
    {
        _relojEfectos = (_relojEfectos + segundos) % 1000f;
        _hastaHumo -= segundos;
        bool emitirHumo = _hastaHumo <= 0f;
        if (emitirHumo) _hastaHumo = 0.08f;
        _pulso = Math.Max(0, _pulso - segundos);
        float suavidad = 1f - MathF.Exp(-14f * segundos);
        if (_rango != _estado.Rango) { _rango = _estado.Rango; _barra = _estado.Progreso; }
        _barra = MathHelper.Lerp(_barra, _estado.Progreso, suavidad);
        var vigentes = new HashSet<PremioPuntaje>(_estado.Acciones);
        for (int i = _humo.Count - 1; i >= 0; i--)
        {
            ParticulaHumo particula = _humo[i];
            particula.Edad += segundos;
            if (particula.Edad >= 0.9f || !vigentes.Contains(particula.Accion))
                _humo.RemoveAt(i);
            else particula.Posicion += particula.Velocidad * segundos;
        }
        foreach (var premio in new List<PremioPuntaje>(_posiciones.Keys))
            if (!vigentes.Contains(premio)) _posiciones.Remove(premio);
        for (int i = 0; i < _estado.Acciones.Count; i++)
        {
            var premio = _estado.Acciones[i];
            float destino = i * 25f;
            bool nueva = !_posiciones.TryGetValue(premio, out float y);
            if (nueva) y = destino + 25f;
            _posiciones[premio] = MathHelper.Lerp(y, destino, suavidad);
            if (premio.Nombre == "VEN POR EL" && (nueva || emitirHumo))
                EmitirHumo(premio, nueva ? 5 : 2);
        }
    }

    public void Draw(Viewport vista)
    {
        float escala = Math.Min(vista.Width / 1280f, vista.Height / 720f);
        Vector2 origen = new(24f * escala, vista.Height - 340f * escala);
        float pulso = MathF.Sin(_pulso / 0.35f * MathHelper.Pi);
        if (_estado.Rango >= 0)
        {
            Rectangle recorte = _imagenes[_estado.Rango].Bounds;
            float escalaImagen = Math.Min(280f / recorte.Width, 92f / recorte.Height)
                * escala * (1f + pulso * 0.05f);
            Core.SpriteBatch.Draw(_imagenes[_estado.Rango], origen + new Vector2(140, 48) * escala,
                recorte, Color.White, 0f, new Vector2(recorte.Width, recorte.Height) * 0.5f,
                escalaImagen, SpriteEffects.None, 0f);
        }
        string multiplicador = "X" + _estado.Multiplicador.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        DibujarTexto(_fuente, multiplicador, origen + new Vector2(300, 33) * escala,
            0.85f * escala, color: _estado.Rango < 0 ? Color.White : ColoresMultiplicador[_estado.Rango]);
        var barra = new Rectangle((int)origen.X, (int)(origen.Y + 102 * escala), (int)(360 * escala), Math.Max(3, (int)(12 * escala)));
        Core.SpriteBatch.Draw(_pixel, barra, Color.Black);
        int borde = Math.Max(1, (int)(2 * escala));
        var interior = new Rectangle(barra.X + borde, barra.Y + borde, barra.Width - borde * 2, barra.Height - borde * 2);
        Core.SpriteBatch.Draw(_pixel, interior, new Color(60, 60, 60));
        interior.Width = (int)(interior.Width * _barra);
        if (interior.Width > 0) Core.SpriteBatch.Draw(_pixel, interior, Color.White);
        DibujarTexto(_fuente, _estado.TextoPiso, origen + new Vector2(0, 122) * escala, escala);
        DibujarHumo(origen, escala);
        for (int i = 0; i < _estado.Acciones.Count; i++)
        {
            var premio = _estado.Acciones[i];
            float y = _posiciones.TryGetValue(premio, out float posicion) ? posicion : i * 25 + 25;
            string texto = premio.Nombre + " +" + premio.Base;
            float tamano = Math.Min(0.72f, 360f / _fuente.MeasureString(texto).X);
            DibujarAccion(premio, texto, origen + new Vector2(0, 165 + y) * escala, tamano * escala, escala);
        }
    }

    public static void DibujarTexto(SpriteFont fuente, string texto, Vector2 posicion, float escala,
        float opacidad = 1f, Color? color = null, Color? colorBorde = null, float? grosorBorde = null)
    {
        float borde = grosorBorde ?? Math.Max(1f, escala * 1.5f);
        for (int y = -1; borde > 0 && y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Core.SpriteBatch.DrawString(fuente, texto, posicion + new Vector2(x, y) * borde,
                        (colorBorde ?? Color.Black) * opacidad, 0f, Vector2.Zero, escala, SpriteEffects.None, 0f);
        Core.SpriteBatch.DrawString(fuente, texto, posicion, (color ?? Color.White) * opacidad,
            0f, Vector2.Zero, escala, SpriteEffects.None, 0f);
    }

    private void DibujarAccion(PremioPuntaje premio, string texto, Vector2 posicion, float tamano, float escalaPantalla)
    {
        EstiloAccion estilo = Estilo(premio.Nombre);
        float pulso = 0.5f - 0.5f * MathF.Cos(_relojEfectos * MathHelper.TwoPi * 1.8f);
        float intensidad = estilo.Brillo * (estilo.Palpitante ? 0.35f + pulso * 0.65f : 0.8f + pulso * 0.2f);
        if (intensidad > 0f)
        {
            // Dos halos alrededor del texto; el relleno y el borde se dibujan nitidos encima.
            for (int capa = 0; capa < 2; capa++)
            {
                float radio = (estilo.Grosor + 1.6f + capa * 1.8f + (estilo.Palpitante ? pulso * 1.5f : 0f)) * escalaPantalla;
                for (int i = 0; i < 12; i++)
                {
                    float angulo = i * MathHelper.TwoPi / 12f;
                    Vector2 desplazamiento = new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * radio;
                    Core.SpriteBatch.DrawString(_fuente, texto, posicion + desplazamiento,
                        estilo.Luz * (intensidad * (capa == 0 ? 0.16f : 0.06f)),
                        0f, Vector2.Zero, tamano, SpriteEffects.None, 0f);
                }
            }
        }
        Color borde = estilo.Palpitante ? Color.Lerp(estilo.Borde, Color.White, pulso * 0.65f) : estilo.Borde;
        DibujarTexto(_fuente, texto, posicion, tamano, color: estilo.Relleno,
            colorBorde: borde, grosorBorde: estilo.Grosor * escalaPantalla);
    }

    private void EmitirHumo(PremioPuntaje premio, int cantidad)
    {
        string texto = premio.Nombre + " +" + premio.Base;
        float ancho = _fuente.MeasureString(texto).X;
        ancho *= Math.Min(0.72f, 360f / ancho);
        for (int i = 0; i < cantidad && _humo.Count < 96; i++)
            _humo.Add(new ParticulaHumo
            {
                Accion = premio,
                Posicion = new Vector2(_azarHumo.NextSingle() * ancho, 5f + (_azarHumo.NextSingle() - 0.5f) * 12f),
                Velocidad = new Vector2((_azarHumo.NextSingle() - 0.5f) * 12f, -5f - _azarHumo.NextSingle() * 8f),
                Radio = 5f + _azarHumo.NextSingle() * 5f
            });
    }

    private void DibujarHumo(Vector2 origen, float escala)
    {
        foreach (ParticulaHumo particula in _humo)
        {
            if (!_posiciones.TryGetValue(particula.Accion, out float y)) continue;
            float progreso = particula.Edad / 0.9f;
            float opacidad = Math.Min(1f, particula.Edad / 0.08f) * (1f - progreso) * 0.75f;
            Vector2 posicion = origen + (new Vector2(0, 165 + y) + particula.Posicion) * escala;
            float radio = particula.Radio + progreso * 5f;
            Core.SpriteBatch.Draw(_texturaHumo, posicion, null, Color.White * opacidad,
                0f, new Vector2(_texturaHumo.Width, _texturaHumo.Height) * 0.5f,
                radio * 2f * escala / _texturaHumo.Width, SpriteEffects.None, 0f);
        }
    }

    public void Dispose()
    {
        _estado.RangoSubido -= SubirRango;
        foreach (var voz in _voces) Core.Audio.ReleaseSoundEffect(voz);
        _pixel.Dispose();
        _texturaHumo.Dispose();
    }
}
