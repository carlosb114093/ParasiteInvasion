using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Recorrido automatico de la interfaz para grabar el video de la evidencia
/// GA2-220501087-AA2-EV01.
///
/// Al dar Play navega solo por las cinco pantallas y los ocho cuadros
/// emergentes en un orden fijo y con tiempos medidos, de modo que la
/// grabacion salga igual en cada toma y sin titubeos del raton. No pulsa
/// teclas: envia a cada boton los mismos eventos que enviaria el raton
/// (entrar, presionar, soltar, clic), asi que en pantalla se ven los estados
/// resaltado y presionado descritos en el informe.
///
/// Uso:
///   1. Crear un GameObject vacio llamado "RecorridoDemo" en la escena
///      01_MenuPrincipal y anadirle este componente.
///   2. Dar Play y grabar la ventana Game con Camtasia.
///   3. F10 aborta el recorrido; con "Arrancar Solo" desmarcado el
///      recorrido no empieza hasta pulsar F9.
///
/// Mientras dura el recorrido el juego queda en silencio, para que el sonido
/// de fondo del nivel no tape la voz en off. El campo "Volumen Juego" permite
/// dejarlo sonando bajito en lugar de mudo.
///
/// La consola imprime la marca de tiempo real de cada bloque, util para
/// alinear la voz en off en Camtasia.
/// </summary>
public class RecorridoDemo : MonoBehaviour
{
    [Header("Arranque")]
    [Tooltip("Empezar el recorrido en cuanto se da Play")]
    public bool arrancarSolo = true;

    [Tooltip("Tecla para empezar a mano cuando Arrancar Solo esta desmarcado")]
    public KeyCode teclaArranque = KeyCode.F9;

    [Tooltip("Tecla para abortar el recorrido en cualquier momento")]
    public KeyCode teclaAborto = KeyCode.F10;

    [Header("Ritmo")]
    [Tooltip("Multiplica todas las esperas. 1 = duracion prevista de unos 3:30")]
    public float ritmo = 1f;

    [Header("Audio")]
    [Tooltip("Volumen del juego mientras dura el recorrido. 0 lo silencia por " +
             "completo para que no tape la voz en off; 0.15 lo deja de fondo")]
    [Range(0f, 1f)]
    public float volumenJuego = 0f;

    // El objeto sobrevive a los cambios de escena, y cada escena del menu
    // trae su propia copia: sin este guardia se acumularian recorridos.
    private static RecorridoDemo instancia;

