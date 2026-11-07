using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla principal del prototipo.
///
/// Enlaza los cinco botones del menu con su destino y gobierna los dos
/// cuadros emergentes que se abren sin salir de esta pantalla: la seleccion
/// de nivel y la confirmacion de salida.
/// </summary>
public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Botones del menu")]
    public Button botonJugar;
    public Button botonOpciones;
    public Button botonComoJugar;
    public Button botonCreditos;
    public Button botonSalir;

    [Header("Cuadros emergentes")]
    public PanelEmergente cuadroNivel;
    public PanelEmergente cuadroSalir;

    [Header("Seleccion de nivel")]
    public Button[] botonesNivel;
    public Button botonComenzar;
    public Button botonCancelarNivel;
    public Text etiquetaNivel;

    [Header("Confirmacion de salida")]
    public Button botonSalirSi;
    public Button botonSalirNo;

    [Header("Pie de pantalla")]
    public Text etiquetaVersion;

    private int nivelElegido = 1;

    void Start()
    {
        AjustesJuego.Cargar();

        botonJugar.onClick.AddListener(cuadroNivel.Abrir);
        botonOpciones.onClick.AddListener(() => Navegador.Ir(Navegador.Opciones));
        botonComoJugar.onClick.AddListener(() => Navegador.Ir(Navegador.ComoJugar));
        botonCreditos.onClick.AddListener(() => Navegador.Ir(Navegador.Creditos));
        botonSalir.onClick.AddListener(cuadroSalir.Abrir);

        for (int i = 0; i < botonesNivel.Length; i++)
        {
            int numero = i + 1;
            botonesNivel[i].onClick.AddListener(() => ElegirNivel(numero));
        }

        botonComenzar.onClick.AddListener(() => Navegador.Ir(Navegador.Juego));
        botonCancelarNivel.onClick.AddListener(cuadroNivel.Cerrar);

        botonSalirSi.onClick.AddListener(Navegador.Salir);
        botonSalirNo.onClick.AddListener(cuadroSalir.Cerrar);

        if (etiquetaVersion != null)
            etiquetaVersion.text = "Prototipo v0.3  -  Evidencia GA2-AA2-EV01";

        ElegirNivel(1);
    }

    /// <summary>
    /// Marca el nivel activo: el elegido queda resaltado y el resto vuelve a
    /// su color normal, de forma que la seleccion sea visible sin leer texto.
    /// </summary>
    private void ElegirNivel(int numero)
    {
        nivelElegido = numero;

        for (int i = 0; i < botonesNivel.Length; i++)
        {
            var imagen = botonesNivel[i].GetComponent<Image>();
            bool activo = (i + 1) == numero;
            imagen.color = activo
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0.62f, 0.62f, 0.66f, 1f);
        }

        if (etiquetaNivel != null)
        {
            string[] descripciones =
            {
                "Nivel 1  -  Vena femoral: recorrido de entrenamiento con pocos parasitos.",
                "Nivel 2  -  Arteria pulmonar: corrientes rapidas y tentaculos que obstruyen.",
                "Nivel 3  -  Camara cardiaca: enjambre parasitario y dosis limitadas."
            };
            etiquetaNivel.text = descripciones[Mathf.Clamp(numero - 1, 0, 2)];
        }

        PlayerPrefs.SetInt("nivel_elegido", nivelElegido);
    }
}
