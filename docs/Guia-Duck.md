# Duck: persecucion, colisiones y parry

## 1. El dibujo y la colision son cosas distintas

Un sprite puede tener margenes transparentes. Usar toda su imagen como zona de
contacto haria que Duck tocara a Sinner antes de que sus dibujos se acercaran.
Por eso usamos circulos invisibles: cada uno guarda un centro y un radio en
`CirculoColision.cs`.

En `Sinner.cs`, `Colision` representa el cuerpo, con radio de 24 pixeles.
Su centro esta 15 pixeles por debajo de `Position`, para acercarlo al cuerpo
dibujado. `ColisionParry` usa ese mismo centro y un radio de 28 pixeles.
En `Duck.cs`, `Colision` tiene radio de 38 pixeles y centro en `Position`.
Estas medidas son aproximaciones ajustables, no colisiones por pixel.

Dos circulos se tocan cuando la distancia entre sus centros es menor o igual
que la suma de sus radios. Para el cuerpo de Sinner y Duck: 24 + 38 = 62 pixeles.
Para el parry: 28 + 38 = 66 pixeles. El parry puede alcanzarlo un poco antes.

`Toca` compara distancias al cuadrado. Es equivalente a comparar distancias,
pero evita calcular una raiz cuadrada.

## 2. Como se acerca Duck

En `Duck.Update`, restamos su posicion al centro de Sinner:

```csharp
Vector2 diferencia = objetivo - Position;
```

Dividir ese vector por su longitud produce una direccion de longitud 1.
Luego multiplicamos por la velocidad y por el tiempo transcurrido:

```csharp
movimiento = direccion * velocidad * segundos;
```

Asi la velocidad se mide en pixeles por segundo y no depende de los FPS.
El desplazamiento se limita a la distancia restante para no sobrepasar el objetivo.
Duck empieza a 360 pixeles a la derecha de Sinner y siempre lo persigue,
excepto mientras esta retrocediendo por un parry.

## 3. Como se resuelve el contacto

En `Game1.Update` guardamos los centros anteriores y actualizamos los personajes.
Despues comprobamos el parry antes de comprobar el contacto con el cuerpo:

1. Si Sinner esta haciendo parry y Duck cruza su zona de parry, se bloquea el contacto.
2. Si Duck todavia no esta rebotando, inicia el rebote y suenan `DuckParry`
   y `ParryExitoso`. No repetimos esos sonidos en cada actualizacion del mismo rebote.
3. Si no se bloqueo el contacto y Duck toca el cuerpo de Sinner, ocurre la derrota.

`HaciendoParry` expone el estado que ya tenia Sinner. Protege durante toda
la animacion: cinco fotogramas de 0,05 segundos, aproximadamente 0,25 segundos.
El sonido `Parry` sigue sonando al pulsar F; `ParryExitoso` solo suena si conecta.
`ParryExitosoDuro` no se utiliza todavia.

## 4. Por que tambien comprobamos el recorrido

Comprobar solo las posiciones finales puede fallar si un personaje pasa de un
lado al otro del rival entre dos actualizaciones. Por eso `SeCruzan` utiliza
los centros anteriores y actuales de ambos.

Resta sus posiciones para obtener el movimiento relativo y busca el punto de
ese recorrido mas cercano al origen. Si su distancia es menor que la suma de
los radios, hubo contacto en algun momento del intervalo. Esto supone un
recorrido recto entre las dos posiciones registradas.

## 5. Como funciona el rebote

`Duck.Rebotar` calcula una direccion desde el centro del parry hacia el lado
por el que llegaba Duck. Lo coloca justo fuera del circulo del parry para
separar los cuerpos y le asigna una velocidad inicial de retroceso.

Durante el rebote, `Update` reduce esa rapidez con `FrenadoRebote`. Cuando
llega a cero, Duck vuelve a perseguir. Con los valores iniciales, retrocede
unos 501 pixeles durante 1,06 segundos.

Las constantes de `Duck.cs` permiten experimentar:

| Constante | Valor | Efecto al aumentarla |
| --- | --- | --- |
| `VelocidadPersecucion` | 190 | Duck se acerca mas rapido. |
| `VelocidadRebote` | 950 | Sale despedido mas rapido y mas lejos. |
| `FrenadoRebote` | 900 | El retroceso termina antes y recorre menos distancia. |
| `RadioColision` | 38 | Duck entra en contacto desde mas lejos. |

`Dukquack` tiene un temporizador independiente. Cada vez que suena, se elige
una espera nueva entre 5 y 12 segundos usando `Random.NextDouble()`.

## 6. La pantalla de derrota

`Game1` guarda el estado en `_derrotado`. Al activarlo, detiene la musica y los
sonidos de los personajes. Deja de actualizar y dibujar el mundo y empieza
a actualizar y dibujar `PantallaDerrota`. Por eso el cambio a negro es inmediato.

`PantallaDerrota.Mostrar` elige una de las tres animaciones una sola vez.
Las hojas tienen celdas de 100x100: Eye tiene 14 fotogramas, Flower 19 y Line 6.
Se repiten sobre negro, centradas usando la mitad del ancho y alto de la ventana.
Por ahora la pantalla permanece ahi. Escape sigue cerrando el juego.

## 7. Para probar y aprender

Primero deja que Duck llegue sin pulsar F. Despues vuelve a ejecutar el juego
y pulsa F justo antes del contacto. Observa la diferencia entre el sonido de
intentar un parry y los dos sonidos de lograrlo.

Prueba cambiar una sola constante por vez. Por ejemplo, aumentar
`FrenadoRebote` a 1800 reduce aproximadamente a la mitad el tiempo y la distancia
del retroceso. Luego prueba un radio mas pequeno para notar como cambia el contacto.

`Tests/DuckChecks` contiene comprobaciones de colision, persecucion, parry,
rebote, sonidos, derrota y renderizado de las tres animaciones. La prueba
activa el estado de parry de forma controlada; no simula una pulsacion fisica de F.

Desde la raiz del proyecto, se puede ejecutar con:

```powershell
dotnet run --project Tests/DuckChecks
```

La prueba abre el juego brevemente, realiza las comprobaciones y se cierra.
Guarda capturas en la carpeta `Capturas` junto al ejecutable de prueba.
