using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de opciones.
///
/// Los deslizadores y listas escriben sobre una copia de trabajo, no sobre
/// los ajustes vigentes: el jugador puede mover controles y descartar los
/// cambios con "Volver" sin haber alterado nada. Solo "Aplicar" traslada la
/// copia a PlayerPrefs.
/// </summary>
public class OpcionesUI : MonoBehaviour
{
    [Header("Audio")]
    public Slider deslizadorGeneral;
    public Slider deslizadorMusica;
    public Slider deslizadorEfectos;
    public Text valorGeneral;
    public Text valorMusica;
    public Text valorEfectos;

    [Header("Video y juego")]
    public Dropdown listaCalidad;
    public Dropdown listaDificultad;
    public Toggle interruptorPantallaCompleta;
    public Toggle interruptorVibracion;

    [Header("Acciones")]
    public Button botonAplicar;
    public Button botonRestablecer;
    public Button botonVolver;

    [Header("Cuadros emergentes")]
    public PanelEmergente cuadroRestablecer;
    public Button botonRestablecerSi;
    public Button botonRestablecerNo;
    public MensajeFlotante aviso;

    // Copia de trabajo: lo que se ve en pantalla antes de confirmar.
    private float general, musica, efectos;
    private int calidad, dificultad;
    private bool pantallaCompleta, vibracion;

    void Start()
    {
        AjustesJuego.Cargar();
        TomarValoresVigentes();

        listaCalidad.ClearOptions();
        listaCalidad.AddOptions(new System.Collections.Generic.List<string>(
            AjustesJuego.NombresCalidad));
        listaDificultad.ClearOptions();
        listaDificultad.AddOptions(new System.Collections.Generic.List<string>(
            AjustesJuego.NombresDificultad));

        deslizadorGeneral.onValueChanged.AddListener(v => { general = v; Refrescar(); });
        deslizadorMusica.onValueChanged.AddListener(v => { musica = v; Refrescar(); });
        deslizadorEfectos.onValueChanged.AddListener(v => { efectos = v; Refrescar(); });
        listaCalidad.onValueChanged.AddListener(v => calidad = v);
        listaDificultad.onValueChanged.AddListener(v => dificultad = v);
        interruptorPantallaCompleta.onValueChanged.AddListener(v => pantallaCompleta = v);
        interruptorVibracion.onValueChanged.AddListener(v => vibracion = v);

        botonAplicar.onClick.AddListener(Aplicar);
        botonRestablecer.onClick.AddListener(cuadroRestablecer.Abrir);
        botonVolver.onClick.AddListener(Navegador.Volver);

        botonRestablecerSi.onClick.AddListener(ConfirmarRestablecer);
        botonRestablecerNo.onClick.AddListener(cuadroRestablecer.Cerrar);

        VolcarEnControles();
    }

    private void TomarValoresVigentes()
    {
        general = AjustesJuego.VolumenGeneral;
        musica = AjustesJuego.VolumenMusica;
        efectos = AjustesJuego.VolumenEfectos;
        calidad = AjustesJuego.Calidad;
        dificultad = AjustesJuego.Dificultad;
        pantallaCompleta = AjustesJuego.PantallaCompleta;
        vibracion = AjustesJuego.Vibracion;
    }

    private void VolcarEnControles()
    {
        deslizadorGeneral.SetValueWithoutNotify(general);
        deslizadorMusica.SetValueWithoutNotify(musica);
        deslizadorEfectos.SetValueWithoutNotify(efectos);
        listaCalidad.SetValueWithoutNotify(calidad);
        listaDificultad.SetValueWithoutNotify(dificultad);
        interruptorPantallaCompleta.SetIsOnWithoutNotify(pantallaCompleta);
        interruptorVibracion.SetIsOnWithoutNotify(vibracion);
        listaCalidad.RefreshShownValue();
        listaDificultad.RefreshShownValue();
        Refrescar();
    }

    /// <summary>Muestra el valor de cada deslizador en porcentaje.</summary>
    private void Refrescar()
    {
        if (valorGeneral != null) valorGeneral.text = Porcentaje(general);
        if (valorMusica != null) valorMusica.text = Porcentaje(musica);
        if (valorEfectos != null) valorEfectos.text = Porcentaje(efectos);

        // Realimentacion inmediata del volumen mientras se arrastra.
        AudioListener.volume = general;
    }

    private static string Porcentaje(float v)
    {
        return Mathf.RoundToInt(v * 100f) + " %";
    }

    private void Aplicar()
    {
        AjustesJuego.VolumenGeneral = general;
        AjustesJuego.VolumenMusica = musica;
        AjustesJuego.VolumenEfectos = efectos;
        AjustesJuego.Calidad = calidad;
        AjustesJuego.Dificultad = dificultad;
        AjustesJuego.PantallaCompleta = pantallaCompleta;
        AjustesJuego.Vibracion = vibracion;
        AjustesJuego.Guardar();

        if (aviso != null) aviso.Mostrar("Ajustes guardados correctamente");
    }

    private void ConfirmarRestablecer()
    {
        AjustesJuego.Restablecer();
        TomarValoresVigentes();
        VolcarEnControles();
        cuadroRestablecer.Cerrar();

        if (aviso != null) aviso.Mostrar("Se restauraron los valores de fabrica");
    }
}
