# Parasite Invasion

Prototipo de videojuego 3D low-poly en Unity: un shooter de oleadas arcade dentro del cuerpo humano. El Piloto Glóbulo Rojo A1, a bordo de la Nave A1, recorre las venas eliminando los parásitos que envía el Parásito Madre.

- **Autor:** Carlos Darío Botina Mora
- **Programa:** Desarrollo de Videojuegos y Entornos Interactivos — SENA, ficha 3466174
- **Motor:** Unity 6000.2.7f2 · pipeline integrado · Input Manager clásico

## Cómo abrirlo

1. En Unity Hub: **Add → Add project from disk** y seleccionar esta carpeta.
2. Abrir con Unity **6000.2.7f2**.
3. Abrir la escena `Assets/Escenas/01_MenuPrincipal` y presionar **Play**.

## Escenas

| Escena | Pantalla |
|---|---|
| `01_MenuPrincipal` | Menú principal |
| `02_Opciones` | Opciones (incluye la dificultad) |
| `03_ComoJugar` | Controles y objetivos |
| `04_Creditos` | Créditos |
| `05_Juego` | Nivel 1: Vena Principal |
| `06_Iluminacion_AA3` | Escena de iluminación (evidencia AA3) |
| `07_Particulas_AA4` | Escena de partículas y sonido (evidencia AA4) |

## Nivel 1: Vena Principal

- La vena es un tubo cerrado en forma de anillo, así que no se ve dónde empieza ni dónde termina.
- Hay 3 oleadas de parásitos (19 en total): **Larva** (lenta, embiste), **Espora** (rápida y frágil) y **Escupidor** (dispara a distancia).
- Premios: **Cápsula de medicina** (+1 dosis), **Vida extra** (+35 de vida) y **Dosis máxima** (llena el oxígeno y da +2 dosis).
- Los tentáculos salen del suelo y de las paredes, se balancean y hacen daño.
- **Victoria:** limpiar las 3 oleadas y cruzar la salida hacia la cámara cardíaca.
- **Derrota:** quedarse sin vida, sin oxígeno o sin tiempo (3 minutos).

## Controles

| Acción | Tecla |
|---|---|
| Moverse | Flechas |
| Subir / bajar | Re Pág / Av Pág |
| Disparar | D, Espacio o clic |
| Esquivar | S |
| Curarse (gasta una dosis) | B |

## Estructura

| Ruta | Contenido |
|---|---|
| `Assets/Escenas/` | Escenas del juego |
| `Assets/Scripts/Juego/` | Piloto, parásitos, disparos, oleadas, premios, tentáculos, cámara y pista en anillo |
| `Assets/Scripts/UI/` | Menús, opciones, HUD y paneles emergentes |
| `Assets/Scripts/Efectos/` | Luz pulsante y sonido de los sistemas de partículas |
| `Assets/Personajes/`, `Obstaculos/`, `Premios/`, `Escenarios/` | Modelos FBX |
| `Assets/Juego/` | Materiales, prefabs y la malla de la vena del Nivel 1 |
| `Assets/UI/Sprites/` | Gráficos de la interfaz |
| `Assets/Sonidos/` | Efectos de sonido |
| `Assets/Editor/` | Herramientas del editor para armar la interfaz y el nivel |
| `Assets/PowerUp/` | Paquete de partículas de la Asset Store |
