using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace _1.UI;

// Una pulsacion inmediata, seguida de repeticiones mientras se mantiene la direccion.
internal sealed class RepeticionNavegacion
{
    public const float Demora = 0.32f;
    public const float Intervalo = 0.09f;
    private int _direccion;
    private float _restante;

    public int Update(GameTime tiempo, bool negativo, bool positivo, bool habilitado = true)
    {
        int direccion = habilitado ? (positivo ? 1 : 0) - (negativo ? 1 : 0) : 0;
        if (direccion != _direccion)
        {
            _direccion = direccion;
            _restante = Demora;
            return direccion;
        }
        if (direccion == 0) return 0;
        _restante -= (float)tiempo.ElapsedGameTime.TotalSeconds;
        if (_restante > 0) return 0;
        _restante += Intervalo;
        if (_restante <= 0) _restante = Intervalo;
        return direccion;
    }

    public void Reiniciar() { _direccion = 0; _restante = 0; }

    public static bool Izquierda(KeyboardState t) => t.IsKeyDown(Keys.Left) || t.IsKeyDown(Keys.A);
    public static bool Derecha(KeyboardState t) => t.IsKeyDown(Keys.Right) || t.IsKeyDown(Keys.D);
    public static bool Arriba(KeyboardState t) => t.IsKeyDown(Keys.Up) || t.IsKeyDown(Keys.W);
    public static bool Abajo(KeyboardState t) => t.IsKeyDown(Keys.Down) || t.IsKeyDown(Keys.S);
}
