using _1.Progression;
using System;
using System.Collections.Generic;
using LutredLibrary;
using LutredLibrary.Scenes;
using LutredLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace _1.Scenes;

// Eleccion de mejoras y descripcion progresiva; el estado se conserva entre pisos.
public sealed class Mejoras : Escena
{
    private static readonly DefinicionMejora[] Catalogo = EstadoMejoras.Catalogo;
    private readonly EstadoMejoras _mejoras;
    private readonly int _piso;
    private int _indiceDescripcion = -1;
    private string _textoDescripcion = "";
    private float _caracteresDescripcion;
    private bool _sinMejorasAplicables;
    private const float DuracionFundido = 1.2f;
    private const float DuracionAparicion = 1.5f;
    private const float PausaBlanca = 0.45f;
    private const float RetrasoEntreIconos = 0.35f;
    private const float FinAparicion = DuracionFundido + PausaBlanca + DuracionAparicion + 2 * RetrasoEntreIconos;
    private Texture2D _humo;
    private AnimatedSprite _fraseInicial;
    private SoundEffectInstance _musicaMejoras;
    private SoundEffect _aceptarMejora;
    private sealed class ParticulaHumo
    {
        public float X, Altura, Radio, Deriva, Fase, Duracion, Edad;
    }
    private readonly ParticulaHumo[] _particulasHumo = new ParticulaHumo[100];
    private readonly Action _continuar;
    private readonly int[] _opciones = new int[3];
    private readonly Texture2D[] _iconos = new Texture2D[3];
    private Texture2D _pixel;
    private SpriteFont _fuente;
    private KeyboardState _tecladoAnterior;
    private MouseState _ratonAnterior;
    private float _tiempo;
    private int _seleccion;
    private bool _saliendo;
    private const float DuracionSeleccion = 0.9f;
    private float _tiempoSalida;
    private bool _cambioSolicitado;

    public Mejoras(Action continuar, EstadoMejoras mejoras = null, int piso = 0)
    {
        _continuar = continuar ?? throw new ArgumentNullException(nameof(continuar));
        _mejoras = mejoras ?? new EstadoMejoras();
        _piso = piso;
        var activas = new List<int>();
        var pendientes = new List<int>();
        for (int i = 0; i < Catalogo.Length; i++)
        {
            if (_mejoras.Disponible(Catalogo[i], piso))
            {
                (Catalogo[i].Implementada ? activas : pendientes).Add(i);
            }
        }
        _sinMejorasAplicables = activas.Count == 0;
        for (int i = 0; i < _opciones.Length; i++)
        {
            // Las mejoras funcionales tienen prioridad; las otras son vistas previas.
            var disponibles = activas.Count > 0 ? activas : pendientes;
            if (disponibles.Count == 0)
            {
                _opciones[i] = -1;
                continue;
            }
            int indice = Random.Shared.Next(disponibles.Count);
            _opciones[i] = disponibles[indice];
            disponibles.RemoveAt(indice);
        }
    }
    protected override void LoadContent()
    {
        _fuente = Content.Load<SpriteFont>("Fonts/BoldPixels/BoldPixels");
        // Una frase por visita; sus nueve cuadros se repiten sin cambiar de variante.
        int variante = Random.Shared.Next(1, 4);
        Texture2D hojaFrase = Content.Load<Texture2D>($"Pharses/Upgrades/MejorasFrase{variante}-Sheet");
        int anchoFrase = hojaFrase.Width / 9;
        var cuadrosFrase = new List<TextureRegion>();
        for (int i = 0; i < 9; i++)
        {
            cuadrosFrase.Add(new TextureRegion(hojaFrase, i * anchoFrase, 0, anchoFrase, hojaFrase.Height));
        }
        _fraseInicial = new AnimatedSprite(new Animation(cuadrosFrase, TimeSpan.FromSeconds(0.12)))
        {
            Origin = new Vector2(anchoFrase, hojaFrase.Height) * 0.5f
        };
        for (int i = 0; i < _iconos.Length; i++)
        {
            if (_opciones[i] >= 0)
            {
                _iconos[i] = Content.Load<Texture2D>($"Sprites/Upgrades/Icons/{Catalogo[_opciones[i]].Id}");
            }
        }
        _humo = _1.Entities.Shame.CrearTexturaHumo();
        for (int i = 0; i < _particulasHumo.Length; i++)
        {
            var particula = new ParticulaHumo();
            ReiniciarHumo(particula);
            particula.Edad = -Random.Shared.NextSingle() * 3f;
            _particulasHumo[i] = particula;
        }
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        Core.Audio.SongVolume = 0.45f;
        Core.Audio.StopSong();
        // Reproducir la pista completa con el mismo sistema que el resto de escenas.
        _musicaMejoras = Core.Audio.CreateSoundEffectInstance(
            Content.Load<SoundEffect>("Music/Misc/Mejoras"), isLooped: true, isMusic: true);
        Core.Audio.PlaySoundEffect(_musicaMejoras);
        // El sonido pertenece al contenido global para que termine tras el cambio de escena.
        _aceptarMejora = Core.Content.Load<SoundEffect>("SFX/Misc/MejoraAdquirida");
        _tecladoAnterior = Keyboard.GetState();
        _ratonAnterior = Mouse.GetState();
    }

