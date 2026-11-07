using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Prepara las escenas y los pantallazos de las evidencias
/// GA2-220501087-AA3-EV01 (iluminacion y atmosfera) y
/// GA2-220501087-AA4-EV01 (sistema de particulas y sonido).
///
/// Las dos escenas parten de 05_Juego para que la evidencia muestre el nivel
/// real del juego (la Vena Principal) y no una escena de prueba aparte:
///   06_Iluminacion_AA3  = 05_Juego + montaje de luces
///   07_Particulas_AA4   = 06_Iluminacion_AA3 + lluvia de medicina con sonido
/// </summary>
public static class EvidenciasAA3AA4
{
    private const string Menu = "Parasite Invasion/Evidencias AA3-AA4/";

    private const string EscenaJuego = "Assets/Escenas/05_Juego.unity";
    private const string EscenaAA3 = "Assets/Escenas/06_Iluminacion_AA3.unity";
    private const string EscenaAA4 = "Assets/Escenas/07_Particulas_AA4.unity";

    private const string SonidoAssetStore = "Assets/PowerUp/Sounds/power-up.wav";
    private const string MaterialParticula = "Assets/PowerUp/Materials/spark-1.mat";
    private const string MaterialVena = "Assets/Materiales/Vena_Iluminacion.mat";

    // Colores de la paleta del juego.
    private static readonly Color RojoCorporal = new Color(1f, 0.72f, 0.66f);
    private static readonly Color BlancoFaro = new Color(1f, 0.95f, 0.86f);
    private static readonly Color CianMotor = new Color(0.45f, 0.88f, 1f);
    private static readonly Color MagentaBio = new Color(1f, 0.32f, 0.62f);
    private static readonly Color VerdeToxico = new Color(0.5f, 1f, 0.28f);

    // =====================================================================
    // 1. Escena AA3: iluminacion
    // =====================================================================

    [MenuItem(Menu + "1. Construir escena AA3 (iluminacion)", false, 1)]
    public static void ConstruirAA3()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var escena = CopiarYAbrir(EscenaJuego, EscenaAA3);
        if (!escena.IsValid()) return;

        AjustarAmbiente();
        MarcarEstaticos();
        AlargarPartida();

        var jugador = GameObject.Find("Jugador");
        var raiz = new GameObject("Iluminacion");

        // --- Luz direccional: la "luz del cuerpo" que llena la vena ---
        var direccional = GameObject.Find("Directional Light");
        if (direccional == null)
            direccional = new GameObject("Directional Light", typeof(Light));

        direccional.name = "Luz Direccional - Ambiente corporal";
        direccional.transform.SetParent(raiz.transform, true);
        direccional.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        var sol = direccional.GetComponent<Light>();
        sol.type = LightType.Directional;
        sol.color = RojoCorporal;
        sol.intensity = 0.35f;
        sol.bounceIntensity = 1.2f;
        ConfigurarSombras(sol, LightShadows.Soft, 0.75f);
        sol.renderMode = LightRenderMode.Auto;
        sol.lightmapBakeType = LightmapBakeType.Realtime;
        RenderSettings.sun = sol;

        // --- Luces de la nave: viajan con el jugador ---
        Transform padreNave = jugador != null ? jugador.transform : raiz.transform;

        var faro = CrearLuz("Luz Focal - Faro de la Nave A1", LightType.Spot,
            padreNave, new Vector3(0.6f, 0.15f, 0f), Quaternion.Euler(8f, 90f, 0f));
        faro.color = BlancoFaro;
        faro.intensity = 5f;
        faro.range = 12f;
        faro.spotAngle = 55f;
        faro.innerSpotAngle = 28f;
        faro.bounceIntensity = 1f;
        faro.renderMode = LightRenderMode.ForcePixel;
        ConfigurarSombras(faro, LightShadows.Soft, 0.85f);

