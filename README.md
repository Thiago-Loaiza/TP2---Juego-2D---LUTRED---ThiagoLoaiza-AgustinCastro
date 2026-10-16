# TP2---Juego-2D---LUTRED---ThiagoLoaiza-AgustinCastro
Archivos del proyecto de MonoGame

El audio sigue el [capítulo 15 de la guía de MonoGame](https://docs.monogame.net/articles/tutorials/building_2d_games/15_audio_controller/).
`Core.Audio` expone el `AudioController`, que se actualiza cada frame y se libera al cerrar el juego.

- `PlaySong` y `StopSong` controlan la música cargada como `Song`.
- `PlaySoundEffect(SoundEffect, ...)` crea una instancia y la libera automáticamente al terminar.
- `CreateSoundEffectInstance` registra instancias reutilizables, como los pasos y los parries. Se reproducen y detienen desde el controlador y se liberan con `ReleaseSoundEffect` antes de descargar el contenido de la escena.
- Las tres pistas de selección se registran con `isMusic: true` para conservar su mezcla sincronizada y usar `SongVolume`. Los efectos usan `SoundEffectVolume`; ambos volúmenes van de 0 a 1.
- `SetVolume` ajusta el volumen individual y `SetPitch` cambia el tono de los pasos al correr. `PauseAudio`, `ResumeAudio`, `MuteAudio`, `UnmuteAudio` y `ToggleMute` controlan el audio global. No tienen teclas asignadas.

Para comprobar el sonido en ejecución: alternar las opciones de selección, entrar en la partida, caminar y correr, realizar parries de Duck y JarJar, y perder. Comprobar que las pistas cambian suavemente, los efectos mantienen sus volúmenes y no queda audio al perder o cerrar.

Los botones dibujados se crean con `Juego/1/UI/BotonAnimado.cs`. Es un `Button` de Gum que contiene un `SpriteRuntime` y su etiqueta. `AnimatedSprite` selecciona el cuadro de la hoja y Gum dibuja esa región en el mismo espacio del botón, por lo que la imagen y la entrada comparten posición y tamaño.

```csharp
var boton = new BotonAnimado(hoja, 400, 200, 9,
    TimeSpan.FromSeconds(0.12), new Rectangle(32, 32, 336, 136))
{
    Text = "Jugar",
    Width = 320,
    Height = 130,
    ColorNormal = new Color(155, 155, 155),
    ColorIluminado = Color.White
};
boton.Anchor(Anchor.Center);
boton.Click += Jugar;
panel.AddChild(boton);
```

Los parámetros indican el tamaño de cada celda, la cantidad de cuadros, el tiempo por cuadro y un recorte opcional dentro de cada celda. Se admiten hojas de varias filas, recorridas de izquierda a derecha y de arriba abajo. El recorte elimina los márgenes vacíos sin modificar el archivo original. Conviene mantener el dibujo alineado y dentro del mismo recorte en todos los cuadros.

La escena llama a `boton.Update(gameTime)` en cada actualización y a `boton.Dispose()` al salir, retirando también sus propios manejadores de `Click`. No hace falta llamar a `Draw` para el botón, lo dibuja `GumService.Default.Draw()`. La hoja se carga con el `ContentManager` de la escena y se libera junto con su contenido.

Al pasar el cursor, el dibujo y el texto se iluminan y la animación se congela en su cuadro actual. Al salir, continúa con el tiempo que conservaba. La navegación con teclado o mando también resalta y detiene el botón enfocado, mientras que el foco inicial por sí solo permite ver el wiggle. Para cambiar la velocidad o el brillo de otros botones, se usan la demora y las propiedades de color de cada instancia.
