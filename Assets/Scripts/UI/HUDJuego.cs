using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicadores de la pantalla de juego y estado de la partida.
///
/// Reune en un solo componente los seis indicadores del HUD (vida, oxigeno,
/// dosis, puntaje, tiempo y nivel) y las reglas minimas que los hacen
/// cambiar. El oxigeno baja solo; los golpes, premios y parasitos
/// eliminados llegan desde los scripts de juego (ControlJugador, Parasito,
/// Recogible y GeneradorOleadas). Las teclas de prueba del prototipo de
/// interfaz siguen disponibles activando "Teclas De Prueba".
/// </summary>
public class HUDJuego : MonoBehaviour
{
    [Header("Barras")]
    public Image barraVida;
    public Image barraOxigeno;
    public Text textoVida;
    public Text textoOxigeno;

    [Header("Contadores")]
    public Text textoDosis;
    public Text textoPuntaje;
    public Text textoTiempo;
    public Text textoNivel;
    public Text textoAmenazas;

    [Header("Avisos")]
    public MensajeFlotante aviso;
    public Image tinteDano;

    [Header("Parametros de la partida")]
    public float vidaMaxima = 100f;
    public float oxigenoMaximo = 100f;

    [Tooltip("Oxigeno que se consume por segundo mientras se avanza")]
    public float consumoOxigeno = 1.6f;

    [Tooltip("Dosis de medicina con las que arranca el nivel")]
    public int dosisIniciales = 6;

    [Tooltip("Duracion del nivel en segundos")]
    public float duracionNivel = 180f;

    [Tooltip("Parasitos que hay que eliminar para despejar el conducto")]
    public int amenazasIniciales = 8;

    [Tooltip("Vida que devuelve cada dosis aplicada con B")]
    public float curacionPorDosis = 25f;

    [Header("Reglas")]
    [Tooltip("Si esta marcado, agotar el tiempo completa el nivel (prototipo de " +
             "interfaz). Si no, agotar el tiempo es derrota.")]
    public bool victoriaAlAgotarTiempo = false;

    [Tooltip("Teclas X, C, V y N que simulan sucesos sin jugar")]
    public bool teclasDePrueba = false;

    /// <summary>Se dispara cuando la vida o el oxigeno llegan a cero.</summary>
    public event Action AlPerder;

    /// <summary>Se dispara al completar el nivel.</summary>
    public event Action AlCompletar;

    private float vida;
    private float oxigeno;
    private int dosis;
    private int puntaje;
    private float tiempo;
    private int amenazas;
    private bool terminada;
    private float tinte;

    public int Puntaje { get { return puntaje; } }
    public int Dosis { get { return dosis; } }
    public float TiempoRestante { get { return tiempo; } }

    /// <summary>Verdadero en cuanto la partida se gana o se pierde.</summary>
    public bool Terminada { get { return terminada; } }

    /// <summary>Frase que explica la derrota en el cuadro de fin de partida.</summary>
    public string MotivoDerrota { get; private set; } = "El piloto no resistio la infeccion.";

    void Start()
    {
        AjustesJuego.Cargar();

        vida = vidaMaxima;
        oxigeno = oxigenoMaximo;
        dosis = dosisIniciales;
        puntaje = 0;
        tiempo = duracionNivel;
        amenazas = amenazasIniciales;

        int nivel = PlayerPrefs.GetInt("nivel_elegido", 1);
        if (textoNivel != null)
            textoNivel.text = "NIVEL " + nivel + "  -  " +
                              AjustesJuego.NombresDificultad[AjustesJuego.Dificultad];

        Refrescar();

        if (aviso != null)
            aviso.Mostrar("Elimina los parasitos y alcanza la camara cardiaca");
    }

    void Update()
    {
        if (!terminada) Transcurrir();
        LeerTeclasDePrueba();
        DesvanecerTinte();
        Refrescar();
    }

    /// <summary>
    /// Consumo continuo: el oxigeno baja con el tiempo y el reloj del nivel
    /// avanza. Al agotarse cualquiera de los dos la partida termina.
    /// </summary>
    private void Transcurrir()
    {
        // Time.deltaTime vale cero mientras el cuadro de pausa esta abierto,
        // de modo que el HUD se detiene sin necesidad de comprobarlo aqui.
        oxigeno = Mathf.Max(0f, oxigeno - consumoOxigeno * Time.deltaTime);
        tiempo = Mathf.Max(0f, tiempo - Time.deltaTime);

        if (vida <= 0f) Perder("El piloto no resistio la infeccion.");
        else if (oxigeno <= 0f) Perder("El piloto se quedo sin oxigeno.");
        else if (tiempo <= 0f)
        {
            if (victoriaAlAgotarTiempo) Terminar(true);
            else Perder("Se agoto el tiempo del nivel.");
        }
    }