        var motor = CrearLuz("Luz Puntual - Motor de la Nave A1", LightType.Point,
            padreNave, new Vector3(-0.7f, 0f, 0f), Quaternion.identity);
        motor.color = CianMotor;
        motor.intensity = 3f;
        motor.range = 3.5f;
        motor.bounceIntensity = 1f;
        motor.renderMode = LightRenderMode.ForcePixel;
        ConfigurarSombras(motor, LightShadows.Hard, 0.6f);

        // --- Bioluminiscencia de las paredes de la vena ---
        var posicionesBio = new[]
        {
            new Vector3(-5.5f, 1.25f, -1.1f),
            new Vector3(1.5f, -1.25f, 1.15f),
            new Vector3(7.5f, 1.2f, 1.0f),
        };
        for (int i = 0; i < posicionesBio.Length; i++)
        {
            var bio = CrearLuz("Luz Puntual - Bioluminiscencia " + (i + 1), LightType.Point,
                raiz.transform, posicionesBio[i], Quaternion.identity);
            bio.color = MagentaBio;
            bio.intensity = 3f;
            bio.range = 5f;
            bio.bounceIntensity = 1f;
            bio.renderMode = LightRenderMode.Auto;
            ConfigurarSombras(bio, LightShadows.None, 0f);

            var pulso = bio.gameObject.AddComponent<PulsoLuz>();
            pulso.amplitud = 0.3f;
            pulso.frecuencia = 0.7f;
            pulso.desfase = i * 0.45f;
        }

        // --- Resplandor toxico del parasito ---
        var tentaculo = GameObject.Find("TentaculoParasitario");
        Vector3 posTentaculo = tentaculo != null
            ? tentaculo.transform.position + Vector3.up * 0.6f
            : new Vector3(11f, -0.3f, 0.9f);

        var toxica = CrearLuz("Luz Puntual - Resplandor toxico del Parasito", LightType.Point,
            raiz.transform, posTentaculo, Quaternion.identity);
        toxica.color = VerdeToxico;
        toxica.intensity = 3.5f;
        toxica.range = 4f;
        toxica.bounceIntensity = 1.5f;
        toxica.renderMode = LightRenderMode.ForcePixel;
        ConfigurarSombras(toxica, LightShadows.Hard, 0.8f);