    private bool enMarcha;
    private float inicio;
    private float volumenPrevio;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (arrancarSolo) Arrancar();
    }

    void Update()
    {
        if (!enMarcha && Input.GetKeyDown(teclaArranque)) Arrancar();
        if (enMarcha && Input.GetKeyDown(teclaAborto)) Abortar();
    }

    /// <summary>
    /// El volumen se impone despues de todos los Update: la pantalla de
    /// opciones tambien escribe en AudioListener.volume al mover los
    /// deslizadores y al pulsar Aplicar, y si no se hiciera aqui el juego
    /// recuperaria el sonido a mitad del recorrido.
    /// </summary>
    void LateUpdate()
    {
        if (enMarcha) AudioListener.volume = Mathf.Clamp01(volumenJuego);
    }

    void OnDisable()
    {
        if (enMarcha) AudioListener.volume = volumenPrevio;
    }

    private void Arrancar()
    {
        if (enMarcha) return;
        enMarcha = true;
        inicio = Time.realtimeSinceStartup;
        volumenPrevio = AudioListener.volume;
        StartCoroutine(Recorrido());
    }

    private void Abortar()
    {
        StopAllCoroutines();
        enMarcha = false;
        Time.timeScale = 1f;
        AudioListener.volume = volumenPrevio;
        Debug.Log("[Recorrido] abortado por el usuario");
    }

    // ------------------------------------------------------------------
    //  Recorrido
    // ------------------------------------------------------------------

    private IEnumerator Recorrido()
    {
        // ---------- Bloque 1: pantalla principal ----------
        Marca("Bloque 1 - Pantalla principal");
        yield return EsperarPantalla<MenuPrincipalUI>();
        yield return Esperar(5f);

        var menu = Buscar<MenuPrincipalUI>();
        if (menu != null)
        {
            // Se pasa el foco por las cinco opciones para que se vea el
            // estado resaltado de cada boton sin activarlo.
            yield return Resaltar(menu.botonJugar);
            yield return Resaltar(menu.botonOpciones);
            yield return Resaltar(menu.botonComoJugar);
            yield return Resaltar(menu.botonCreditos);
            yield return Resaltar(menu.botonSalir);
            yield return Esperar(1f);
        }

        // ---------- Bloque 2: cuadro de seleccion de nivel ----------
        Marca("Bloque 2 - Pop-up de seleccion de nivel");
        menu = Buscar<MenuPrincipalUI>();
        if (menu != null)
        {
            yield return Pulsar(menu.botonJugar, 0.5f, 1.8f);
            yield return Esperar(2.5f);

            // Cambiar de nivel actualiza la descripcion y el resaltado.
            yield return PulsarNivel(menu, 1, 1.8f);
            yield return PulsarNivel(menu, 2, 1.8f);
            yield return PulsarNivel(menu, 0, 1.8f);

            yield return Esperar(1.5f);
            yield return Pulsar(menu.botonCancelarNivel, 0.5f, 1.6f);
        }

        // ---------- Bloque 3: confirmacion de salida ----------
        Marca("Bloque 3 - Pop-up de confirmar salida");
        menu = Buscar<MenuPrincipalUI>();
        if (menu != null)
        {
            yield return Pulsar(menu.botonSalir, 0.6f, 1.6f);
            yield return Esperar(3.5f);
            // Se pulsa la opcion que no destruye nada: "Si" cerraria el Play.
            yield return Pulsar(menu.botonSalirNo, 0.6f, 1.5f);
        }

        // ---------- Bloque 4: pantalla de opciones ----------
        Marca("Bloque 4 - Pantalla de opciones");
        menu = Buscar<MenuPrincipalUI>();
        if (menu != null) yield return Pulsar(menu.botonOpciones, 0.8f, 0.5f);

        yield return EsperarPantalla<OpcionesUI>();
        yield return Esperar(3f);

        var opciones = Buscar<OpcionesUI>();
        if (opciones != null)
        {
            // Deslizadores: el porcentaje se actualiza mientras se arrastra.
            yield return AnimarDeslizador(opciones.deslizadorGeneral, 0.45f, 1.6f);
            yield return Esperar(0.6f);
            yield return AnimarDeslizador(opciones.deslizadorMusica, 0.25f, 1.6f);
            yield return Esperar(0.6f);
            yield return AnimarDeslizador(opciones.deslizadorEfectos, 1f, 1.6f);
            yield return Esperar(1f);
            yield return AnimarDeslizador(opciones.deslizadorGeneral, 0.85f, 1.4f);
            yield return Esperar(1f);

            // Listas desplegables.
            yield return AbrirLista(opciones.listaCalidad, 2, 2.2f);
            yield return Esperar(1f);
            yield return AbrirLista(opciones.listaDificultad, 2, 2.2f);
            yield return Esperar(1f);

            // Interruptores.
            yield return Pulsar(opciones.interruptorVibracion, 0.5f, 1.4f);
            yield return Pulsar(opciones.interruptorPantallaCompleta, 0.4f, 1.4f);

            // Aplicar guarda y lanza el aviso flotante.
            yield return Pulsar(opciones.botonAplicar, 0.6f, 0.5f);
            yield return Esperar(3.4f);

            // Restablecer pide confirmacion antes de tocar nada.
            Marca("Bloque 4b - Pop-up de restablecer ajustes");
            yield return Pulsar(opciones.botonRestablecer, 0.8f, 1.6f);
            yield return Esperar(3f);
            yield return Pulsar(opciones.botonRestablecerSi, 0.6f, 1f);
            yield return Esperar(3.2f);

            // Volver descarta y devuelve a la pantalla de origen.
            yield return Pulsar(opciones.botonVolver, 0.8f, 0.5f);
        }

        // ---------- Bloque 5: como jugar ----------
        Marca("Bloque 5 - Pantalla Como jugar");
        yield return EsperarPantalla<MenuPrincipalUI>();
        menu = Buscar<MenuPrincipalUI>();
        if (menu != null) yield return Pulsar(menu.botonComoJugar, 0.8f, 0.5f);

        yield return EsperarPantalla<PantallaSimple>();
        yield return Esperar(9f);

        var simple = Buscar<PantallaSimple>();
        if (simple != null) yield return Pulsar(simple.botonVolver, 0.6f, 0.5f);

        // ---------- Bloque 6: creditos ----------
        Marca("Bloque 6 - Pantalla de creditos");
        yield return EsperarPantalla<MenuPrincipalUI>();
        menu = Buscar<MenuPrincipalUI>();
        if (menu != null) yield return Pulsar(menu.botonCreditos, 0.8f, 0.5f);

        yield return EsperarPantalla<PantallaSimple>();
        yield return Esperar(2.5f);

        // El contenido supera la altura del panel: se recorre la barra.
        var barra = Buscar<ScrollRect>();
        yield return AnimarScroll(barra, 0f, 6f);
        yield return Esperar(1.5f);
        yield return AnimarScroll(barra, 1f, 3f);
        yield return Esperar(1f);

        simple = Buscar<PantallaSimple>();
        if (simple != null) yield return Pulsar(simple.botonVolver, 0.6f, 0.5f);

        // ---------- Bloque 7: entrar a la partida ----------
        Marca("Bloque 7 - Entrada a la partida");
        yield return EsperarPantalla<MenuPrincipalUI>();
        menu = Buscar<MenuPrincipalUI>();
        if (menu != null)
        {
            yield return Pulsar(menu.botonJugar, 0.8f, 1.4f);
            yield return Esperar(1.8f);
            yield return Pulsar(menu.botonComenzar, 0.6f, 0.5f);
        }

        yield return EsperarPantalla<HUDJuego>();
        yield return Esperar(3f);

        // ---------- Bloque 8: indicadores del HUD ----------
        Marca("Bloque 8 - Indicadores del HUD");
        var hud = Buscar<HUDJuego>();
        if (hud != null)
        {
            // Puntaje y contador de amenazas.
            for (int i = 0; i < 4; i++)
            {
                hud.SumarPuntaje(150);
                yield return Esperar(1.7f);
            }

            // Aviso flotante de recogida y contador de dosis.
            hud.RecogerDosis();
            yield return Esperar(2.4f);
            hud.RecogerDosis();
            yield return Esperar(2.4f);

            // Danos sucesivos: destello rojo y barra de vida en ambar por
            // debajo del 34 por ciento.
            for (int i = 0; i < 3; i++)
            {
                hud.RecibirDano(25f);
                yield return Esperar(1.6f);
            }

            yield return Esperar(2.5f);

            // Gastar una dosis recupera vida y la barra vuelve a blanco.
            hud.UsarDosis();
            yield return Esperar(3f);
        }

        // ---------- Bloque 9: pausa y abandono ----------
        Marca("Bloque 9 - Menu de pausa y confirmacion de abandono");
        var partida = Buscar<ControlPartida>();
        if (partida != null)
        {
            yield return Pulsar(partida.botonPausa, 0.8f, 0.6f);
            yield return Esperar(4f);

            yield return Pulsar(partida.botonAbandonar, 0.6f, 0.6f);
            yield return Esperar(3.5f);

            // Cancelar devuelve al punto exacto en que estaba el usuario.
            yield return Pulsar(partida.botonAbandonoNo, 0.6f, 0.6f);
            yield return Esperar(2.5f);

            yield return Pulsar(partida.botonReanudar, 0.6f, 0.6f);
            yield return Esperar(2f);
        }

        // ---------- Bloque 10: resultados ----------
        Marca("Bloque 10 - Nivel completado y fin de la partida");
        partida = Buscar<ControlPartida>();
        hud = Buscar<HUDJuego>();

        if (partida != null && hud != null)
        {
            // El cuadro de victoria se abre a mano porque agotar los tres
            // minutos del nivel no cabe en el video; el resumen se rellena
            // con las mismas cifras que usaria la partida real.
            if (partida.resumenVictoria != null)
                partida.resumenVictoria.text = "Conducto despejado.\n\n" +
                    "Puntaje alcanzado: " + hud.Puntaje + "\n" +
                    "Dosis sin usar: " + hud.Dosis;

            partida.cuadroVictoria.Abrir();
            yield return Esperar(6f);
            partida.cuadroVictoria.Cerrar();
            yield return Esperar(1.5f);

            // La derrota si se provoca de verdad: al llegar la vida a cero
            // el HUD avisa y ControlPartida muestra el cuadro por su camino
            // normal.
            hud.RecibirDano(1000f);
            yield return Esperar(6.5f);

            yield return Pulsar(partida.botonSalirDerrota, 0.8f, 0.5f);
        }

        // ---------- Cierre ----------
        Marca("Cierre - Regreso al menu principal");
        yield return EsperarPantalla<MenuPrincipalUI>();
        yield return Esperar(5f);

        enMarcha = false;
        AudioListener.volume = volumenPrevio;
        Marca("FIN del recorrido");
    }

    // ------------------------------------------------------------------
    //  Utilidades
    // ------------------------------------------------------------------

    /// <summary>Espera en tiempo real: los cuadros de pausa congelan el reloj.</summary>
    private IEnumerator Esperar(float segundos)
    {
        float factor = Mathf.Max(0.1f, ritmo);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, segundos * factor));
    }

    private static T Buscar<T>() where T : Component
    {
        return Object.FindFirstObjectByType<T>();
    }

    /// <summary>
    /// Espera a que la escena nueva haya terminado de arrancar, reconociendola
    /// por el componente que la gobierna.
    /// </summary>
    private IEnumerator EsperarPantalla<T>() where T : Component
    {
        float limite = 8f;
        float transcurrido = 0f;

        while (Buscar<T>() == null && transcurrido < limite)
        {
            transcurrido += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Buscar<T>() == null)
            Debug.LogWarning("[Recorrido] no aparecio " + typeof(T).Name +
                             "; se continua igualmente");

        yield return Esperar(0.6f);
    }

    /// <summary>Pasa el foco por un control sin activarlo.</summary>
    private IEnumerator Resaltar(Selectable control)
    {
        if (control == null) yield break;

        var datos = new PointerEventData(EventSystem.current);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(control.gameObject);

        ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.pointerEnterHandler);
        yield return Esperar(0.9f);
        ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.pointerExitHandler);
        yield return Esperar(0.25f);
    }

    /// <summary>
    /// Reproduce sobre un control la secuencia completa de un clic de raton,
    /// para que se vean los estados resaltado y presionado antes de que la
    /// accion se ejecute.
    /// </summary>
    private IEnumerator Pulsar(Selectable control, float antes, float despues)
    {
        if (control == null)
        {
            yield return Esperar(despues);
            yield break;
        }

        yield return Esperar(antes);

        var objetivo = control.gameObject;
        var datos = new PointerEventData(EventSystem.current);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(objetivo);

        ExecuteEvents.Execute(objetivo, datos, ExecuteEvents.pointerEnterHandler);
        yield return Esperar(0.4f);

        ExecuteEvents.Execute(objetivo, datos, ExecuteEvents.pointerDownHandler);
        yield return Esperar(0.15f);

        ExecuteEvents.Execute(objetivo, datos, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(objetivo, datos, ExecuteEvents.pointerClickHandler);

        // El clic puede haber cargado otra pantalla y destruido el boton.
        if (objetivo != null)
            ExecuteEvents.Execute(objetivo, datos, ExecuteEvents.pointerExitHandler);

        yield return Esperar(despues);
    }

    private IEnumerator PulsarNivel(MenuPrincipalUI menu, int indice, float despues)
    {
        if (menu == null || menu.botonesNivel == null ||
            indice < 0 || indice >= menu.botonesNivel.Length)
        {
            yield break;
        }

        yield return Pulsar(menu.botonesNivel[indice], 0.4f, despues);
    }

    /// <summary>Arrastra un deslizador poco a poco hasta el valor pedido.</summary>
    private IEnumerator AnimarDeslizador(Slider control, float destino, float segundos)
    {
        if (control == null) yield break;

        float factor = Mathf.Max(0.1f, ritmo);
        float duracion = Mathf.Max(0.05f, segundos * factor);
        float origen = control.value;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float avance = Mathf.Clamp01(t / duracion);
            // Suavizado en los extremos para que el arrastre no parezca un salto.
            control.value = Mathf.Lerp(origen, destino,
                                       avance * avance * (3f - 2f * avance));
            yield return null;
        }

        control.value = destino;
    }

    /// <summary>Abre una lista desplegable, elige una opcion y la cierra.</summary>
    private IEnumerator AbrirLista(Dropdown lista, int opcion, float visible)
    {
        if (lista == null) yield break;

        lista.Show();
        yield return Esperar(visible);

        int indice = Mathf.Clamp(opcion, 0, Mathf.Max(0, lista.options.Count - 1));
        lista.value = indice;
        lista.RefreshShownValue();

        yield return Esperar(0.5f);
        lista.Hide();
        yield return Esperar(0.8f);
    }

    /// <summary>Recorre una vista con barra de desplazamiento.</summary>
    private IEnumerator AnimarScroll(ScrollRect barra, float destino, float segundos)
    {
        if (barra == null) yield break;

        float factor = Mathf.Max(0.1f, ritmo);
        float duracion = Mathf.Max(0.05f, segundos * factor);
        float origen = barra.verticalNormalizedPosition;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float avance = Mathf.Clamp01(t / duracion);
            barra.verticalNormalizedPosition = Mathf.Lerp(origen, destino, avance);
            yield return null;
        }

        barra.verticalNormalizedPosition = destino;
    }

    /// <summary>
    /// Deja en la consola la marca de tiempo real del bloque, para alinear
    /// despues la voz en off en Camtasia.
    /// </summary>
    private void Marca(string texto)
    {
        float t = Time.realtimeSinceStartup - inicio;
        int minutos = Mathf.FloorToInt(t / 60f);
        float segundos = t - minutos * 60f;
        Debug.Log(string.Format("[Recorrido] {0:0}:{1:00.0}  {2}",
                                minutos, segundos, texto));
    }
}
