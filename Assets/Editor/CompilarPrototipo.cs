using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Genera la aplicacion de escritorio del prototipo.
///
/// La compilacion se lanza desde la linea de comandos para que la evidencia
/// pueda rehacerse siempre igual. Se fuerza ventana de 1920x1080 porque es la
/// resolucion con la que se disenaron las pantallas y con la que se toman las
/// capturas del informe.
/// </summary>
public static class CompilarPrototipo
{
    private const string Carpeta = "Build";
    private const string Ejecutable = "ParasiteInvasion.exe";

    [MenuItem("Parasite Invasion/Compilar prototipo (Windows)")]
    public static void Compilar()
    {
        PlayerSettings.productName = "Parasite Invasion";
        PlayerSettings.companyName = "SENA - Analisis y Desarrollo de Software";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        // Necesario para que el recorrido automatico de capturas siga
        // avanzando aunque la ventana pierda el foco.
        PlayerSettings.runInBackground = true;

        var escenas = new List<string>();
        foreach (var e in EditorBuildSettings.scenes)
            if (e.enabled) escenas.Add(e.path);

        if (escenas.Count == 0)
        {
            Debug.LogError("No hay escenas en Build Settings. Ejecute primero " +
                           "ConstructorInterfaz.ConstruirTodo.");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(Carpeta);

        var opciones = new BuildPlayerOptions
        {
            scenes = escenas.ToArray(),
            locationPathName = Path.Combine(Carpeta, Ejecutable),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport informe = BuildPipeline.BuildPlayer(opciones);

        if (informe.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Compilacion correcta: " + opciones.locationPathName +
                      "  (" + informe.summary.totalSize / (1024 * 1024) + " MB)");
        }
        else
        {
            Debug.LogError("La compilacion fallo: " + informe.summary.result);
            EditorApplication.Exit(1);
        }
    }
}