    /// <summary>
    /// Teclas de demostracion del prototipo. Cada una representa un suceso
    /// que en la version final llegara desde la logica de juego.
    /// </summary>
    private void LeerTeclasDePrueba()
    {
        if (terminada || Time.timeScale <= 0f) return;

        // B es un control real del juego: aplica una dosis guardada.
        if (Input.GetKeyDown(KeyCode.B)) UsarDosis();

        if (!teclasDePrueba) return;

        if (Input.GetKeyDown(KeyCode.X)) RecibirDano(12f);
        if (Input.GetKeyDown(KeyCode.C)) SumarPuntaje(150);
        if (Input.GetKeyDown(KeyCode.V)) RecogerDosis();
        if (Input.GetKeyDown(KeyCode.N)) RecuperarOxigeno(25f);
    }

    public void RecibirDano(float cantidad)
    {
        vida = Mathf.Max(0f, vida - cantidad);
        tinte = 1f;
        if (vida <= 0f) Perder("El piloto no resistio la infeccion.");
    }

    /// <summary>Lo llama el generador de oleadas con el total real del nivel.</summary>
    public void ConfigurarAmenazas(int total)
    {
        amenazas = Mathf.Max(0, total);
    }

    /// <summary>Muestra un aviso breve en la parte baja de la pantalla.</summary>
    public void Avisar(string texto)
    {
        if (aviso != null) aviso.Mostrar(texto);
    }

    public void RecuperarVida(float cantidad)
    {
        vida = Mathf.Min(vidaMaxima, vida + cantidad);
    }

    /// <summary>El jugador cruzo la salida con el conducto despejado.</summary>
    public void CompletarNivel()
    {
        Terminar(true);
    }

    public void SumarPuntaje(int puntos)
    {
        puntaje += puntos;
        amenazas = Mathf.Max(0, amenazas - 1);
        if (aviso != null) aviso.Mostrar("Parasito eliminado  +" + puntos);
    }

    public void RecogerDosis()
    {
        dosis++;
        if (aviso != null) aviso.Mostrar("Capsula de medicina recogida");
    }

    /// <summary>Gasta una dosis para recuperar vida.</summary>
    public void UsarDosis()
    {
        if (dosis <= 0)
        {
            if (aviso != null) aviso.Mostrar("No quedan dosis disponibles");
            return;
        }

        dosis--;
        vida = Mathf.Min(vidaMaxima, vida + curacionPorDosis);
        if (aviso != null)
            aviso.Mostrar("Dosis aplicada  +" + Mathf.RoundToInt(curacionPorDosis) + " de vida");
    }

    public void RecuperarOxigeno(float cantidad)
    {
        oxigeno = Mathf.Min(oxigenoMaximo, oxigeno + cantidad);
    }

    private void Perder(string motivo)
    {
        if (terminada) return;
        MotivoDerrota = motivo;
        Terminar(false);
    }

    private void Terminar(bool victoria)
    {
        if (terminada) return;
        terminada = true;

        if (victoria) { if (AlCompletar != null) AlCompletar(); }
        else { if (AlPerder != null) AlPerder(); }
    }

    /// <summary>Atenua el destello rojo que acompana a cada golpe.</summary>
    private void DesvanecerTinte()
    {
        if (tinteDano == null) return;

        tinte = Mathf.MoveTowards(tinte, 0f, Time.unscaledDeltaTime * 2f);
        Color c = tinteDano.color;
        tinteDano.color = new Color(c.r, c.g, c.b, tinte * 0.45f);
    }

    /// <summary>Traslada el estado numerico a los elementos visuales.</summary>
    private void Refrescar()
    {
        float pVida = vida / vidaMaxima;
        float pOxigeno = oxigeno / oxigenoMaximo;

        if (barraVida != null)
        {
            barraVida.fillAmount = pVida;
            // Por debajo de un tercio la barra pasa a ambar como aviso.
            barraVida.color = pVida < 0.34f
                ? new Color(1f, 0.68f, 0.25f) : Color.white;
        }

        if (barraOxigeno != null) barraOxigeno.fillAmount = pOxigeno;

        if (textoVida != null) textoVida.text = Mathf.CeilToInt(vida) + " / " +
                                                Mathf.RoundToInt(vidaMaxima);
        if (textoOxigeno != null) textoOxigeno.text = Mathf.CeilToInt(oxigeno) + " %";
        if (textoDosis != null) textoDosis.text = "x " + dosis;
        if (textoPuntaje != null) textoPuntaje.text = puntaje.ToString("00000");
        if (textoAmenazas != null) textoAmenazas.text = amenazas.ToString("00");

        if (textoTiempo != null)
        {
            int minutos = Mathf.FloorToInt(tiempo / 60f);
            int segundos = Mathf.FloorToInt(tiempo % 60f);
            textoTiempo.text = string.Format("{0:0}:{1:00}", minutos, segundos);
        }
    }
}
