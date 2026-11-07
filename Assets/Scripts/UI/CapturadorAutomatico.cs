using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Recorre por si solo las pantallas del prototipo y guarda una captura de
/// cada una.
///
/// Sirve para documentar la interfaz sin depender de que alguien pulse los
/// botones en el orden correcto: la aplicacion compilada se ejecuta con el
/// argumento -capturas seguido de una carpeta y deja alli las imagenes
/// numeradas. Sin ese argumento el componente se elimina y el juego se
/// comporta con normalidad.
/// </summary>
public class CapturadorAutomatico : MonoBehaviour
{
    private const string Argumento = "-capturas";

    private string carpeta;
    private int indice;

    void Awake()
    {
        carpeta = LeerCarpeta();

        if (string.IsNullOrEmpty(carpeta))
        {
            Destroy(gameObject);
            return;
        }

        Directory.CreateDirectory(carpeta);
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Recorrido());
    }

    private static string LeerCarpeta()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == Argumento) return args[i + 1];
        return null;
    }

    private IEnumerator Recorrido()
    {
        yield return Esperar(1.5f);

        // ---- Pantalla principal ----
        var menu = Object.FindFirstObjectByType<MenuPrincipalUI>();
        yield return Capturar("MenuPrincipal");

        if (menu != null)
        {
            menu.cuadroNivel.Abrir();
            yield return Esperar(0.8f);
            yield return Capturar("PopUp_SeleccionNivel");
            menu.cuadroNivel.Cerrar();
            yield return Esperar(0.5f);

            menu.cuadroSalir.Abrir();
            yield return Esperar(0.8f);
            yield return Capturar("PopUp_ConfirmarSalida");
            menu.cuadroSalir.Cerrar();
            yield return Esperar(0.4f);
        }

        // ---- Opciones ----
        yield return Cargar(Navegador.Opciones);
        var opciones = Object.FindFirstObjectByType<OpcionesUI>();
        yield return Capturar("Opciones");

        if (opciones != null)
        {
            opciones.listaCalidad.Show();
            yield return Esperar(0.6f);
            yield return Capturar("Opciones_ListaDesplegable");
            opciones.listaCalidad.Hide();
            yield return Esperar(0.4f);

            opciones.cuadroRestablecer.Abrir();
            yield return Esperar(0.8f);
            yield return Capturar("PopUp_Restablecer");
            opciones.cuadroRestablecer.Cerrar();
            yield return Esperar(0.5f);

            opciones.aviso.Mostrar("Ajustes guardados correctamente");
            yield return Esperar(0.6f);
            yield return Capturar("Aviso_AjustesGuardados");
        }

        // ---- Como jugar y creditos ----
        yield return Cargar(Navegador.ComoJugar);
        yield return Capturar("ComoJugar");

        yield return Cargar(Navegador.Creditos);
        yield return Capturar("Creditos");

        // ---- Pantalla de juego ----
        yield return Cargar(Navegador.Juego);
        yield return Esperar(1f);

        var hud = Object.FindFirstObjectByType<HUDJuego>();
        var partida = Object.FindFirstObjectByType<ControlPartida>();

        if (hud != null)
        {
            hud.SumarPuntaje(1450);
            hud.RecibirDano(28f);
            yield return Esperar(1.2f);
        }

        yield return Capturar("HUD_Juego");

        if (hud != null)
        {
            hud.RecogerDosis();
            yield return Esperar(0.5f);
            yield return Capturar("HUD_AvisoRecogida");
        }

        if (partida != null)
        {
            partida.cuadroPausa.Abrir();
            yield return Esperar(0.8f);
            yield return Capturar("PopUp_Pausa");

            partida.cuadroPausa.Cerrar();
            partida.cuadroAbandono.Abrir();
            yield return Esperar(0.8f);
            yield return Capturar("PopUp_Abandonar");
            partida.cuadroAbandono.Cerrar();
            yield return Esperar(0.4f);

            partida.cuadroVictoria.Abrir();
            yield return Esperar(0.8f);
            yield return Capturar("PopUp_NivelCompletado");
            partida.cuadroVictoria.Cerrar();
            yield return Esperar(0.4f);
        }

        // La derrota se provoca agotando la vida: asi el cuadro aparece por el
        // mismo camino que en una partida real.
        if (hud != null)
        {
            hud.RecibirDano(1000f);
            yield return Esperar(1f);
            yield return Capturar("PopUp_FinPartida");
        }

        Debug.Log("Capturas generadas en " + carpeta);
        Application.Quit();
    }

    private IEnumerator Cargar(string pantalla)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(pantalla);
        yield return null;
        yield return Esperar(1.2f);
    }

    /// <summary>Espera en tiempo real: los cuadros de pausa congelan el reloj.</summary>
    private static IEnumerator Esperar(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
    }

    private IEnumerator Capturar(string nombre)
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        indice++;
        string archivo = Path.Combine(carpeta,
            string.Format("{0:00}_{1}.png", indice, nombre));
        File.WriteAllBytes(archivo, tex.EncodeToPNG());
        Object.Destroy(tex);

        Debug.Log("Captura " + archivo);
    }
}
