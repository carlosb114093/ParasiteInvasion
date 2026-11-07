using UnityEngine;

/// <summary>
/// Ajustes que el jugador define en la pantalla de opciones.
///
/// Se guardan en PlayerPrefs para que sobrevivan al cierre del juego y se
/// aplican al cargar cualquier pantalla, de modo que el volumen o la calidad
/// elegidos siguen vigentes al pasar del menu a la partida.
/// </summary>
public static class AjustesJuego
{
    private const string ClaveGeneral = "vol_general";
    private const string ClaveMusica = "vol_musica";
    private const string ClaveEfectos = "vol_efectos";
    private const string ClaveCalidad = "calidad";
    private const string ClaveDificultad = "dificultad";
    private const string ClavePantallaCompleta = "pantalla_completa";
    private const string ClaveVibracion = "vibracion";

    public static float VolumenGeneral = 0.8f;
    public static float VolumenMusica = 0.7f;
    public static float VolumenEfectos = 0.9f;

    /// <summary>0 Baja, 1 Media, 2 Alta.</summary>
    public static int Calidad = 1;

    /// <summary>0 Facil, 1 Normal, 2 Dificil.</summary>
    public static int Dificultad = 1;

    public static bool PantallaCompleta = true;
    public static bool Vibracion = true;

    public static readonly string[] NombresCalidad = { "Baja", "Media", "Alta" };
    public static readonly string[] NombresDificultad = { "Facil", "Normal", "Dificil" };

    public static void Cargar()
    {
        VolumenGeneral = PlayerPrefs.GetFloat(ClaveGeneral, 0.8f);
        VolumenMusica = PlayerPrefs.GetFloat(ClaveMusica, 0.7f);
        VolumenEfectos = PlayerPrefs.GetFloat(ClaveEfectos, 0.9f);
        Calidad = PlayerPrefs.GetInt(ClaveCalidad, 1);
        Dificultad = PlayerPrefs.GetInt(ClaveDificultad, 1);
        PantallaCompleta = PlayerPrefs.GetInt(ClavePantallaCompleta, 1) == 1;
        Vibracion = PlayerPrefs.GetInt(ClaveVibracion, 1) == 1;
        Aplicar();
    }

    public static void Guardar()
    {
        PlayerPrefs.SetFloat(ClaveGeneral, VolumenGeneral);
        PlayerPrefs.SetFloat(ClaveMusica, VolumenMusica);
        PlayerPrefs.SetFloat(ClaveEfectos, VolumenEfectos);
        PlayerPrefs.SetInt(ClaveCalidad, Calidad);
        PlayerPrefs.SetInt(ClaveDificultad, Dificultad);
        PlayerPrefs.SetInt(ClavePantallaCompleta, PantallaCompleta ? 1 : 0);
        PlayerPrefs.SetInt(ClaveVibracion, Vibracion ? 1 : 0);
        PlayerPrefs.Save();
        Aplicar();
    }

    /// <summary>Devuelve todos los ajustes a sus valores de fabrica.</summary>
    public static void Restablecer()
    {
        VolumenGeneral = 0.8f;
        VolumenMusica = 0.7f;
        VolumenEfectos = 0.9f;
        Calidad = 1;
        Dificultad = 1;
        PantallaCompleta = true;
        Vibracion = true;
        Guardar();
    }

    /// <summary>Traslada los valores guardados al motor.</summary>
    public static void Aplicar()
    {
        AudioListener.volume = VolumenGeneral;

        // Los tres niveles de la interfaz se reparten sobre los niveles de
        // calidad que traiga el proyecto, sean tres o seis.
        int disponibles = QualitySettings.names.Length;
        if (disponibles > 0)
        {
            int nivel = Mathf.RoundToInt(Calidad * (disponibles - 1) / 2f);
            QualitySettings.SetQualityLevel(Mathf.Clamp(nivel, 0, disponibles - 1), true);
        }

        // En el editor la ventana no cambia de modo, solo en la aplicacion
        // compilada.
        if (!Application.isEditor)
            Screen.fullScreen = PantallaCompleta;
    }
}
