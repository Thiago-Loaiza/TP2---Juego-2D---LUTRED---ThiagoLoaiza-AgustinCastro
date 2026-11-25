using System;
using System.Collections.Generic;
using _1.Colisiones;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _1.Entities;

public sealed class GranJabalina
{
    private readonly Texture2D _hoja;
    private readonly HashSet<object> _impactados = new();
    private Vector2 _posicion, _anterior, _direccion;
    private float _vida, _recarga;
    public event Action ParryRealizado;
    public GranJabalina(Texture2D hoja) => _hoja = hoja;
    public bool Activa => _vida > 0f;
    public float Recarga => _recarga;
    public CirculoColision Colision => new(_posicion, 35f);
    public Vector2 Anterior => _anterior;

    public void Update(float segundos, bool lanzar, Vector2 origen, Vector2 direccion)
    {
        _recarga = Math.Max(0f, _recarga - segundos);
        if (lanzar && _recarga == 0f)
        {
            _posicion = _anterior = origen;
            _direccion = direccion;
            _vida = 2f;
            _recarga = 30f;
            _impactados.Clear();
        }
        if (!Activa) return;
        _anterior = _posicion;
        _posicion += _direccion * 900f * Math.Min(segundos, _vida);
        _vida = Math.Max(0f, _vida - segundos);
    }

    public bool Toca(CirculoColision objetivo, Vector2 anterior, object entidad = null)
    {
        if (!Activa || (entidad != null && _impactados.Contains(entidad))) return false;
        if (!Colision.SeCruzan(_anterior, objetivo, anterior)) return false;
        if (entidad != null) _impactados.Add(entidad);
        RegistrarParry();
        return true;
    }

    // Erosion Bird usa su propia deteccion de colisiones por cuadros de animacion.
    public void RegistrarParry() => ParryRealizado?.Invoke();

    public void Cancelar() => _vida = 0f;
    public void Draw(SpriteBatch lote)
    {
        if (!Activa) return;
        int cuadro = (int)((2f - _vida) * 15f) % 9;
        float rotacion = MathF.Atan2(_direccion.Y, _direccion.X) + MathHelper.Pi;
        lote.Draw(_hoja, _posicion, new Rectangle(cuadro * 200, 0, 200, 100), Color.White,
            rotacion, new Vector2(100, 50), 1f, SpriteEffects.None, 0f);
    }
}
