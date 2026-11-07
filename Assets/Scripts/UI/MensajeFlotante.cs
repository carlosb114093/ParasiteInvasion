using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso breve que aparece, se mantiene unos segundos y se desvanece solo.
///
/// Se usa para las confirmaciones que no exigen respuesta del jugador
/// ("ajustes guardados", "dosis recogida"), de modo que no haga falta un
/// cuadro emergente con boton de aceptar para cada mensaje menor.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MensajeFlotante : MonoBehaviour
{
    [Tooltip("Texto del aviso")]
    public Text etiqueta;

    [Tooltip("Segundos que permanece completamente visible")]
    public float permanencia = 2f;

    [Tooltip("Segundos que tarda en aparecer y en desvanecerse")]
    public float transicion = 0.25f;

    private CanvasGroup grupo;
    private float restante;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
    }

    public void Mostrar(string texto)
    {
        if (etiqueta != null) etiqueta.text = texto;
        restante = permanencia + transicion;
    }

    void Update()
    {
        if (restante <= 0f)
        {
            // Se desvanece con tiempo real: el aviso tambien debe apagarse
            // cuando la partida esta congelada por la pausa.
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, 0f,
                Time.unscaledDeltaTime / Mathf.Max(transicion, 0.01f));
            return;
        }

        restante -= Time.unscaledDeltaTime;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, 1f,
            Time.unscaledDeltaTime / Mathf.Max(transicion, 0.01f));
    }
}
