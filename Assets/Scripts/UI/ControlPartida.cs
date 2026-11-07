using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordina los cuadros emergentes de la pantalla de juego.
///
/// Escucha al HUD para saber cuando la partida termina y decide que cuadro
/// mostrar: pausa, confirmacion de abandono, fin de partida o nivel
/// completado. Solo puede haber uno visible a la vez, por lo que abrir uno
/// cierra el anterior.
/// </summary>
public class ControlPartida : MonoBehaviour
{
    [Header("Origen del estado")]
    public HUDJuego hud;

    [Header("Menu de pausa")]
    public PanelEmergente cuadroPausa;
    public Button botonPausa;
    public Button botonReanudar;
    public Button botonReiniciarPausa;
    public Button botonOpcionesPausa;
    public Button botonAbandonar;

    [Header("Confirmar abandono")]
    public PanelEmergente cuadroAbandono;
    public Button botonAbandonoSi;
    public Button botonAbandonoNo;

    [Header("Fin de partida")]
    public PanelEmergente cuadroDerrota;
    public Text resumenDerrota;
    public Button botonReintentar;
    public Button botonSalirDerrota;

    [Header("Nivel completado")]
    public PanelEmergente cuadroVictoria;
    public Text resumenVictoria;
    public Button botonSiguiente;
    public Button botonSalirVictoria;

    void Start()
    {
        botonPausa.onClick.AddListener(AbrirPausa);
        botonReanudar.onClick.AddListener(cuadroPausa.Cerrar);
        botonReiniciarPausa.onClick.AddListener(Navegador.Reiniciar);
        botonOpcionesPausa.onClick.AddListener(() => Navegador.Ir(Navegador.Opciones));
        botonAbandonar.onClick.AddListener(AbrirAbandono);

        botonAbandonoSi.onClick.AddListener(() => Navegador.Ir(Navegador.MenuPrincipal));
        botonAbandonoNo.onClick.AddListener(VolverAPausa);

        botonReintentar.onClick.AddListener(Navegador.Reiniciar);
        botonSalirDerrota.onClick.AddListener(() => Navegador.Ir(Navegador.MenuPrincipal));

        botonSiguiente.onClick.AddListener(Navegador.Reiniciar);
        botonSalirVictoria.onClick.AddListener(() => Navegador.Ir(Navegador.MenuPrincipal));

        if (hud != null)
        {
            hud.AlPerder += MostrarDerrota;
            hud.AlCompletar += MostrarVictoria;
        }
    }

    void Update()
    {
        // La pausa se alterna con Escape o con P, pero solo mientras no haya
        // otro cuadro ocupando la pantalla. El cuadro de pausa no atiende
        // Escape por su cuenta: si lo hiciera, la misma pulsacion podria
        // cerrarlo aqui y reabrirlo alli dentro del mismo fotograma.
        bool otroCuadroAbierto = cuadroAbandono.Abierto ||
                                 cuadroDerrota.Abierto ||
                                 cuadroVictoria.Abierto;

        if (otroCuadroAbierto) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            cuadroPausa.Alternar();
    }

    private void AbrirPausa()
    {
        cuadroPausa.Abrir();
    }

    /// <summary>
    /// El abandono se confirma sobre la pausa: se oculta el menu para que los
    /// dos cuadros no se superpongan.
    /// </summary>
    private void AbrirAbandono()
    {
        cuadroPausa.Cerrar();
        cuadroAbandono.Abrir();
    }

    private void VolverAPausa()
    {
        cuadroAbandono.Cerrar();
        cuadroPausa.Abrir();
    }

    private void MostrarDerrota()
    {
        cuadroPausa.Cerrar();
        cuadroAbandono.Cerrar();

        if (resumenDerrota != null)
            resumenDerrota.text = hud.MotivoDerrota + "\n\n" +
                                  "Puntaje alcanzado: " + hud.Puntaje + "\n" +
                                  "Dosis sin usar: " + hud.Dosis;

        cuadroDerrota.Abrir();
    }

    private void MostrarVictoria()
    {
        cuadroPausa.Cerrar();
        cuadroAbandono.Cerrar();

        if (resumenVictoria != null)
            resumenVictoria.text = "Conducto despejado.\n\n" +
                                   "Puntaje alcanzado: " + hud.Puntaje + "\n" +
                                   "Tiempo sobrante: " + Mathf.FloorToInt(hud.TiempoRestante) + " s\n" +
                                   "Dosis sin usar: " + hud.Dosis;

        cuadroVictoria.Abrir();
    }
}