        Guardar(escena);
        Selection.activeGameObject = raiz;
        EditorGUIUtility.PingObject(raiz);
        Debug.Log("[AA3] Escena lista: " + EscenaAA3);
    }

    private static Light CrearLuz(string nombre, LightType tipo, Transform padre,
                                  Vector3 posicion, Quaternion rotacion)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        // Las luces que cuelgan del jugador se colocan en coordenadas locales;
        // las demas, en coordenadas del mundo.
        if (padre.name == "Iluminacion")
        {
            go.transform.position = posicion;
            go.transform.rotation = rotacion;
        }
        else
        {
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
        }

        var luz = go.AddComponent<Light>();
        luz.type = tipo;
        luz.lightmapBakeType = LightmapBakeType.Realtime;
        return luz;
    }

    private static void ConfigurarSombras(Light luz, LightShadows tipo, float fuerza)
    {
        luz.shadows = tipo;
        if (tipo == LightShadows.None) return;

        luz.shadowStrength = fuerza;
        luz.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
        luz.shadowBias = 0.05f;
        luz.shadowNormalBias = 0.4f;
        luz.shadowNearPlane = 0.2f;
    }

    /// <summary>
    /// Baja la luz ambiental para que cada fuente se note. Se conservan los
    /// tonos rojizos y la niebla que ya tenia el nivel.
    /// </summary>
    private static void AjustarAmbiente()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.26f, 0.08f, 0.10f);
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.04f, 0.06f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.01f, 0.02f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.30f, 0.05f, 0.08f);
        RenderSettings.fogDensity = 0.02f;
    }

    /// <summary>
    /// El conducto de la vena no se mueve: se marca como estatico para que
    /// aporte y reciba la luz rebotada (iluminacion global).
    /// </summary>
    private static void MarcarEstaticos()
    {
        var vena = GameObject.Find("ConductoVena");
        if (vena == null) return;

        var banderas = StaticEditorFlags.ContributeGI |
                       StaticEditorFlags.OccludeeStatic |
                       StaticEditorFlags.ReflectionProbeStatic;

        foreach (var t in vena.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);

        // El material importado del FBX trae emision y se ve plano: la pared
        // no reacciona a las luces. Se le da un material Standard sin
        // emision y con brillo humedo para que cada fuente se note.
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialVena);
        if (material != null) AssetDatabase.DeleteAsset(MaterialVena);
        material = null;
        if (material == null)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Materiales"));
            material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.42f, 0.06f, 0.09f);
            material.SetFloat("_Glossiness", 0.72f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, MaterialVena);
        }

        foreach (var r in vena.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
    }

    /// <summary>
    /// Con el consumo normal de oxigeno la partida se pierde al minuto. En
    /// las escenas de evidencia se reduce para poder grabar el video completo
    /// sin que salte la pantalla de derrota.
    /// </summary>
    private static void AlargarPartida()
    {
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(mb);
            var prop = so.FindProperty("consumoOxigeno");
            if (prop == null || prop.propertyType != SerializedPropertyType.Float)
                continue;

            prop.floatValue = 0.4f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // =====================================================================
    // 2. Escena AA4: particulas y sonido
    // =====================================================================

    [MenuItem(Menu + "2. Construir escena AA4 (particulas y sonido)", false, 2)]
    public static void ConstruirAA4()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaAA3) == null)
        {
            ConstruirAA3();
        }

        var escena = CopiarYAbrir(EscenaAA3, EscenaAA4);
        if (!escena.IsValid()) return;

        var capsula = GameObject.Find("CapsulaMedicina");
        Vector3 origen = capsula != null
            ? capsula.transform.position + Vector3.up * 0.35f
            : new Vector3(-4f, -0.85f, 1.3f);

        var go = new GameObject("Particulas - Lluvia de Medicina");
        go.transform.position = origen;
        // El cono emite por su eje Z local: se gira para que dispare hacia
        // arriba y la gravedad haga caer las gotas.
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var sistema = go.AddComponent<ParticleSystem>();
        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // --- Modulo principal: tiempos, tamano, color base y gravedad ---
        var principal = sistema.main;
        principal.duration = 4f;                      // tiempo de vida del sistema (un ciclo)
        principal.loop = true;
        principal.startDelay = 1.5f;                  // tiempo para activarse
        principal.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        principal.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.4f);  // tamanos diferentes
        principal.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        principal.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.86f, 0.25f), new Color(0.35f, 0.95f, 1f));
        principal.gravityModifier = 0.9f;             // caida
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.scalingMode = ParticleSystemScalingMode.Local;
        principal.maxParticles = 500;
        principal.playOnAwake = true;

        // --- Emision: curva del numero de particulas ---
        var emision = sistema.emission;
        var curvaEmision = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.3f, 1f),
            new Keyframe(0.7f, 0.6f),
            new Keyframe(1f, 0f));
        emision.rateOverTime = new ParticleSystem.MinMaxCurve(130f, curvaEmision);
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

        // --- Forma: cono estrecho que sale de la capsula ---
        var forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 22f;
        forma.radius = 0.12f;

        // --- Curva de velocidad: arrancan rapido y se frenan ---
        var velocidad = sistema.velocityOverLifetime;
        velocidad.enabled = true;
        velocidad.speedModifier = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.35f, 0.55f), new Keyframe(1f, 0.25f)));

        // --- Gradiente de color: dorado -> cian -> rosado que se desvanece ---
        var color = sistema.colorOverLifetime;
        color.enabled = true;
        var gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.35f), 0f),
                new GradientColorKey(new Color(0.4f, 0.95f, 1f), 0.45f),
                new GradientColorKey(new Color(1f, 0.45f, 0.7f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.85f, 0.7f),
                new GradientAlphaKey(0f, 1f),
            });
        color.color = new ParticleSystem.MinMaxGradient(gradiente);

        // --- Tamano durante la vida: crecen al salir y se encogen al caer ---
        var tamano = sistema.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.15f)));

        var giro = sistema.rotationOverLifetime;
        giro.enabled = true;
        giro.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        // --- Render ---
        var render = go.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Billboard;
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialParticula);
        if (material == null)
            material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        render.sharedMaterial = material;

        // --- Sonido del Asset Store asociado al efecto ---
        var fuente = go.AddComponent<AudioSource>();
        fuente.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SonidoAssetStore);
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.volume = 0.75f;
        fuente.pitch = 1.1f;
        fuente.priority = 100;
        fuente.spatialBlend = 0.65f;
        fuente.dopplerLevel = 0f;
        fuente.rolloffMode = AudioRolloffMode.Logarithmic;
        fuente.minDistance = 2f;
        fuente.maxDistance = 30f;

        go.AddComponent<SonidoSistemaParticulas>();

        if (fuente.clip == null)
            Debug.LogWarning("[AA4] No se encontro " + SonidoAssetStore);

        Guardar(escena);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[AA4] Escena lista: " + EscenaAA4);
    }

    // =====================================================================
    // 3. Pantallazos
    // =====================================================================

    [MenuItem(Menu + "3. Capturas automaticas AA3 (una por luz)", false, 20)]
    public static void CapturasAA3()
    {
        if (EditorSceneManager.GetActiveScene().path != EscenaAA3)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(EscenaAA3, OpenSceneMode.Single);
        }

        var tomas = new List<Toma>();
        tomas.Add(new Toma("AA3_01_Escena_general", "Iluminacion", 22f));
        foreach (var luz in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            float tam = luz.type == LightType.Directional ? 22f : Mathf.Max(3f, luz.range * 1.4f);
            tomas.Add(new Toma(null, luz.gameObject.name, tam));
        }

        // Orden estable: por tipo de luz y luego por nombre.
        tomas.Sort(1, tomas.Count - 1, new ComparadorTomas());
        for (int i = 1; i < tomas.Count; i++)
            tomas[i].archivo = string.Format("AA3_{0:00}_{1}", i + 1, Limpiar(tomas[i].objeto));

        Secuencia.Iniciar(tomas, "AA3");
    }

    [MenuItem(Menu + "Capturar pantallazo AA3 %#F9", false, 40)]
    public static void CapturaManualAA3() { CapturaManual("AA3"); }

    [MenuItem(Menu + "Capturar pantallazo AA4 %#F10", false, 41)]
    public static void CapturaManualAA4() { CapturaManual("AA4"); }

    private static void CapturaManual(string evidencia)
    {
        string carpeta = CarpetaCapturas(evidencia);
        Directory.CreateDirectory(carpeta);
        int n = Directory.GetFiles(carpeta, evidencia + "_manual_*.png").Length + 1;
        string ruta = Path.Combine(carpeta, string.Format("{0}_manual_{1:00}.png", evidencia, n));

        // Se deja pasar un repintado para que el menu desaparezca de la imagen.
        InternalEditorUtility.RepaintAllViews();
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => Capturar(ruta);
    }

    private class Toma
    {
        public string archivo;
        public string objeto;
        public float tamano;
        public Toma(string archivo, string objeto, float tamano)
        {
            this.archivo = archivo; this.objeto = objeto; this.tamano = tamano;
        }
    }

    private class ComparadorTomas : IComparer<Toma>
    {
        static int Orden(string n)
        {
            if (n.StartsWith("Luz Direccional")) return 0;
            if (n.StartsWith("Luz Focal")) return 1;
            return 2;
        }
        public int Compare(Toma a, Toma b)
        {
            int c = Orden(a.objeto).CompareTo(Orden(b.objeto));
            return c != 0 ? c : string.CompareOrdinal(a.objeto, b.objeto);
        }
    }

    /// <summary>
    /// Recorre las tomas sin bloquear el editor: selecciona el objeto, lo
    /// encuadra en la vista Scene, espera a que se repinten Scene, Game,
    /// Hierarchy e Inspector y guarda el pantallazo.
    /// </summary>
    private static class Secuencia
    {
        static List<Toma> tomas;
        static string evidencia;
        static int indice;
        static double siguiente;
        static bool esperandoCaptura;

        public static void Iniciar(List<Toma> lista, string ev)
        {
            tomas = lista; evidencia = ev; indice = 0; esperandoCaptura = false;
            siguiente = EditorApplication.timeSinceStartup + 0.5;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < siguiente) return;

            if (indice >= tomas.Count)
            {
                EditorApplication.update -= Tick;
                Debug.Log("[" + evidencia + "] Capturas terminadas en " + CarpetaCapturas(evidencia));
                return;
            }

            var toma = tomas[indice];

            if (!esperandoCaptura)
            {
                var go = GameObject.Find(toma.objeto);
                if (go == null)
                {
                    Debug.LogWarning("No se encontro " + toma.objeto);
                    indice++;
                    return;
                }

                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);

                // La vista Scene se coloca dentro del conducto, mirando hacia
                // donde avanza el juego (+X), para que se vea el efecto de
                // la luz sobre las paredes y no el exterior del tubo.
                var luz = go.GetComponent<Light>();
                bool general = luz == null || luz.type == LightType.Directional;
                Vector3 centro = general ? new Vector3(0.5f, 0f, 0f) : go.transform.position;
                float tam = general ? 3.2f : 1.6f;
                foreach (SceneView vista in SceneView.sceneViews)
                {
                    vista.showGrid = false;
                    vista.LookAt(centro, Quaternion.Euler(10f, 90f, 0f), tam, false, true);
                    vista.Repaint();
                }

                InternalEditorUtility.RepaintAllViews();
                esperandoCaptura = true;
                siguiente = EditorApplication.timeSinceStartup + 1.8;
                return;
            }

            Capturar(Path.Combine(CarpetaCapturas(evidencia), toma.archivo + ".png"));
            esperandoCaptura = false;
            indice++;
            siguiente = EditorApplication.timeSinceStartup + 0.4;
        }
    }

    private static string CarpetaCapturas(string evidencia)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas", evidencia));
    }

    private static string Limpiar(string nombre)
    {
        var s = nombre.Replace(" - ", "_").Replace(' ', '_');
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c.ToString(), "");
        return s;
    }

    private static void Capturar(string ruta)
    {
        string script = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
            "Herramientas", "capturar_editor.ps1"));

        var info = new ProcessStartInfo("powershell.exe",
            "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Ruta \"" + ruta + "\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using (var p = Process.Start(info))
        {
            string salida = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();
            p.WaitForExit(20000);
            if (!string.IsNullOrEmpty(error)) Debug.LogError("[Captura] " + error);
            else Debug.Log("[Captura] " + salida.Trim());
        }
    }

    // =====================================================================
    // Utilidades
    // =====================================================================

    private static UnityEngine.SceneManagement.Scene CopiarYAbrir(string origen, string destino)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destino) != null)
            AssetDatabase.DeleteAsset(destino);

        if (!AssetDatabase.CopyAsset(origen, destino))
        {
            Debug.LogError("No se pudo copiar " + origen + " en " + destino);
            return default;
        }

        return EditorSceneManager.OpenScene(destino, OpenSceneMode.Single);
    }

    private static void Guardar(UnityEngine.SceneManagement.Scene escena)
    {
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
    }
}
