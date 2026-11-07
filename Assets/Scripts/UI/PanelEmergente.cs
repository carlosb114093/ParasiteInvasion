using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cuadro emergente (pop-up) del prototipo.
///
/// Se encarga del comportamiento comun a todos los cuadros: aparecer con una
/// pequena escala de entrada, oscurecer lo que hay detras, bloquear el resto
/// de la interfaz mientras esta abierto y cerrarse con Escape. Cada cuadro
/// concreto (pausa, confirmacion, fin de partida) solo anade sus botones.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PanelEmergente : MonoBehaviour
{
    [Tooltip("Velo oscuro que se muestra detras del cuadro")]
    public GameObject velo;

    [Tooltip("Contenedor que crece al abrirse")]
    public RectTransform cuadro;

    [Tooltip("Elemento que recibe el foco al abrir, para poder navegar con teclado")]
    public Selectable primerElemento;

    [Tooltip("Permitir cerrar con la tecla Escape")]
    public bool cierraConEscape = true;

    [Tooltip("Congelar el juego mientras el cuadro este visible")]
    public bool congelaElJuego;

    [Tooltip("Duracion de la animacion de apertura en segundos")]
    public float duracionApertura = 0.18f;

    private CanvasGroup grupo;
    private float avance;
    private bool abierto;

    /// <summary>Indica si el cuadro esta visible en este momento.</summary>
    public bool Abierto { get { return abierto; } }

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        // Arranca oculto sin desactivar el objeto: asi las referencias que
        // otros scripts guardan hacia sus botones siguen siendo validas.
        AplicarVisibilidad(0f);
        abierto = false;
    }

    void Update()
    {
        // Animacion de entrada y salida independiente de Time.timeScale, que
        // vale cero cuando el cuadro congela la partida.
        float objetivo = abierto ? 1f : 0f;
        if (!Mathf.Approximately(avance, objetivo))
        {
            float paso = Time.unscaledDeltaTime / Mathf.Max(duracionApertura, 0.01f);
            avance = Mathf.MoveTowards(avance, objetivo, paso);
            AplicarVisibilidad(avance);
        }

        if (abierto && cierraConEscape && Input.GetKeyDown(KeyCode.Escape))
            Cerrar();
    }

    public void Abrir()
    {
        if (abierto) return;
        abierto = true;
        gameObject.SetActive(true);

        if (congelaElJuego) Time.timeScale = 0f;

        if (primerElemento != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primerElemento.gameObject);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;

        if (congelaElJuego) Time.timeScale = 1f;
    }

    /// <summary>Abre o cierra segun el estado actual.</summary>
    public void Alternar()
    {
        if (abierto) Cerrar(); else Abrir();
    }

    /// <summary>
    /// Traduce el avance de la animacion a opacidad, escala y bloqueo de
    /// clics. Con avance cero el cuadro es transparente y deja pasar el raton.
    /// </summary>
    private void AplicarVisibilidad(float t)
    {
        float suave = t * t * (3f - 2f * t);

        grupo.alpha = suave;
        grupo.interactable = t > 0.5f;
        grupo.blocksRaycasts = t > 0.01f;

        if (velo != null) velo.SetActive(t > 0.01f);

        if (cuadro != null)
        {
            float escala = Mathf.Lerp(0.88f, 1f, suave);
            cuadro.localScale = new Vector3(escala, escala, 1f);
        }
    }
}
