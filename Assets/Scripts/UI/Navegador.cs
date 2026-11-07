using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Punto unico de navegacion entre las pantallas del prototipo.
///
/// Todos los botones que cambian de pantalla pasan por aqui, de modo que el
/// vinculo entre pantallas queda descrito en un solo lugar y no repartido por
/// cada escena. Ademas recuerda la pantalla de origen para que "Volver"
/// devuelva al lugar correcto cuando una misma pantalla se abre desde sitios
/// distintos (por ejemplo Opciones desde el menu o desde la pausa).
/// </summary>
public static class Navegador
{
    public const string MenuPrincipal = "01_MenuPrincipal";
    public const string Opciones = "02_Opciones";
    public const string ComoJugar = "03_ComoJugar";
    public const string Creditos = "04_Creditos";
    public const string Juego = "05_Juego";

    /// <summary>Pantalla desde la que se abrio la pantalla actual.</summary>
    public static string PantallaAnterior { get; private set; } = MenuPrincipal;

    /// <summary>
    /// Carga una pantalla anotando cual queda como origen para el retorno.
    /// </summary>
    public static void Ir(string pantalla)
    {
        // El tiempo puede venir congelado por el cuadro de pausa: se restaura
        // siempre antes de cambiar de escena.
        Time.timeScale = 1f;
        PantallaAnterior = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(pantalla);
    }

    /// <summary>Regresa a la pantalla que abrio la actual.</summary>
    public static void Volver()
    {
        Ir(PantallaAnterior);
    }

    /// <summary>Recarga la pantalla en curso; usado por "Reintentar".</summary>
    public static void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Cierra la aplicacion. Dentro del editor no existe proceso que cerrar,
    /// asi que se detiene la reproduccion para poder probar el boton.
    /// </summary>
    public static void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
