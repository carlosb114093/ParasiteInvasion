using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Convierte la pantalla de juego (05_Juego) en el Nivel 1 jugable.
///
/// Igual que ConstructorInterfaz, el nivel se arma por codigo para que sea
/// reproducible: crea los materiales y prefabs de parasitos, disparos y
/// efectos, cambia el control de animaciones del piloto por el control de
/// juego, conecta premios, tentaculos y camara, y configura las oleadas.
/// Se puede ejecutar varias veces: lo que crea va dentro del objeto "Nivel1"
/// y se rehace en cada pasada.
///
/// Menu: Parasite Invasion / Montar nivel 1 jugable
/// </summary>
public static class ConstructorNivel1
{
    private const string RutaJuego = "Assets/Escenas/05_Juego.unity";
    private const string RutaComoJugar = "Assets/Escenas/03_ComoJugar.unity";
    private const string RutaBase = "Assets/Juego/";
    private const string RutaMateriales = RutaBase + "Materiales/";
    private const string RutaPrefabs = RutaBase + "Prefabs/";
    private const string RutaMallas = RutaBase + "Mallas/";

    /// <summary>Radio del anillo de la vena, hasta el centro del tubo.</summary>
    private const float RadioAnillo = 32f;

    private static readonly Color Cian = new Color(0.13f, 0.83f, 0.77f);
    private static readonly Color Rojo = new Color(0.9f, 0.22f, 0.36f);

