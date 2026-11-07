using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Graba en MP4 (con audio) la ventana Game de las escenas de evidencia
/// AA3 y AA4 usando Unity Recorder.
///
/// El menu abre la escena, entra en Play y graba durante un tiempo fijo; al
/// terminar sale de Play solo. Como entrar en Play recarga los scripts, la
/// orden pendiente se guarda en SessionState y se retoma al iniciar el juego.
/// Los videos quedan en la carpeta Video del proyecto.
/// </summary>
[InitializeOnLoad]
public static class GrabarVideosEvidencias
{
    private const string Menu = "Parasite Invasion/Evidencias AA3-AA4/";
    private const string ClavePendiente = "GrabarVideosEvidencias.Pendiente";

    private static RecorderController controlador;
    private static float fin;

    static GrabarVideosEvidencias()
    {
        EditorApplication.playModeStateChanged += AlCambiarModo;
    }

    [MenuItem(Menu + "4. Grabar video AA3 (80 s)", false, 60)]
    private static void GrabarAA3()
    {
        Preparar("Assets/Escenas/06_Iluminacion_AA3.unity", "AA3_Iluminacion_ParasiteInvasion", 80f);
    }

    [MenuItem(Menu + "5. Grabar video AA4 (40 s)", false, 61)]
    private static void GrabarAA4()
    {
        Preparar("Assets/Escenas/07_Particulas_AA4.unity", "AA4_Particulas_Sonido_ParasiteInvasion", 40f);
    }

    private static void Preparar(string escena, string archivo, float segundos)
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.OpenScene(escena, OpenSceneMode.Single);
        SessionState.SetString(ClavePendiente, archivo + "|" + segundos.ToString(System.Globalization.CultureInfo.InvariantCulture));
        EditorApplication.isPlaying = true;
    }

    private static void AlCambiarModo(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.EnteredPlayMode) return;

        string pendiente = SessionState.GetString(ClavePendiente, "");
        if (string.IsNullOrEmpty(pendiente)) return;
        SessionState.EraseString(ClavePendiente);

        var partes = pendiente.Split('|');
        float segundos = float.Parse(partes[1], System.Globalization.CultureInfo.InvariantCulture);
        Iniciar(partes[0], segundos);
    }

    private static void Iniciar(string archivo, float segundos)
    {
        string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Video"));
        Directory.CreateDirectory(carpeta);

        var video = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        video.name = archivo;
        video.Enabled = true;
        video.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
        };
        video.CaptureAlpha = false;
        video.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080,
        };
        video.AudioInputSettings.PreserveAudio = true;
        video.OutputFile = Path.Combine(carpeta, archivo);

        var ajustes = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        ajustes.AddRecorderSettings(video);
        ajustes.SetRecordModeToManual();
        ajustes.FrameRate = 30f;
        ajustes.FrameRatePlayback = FrameRatePlayback.Constant;
        ajustes.CapFrameRate = true;

        controlador = new RecorderController(ajustes);
        controlador.PrepareRecording();
        controlador.StartRecording();

        fin = Time.time + segundos;
        EditorApplication.update -= Vigilar;
        EditorApplication.update += Vigilar;
        Debug.Log("[Grabacion] Grabando " + archivo + " durante " + segundos + " s");
    }

    private static void Vigilar()
    {
        if (!EditorApplication.isPlaying)
        {
            Terminar();
            return;
        }
        if (Time.time >= fin) Terminar();
    }

    private static void Terminar()
    {
        EditorApplication.update -= Vigilar;
        if (controlador != null && controlador.IsRecording())
        {
            controlador.StopRecording();
            Debug.Log("[Grabacion] Video guardado en la carpeta Video del proyecto.");
        }
        controlador = null;
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }
}
