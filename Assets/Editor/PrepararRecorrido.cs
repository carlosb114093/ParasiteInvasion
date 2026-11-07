using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Deja la escena del menu lista para grabar el video de la evidencia
/// GA2-220501087-AA2-EV01.
///
/// Abre 01_MenuPrincipal, coloca en ella el objeto que gobierna el recorrido
/// automatico y guarda. Se hace desde el menu y no a mano para que la escena
/// quede siempre igual y para no dejar el objeto olvidado en otra escena.
/// </summary>
public static class PrepararRecorrido
{
    private const string EscenaMenu = "Assets/Escenas/01_MenuPrincipal.unity";
    private const string NombreObjeto = "RecorridoDemo";

    [MenuItem("Parasite Invasion/Preparar recorrido demo")]
    public static void Preparar()
    {
        // Si hay cambios sin guardar en la escena abierta, se pregunta antes
        // de cambiar de escena para no perder trabajo.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var escena = EditorSceneManager.OpenScene(EscenaMenu, OpenSceneMode.Single);

        if (!escena.IsValid())
        {
            Debug.LogError("No se encontro la escena " + EscenaMenu);
            return;
        }

        var existente = Object.FindFirstObjectByType<RecorridoDemo>();

        if (existente != null)
        {
            Selection.activeGameObject = existente.gameObject;
            EditorGUIUtility.PingObject(existente.gameObject);
            Debug.Log("El objeto " + NombreObjeto + " ya estaba en la escena. " +
                      "Da Play y empieza a grabar.");
            return;
        }

        var objeto = new GameObject(NombreObjeto);
        objeto.AddComponent<RecorridoDemo>();

        Undo.RegisterCreatedObjectUndo(objeto, "Crear " + NombreObjeto);
        Selection.activeGameObject = objeto;
        EditorGUIUtility.PingObject(objeto);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("Listo: " + NombreObjeto + " agregado a 01_MenuPrincipal. " +
                  "Pon la ventana Game en 1920x1080 con Maximize On Play, " +
                  "da Play y graba. F10 aborta el recorrido.");
    }

    /// <summary>
    /// Quita el objeto de la escena cuando ya no haga falta, para que no se
    /// cuele en la version compilada que se entrega.
    /// </summary>
    [MenuItem("Parasite Invasion/Quitar recorrido demo")]
    public static void Quitar()
    {
        var existente = Object.FindFirstObjectByType<RecorridoDemo>();

        if (existente == null)
        {
            Debug.Log("No hay ningun " + NombreObjeto + " en la escena abierta.");
            return;
        }

        Undo.DestroyObjectImmediate(existente.gameObject);

        var escena = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log(NombreObjeto + " eliminado de " + escena.name + ".");
    }
}