    [MenuItem("Parasite Invasion/Montar nivel 1 jugable")]
    public static void MontarDesdeMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Montar();
    }

    public static void Montar()
    {
        Directory.CreateDirectory(RutaMateriales);
        Directory.CreateDirectory(RutaPrefabs);
        Directory.CreateDirectory(RutaMallas);
        AssetDatabase.Refresh();

        // La escena se abre ANTES de crear los prefabs: abrir una escena en
        // modo Single descarga los recursos sin uso y dejaria invalidas las
        // referencias a prefabs recien guardados.
        var escena = EditorSceneManager.OpenScene(RutaJuego, OpenSceneMode.Single);

        var viejo = GameObject.Find("Nivel1");
        if (viejo != null) Object.DestroyImmediate(viejo);

        var hud = Object.FindFirstObjectByType<HUDJuego>();
        if (hud == null)
        {
            Debug.LogError("ConstructorNivel1: no hay HUDJuego en 05_Juego. Ejecute " +
                           "primero Parasite Invasion / Construir interfaz.");
            return;
        }

        var jugador = PrepararJugador(hud);
        if (jugador == null) return;

        // El piloto mide unas 2,4 unidades y el conducto tiene radio ~6: todo
        // lo demas (zona de juego, parasitos, disparos, premios) se mide en
        // proporcion a esas dos cifras.
        float radioReal = RadioDelConducto(escena);
        jugador.radioConducto = radioReal * 0.7f;
        jugador.velocidad = 6.5f;
        jugador.invulnerabilidad = 1.3f;
        float radioDisparo = radioReal * 0.95f;

        // ---- Recursos compartidos ----
        var matTrazo = CrearMaterial("Trazo", Shader.Find("Sprites/Default"), Color.white,
                                Color.black);
        var chispa = AssetDatabase.LoadAssetAtPath<Material>(
                         "Assets/PowerUp/Materials/spark-1.mat");
        if (chispa == null) chispa = matTrazo;

        var estallido = CrearEstallido("Estallido", chispa);

        var balaPiloto = CrearProyectil("ProyectilPiloto", Cian, 0.2f, 28f, 1f, 1.6f,
                                        radioDisparo, matTrazo, estallido);
        var balaParasito = CrearProyectil("ProyectilParasito", Rojo, 0.28f, 11f, 6f, 4f,
                                          radioDisparo, matTrazo, estallido);

        var larva = CrearParasito("ParasitoLarva", new Color(0.42f, 0.78f, 0.25f),
            new Color(1f, 0.85f, 0.2f), 0.95f, 3f, 150, 2.4f, 1.4f, 0f, 7f, null,
            estallido);
        var espora = CrearParasito("ParasitoEspora", new Color(1f, 0.78f, 0.22f),
            new Color(1f, 0.3f, 0.2f), 0.6f, 1f, 100, 4.5f, 2.2f, 0f, 5f, null,
            estallido);
        var escupidor = CrearParasito("ParasitoEscupidor", new Color(0.78f, 0.18f, 0.45f),
            new Color(1f, 0.25f, 0.3f), 1.15f, 5f, 300, 2.2f, 1.6f, 10f, 9f,
            balaParasito, estallido);

        jugador.prefabProyectil = balaPiloto;
        EditorUtility.SetDirty(jugador);

        var nivel = new GameObject("Nivel1").transform;

        // La vena en anillo se crea antes de colocar nada: premios,
        // tentaculos y camara se situan con sus coordenadas de pista.
        var anillo = CrearAnillo(nivel, escena, radioReal);

        PrepararCamara(jugador, anillo);
        PrepararPremios(nivel, estallido, anillo);
        PrepararTentaculos(nivel, anillo);

        var salida = CrearSalida(nivel, jugador);
        var generador = CrearGenerador(nivel, hud, jugador, salida, larva, espora,
                                       escupidor);

        // ---- Reglas del HUD ----
        hud.teclasDePrueba = false;
        hud.victoriaAlAgotarTiempo = false;
        hud.consumoOxigeno = 0.9f;
        hud.duracionNivel = 180f;
        hud.dosisIniciales = 3;
        int total = generador.TotalParasitos;
        int cantidadOleadas = generador.oleadas.Count;
        hud.amenazasIniciales = total;
        EditorUtility.SetDirty(hud);

        var ayuda = hud.transform.Find("Ayuda");
        if (ayuda != null && ayuda.GetComponent<Text>() != null)
            ayuda.GetComponent<Text>().text =
                "Flechas mover    Re/Av Pag altura    D o Espacio disparar    " +
                "S esquivar    B dosis    P pausa";

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        ActualizarComoJugar();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(RutaJuego, OpenSceneMode.Single);
        Debug.Log("Nivel 1 montado: " + cantidadOleadas + " oleadas, " +
                  total + " parasitos. Radio del conducto: " +
                  radioReal.ToString("0.00"));
    }

    // ------------------------------------------------------------------
    // Piloto, camara y escenario
    // ------------------------------------------------------------------

    private static ControlJugador PrepararJugador(HUDJuego hud)
    {
        var go = GameObject.Find("Jugador");
        if (go == null)
        {
            Debug.LogError("ConstructorNivel1: no se encontro el objeto Jugador.");
            return null;
        }

        var control = go.GetComponent<ControlJugador>();
        if (control == null) control = go.AddComponent<ControlJugador>();

        // Se heredan las medidas que ya se habian ajustado para la evidencia
        // de animaciones y se retira ese control de demostracion.
        var anterior = go.GetComponent<AnimationController>();
        if (anterior != null)
        {
            control.velocidad = anterior.velocidad;
            control.ajusteGiroY = anterior.ajusteGiroY;
            control.radioConducto = anterior.radioConducto;
            control.limiteTrasero = -anterior.limiteLongitudinal;
            control.limiteFrontal = anterior.limiteLongitudinal - 2f;
            Object.DestroyImmediate(anterior);
        }

        control.hud = hud;
        control.sonidoDisparo = Clip("Assets/Sonidos/disparo.wav");
        control.sonidoDano = Clip("Assets/Sonidos/dano.wav");
        control.sonidoSalto = Clip("Assets/Sonidos/salto.wav");
        control.sonidoRecoger = Clip("Assets/PowerUp/Sounds/power-up.wav");

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // El colisionador abraza el cuerpo visible del piloto.
        Bounds b = Limites(go);
        var capsula = go.GetComponent<CapsuleCollider>();
        if (capsula == null) capsula = go.AddComponent<CapsuleCollider>();
        capsula.isTrigger = true;
        capsula.direction = 1;
        capsula.center = go.transform.InverseTransformPoint(b.center);
        capsula.height = Mathf.Max(b.size.y, 0.4f);
        capsula.radius = Mathf.Max(Mathf.Min(b.extents.x, b.extents.z), 0.15f);

        control.bocaDisparo = new Vector3(b.extents.x + 0.2f,
                                          b.center.y - go.transform.position.y, 0f);

        EditorUtility.SetDirty(control);
        return control;
    }

    private static void PrepararCamara(ControlJugador jugador, PistaAnillo anillo)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var sigue = cam.GetComponent<CamaraSeguimiento>();
        if (sigue == null) sigue = cam.gameObject.AddComponent<CamaraSeguimiento>();
        sigue.objetivo = jugador.transform;
        sigue.distancia = 8.5f;
        sigue.altura = 2.6f;
        sigue.acompanamiento = 0.6f;
        sigue.mirarAdelante = 10f;

        float s = anillo.APista(jugador.transform.position).x;
        cam.transform.position = anillo.AMundo(new Vector3(s - sigue.distancia,
                                                           sigue.altura, 0f));
        cam.transform.rotation = Quaternion.LookRotation(
            anillo.AMundo(new Vector3(s + sigue.mirarAdelante, 1f, 0f)) -
            cam.transform.position);
        cam.nearClipPlane = 0.05f;
        EditorUtility.SetDirty(sigue);
    }

    private static float RadioDelConducto(Scene escena)
    {
        var conducto = Buscar(escena, "ConductoVena");
        if (conducto == null) return 6f;

        // Tras la primera pasada el tubo recto queda oculto y un objeto
        // inactivo no informa sus limites: se enciende un instante.
        bool activo = conducto.activeSelf;
        conducto.SetActive(true);
        Bounds b = Limites(conducto);
        conducto.SetActive(activo);
        return Mathf.Min(b.extents.y, b.extents.z);
    }

    /// <summary>Busca un objeto por nombre en la escena, aunque este inactivo.</summary>
    private static GameObject Buscar(Scene escena, string nombre)
    {
        foreach (var raiz in escena.GetRootGameObjects())
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t.gameObject;
        return null;
    }

    // ------------------------------------------------------------------
    // Vena en anillo
    // ------------------------------------------------------------------

    /// <summary>
    /// Cambia el conducto recto por un tubo cerrado en forma de anillo. El
    /// tubo recto (evidencia de las actividades anteriores) solo se oculta.
    /// El centro del anillo se coloca de modo que el punto de partida del
    /// piloto (el origen) quede en s = 0 mirando hacia +X, como antes.
    /// </summary>
    private static PistaAnillo CrearAnillo(Transform nivel, Scene escena, float radioTubo)
    {
        var recto = Buscar(escena, "ConductoVena");
        Material material = null;
        if (recto != null)
        {
            var r = recto.GetComponentInChildren<Renderer>(true);
            if (r != null) material = r.sharedMaterial;
            recto.SetActive(false);
        }
        if (material == null)
            material = CrearMaterial("Vena", Shader.Find("Standard"),
                                     new Color(0.55f, 0.08f, 0.12f), Color.black);

        var go = new GameObject("VenaAnillo");
        go.transform.SetParent(nivel);
        go.transform.position = new Vector3(0f, 0f, -RadioAnillo);

        var pista = go.AddComponent<PistaAnillo>();
        pista.radioAnillo = RadioAnillo;
        pista.radioTubo = radioTubo;

        string ruta = RutaMallas + "VenaAnillo.asset";
        AssetDatabase.DeleteAsset(ruta);
        var malla = MallaAnillo(RadioAnillo, radioTubo, 160, 40);
        AssetDatabase.CreateAsset(malla, ruta);

        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var render = go.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;
        // Un tubo cerrado haria sombra sobre todo su interior.
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return pista;
    }

    /// <summary>
    /// Toro visto desde dentro: las caras miran hacia el centro del tubo. La
    /// pared lleva una ondulacion suave para que parezca tejido y no una
    /// tuberia, y la textura se repite a lo largo del anillo con la misma
    /// proporcion que alrededor del tubo.
    /// </summary>
    private static Mesh MallaAnillo(float radioAnillo, float radioTubo,
                                    int pasosAnillo, int pasosTubo)
    {
        int columnas = pasosTubo + 1;
        var vertices = new Vector3[(pasosAnillo + 1) * columnas];
        var normales = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length];
        float repeticiones = Mathf.Round(radioAnillo / radioTubo);

        for (int i = 0; i <= pasosAnillo; i++)
        {
            float a = (float)i / pasosAnillo * Mathf.PI * 2f;
            Vector3 exterior = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));

            for (int j = 0; j <= pasosTubo; j++)
            {
                float f = (float)j / pasosTubo * Mathf.PI * 2f;
                Vector3 seccion = exterior * Mathf.Cos(f) + Vector3.up * Mathf.Sin(f);
                float ondula = 1f + 0.035f * Mathf.Sin(a * 14f + f * 3f) +
                               0.02f * Mathf.Sin(a * 37f - f * 2f);

                int k = i * columnas + j;
                vertices[k] = exterior * radioAnillo + seccion * radioTubo * ondula;
                normales[k] = -seccion;
                uv[k] = new Vector2((float)j / pasosTubo,
                                    (float)i / pasosAnillo * repeticiones);
            }
        }

        var triangulos = new int[pasosAnillo * pasosTubo * 6];
        int t = 0;
        for (int i = 0; i < pasosAnillo; i++)
        {
            for (int j = 0; j < pasosTubo; j++)
            {
                int a = i * columnas + j, b = (i + 1) * columnas + j;
                triangulos[t++] = a; triangulos[t++] = b; triangulos[t++] = b + 1;
                triangulos[t++] = a; triangulos[t++] = b + 1; triangulos[t++] = a + 1;
            }
        }

        // Unity muestra la cara cuyo producto (B-A)x(C-A) apunta hacia quien
        // mira. Si el primer triangulo no apunta hacia dentro se invierten
        // todos.
        Vector3 cara = Vector3.Cross(vertices[triangulos[1]] - vertices[triangulos[0]],
                                     vertices[triangulos[2]] - vertices[triangulos[0]]);
        if (Vector3.Dot(cara, normales[triangulos[0]]) < 0f)
        {
            for (int k = 0; k < triangulos.Length; k += 3)
            {
                int tmp = triangulos[k + 1];
                triangulos[k + 1] = triangulos[k + 2];
                triangulos[k + 2] = tmp;
            }
        }

        var malla = new Mesh { name = "VenaAnillo" };
        malla.vertices = vertices;
        malla.normals = normales;
        malla.uv = uv;
        malla.triangles = triangulos;
        malla.RecalculateBounds();
        malla.RecalculateTangents();
        return malla;
    }

    private static void PrepararPremios(Transform nivel, Estallido efecto,
                                        PistaAnillo anillo)
    {
        // Premios repartidos alrededor de todo el anillo, en coordenadas de
        // pista (recorrido, altura, lateral). Se agrandan para que se
        // distingan junto al piloto.
        var capsula = GameObject.Find("CapsulaMedicina");
        if (capsula != null)
        {
            Vector3[] puntos =
            {
                new Vector3(4f, 1.6f, 2.2f), new Vector3(9f, -1.8f, -2.4f),
                new Vector3(38f, 2.5f, -1.5f), new Vector3(75f, -2.2f, 2.0f),
                new Vector3(118f, 0.5f, 3.0f), new Vector3(160f, 2.8f, 0.5f),
                new Vector3(190f, -1.0f, -2.8f),
            };
            Colocar(capsula, anillo.AMundo(puntos[0]), 100f);
            HacerRecogible(capsula, Recogible.Tipo.CapsulaMedicina, efecto);
            for (int i = 1; i < puntos.Length; i++)
                Copiar(capsula, nivel, "CapsulaMedicina_" + (i + 1), anillo.AMundo(puntos[i]),
                       c => HacerRecogible(c, Recogible.Tipo.CapsulaMedicina, efecto));
        }

        var vida = GameObject.Find("VidaExtra");
        if (vida != null)
        {
            Colocar(vida, anillo.AMundo(new Vector3(14f, 2.4f, 0.5f)), 110f);
            HacerRecogible(vida, Recogible.Tipo.VidaExtra, efecto);
            Copiar(vida, nivel, "VidaExtra_2", anillo.AMundo(new Vector3(100f, -2.4f, -1.0f)),
                   c => HacerRecogible(c, Recogible.Tipo.VidaExtra, efecto));
        }

        var dosis = GameObject.Find("DosisMaxima");
        if (dosis != null)
        {
            Colocar(dosis, anillo.AMundo(new Vector3(55f, -1.2f, 2.2f)), 80f);
            HacerRecogible(dosis, Recogible.Tipo.DosisMaxima, efecto);
            Copiar(dosis, nivel, "DosisMaxima_2", anillo.AMundo(new Vector3(140f, 1.5f, -2.5f)),
                   c => HacerRecogible(c, Recogible.Tipo.DosisMaxima, efecto));
        }
    }

    private static void Colocar(GameObject go, Vector3 posicion, float escala)
    {
        go.transform.position = posicion;
        go.transform.localScale = Vector3.one * escala;
    }

    private static void HacerRecogible(GameObject go, Recogible.Tipo tipo, Estallido efecto)
    {
        var r = go.GetComponent<Recogible>();
        if (r == null) r = go.AddComponent<Recogible>();
        r.tipo = tipo;
        r.efecto = efecto;
        r.colorEfecto = tipo == Recogible.Tipo.VidaExtra
            ? new Color(1f, 0.35f, 0.45f) : new Color(0.4f, 1f, 0.9f);

        var esfera = go.GetComponent<SphereCollider>();
        if (esfera == null) esfera = go.AddComponent<SphereCollider>();
        esfera.isTrigger = true;
        Bounds b = Limites(go);
        float escala = Mathf.Max(go.transform.lossyScale.x, 0.0001f);
        esfera.center = go.transform.InverseTransformPoint(b.center);
        esfera.radius = (b.extents.magnitude * 0.8f + 0.15f) / escala;
        EditorUtility.SetDirty(r);
    }

    /// <summary>
    /// Tentaculos anclados a la pared alrededor del anillo. Cada uno se
    /// indica con su recorrido y el angulo de la pared de la que brota:
    /// 0 suelo, 180 techo, 90 pared interior, -90 pared exterior.
    /// </summary>
    private static void PrepararTentaculos(Transform nivel, PistaAnillo anillo)
    {
        var go = GameObject.Find("TentaculoParasitario");
        if (go == null) return;

        Vector2[] anclajes =
        {
            new Vector2(8f, 0f), new Vector2(17f, 180f), new Vector2(45f, 90f),
            new Vector2(62f, -90f), new Vector2(86f, 0f), new Vector2(106f, 180f),
            new Vector2(130f, 90f), new Vector2(150f, -90f), new Vector2(176f, 0f),
        };

        for (int i = 0; i < anclajes.Length; i++)
        {
            float s = anclajes[i].x;
            float angulo = anclajes[i].y;
            float rad = angulo * Mathf.Deg2Rad;

            // El tentaculo apunta hacia el centro del tubo desde su pared.
            Vector3 punto = anillo.AMundo(new Vector3(s, -3.7f * Mathf.Cos(rad),
                                                      -3.7f * Mathf.Sin(rad)));
            Vector3 avance = anillo.Tangente(s);
            Quaternion giro = Quaternion.AngleAxis(angulo, avance) *
                              Quaternion.FromToRotation(Vector3.right, avance) *
                              Quaternion.Euler(-90f, 0f, 0f);

            if (i == 0)
            {
                Colocar(go, punto, 140f);
                go.transform.rotation = giro;
                HacerTentaculo(go);
            }
            else
            {
                Copiar(go, nivel, "TentaculoParasitario_" + (i + 1), punto, t =>
                {
                    t.transform.rotation = giro;
                    HacerTentaculo(t);
                });
            }
        }
    }

    private static void HacerTentaculo(GameObject go)
    {
        var t = go.GetComponent<TentaculoObstaculo>();
        if (t == null) t = go.AddComponent<TentaculoObstaculo>();
        t.dano = 6f;

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // La caja se ajusta a la malla en su propio espacio local, asi gira
        // junto con el tentaculo cuando este se balancea.
        var malla = go.GetComponentInChildren<MeshFilter>();
        if (malla != null && malla.sharedMesh != null)
        {
            var caja = malla.GetComponent<BoxCollider>();
            if (caja == null) caja = malla.gameObject.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.center = malla.sharedMesh.bounds.center;
            caja.size = malla.sharedMesh.bounds.size * 0.75f;
        }
        EditorUtility.SetDirty(t);
    }

    /// <summary>
    /// Duplica un objeto de la escena conservando el vinculo con su modelo
    /// FBX, lo coloca en otra posicion y le aplica un ajuste final.
    /// </summary>
    private static void Copiar(GameObject original, Transform padre, string nombre,
                               Vector3 posicion, System.Action<GameObject> ajustar)
    {
        GameObject copia;
        var fuente = PrefabUtility.GetCorrespondingObjectFromSource(original);
        if (fuente != null) copia = (GameObject)PrefabUtility.InstantiatePrefab(fuente, padre);
        else copia = Object.Instantiate(original, padre);

        copia.name = nombre;
        copia.transform.position = posicion;
        copia.transform.rotation = original.transform.rotation;
        copia.transform.localScale = original.transform.lossyScale;
        ajustar(copia);
    }

    private static GameObject CrearSalida(Transform nivel, ControlJugador jugador)
    {
        var salida = new GameObject("SalidaCamaraCardiaca");
        salida.transform.SetParent(nivel);
        salida.transform.position = new Vector3(jugador.limiteFrontal, 0f, 0f);

        var aro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aro.name = "Anillo";
        Object.DestroyImmediate(aro.GetComponent<Collider>());
        aro.transform.SetParent(salida.transform, false);
        aro.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        float d = jugador.radioConducto * 2.2f;
        aro.transform.localScale = new Vector3(d, 0.03f, d);
        aro.GetComponent<Renderer>().sharedMaterial =
            CrearMaterial("Salida", Shader.Find("Standard"), new Color(0.1f, 0.5f, 0.5f, 1f),
                     Cian * 2.5f);

        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(salida.transform, false);
        luz.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
        luz.type = LightType.Point;
        luz.color = Cian;
        luz.range = 7f;
        luz.intensity = 3f;

        return salida;
    }

    private static GeneradorOleadas CrearGenerador(Transform nivel, HUDJuego hud,
        ControlJugador jugador, GameObject salida, Parasito larva, Parasito espora,
        Parasito escupidor)
    {
        var go = new GameObject("GeneradorOleadas");
        go.transform.SetParent(nivel);
        var g = go.AddComponent<GeneradorOleadas>();
        g.hud = hud;
        g.jugador = jugador;
        g.salida = salida;

        g.oleadas = new List<GeneradorOleadas.Oleada>
        {
            Oleada("Larvas", 1.8f, Grupo(larva, 4)),
            Oleada("Enjambre de esporas", 1.1f, Grupo(larva, 3), Grupo(espora, 4)),
            Oleada("Escupidores", 1.2f, Grupo(escupidor, 2), Grupo(espora, 4),
                   Grupo(larva, 2)),
        };

        EditorUtility.SetDirty(g);
        return g;
    }

    private static GeneradorOleadas.Oleada Oleada(string nombre, float intervalo,
                                                 params GeneradorOleadas.Grupo[] grupos)
    {
        var o = new GeneradorOleadas.Oleada { nombre = nombre, intervalo = intervalo };
        o.grupos.AddRange(grupos);
        return o;
    }

    private static GeneradorOleadas.Grupo Grupo(Parasito prefab, int cantidad)
    {
        return new GeneradorOleadas.Grupo { prefab = prefab, cantidad = cantidad };
    }

    private static void ActualizarComoJugar()
    {
        if (!File.Exists(RutaComoJugar)) return;
        var escena = EditorSceneManager.OpenScene(RutaComoJugar, OpenSceneMode.Single);

        var nota = GameObject.Find("Nota");
        if (nota != null && nota.GetComponent<Text>() != null)
            nota.GetComponent<Text>().text =
                "Supera las tres oleadas de parasitos y cruza el anillo de luz antes de " +
                "que se acaben el tiempo o el oxigeno. Cada parasito eliminado libera oxigeno.";

        var tecla = GameObject.Find("Fila2/Tecla");
        if (tecla != null && tecla.GetComponent<Text>() != null)
            tecla.GetComponent<Text>().text = "D o Espacio";

        var accion = GameObject.Find("Fila0/Accion");
        if (accion != null && accion.GetComponent<Text>() != null)
            accion.GetComponent<Text>().text = "Desplazar al piloto por el conducto";

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // ------------------------------------------------------------------
    // Prefabs
    // ------------------------------------------------------------------

    private static Estallido CrearEstallido(string nombre, Material material)
    {
        var go = new GameObject(nombre);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
        main.startColor = new Color(0.6f, 1f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        var emision = ps.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.4f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var degradado = new Gradient();
        degradado.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = degradado;

        var tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        go.AddComponent<Estallido>();

        return Guardar<Estallido>(go, nombre);
    }

    private static Proyectil CrearProyectil(string nombre, Color color, float radio,
        float velocidad, float dano, float duracion, float radioConducto,
        Material trazo, Estallido impacto)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = nombre;
        go.transform.localScale = Vector3.one * radio * 2f;
        go.GetComponent<Renderer>().sharedMaterial =
            CrearMaterial(nombre, Shader.Find("Standard"), color, color * 3f);

        var esfera = go.GetComponent<SphereCollider>();
        esfera.isTrigger = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var estela = go.AddComponent<TrailRenderer>();
        estela.sharedMaterial = trazo;
        estela.time = 0.12f;
        estela.widthMultiplier = radio * 1.6f;
        estela.minVertexDistance = 0.05f;
        var degradado = new Gradient();
        degradado.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        estela.colorGradient = degradado;

        var p = go.AddComponent<Proyectil>();
        p.velocidad = velocidad;
        p.dano = dano;
        p.duracion = duracion;
        p.radioConducto = radioConducto;
        p.efectoImpacto = impacto;

        return Guardar<Proyectil>(go, nombre);
    }

    /// <summary>
    /// Parasito low-poly armado con primitivas: cuerpo ovalado que late,
    /// ocho puas alrededor y un ojo luminoso que mira al piloto.
    /// </summary>
    private static Parasito CrearParasito(string nombre, Color piel, Color ojo,
        float radio, float vida, int puntos, float velocidad, float vaiven,
        float distancia, float dano, Proyectil bala, Estallido muerte)
    {
        var raiz = new GameObject(nombre);
        var matPiel = CrearMaterial(nombre + "_Piel", Shader.Find("Standard"), piel, piel * 0.35f);
        var matOjo = CrearMaterial(nombre + "_Ojo", Shader.Find("Standard"), ojo, ojo * 2.5f);

        var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cuerpo.name = "Cuerpo";
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        cuerpo.transform.SetParent(raiz.transform, false);
        cuerpo.transform.localScale = new Vector3(2f, 1.8f, 2.4f) * radio;
        cuerpo.GetComponent<Renderer>().sharedMaterial = matPiel;

        for (int i = 0; i < 8; i++)
        {
            var pua = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pua.name = "Pua" + i;
            Object.DestroyImmediate(pua.GetComponent<Collider>());
            pua.transform.SetParent(cuerpo.transform, false);

            float angulo = i * 45f;
            Vector3 hacia = Quaternion.Euler(0f, 0f, angulo) * Vector3.up;
            hacia = (hacia + Vector3.back * 0.35f).normalized;
            pua.transform.localPosition = hacia * 0.48f;
            pua.transform.localRotation = Quaternion.FromToRotation(Vector3.up, hacia);
            pua.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            pua.GetComponent<Renderer>().sharedMaterial = matPiel;
        }

        var ojoGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ojoGo.name = "Ojo";
        Object.DestroyImmediate(ojoGo.GetComponent<Collider>());
        ojoGo.transform.SetParent(cuerpo.transform, false);
        ojoGo.transform.localPosition = new Vector3(0f, 0.08f, 0.42f);
        ojoGo.transform.localScale = Vector3.one * (bala != null ? 0.42f : 0.32f);
        ojoGo.GetComponent<Renderer>().sharedMaterial = matOjo;

        var esfera = raiz.AddComponent<SphereCollider>();
        esfera.isTrigger = true;
        esfera.radius = radio * 1.05f;
        var rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var p = raiz.AddComponent<Parasito>();
        p.vida = vida;
        p.puntos = puntos;
        p.velocidad = velocidad;
        p.vaiven = vaiven;
        p.distanciaPreferida = distancia;
        p.danoContacto = dano;
        p.prefabProyectil = bala;
        p.cuerpo = cuerpo.transform;
        p.colorEstallido = piel;
        p.efectoMuerte = muerte;

        return Guardar<Parasito>(raiz, nombre);
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------

    private static T Guardar<T>(GameObject go, string nombre) where T : Component
    {
        string ruta = RutaPrefabs + nombre + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<T>();
    }

    private static Material CrearMaterial(string nombre, Shader shader, Color color,
                                     Color emision)
    {
        string ruta = RutaMateriales + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        else
        {
            mat.shader = shader;
        }

        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.55f);
        if (mat.HasProperty("_EmissionColor"))
        {
            bool brilla = emision.maxColorComponent > 0.001f;
            if (brilla) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emision);
            mat.globalIlluminationFlags = brilla
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static AudioClip Clip(string ruta)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
    }

    /// <summary>Caja que envuelve todas las mallas visibles de un objeto.</summary>
    private static Bounds Limites(GameObject go)
    {
        // Solo cuentan las mallas: las particulas decorativas que cuelgan de
        // algunos objetos inflarian la caja.
        bool hay = false;
        Bounds b = new Bounds(go.transform.position, Vector3.one * 0.5f);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }
}
