using System;
using _1.Entities;
using _1.Mapa.Estructuras;
using LutredLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Scenes;

public partial class Partida
{
    private Effect _coloresInvertidos;
    public Effect FiltroDupe => !_derrotado && !_nivelCompletado && _efectoDupe.Activo ? _coloresInvertidos : null;
    private Effect _tintaAntigua;
    private Texture2D _pixelTransicion;
    private Texture2D _degradadoFondo;
    private float _faseLatidoFondo;

    private void DibujarExterior()
    {
        Rectangle visible = AreaVisible();
        Rectangle mapa = _limitesExteriores;
        if (visible.Top < mapa.Top)
            DibujarNegro(new Rectangle(visible.Left, visible.Top, visible.Width, mapa.Top - visible.Top));
        if (visible.Bottom > mapa.Bottom)
            DibujarNegro(new Rectangle(visible.Left, mapa.Bottom, visible.Width, visible.Bottom - mapa.Bottom));
        if (visible.Left < mapa.Left)
            DibujarNegro(new Rectangle(visible.Left, visible.Top, mapa.Left - visible.Left, visible.Height));
        if (visible.Right > mapa.Right)
            DibujarNegro(new Rectangle(mapa.Right, visible.Top, visible.Right - mapa.Right, visible.Height));
    }

    private void DibujarNegro(Rectangle region)
    {
        Core.SpriteBatch.Draw(_pixelTransicion, region, Color.Black);
    }

    private void DibujarDegradadoFondo()
    {
        float intensidad = _numeroNivel < 4 ? 0f : _numeroNivel < 8 ? 0.3f : _numeroNivel < 12 ? 0.6f : 1f;
        if (intensidad == 0f) return;

        float latido = (MathF.Sin(_faseLatidoFondo) + 1f) * 0.5f;
        var viewport = Core.GraphicsDevice.Viewport;
        // Incluso en el pico del latido, el fondo oscuro no supera la mitad inferior.
        int altura = Math.Max(1, (int)(viewport.Height * MathHelper.Lerp(0.44f, 0.5f, latido)));
        Color color = _enFellDoor ? new Color(8, 0, 2) : Color.Black;
        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: BlendState.AlphaBlend);
        Core.SpriteBatch.Draw(_degradadoFondo,
            new Rectangle(0, viewport.Height - altura, viewport.Width, altura),
            color * (intensidad * MathHelper.Lerp(0.94f, 1f, latido)));
        Core.SpriteBatch.End();
    }

    private void DibujarPartida(GameTime gameTime)
    {
        if (_nivelCompletado)
        {
            Core.GraphicsDevice.Clear(Color.Black);
            return;
        }

        if (_faseEntrada == FaseEntrada.Transicion)
        {
            Core.GraphicsDevice.Clear(Color.Black);
            DibujarBorde();
            return;
        }

        Core.GraphicsDevice.Clear(_derrotado ? Color.Black : _enFellDoor ? new Color(48, 3, 8) : Color.White);
        if (!_derrotado) DibujarDegradadoFondo();
        Matrix transformacion = _camara.ObtenerTransformacion(Core.GraphicsDevice.Viewport);

        if (!_derrotado)
        {
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointWrap,
                effect: _enFellDoor ? null : _tintaAntigua,
                transformMatrix: transformacion);
            Rectangle visible = AreaVisible();
            foreach (EstructuraSolida estructura in _estructuras)
                if (estructura.Limites.Intersects(visible)) estructura.Draw(Core.SpriteBatch);
            if (_enFellDoor)
                Core.SpriteBatch.Draw(_pixelTransicion, visible, new Color(100, 0, 12) * 0.65f);
            Core.SpriteBatch.End();
        }

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _derrotado ? Matrix.Identity : transformacion
        );

        if (_derrotado)
        {
            _pantallaDerrota.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
        }
        else
        {
            DibujarExterior();
            if (_enFellDoor)
            {
                foreach (DemonioFellDoor demonio in _demoniosFellDoor) demonio.Draw(Core.SpriteBatch);
                _shame.Draw(Core.SpriteBatch);
                _sinner.Draw(Core.SpriteBatch);
            }
            else
            {
                DibujarPuertaFellDoor();
                for (int i = 0; i < _jarJars.Count; i++)
                {
                    _jarJars[i].DrawAura(Core.SpriteBatch);
                }
                for (int i = 0; i < _orbes.Count; i++)
                {
                    _orbes[i].Draw(Core.SpriteBatch);
                }
                for (int i = 0; i < _jarJars.Count; i++)
                {
                    _jarJars[i].Draw(Core.SpriteBatch);
                }
                _delusion?.Draw(Core.SpriteBatch);
                _erosionBird?.DrawLanzas(Core.SpriteBatch);
                _shame.Draw(Core.SpriteBatch);
                foreach (Dupe dupe in _dupes) dupe.Draw(Core.SpriteBatch);
                _sinner.Draw(Core.SpriteBatch);
                _delusion?.DrawBolas(Core.SpriteBatch);
                for (int i = 0; i < _dukes.Count; i++)
                {
                    _dukes[i].Draw(Core.SpriteBatch);
                }
            }
        }

        Core.SpriteBatch.End();

        if (_faseEntrada == FaseEntrada.Aparicion)
        {
            var viewport = Core.GraphicsDevice.Viewport;
            Core.SpriteBatch.Begin(blendState: BlendState.AlphaBlend);
            Core.SpriteBatch.Draw(_pixelTransicion,
                new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White * _opacidadBlanca);
            Core.SpriteBatch.End();
        }
        DibujarBorde();

        if (!_derrotado)
        {
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            DibujarIndicadorOrbes(transformacion);
            DibujarContadorOrbes();
            DibujarTemporizador();
            DibujarAyudaPrimerLayer();
            _marcador.Draw(Core.GraphicsDevice.Viewport);
            Core.SpriteBatch.End();

            // La capa negra cubre tambien el borde y Zorak queda encima de todo.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            if (!_enFellDoor)
            {
                _erosionBird?.Draw(Core.SpriteBatch);
                _tooManyParries?.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
                _zorak?.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport);
            }
            Core.SpriteBatch.End();
        }
    }
}