    private static Rectangle AreaOpcion(int indice)
    {
        var viewport = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        int ancho = (int)(280 * escala);
        int alto = (int)(260 * escala);
        int x = viewport.Width / 2 + (int)((indice - 1) * 340 * escala);
        return new Rectangle(x - ancho / 2, viewport.Height / 2 - alto / 2 - (int)(85 * escala), ancho, alto);
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Audio.IsPaused)
        {
            return;
        }
        float segundos = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _tiempo += segundos;
        if (_tiempo > DuracionFundido + PausaBlanca)
        {
            _fraseInicial.Update(gameTime);
        }
        for (int i = 0; i < _particulasHumo.Length; i++)
        {
            var particula = _particulasHumo[i];
            particula.Edad += segundos;
            if (particula.Edad >= particula.Duracion)
            {
                ReiniciarHumo(particula);
            }
        }
        if (_saliendo)
        {
            _tiempoSalida += segundos;
            if (_tiempoSalida >= DuracionSeleccion && !_cambioSolicitado)
            {
                _cambioSolicitado = true;
                _continuar();
            }
            return;
        }
        var teclado = Keyboard.GetState();
        var raton = Mouse.GetState();
        bool Pulsado(Keys tecla) => teclado.IsKeyDown(tecla) && _tecladoAnterior.IsKeyUp(tecla);
        if (_tiempo >= FinAparicion && Core.Instance.IsActive)
        {
            if (Pulsado(Keys.Left) || Pulsado(Keys.A))
            {
                _seleccion = (_seleccion + 2) % 3;
            }
            if (Pulsado(Keys.Right) || Pulsado(Keys.D))
            {
                _seleccion = (_seleccion + 1) % 3;
            }
            bool confirmar = Pulsado(Keys.Enter) || Pulsado(Keys.F);
            for (int i = 0; i < _opciones.Length; i++)
            {
                if (AreaOpcion(i).Contains(raton.Position))
                {
                    if (raton.Position != _ratonAnterior.Position)
                    {
                        _seleccion = i;
                    }
                    if (raton.LeftButton == ButtonState.Pressed && _ratonAnterior.LeftButton == ButtonState.Released)
                    {
                        _seleccion = i;
                        confirmar = true;
                    }
                }
            }
            ActualizarDescripcion(segundos);
            if (confirmar && (_sinMejorasAplicables || (_opciones[_seleccion] >= 0 &&
                _mejoras.Aplicar(Catalogo[_opciones[_seleccion]], _piso))))
            {
                _saliendo = true;
                if (!_sinMejorasAplicables)
                {
                    Core.Audio.PlaySoundEffect(_aceptarMejora, volume: 0.8f);
                }
                if (_sinMejorasAplicables)
                {
                    _cambioSolicitado = true;
                    _continuar();
                }
            }
        }
        _tecladoAnterior = teclado;
        _ratonAnterior = raton;
    }

    public override void Draw(GameTime gameTime)
    {
        float fundido = MathHelper.SmoothStep(0f, 1f, Math.Clamp(_tiempo / DuracionFundido, 0f, 1f));
        Core.GraphicsDevice.Clear(Color.Lerp(Color.Black, Color.White, fundido));
        float alfa = Aparicion(0f);
        float seleccion = MathHelper.SmoothStep(0f, 1f, Math.Clamp(_tiempoSalida / DuracionSeleccion, 0f, 1f));
        float alfaInterfaz = alfa * (1f - seleccion);
        if (alfa <= 0f)
        {
            return;
        }
        var viewport = Core.GraphicsDevice.Viewport;
        float escala = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // Capa de fondo: humo suavizado por un velo blanco antes de dibujar la interfaz.
        DibujarHumo(escala, alfa);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White * 0.2f);
        Core.SpriteBatch.End();
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _fraseInicial.Position = new Vector2(viewport.Width / 2f, viewport.Height / 2f - 260 * escala);
        _fraseInicial.Scale = new Vector2(0.9f * escala);
        _fraseInicial.Color = Color.White * alfaInterfaz;
        _fraseInicial.Draw(Core.SpriteBatch);
        for (int i = 0; i < _opciones.Length; i++)
        {
            if (_opciones[i] < 0)
            {
                continue;
            }
            float alfaIcono = Aparicion(i * RetrasoEntreIconos) * (1f - seleccion);
            float ascenso = _saliendo && _seleccion == i ? seleccion * 28f * escala : 0f;
            Rectangle area = AreaOpcion(i);
            if (_seleccion == i && !_saliendo)
            {
                Core.SpriteBatch.Draw(_pixel, area, Color.Black * (0.07f * alfaIcono));
                Core.SpriteBatch.Draw(_pixel, new Rectangle(area.X, area.Bottom - (int)(3 * escala), area.Width, Math.Max(1, (int)(3 * escala))), Color.Black * alfaIcono);
            }
            Texture2D icono = _iconos[i];
            float tamano = (_seleccion == i ? 164 : 144) * escala;
            float escalaIcono = tamano / Math.Max(icono.Width, icono.Height);
            // Cada icono flota a su propio ritmo; al aceptar se conserva la altura de partida.
            float tiempoFlotacion = _tiempo - _tiempoSalida;
            float flotacion = MathF.Sin(tiempoFlotacion * (1.6f + i * 0.23f) + i * 2.1f)
                * (6f + i) * escala * Aparicion(i * RetrasoEntreIconos);
            Core.SpriteBatch.Draw(icono, new Vector2(area.Center.X, area.Center.Y - 28 * escala + (1f - Aparicion(i * RetrasoEntreIconos)) * 18 * escala + flotacion - ascenso), null,
                Color.White * alfaIcono, 0f, new Vector2(icono.Width, icono.Height) / 2f,
                escalaIcono, SpriteEffects.None, 0f);
            DibujarTexto(Catalogo[_opciones[i]].Nombre, new Vector2(area.Center.X, area.Bottom - 54 * escala), 1.5f * escala, alfaIcono);
        }
        if (_indiceDescripcion >= 0)
        {
            DibujarDescripcion(escala, alfaInterfaz);
        }
        // Flechas pixeladas para mantener la fuente actual sin glifos faltantes.
        DibujarIndicacionSeleccion(escala, alfaInterfaz * 0.5f);
        DibujarTexto(_sinMejorasAplicables ? "Sin mejoras aplicables: Enter / F para continuar" : "Enter / F para aceptar la mejora", new Vector2(viewport.Width / 2f, viewport.Height / 2f + 333 * escala), 0.75f * escala, alfaInterfaz * 0.5f);
        // Velo blanco sobre la escena completa que se disuelve al comenzar la revelacion.
        float velo = 1f - Aparicion(0f);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White * velo);
        Core.SpriteBatch.End();
    }

    private void DibujarIndicacionSeleccion(float escala, float alfa)
    {
        const string izquierda = "A, D / ";
        const string derecha = " para seleccionar";
        float fuente = 0.75f * escala;
        float anchoIzquierda = _fuente.MeasureString(izquierda).X * fuente;
        float anchoDerecha = _fuente.MeasureString(derecha).X * fuente;
        float anchoFlechas = 42f * escala;
        var viewport = Core.GraphicsDevice.Viewport;
        float x = (viewport.Width - anchoIzquierda - anchoFlechas - anchoDerecha) * 0.5f;
        float y = viewport.Height / 2f + 306 * escala - _fuente.MeasureString(izquierda).Y * fuente * 0.5f;
        Color color = Color.Black * alfa;
        Core.SpriteBatch.DrawString(_fuente, izquierda, new Vector2(x, y), color,
            0f, Vector2.Zero, fuente, SpriteEffects.None, 0f);
        Core.SpriteBatch.DrawString(_fuente, derecha, new Vector2(x + anchoIzquierda + anchoFlechas, y), color,
            0f, Vector2.Zero, fuente, SpriteEffects.None, 0f);
        int centroY = (int)(viewport.Height / 2f + 306 * escala);
        int pixel = Math.Max(1, (int)(2 * escala));
        for (int i = 0; i < 2; i++)
        {
            int inicioX = (int)(x + anchoIzquierda + i * 23 * escala);
            int ancho = Math.Max(1, (int)(16 * escala));
            Core.SpriteBatch.Draw(_pixel, new Rectangle(inicioX, centroY, ancho, pixel), color);
            int puntaX = i == 0 ? inicioX : inicioX + ancho - pixel;
            for (int paso = 1; paso <= 3; paso++)
            {
                int bordeX = puntaX + (i == 0 ? 1 : -1) * paso * pixel;
                Core.SpriteBatch.Draw(_pixel, new Rectangle(bordeX, centroY - paso * pixel, pixel, pixel), color);
                Core.SpriteBatch.Draw(_pixel, new Rectangle(bordeX, centroY + paso * pixel, pixel, pixel), color);
            }
        }
    }
    private void ActualizarDescripcion(float segundos)
    {
        if (_opciones[_seleccion] < 0)
        {
            return;
        }
        if (_indiceDescripcion != _seleccion)
        {
            _indiceDescripcion = _seleccion;
            var mejora = Catalogo[_opciones[_seleccion]];
            _textoDescripcion = _mejoras.Describir(mejora);
            // Al volver a una mejora ya visitada, se muestra toda la informacion.
            _caracteresDescripcion = _mejoras.DescripcionesVistas.Add(mejora.Id) ? 0f : _textoDescripcion.Length;
        }
        _caracteresDescripcion = Math.Min(_textoDescripcion.Length, _caracteresDescripcion + segundos * 48f);
    }

    private void DibujarDescripcion(float escala, float alfa)
    {
        var viewport = Core.GraphicsDevice.Viewport;
        Rectangle opcion = AreaOpcion(_indiceDescripcion);
        int ancho = (int)(430 * escala);
        int x = Math.Clamp(opcion.Center.X - ancho / 2, (int)(18 * escala), viewport.Width - ancho - (int)(18 * escala));
        int y = opcion.Bottom + (int)(14 * escala);
        float escalaTexto = 0.72f * escala;
        string envuelto = EnvolverTexto(_textoDescripcion, ancho - 36 * escala, escalaTexto);
        float altoTexto = _fuente.MeasureString(envuelto).Y * escalaTexto;
        float altoDisponible = viewport.Height / 2f + 282 * escala - y;
        while (altoTexto + 52 * escala > altoDisponible && escalaTexto > 0.5f * escala)
        {
            escalaTexto -= 0.02f * escala;
            envuelto = EnvolverTexto(_textoDescripcion, ancho - 36 * escala, escalaTexto);
            altoTexto = _fuente.MeasureString(envuelto).Y * escalaTexto;
        }
        var area = new Rectangle(x, y, ancho, (int)Math.Max(132 * escala, altoTexto + 52 * escala));
        float brillo = 0.85f + MathF.Sin(_tiempo * 2f) * 0.15f;
        for (int i = 5; i >= 1; i--)
        {
            Rectangle halo = area;
            halo.Inflate((int)(i * 2 * escala), (int)(i * 2 * escala));
            DibujarBorde(halo, Math.Max(1, (int)(2 * escala)), Color.Red * (0.045f * brillo * alfa));
        }
        Core.SpriteBatch.Draw(_pixel, area, Color.Black * (0.96f * alfa));
        DibujarBorde(area, Math.Max(1, (int)(2 * escala)), new Color(255, 35, 45) * (brillo * alfa));
        var mejora = Catalogo[_opciones[_indiceDescripcion]];
        Core.SpriteBatch.DrawString(_fuente, mejora.Nombre,
            new Vector2(area.X + 20 * escala, area.Y + 10 * escala), new Color(255, 70, 80) * alfa,
            0f, Vector2.Zero, escala, SpriteEffects.None, 0f);
        // El ajuste de linea se calcula sobre el texto completo, para que no salte al escribir.
        int visibles = (int)_caracteresDescripcion;
        int longitud = 0;
        int caracteres = 0;
        while (longitud < envuelto.Length && caracteres < visibles)
        {
            if (envuelto[longitud] != '\n')
            {
                caracteres++;
            }
            longitud++;
        }
        string texto = _caracteresDescripcion >= _textoDescripcion.Length ? envuelto : envuelto.Substring(0, longitud);
        Core.SpriteBatch.DrawString(_fuente, texto,
            new Vector2(area.X + 18 * escala, area.Y + 40 * escala), Color.White * alfa,
            0f, Vector2.Zero, escalaTexto, SpriteEffects.None, 0f);
    }

    private string EnvolverTexto(string texto, float ancho, float escala)
    {
        var lineas = new List<string>();
        string[] parrafos = texto.Split('\n');
        for (int i = 0; i < parrafos.Length; i++)
        {
            string linea = "";
            string[] palabras = parrafos[i].Split(' ');
            for (int j = 0; j < palabras.Length; j++)
            {
                string propuesta = linea.Length == 0 ? palabras[j] : linea + " " + palabras[j];
                if (linea.Length > 0 && _fuente.MeasureString(propuesta).X * escala > ancho)
                {
                    lineas.Add(linea);
                    linea = palabras[j];
                }
                else
                {
                    linea = propuesta;
                }
            }
            lineas.Add(linea);
        }
        return string.Join("\n", lineas);
    }
    private void DibujarBorde(Rectangle area, int grosor, Color color)
    {
        Core.SpriteBatch.Draw(_pixel, new Rectangle(area.X, area.Y, area.Width, grosor), color);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(area.X, area.Bottom - grosor, area.Width, grosor), color);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(area.X, area.Y, grosor, area.Height), color);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(area.Right - grosor, area.Y, grosor, area.Height), color);
    }
    private float Aparicion(float retraso)
    {
        float progreso = Math.Clamp((_tiempo - DuracionFundido - PausaBlanca - retraso) / DuracionAparicion, 0f, 1f);
        return MathHelper.SmoothStep(0f, 1f, progreso);
    }

    private static void ReiniciarHumo(ParticulaHumo particula)
    {
        particula.X = Random.Shared.NextSingle();
        particula.Altura = MathHelper.Lerp(0.18f, 0.52f, Random.Shared.NextSingle());
        particula.Radio = MathHelper.Lerp(16f, 72f, MathF.Pow(Random.Shared.NextSingle(), 1.5f));
        particula.Deriva = MathHelper.Lerp(-75f, 75f, Random.Shared.NextSingle());
        particula.Fase = Random.Shared.NextSingle() * MathHelper.TwoPi;
        particula.Duracion = MathHelper.Lerp(4f, 8f, Random.Shared.NextSingle());
        particula.Edad = 0f;
    }

    private void DibujarHumo(float escala, float alfa)
    {
        var viewport = Core.GraphicsDevice.Viewport;
        for (int i = 0; i < _particulasHumo.Length; i++)
        {
            var particula = _particulasHumo[i];
            if (particula.Edad < 0f)
            {
                continue;
            }
            float progreso = particula.Edad / particula.Duracion;
            float desvanecer = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progreso - 0.55f) / 0.45f, 0f, 1f));
            float radio = particula.Radio * escala * (1f - desvanecer);
            float x = particula.X * viewport.Width + (particula.Deriva * progreso +
                (MathF.Sin(progreso * 6f + particula.Fase) - MathF.Sin(particula.Fase)) * 16f) * escala;
            float y = MathHelper.Lerp(viewport.Height + (particula.Radio + 12f) * escala,
                viewport.Height * (1f - particula.Altura), progreso);
            float opacidad = Math.Min(1f, progreso / 0.1f) * (1f - desvanecer) * alfa * 0.95f;
            Core.SpriteBatch.Draw(_humo, new Vector2(x, y), null, Color.White * opacidad,
                0f, new Vector2(_humo.Width, _humo.Height) * 0.5f,
                radio * 2f / _humo.Width, SpriteEffects.None, 0f);
        }
    }
    private void DibujarTexto(string texto, Vector2 posicion, float escala, float alfa)
    {
        Core.SpriteBatch.DrawString(_fuente, texto, posicion, Color.Black * alfa, 0f,
            _fuente.MeasureString(texto) / 2f, escala, SpriteEffects.None, 0f);
    }

    protected override void UnloadContent()
    {
        Core.Audio.ReleaseSoundEffect(_musicaMejoras);
        _pixel?.Dispose();
        _humo?.Dispose();
    }
}
