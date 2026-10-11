# TP2---Juego-2D---LUTRED---ThiagoLoaiza-AgustinCastro
Archivos del proyecto de MonoGame

## Recursos de Content

Al compilar el juego, `Juego/1/Tools/Sync-Content.ps1` sincroniza
`Content.mgcb` con los archivos de `Juego/1/Content`.

- Agrega imagenes PNG, JPG, JPEG, BMP, GIF, TGA y DDS; audio WAV, MP3 y OGG;
  fuentes `.spritefont` y efectos `.fx`.
- El audio dentro de `SFX` usa `SoundEffectProcessor`; el resto usa `SongProcessor`.
- Conserva las opciones de los recursos existentes y quita entradas cuyos
  archivos ya no existen. Ignora `bin` y `obj`.
- Las fuentes TTF nuevas requieren configuracion manual en MGCB o una
  descripcion `.spritefont`.
- Si MGCB Editor estaba abierto, cerrarlo sin guardar y volver a abrirlo para
  ver la lista actualizada. Una ventana antigua podria sobrescribir la lista.

Agregar un recurso a la lista permite compilarlo; para usarlo en el juego
tambien hay que cargarlo desde C# con su ruta relativa a `Content`, sin extension.
