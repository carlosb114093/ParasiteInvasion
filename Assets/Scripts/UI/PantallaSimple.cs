using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de solo lectura, como "Como jugar" o "Creditos".
///
/// Su unica interaccion es el retorno, de modo que basta con enlazar el boton
/// Volver y atender la tecla Escape. Se devuelve a la pantalla que la abrio,
/// no siempre al menu, porque estas pantallas podrian consultarse tambien
/// desde la pausa.
/// </summary>
public class PantallaSimple : MonoBehaviour
{
    public Button botonVolver;

    void Start()
    {
        AjustesJuego.Cargar();

        if (botonVolver != null)
            botonVolver.onClick.AddListener(Navegador.Volver);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Navegador.Volver();
    }
}
