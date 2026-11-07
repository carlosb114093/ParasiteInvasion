using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye por codigo las cinco pantallas de la interfaz del prototipo.
///
/// Se prefiere generar las escenas con un script antes que montarlas a mano
/// en el editor porque asi la interfaz es reproducible: si se cambia una
/// medida, un color o un texto basta con volver a ejecutar el menu
/// "Parasite Invasion / Construir interfaz" y las cinco pantallas quedan
/// coherentes entre si. El mismo metodo se invoca desde la linea de comandos
/// para regenerar el proyecto sin abrir el editor.
/// </summary>
public static class ConstructorInterfaz
{
    // -------------------------------------------------------------------
    // Paleta y medidas comunes
    // -------------------------------------------------------------------

    private const string RutaSprites = "Assets/UI/Sprites/";
    private const string RutaEscenas = "Assets/Escenas/";

    private static readonly Color Crema = Col(245, 233, 236);
    private static readonly Color Cian = Col(33, 212, 197);
    private static readonly Color Rojo = Col(230, 57, 92);
    private static readonly Color Ambar = Col(255, 193, 69);
    private static readonly Color Apagado = Col(178, 150, 158);

    private static readonly Vector2 Referencia = new Vector2(1920f, 1080f);

    private static Font fuente;
    private static readonly Dictionary<string, Sprite> cacheSprites =
        new Dictionary<string, Sprite>();

    // -------------------------------------------------------------------
    // Punto de entrada
    // -------------------------------------------------------------------

    [MenuItem("Parasite Invasion/Construir interfaz")]
    public static void ConstruirTodo()
    {
        PrepararSprites();
        Directory.CreateDirectory(RutaEscenas);

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");

        ConstruirMenuPrincipal();
        ConstruirOpciones();
        ConstruirComoJugar();
        ConstruirCreditos();
        ConstruirJuego();

        RegistrarEscenas();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Interfaz construida: 5 pantallas en " + RutaEscenas);

        // La pantalla de juego se acaba de rehacer desde la escena de
        // animaciones: se le vuelve a montar la jugabilidad del Nivel 1.
        ConstructorNivel1.Montar();
    }

    /// <summary>
    /// Ajusta la importacion de los PNG generados: los convierte en sprites y
    /// define el borde de nueve sectores para que paneles y botones se
    /// estiren sin deformar sus esquinas redondeadas.
    /// </summary>
    private static void PrepararSprites()
    {
        foreach (string ruta in Directory.GetFiles(RutaSprites, "*.png"))
        {
            string relativa = ruta.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(relativa) as TextureImporter;
            if (imp == null) continue;

            string nombre = Path.GetFileNameWithoutExtension(relativa);

            Vector4 borde = Vector4.zero;
            if (nombre.StartsWith("panel_")) borde = new Vector4(32, 32, 32, 32);
            else if (nombre.StartsWith("boton_")) borde = new Vector4(20, 20, 20, 20);
            else if (nombre.StartsWith("barra_")) borde = new Vector4(14, 10, 14, 10);

            bool cambia = imp.textureType != TextureImporterType.Sprite ||
                          imp.spriteBorder != borde ||
                          imp.mipmapEnabled;

            if (!cambia) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = borde;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
    }

    /// <summary>Declara las pantallas en Build Settings para poder cargarlas por nombre.</summary>
    private static void RegistrarEscenas()
    {
        string[] nombres =
        {
            Navegador.MenuPrincipal, Navegador.Opciones, Navegador.ComoJugar,
            Navegador.Creditos, Navegador.Juego
        };

        var lista = new List<EditorBuildSettingsScene>();
        foreach (string n in nombres)
            lista.Add(new EditorBuildSettingsScene(RutaEscenas + n + ".unity", true));

        EditorBuildSettings.scenes = lista.ToArray();
    }

    // -------------------------------------------------------------------
    // Utilidades de construccion
    // -------------------------------------------------------------------

    private static Color Col(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    private static Sprite Sp(string nombre)
    {
        Sprite s;
        if (cacheSprites.TryGetValue(nombre, out s) && s != null) return s;
        s = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprites + nombre + ".png");
        cacheSprites[nombre] = s;
        return s;
    }

    private static DefaultControls.Resources RecursosBase()
    {
        var r = new DefaultControls.Resources();
        r.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        r.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        r.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        r.knob = Sp("perilla_deslizador");
        r.checkmark = Sp("ic_check");
        r.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        r.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return r;
    }

    /// <summary>Crea el lienzo raiz y el sistema de eventos de una pantalla.</summary>
    private static Canvas CrearLienzo(string nombre)
    {
        var go = new GameObject(nombre, typeof(Canvas), typeof(CanvasScaler),
                                typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var escalador = go.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = Referencia;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem),
                           typeof(StandaloneInputModule));

        return canvas;
    }

    private static RectTransform Rt(GameObject go)
    {
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Coloca un elemento respecto a un ancla concreta de su padre.</summary>
    private static RectTransform Situar(GameObject go, Vector2 ancla, Vector2 pos,
                                        Vector2 tam)
    {
        var rt = Rt(go);
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    /// <summary>Estira un elemento hasta ocupar todo su padre.</summary>
    private static RectTransform Estirar(GameObject go, float margen = 0f)
    {
        var rt = Rt(go);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    private static GameObject Nodo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go;
    }

    private static Image Imagen(string nombre, Transform padre, Sprite sprite,
                                Color color, Image.Type tipo = Image.Type.Simple)
    {
        var go = Nodo(nombre, padre);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = tipo;
        if (tipo == Image.Type.Sliced) img.pixelsPerUnitMultiplier = 1f;
        img.raycastTarget = false;
        return img;
    }

    private static Text Texto(string nombre, Transform padre, string contenido,
                              int tamano, TextAnchor alineacion, Color color,
                              FontStyle estilo = FontStyle.Normal)
    {
        var go = Nodo(nombre, padre);
        var t = go.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = color;
        t.fontStyle = estilo;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = true;
        return t;
    }

    /// <summary>
    /// Boton del prototipo: fondo de nueve sectores, icono opcional a la
    /// izquierda y etiqueta. El intercambio de sprites cubre los cuatro
    /// estados (normal, resaltado, presionado e inactivo).
    /// </summary>
    private static Button Boton(string nombre, Transform padre, string etiqueta,
                                string icono, bool acento = false,
                                int tamanoTexto = 30)
    {
        var go = Nodo(nombre, padre);
        var fondo = go.AddComponent<Image>();
        fondo.sprite = Sp(acento ? "boton_acento" : "boton_normal");
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 1f;
        fondo.color = Color.white;

        var b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.SpriteSwap;
        var estados = new SpriteState();
        estados.highlightedSprite = Sp("boton_resaltado");
        estados.pressedSprite = Sp("boton_presionado");
        estados.selectedSprite = Sp("boton_resaltado");
        estados.disabledSprite = Sp("boton_inactivo");
        b.spriteState = estados;
        b.targetGraphic = fondo;

        float izquierda = 26f;
        if (!string.IsNullOrEmpty(icono))
        {
            var ic = Imagen("Icono", go.transform, Sp(icono),
                            acento ? Cian : Crema);
            Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(26f, 0f),
                   new Vector2(38f, 38f));
            izquierda = 78f;
        }

        var txt = Texto("Etiqueta", go.transform, etiqueta, tamanoTexto,
                        string.IsNullOrEmpty(icono) ? TextAnchor.MiddleCenter
                                                    : TextAnchor.MiddleLeft,
                        acento ? Cian : Crema, FontStyle.Bold);
        // La etiqueta nunca debe partirse en dos lineas dentro del boton: se
        // fuerza una sola linea y se deja que Unity reduzca el cuerpo de letra
        // cuando el texto no quepa a lo ancho.
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 14;
        txt.resizeTextMaxSize = tamanoTexto;

        var rt = Estirar(txt.gameObject);
        rt.offsetMin = new Vector2(izquierda, 0f);
        rt.offsetMax = new Vector2(-20f, 0f);

        return b;
    }

    /// <summary>Barra de progreso con fondo, relleno recortado, icono y cifra.</summary>
    private static Image Barra(string nombre, Transform padre, Vector2 ancla,
                               Vector2 pos, Vector2 tam, string icono,
                               string spriteRelleno, Color colorIcono,
                               out Text cifra)
    {
        var raiz = Nodo(nombre, padre);
        Situar(raiz, ancla, pos, tam);

        var ic = Imagen("Icono", raiz.transform, Sp(icono), colorIcono);
        Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0f),
               new Vector2(34f, 34f));

        var fondo = Imagen("Fondo", raiz.transform, Sp("barra_fondo"),
                           Color.white, Image.Type.Sliced);
        var rtFondo = Estirar(fondo.gameObject);
        rtFondo.offsetMin = new Vector2(46f, 0f);
        rtFondo.offsetMax = new Vector2(0f, 0f);

        var relleno = Imagen("Relleno", fondo.transform, Sp(spriteRelleno),
                             Color.white, Image.Type.Filled);
        relleno.fillMethod = Image.FillMethod.Horizontal;
        relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
        relleno.fillAmount = 0.82f;
        Estirar(relleno.gameObject, 3f);

        cifra = Texto("Cifra", fondo.transform, "82 / 100", 20,
                      TextAnchor.MiddleRight, Crema, FontStyle.Bold);
        var rtC = Estirar(cifra.gameObject);
        rtC.offsetMin = new Vector2(0f, 0f);
        rtC.offsetMax = new Vector2(-14f, 0f);

        return relleno;
    }

    /// <summary>
    /// Armazon comun de un cuadro emergente: velo oscuro, panel centrado,
    /// titulo con icono y el componente que gobierna su apertura.
    /// </summary>
    private static PanelEmergente CuadroEmergente(string nombre, Transform padre,
                                                  Vector2 tam, string titulo,
                                                  string icono,
                                                  out RectTransform contenido,
                                                  bool congela = false,
                                                  bool cierraConEscape = true)
    {
        var raiz = Nodo(nombre, padre);
        Estirar(raiz);
        var grupo = raiz.AddComponent<CanvasGroup>();
        var pe = raiz.AddComponent<PanelEmergente>();
        pe.congelaElJuego = congela;
        pe.cierraConEscape = cierraConEscape;

        var velo = Imagen("Velo", raiz.transform, null, new Color(0f, 0f, 0f, 0.72f));
        velo.raycastTarget = true;
        Estirar(velo.gameObject);

        var cuadro = Imagen("Cuadro", raiz.transform, Sp("panel_emergente"),
                            Color.white, Image.Type.Sliced);
        cuadro.raycastTarget = true;
        var rtCuadro = Situar(cuadro.gameObject, new Vector2(0.5f, 0.5f),
                              Vector2.zero, tam);

        var franja = Imagen("FranjaTitulo", cuadro.transform, Sp("panel_secundario"),
                            new Color(1f, 1f, 1f, 0.55f), Image.Type.Sliced);
        Situar(franja.gameObject, new Vector2(0.5f, 1f),
                         new Vector2(0f, -18f), new Vector2(tam.x - 36f, 72f));

        if (!string.IsNullOrEmpty(icono))
        {
            var ic = Imagen("IconoTitulo", franja.transform, Sp(icono), Cian);
            Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(24f, 0f),
                   new Vector2(38f, 38f));
        }

        var tit = Texto("Titulo", franja.transform, titulo, 32,
                        TextAnchor.MiddleCenter, Crema, FontStyle.Bold);
        Estirar(tit.gameObject, 12f);

        contenido = rtCuadro;
        pe.cuadro = rtCuadro;
        pe.velo = velo.gameObject;
        return pe;
    }

    /// <summary>Aviso flotante reutilizado por varias pantallas.</summary>
    private static MensajeFlotante Aviso(Transform padre, Vector2 ancla, Vector2 pos)
    {
        var raiz = Nodo("AvisoFlotante", padre);
        Situar(raiz, ancla, pos, new Vector2(760f, 66f));
        raiz.AddComponent<CanvasGroup>();

        var fondo = Imagen("Fondo", raiz.transform, Sp("panel_secundario"),
                           Color.white, Image.Type.Sliced);
        Estirar(fondo.gameObject);

        var ic = Imagen("Icono", raiz.transform, Sp("ic_check"), Cian);
        Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(24f, 0f),
               new Vector2(30f, 30f));

        var t = Texto("Texto", raiz.transform, "Mensaje de la interfaz", 26,
                      TextAnchor.MiddleLeft, Crema);
        var rt = Estirar(t.gameObject);
        rt.offsetMin = new Vector2(66f, 0f);
        rt.offsetMax = new Vector2(-20f, 0f);

        var mf = raiz.AddComponent<MensajeFlotante>();
        mf.etiqueta = t;
        return mf;
    }

    /// <summary>Fondo compartido por las pantallas de menu.</summary>
    private static void FondoDeMenu(Transform padre, float oscurecer = 0f)
    {
        var fondo = Imagen("Fondo", padre, Sp("fondo_menu"), Color.white);
        Estirar(fondo.gameObject);
        fondo.preserveAspect = false;

        var vin = Imagen("Vineta", padre, Sp("vineta"), new Color(1f, 1f, 1f, 0.9f));
        Estirar(vin.gameObject);

        if (oscurecer > 0f)
        {
            var velo = Imagen("Oscurecer", padre, null,
                              new Color(0.02f, 0.01f, 0.02f, oscurecer));
            Estirar(velo.gameObject);
        }
    }

    private static Camera CamaraDeInterfaz()
    {
        var go = new GameObject("Camara", typeof(Camera));
        var cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Col(8, 3, 5);
        cam.orthographic = true;
        cam.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        return cam;
    }

    private static void GuardarEscena(Scene escena, string nombre)
    {
        EditorSceneManager.SaveScene(escena, RutaEscenas + nombre + ".unity");
    }

    // -------------------------------------------------------------------
    // Pantalla 1: menu principal
    // -------------------------------------------------------------------

    private static void ConstruirMenuPrincipal()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                 NewSceneMode.Single);
        CamaraDeInterfaz();

        // Recorre las pantallas y guarda las capturas del informe; se elimina
        // solo cuando el juego arranca sin el argumento -capturas.
        new GameObject("Capturador", typeof(CapturadorAutomatico));

        var canvas = CrearLienzo("LienzoMenu");
        var raiz = canvas.transform;

        FondoDeMenu(raiz);

        var logo = Imagen("Logotipo", raiz, Sp("logo_parasite_invasion"), Color.white);
        Situar(logo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -60f),
               new Vector2(880f, 308f));
        logo.preserveAspect = true;

        // ---- Panel con la lista de opciones ----
        var panel = Imagen("PanelMenu", raiz, Sp("panel_principal"), Color.white,
                           Image.Type.Sliced);
        panel.raycastTarget = true;
        Situar(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f),
               new Vector2(540f, 540f));

        var rotuloMenu = Texto("Rotulo", panel.transform, "MENU PRINCIPAL", 26,
                               TextAnchor.MiddleCenter, Cian, FontStyle.Bold);
        Situar(rotuloMenu.gameObject, new Vector2(0.5f, 1f),
               new Vector2(0f, -26f), new Vector2(440f, 34f));

        string[,] opciones =
        {
            { "BotonJugar", "JUGAR", "ic_play" },
            { "BotonOpciones", "OPCIONES", "ic_config" },
            { "BotonComoJugar", "COMO JUGAR", "ic_ayuda" },
            { "BotonCreditos", "CREDITOS", "ic_nivel" },
            { "BotonSalir", "SALIR", "ic_salir" },
        };

        var botones = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            botones[i] = Boton(opciones[i, 0], panel.transform, opciones[i, 1],
                               opciones[i, 2], i == 0);
            Situar(botones[i].gameObject, new Vector2(0.5f, 1f),
                   new Vector2(0f, -84f - i * 86f), new Vector2(452f, 72f));
        }

        // ---- Pie de pantalla ----
        var version = Texto("Version", raiz, "Prototipo v0.3  -  Evidencia GA2-AA2-EV01",
                            22, TextAnchor.LowerLeft, Apagado);
        Situar(version.gameObject, new Vector2(0f, 0f), new Vector2(44f, 30f),
               new Vector2(760f, 34f));

        var ficha = Texto("Ficha", raiz,
                          "SENA  -  Ficha 3466174  -  Regional Tolima", 22,
                          TextAnchor.LowerRight, Apagado);
        Situar(ficha.gameObject, new Vector2(1f, 0f), new Vector2(-44f, 30f),
               new Vector2(760f, 34f));

        var pista = Texto("Pista", raiz,
                          "Usa el raton o las flechas del teclado para navegar", 22,
                          TextAnchor.LowerCenter, Apagado);
        Situar(pista.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 30f),
               new Vector2(760f, 34f));

        // ---- Cuadro emergente: seleccion de nivel ----
        RectTransform cNivel;
        var cuadroNivel = CuadroEmergente("CuadroSeleccionNivel", raiz,
                                          new Vector2(1000f, 560f),
                                          "SELECCION DE NIVEL", "ic_nivel",
                                          out cNivel);

        var botonesNivel = new Button[3];
        string[] nombresNivel = { "VENA FEMORAL", "ARTERIA PULMONAR", "CAMARA CARDIACA" };
        for (int i = 0; i < 3; i++)
        {
            var bn = Boton("BotonNivel" + (i + 1), cNivel, "", "", false);
            Situar(bn.gameObject, new Vector2(0.5f, 1f),
                   new Vector2(-300f + i * 300f, -120f), new Vector2(276f, 200f));

            var num = Texto("Numero", bn.transform, (i + 1).ToString(), 72,
                            TextAnchor.UpperCenter, Crema, FontStyle.Bold);
            Situar(num.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -14f),
                   new Vector2(240f, 84f));

            var nom = Texto("Nombre", bn.transform, nombresNivel[i], 22,
                            TextAnchor.UpperCenter, Cian, FontStyle.Bold);
            Situar(nom.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -104f),
                   new Vector2(248f, 60f));

            // Una estrella por grado de dificultad, en lugar de un texto.
            for (int e = 0; e <= i; e++)
            {
                var estrella = Imagen("Estrella" + e, bn.transform,
                                      Sp("ic_puntaje"), Ambar);
                Situar(estrella.gameObject, new Vector2(0.5f, 0f),
                       new Vector2((e - i * 0.5f) * 32f, 16f),
                       new Vector2(26f, 26f));
            }

            botonesNivel[i] = bn;
        }

        var descripcion = Texto("Descripcion", cNivel,
            "Nivel 1  -  Vena femoral: recorrido de entrenamiento con pocos parasitos.",
            24, TextAnchor.MiddleCenter, Apagado);
        Situar(descripcion.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -336f),
               new Vector2(880f, 56f));

        var bComenzar = Boton("BotonComenzar", cNivel, "COMENZAR PARTIDA",
                              "ic_play", true, 24);
        Situar(bComenzar.gameObject, new Vector2(0.5f, 0f), new Vector2(-180f, 30f),
               new Vector2(340f, 66f));

        var bCancelar = Boton("BotonCancelar", cNivel, "CANCELAR", "ic_atras",
                              false, 24);
        Situar(bCancelar.gameObject, new Vector2(0.5f, 0f), new Vector2(180f, 30f),
               new Vector2(340f, 66f));

        cuadroNivel.primerElemento = bComenzar;

        // ---- Cuadro emergente: confirmar salida ----
        RectTransform cSalir;
        var cuadroSalir = CuadroEmergente("CuadroConfirmarSalida", raiz,
                                          new Vector2(760f, 360f),
                                          "CONFIRMAR SALIDA", "ic_advertencia",
                                          out cSalir);

        var icAviso = Imagen("IconoAviso", cSalir, Sp("ic_advertencia"), Ambar);
        Situar(icAviso.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -110f),
               new Vector2(64f, 64f));

        var pregunta = Texto("Pregunta", cSalir,
            "Vas a cerrar Parasite Invasion.\nEl progreso no guardado se perdera.",
            26, TextAnchor.UpperCenter, Crema);
        Situar(pregunta.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -190f),
               new Vector2(660f, 80f));

        var bSi = Boton("BotonSi", cSalir, "SI, SALIR", "ic_salir", false, 26);
        Situar(bSi.gameObject, new Vector2(0.5f, 0f), new Vector2(-165f, 30f),
               new Vector2(310f, 66f));

        var bNo = Boton("BotonNo", cSalir, "CANCELAR", "ic_atras", true, 26);
        Situar(bNo.gameObject, new Vector2(0.5f, 0f), new Vector2(165f, 30f),
               new Vector2(310f, 66f));

        cuadroSalir.primerElemento = bNo;

        // ---- Cableado ----
        var control = canvas.gameObject.AddComponent<MenuPrincipalUI>();
        control.botonJugar = botones[0];
        control.botonOpciones = botones[1];
        control.botonComoJugar = botones[2];
        control.botonCreditos = botones[3];
        control.botonSalir = botones[4];
        control.cuadroNivel = cuadroNivel;
        control.cuadroSalir = cuadroSalir;
        control.botonesNivel = botonesNivel;
        control.botonComenzar = bComenzar;
        control.botonCancelarNivel = bCancelar;
        control.etiquetaNivel = descripcion;
        control.botonSalirSi = bSi;
        control.botonSalirNo = bNo;
        control.etiquetaVersion = version;

        GuardarEscena(escena, Navegador.MenuPrincipal);
    }

    // -------------------------------------------------------------------
    // Pantalla 2: opciones
    // -------------------------------------------------------------------

    /// <summary>Fila de deslizador con etiqueta, control y porcentaje.</summary>
    private static Slider FilaDeslizador(Transform padre, float y, string etiqueta,
                                         string icono, float valor, out Text cifra)
    {
        var fila = Nodo("Fila" + etiqueta, padre);
        Situar(fila, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(920f, 56f));

        var ic = Imagen("Icono", fila.transform, Sp(icono), Cian);
        Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(6f, 0f),
               new Vector2(34f, 34f));

        var lbl = Texto("Etiqueta", fila.transform, etiqueta, 26,
                        TextAnchor.MiddleLeft, Crema);
        Situar(lbl.gameObject, new Vector2(0f, 0.5f), new Vector2(52f, 0f),
               new Vector2(300f, 40f));

        var go = DefaultControls.CreateSlider(RecursosBase());
        go.name = "Deslizador";
        go.transform.SetParent(fila.transform, false);
        Situar(go, new Vector2(0f, 0.5f), new Vector2(370f, 0f), new Vector2(440f, 26f));

        var s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = valor;

        // Se sustituye el aspecto por omision por los sprites del proyecto.
        var fondo = go.transform.Find("Background").GetComponent<Image>();
        fondo.sprite = Sp("barra_fondo");
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 1f;
        fondo.color = Color.white;

        var relleno = go.transform.Find("Fill Area/Fill").GetComponent<Image>();
        relleno.sprite = Sp("barra_oxigeno");
        relleno.type = Image.Type.Sliced;
        relleno.pixelsPerUnitMultiplier = 1f;
        relleno.color = Color.white;

        var perilla = go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
        perilla.sprite = Sp("perilla_deslizador");
        perilla.color = Color.white;
        Rt(perilla.gameObject).sizeDelta = new Vector2(30f, 30f);

        cifra = Texto("Valor", fila.transform, Mathf.RoundToInt(valor * 100f) + " %",
                      26, TextAnchor.MiddleRight, Cian, FontStyle.Bold);
        Situar(cifra.gameObject, new Vector2(1f, 0.5f), new Vector2(-6f, 0f),
               new Vector2(140f, 40f));

        return s;
    }

    private static Dropdown FilaLista(Transform padre, float y, string etiqueta,
                                      string icono, string[] opciones, int valor)
    {
        var fila = Nodo("Fila" + etiqueta, padre);
        Situar(fila, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(920f, 56f));

        var ic = Imagen("Icono", fila.transform, Sp(icono), Cian);
        Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(6f, 0f),
               new Vector2(34f, 34f));

        var lbl = Texto("Etiqueta", fila.transform, etiqueta, 26,
                        TextAnchor.MiddleLeft, Crema);
        Situar(lbl.gameObject, new Vector2(0f, 0.5f), new Vector2(52f, 0f),
               new Vector2(300f, 40f));

        var go = DefaultControls.CreateDropdown(RecursosBase());
        go.name = "Lista";
        go.transform.SetParent(fila.transform, false);
        Situar(go, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(320f, 52f));

        var d = go.GetComponent<Dropdown>();
        d.ClearOptions();
        d.AddOptions(new List<string>(opciones));
        d.value = valor;
        d.RefreshShownValue();

        var fondo = go.GetComponent<Image>();
        fondo.sprite = Sp("boton_normal");
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 1f;
        fondo.color = Color.white;

        foreach (var t in go.GetComponentsInChildren<Text>(true))
        {
            t.font = fuente;
            t.fontSize = 24;
            t.color = Crema;
        }

        var flecha = go.transform.Find("Arrow").GetComponent<Image>();
        flecha.color = Cian;

        // La lista desplegada se construye clonando esta plantilla, de modo que
        // hay que oscurecerla entera: si se deja el aspecto por omision, sus
        // fondos blancos dejan ilegible el texto claro de la interfaz.
        var plantillaRt = (RectTransform)go.transform.Find("Template");
        plantillaRt.sizeDelta = new Vector2(plantillaRt.sizeDelta.x,
                                            opciones.Length * 46f + 10f);

        var plantilla = plantillaRt.GetComponent<Image>();
        plantilla.sprite = Sp("panel_secundario");
        plantilla.type = Image.Type.Sliced;
        plantilla.pixelsPerUnitMultiplier = 1f;
        plantilla.color = Color.white;

        // El area de recorte hace de mascara, y la mascara se calcula con el
        // alfa de su imagen: dejarla transparente ocultaria toda la lista. Se
        // le da el mismo tono opaco que el interior de la plantilla.
        var vista = plantillaRt.Find("Viewport").GetComponent<Image>();
        vista.color = Col(14, 5, 9);

        var elemento = (RectTransform)plantillaRt.Find("Viewport/Content/Item");
        elemento.sizeDelta = new Vector2(elemento.sizeDelta.x, 46f);

        var contenido = (RectTransform)plantillaRt.Find("Viewport/Content");
        contenido.sizeDelta = new Vector2(contenido.sizeDelta.x, 46f);

        var fondoElemento = elemento.Find("Item Background").GetComponent<Image>();
        fondoElemento.sprite = Sp("boton_normal");
        fondoElemento.type = Image.Type.Sliced;
        fondoElemento.pixelsPerUnitMultiplier = 1f;
        fondoElemento.color = Color.white;

        var interruptor = elemento.GetComponent<Toggle>();
        var colores = interruptor.colors;
        colores.normalColor = new Color(0.14f, 0.05f, 0.09f, 1f);
        colores.highlightedColor = new Color(0.55f, 0.13f, 0.22f, 1f);
        colores.pressedColor = new Color(0.72f, 0.18f, 0.28f, 1f);
        colores.selectedColor = new Color(0.32f, 0.09f, 0.15f, 1f);
        interruptor.colors = colores;

        var marcaElemento = elemento.Find("Item Checkmark").GetComponent<Image>();
        marcaElemento.sprite = Sp("ic_check");
        marcaElemento.color = Cian;
        Situar(marcaElemento.gameObject, new Vector2(0f, 0.5f),
               new Vector2(12f, 0f), new Vector2(22f, 22f));

        var etiquetaElemento = elemento.Find("Item Label").GetComponent<Text>();
        etiquetaElemento.color = Crema;
        etiquetaElemento.fontSize = 22;
        etiquetaElemento.alignment = TextAnchor.MiddleLeft;
        var rtEtiqueta = Estirar(etiquetaElemento.gameObject);
        rtEtiqueta.offsetMin = new Vector2(44f, 0f);
        rtEtiqueta.offsetMax = new Vector2(-12f, 0f);

        return d;
    }

    private static Toggle FilaInterruptor(Transform padre, float y, string etiqueta,
                                          string icono, bool valor)
    {
        var fila = Nodo("Fila" + etiqueta, padre);
        Situar(fila, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(920f, 56f));

        var ic = Imagen("Icono", fila.transform, Sp(icono), Cian);
        Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(6f, 0f),
               new Vector2(34f, 34f));

        var lbl = Texto("Etiqueta", fila.transform, etiqueta, 26,
                        TextAnchor.MiddleLeft, Crema);
        Situar(lbl.gameObject, new Vector2(0f, 0.5f), new Vector2(52f, 0f),
               new Vector2(420f, 40f));

        var go = DefaultControls.CreateToggle(RecursosBase());
        go.name = "Interruptor";
        go.transform.SetParent(fila.transform, false);
        Situar(go, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(60f, 44f));

        var t = go.GetComponent<Toggle>();
        t.isOn = valor;

        var fondo = go.transform.Find("Background").GetComponent<Image>();
        fondo.sprite = Sp("boton_normal");
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 1f;
        fondo.color = Color.white;
        var rtFondo = Situar(fondo.gameObject, new Vector2(1f, 0.5f),
                             new Vector2(0f, 0f), new Vector2(52f, 44f));

        var marca = go.transform.Find("Background/Checkmark").GetComponent<Image>();
        marca.sprite = Sp("ic_check");
        marca.color = Cian;
        Situar(marca.gameObject, new Vector2(0.5f, 0.5f), Vector2.zero,
               new Vector2(30f, 30f));

        // La etiqueta que crea DefaultControls sobra: ya hay una a la izquierda.
        var etiquetaPorOmision = go.transform.Find("Label");
        if (etiquetaPorOmision != null) Object.DestroyImmediate(etiquetaPorOmision.gameObject);

        return t;
    }

    private static void ConstruirOpciones()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                 NewSceneMode.Single);
        CamaraDeInterfaz();
        var canvas = CrearLienzo("LienzoOpciones");
        var raiz = canvas.transform;

        FondoDeMenu(raiz, 0.45f);

        var titulo = Texto("Titulo", raiz, "OPCIONES", 56, TextAnchor.UpperCenter,
                           Crema, FontStyle.Bold);
        Situar(titulo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -56f),
               new Vector2(900f, 70f));

        var subtitulo = Texto("Subtitulo", raiz,
                              "Ajusta el audio, la calidad grafica y la dificultad",
                              24, TextAnchor.UpperCenter, Apagado);
        Situar(subtitulo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -126f),
               new Vector2(900f, 40f));

        var panel = Imagen("PanelOpciones", raiz, Sp("panel_principal"), Color.white,
                           Image.Type.Sliced);
        panel.raycastTarget = true;
        Situar(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f),
               new Vector2(1040f, 700f));

        // ---- Bloque de audio ----
        var rotuloAudio = Texto("RotuloAudio", panel.transform, "AUDIO", 24,
                                TextAnchor.MiddleLeft, Cian, FontStyle.Bold);
        Situar(rotuloAudio.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -30f),
               new Vector2(920f, 34f));

        Text vGeneral, vMusica, vEfectos;
        var sGeneral = FilaDeslizador(panel.transform, -76f, "Volumen general",
                                      "ic_sonido", 0.8f, out vGeneral);
        var sMusica = FilaDeslizador(panel.transform, -140f, "Musica",
                                     "ic_musica", 0.7f, out vMusica);
        var sEfectos = FilaDeslizador(panel.transform, -204f, "Efectos de sonido",
                                      "ic_disparo", 0.9f, out vEfectos);

        var separador = Imagen("Separador", panel.transform, null,
                               new Color(1f, 1f, 1f, 0.12f));
        Situar(separador.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -256f),
               new Vector2(920f, 2f));

        // ---- Bloque de video y juego ----
        var rotuloVideo = Texto("RotuloVideo", panel.transform, "VIDEO Y JUEGO", 24,
                                TextAnchor.MiddleLeft, Cian, FontStyle.Bold);
        Situar(rotuloVideo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -286f),
               new Vector2(920f, 34f));

        var lCalidad = FilaLista(panel.transform, -336f, "Calidad grafica",
                                 "ic_config", AjustesJuego.NombresCalidad, 1);
        var lDificultad = FilaLista(panel.transform, -400f, "Dificultad",
                                    "ic_parasito", AjustesJuego.NombresDificultad, 1);
        var iPantalla = FilaInterruptor(panel.transform, -464f, "Pantalla completa",
                                        "ic_nivel", true);
        var iVibracion = FilaInterruptor(panel.transform, -520f,
                                         "Vibracion del mando", "ic_escudo", true);

        // ---- Acciones ----
        var bAplicar = Boton("BotonAplicar", panel.transform, "APLICAR",
                             "ic_check", true, 26);
        Situar(bAplicar.gameObject, new Vector2(0.5f, 0f), new Vector2(-335f, 28f),
               new Vector2(320f, 66f));

        var bRestablecer = Boton("BotonRestablecer", panel.transform,
                                 "RESTABLECER", "ic_reiniciar", false, 26);
        Situar(bRestablecer.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 28f),
               new Vector2(320f, 66f));

        var bVolver = Boton("BotonVolver", panel.transform, "VOLVER", "ic_atras",
                            false, 26);
        Situar(bVolver.gameObject, new Vector2(0.5f, 0f), new Vector2(335f, 28f),
               new Vector2(320f, 66f));

        // ---- Cuadro emergente de confirmacion ----
        RectTransform cReset;
        var cuadroReset = CuadroEmergente("CuadroRestablecer", raiz,
                                          new Vector2(760f, 360f),
                                          "RESTABLECER AJUSTES", "ic_advertencia",
                                          out cReset);

        var icReset = Imagen("IconoAviso", cReset, Sp("ic_reiniciar"), Ambar);
        Situar(icReset.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -110f),
               new Vector2(64f, 64f));

        var textoReset = Texto("Pregunta", cReset,
            "Se devolveran el audio, la calidad y la dificultad\na sus valores de fabrica.",
            26, TextAnchor.UpperCenter, Crema);
        Situar(textoReset.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -190f),
               new Vector2(660f, 80f));

        var bResetSi = Boton("BotonResetSi", cReset, "RESTABLECER", "ic_check",
                             false, 26);
        Situar(bResetSi.gameObject, new Vector2(0.5f, 0f), new Vector2(-165f, 30f),
               new Vector2(310f, 66f));

        var bResetNo = Boton("BotonResetNo", cReset, "CANCELAR", "ic_atras",
                             true, 26);
        Situar(bResetNo.gameObject, new Vector2(0.5f, 0f), new Vector2(165f, 30f),
               new Vector2(310f, 66f));

        cuadroReset.primerElemento = bResetNo;

        var aviso = Aviso(raiz, new Vector2(0.5f, 0f), new Vector2(0f, 40f));

        // ---- Cableado ----
        var control = canvas.gameObject.AddComponent<OpcionesUI>();
        control.deslizadorGeneral = sGeneral;
        control.deslizadorMusica = sMusica;
        control.deslizadorEfectos = sEfectos;
        control.valorGeneral = vGeneral;
        control.valorMusica = vMusica;
        control.valorEfectos = vEfectos;
        control.listaCalidad = lCalidad;
        control.listaDificultad = lDificultad;
        control.interruptorPantallaCompleta = iPantalla;
        control.interruptorVibracion = iVibracion;
        control.botonAplicar = bAplicar;
        control.botonRestablecer = bRestablecer;
        control.botonVolver = bVolver;
        control.cuadroRestablecer = cuadroReset;
        control.botonRestablecerSi = bResetSi;
        control.botonRestablecerNo = bResetNo;
        control.aviso = aviso;

        GuardarEscena(escena, Navegador.Opciones);
    }

    // -------------------------------------------------------------------
    // Pantalla 3: como jugar
    // -------------------------------------------------------------------

    private static void ConstruirComoJugar()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                 NewSceneMode.Single);
        CamaraDeInterfaz();
        var canvas = CrearLienzo("LienzoComoJugar");
        var raiz = canvas.transform;

        FondoDeMenu(raiz, 0.45f);

        var titulo = Texto("Titulo", raiz, "COMO JUGAR", 56, TextAnchor.UpperCenter,
                           Crema, FontStyle.Bold);
        Situar(titulo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -56f),
               new Vector2(900f, 70f));

        var panel = Imagen("PanelAyuda", raiz, Sp("panel_principal"), Color.white,
                           Image.Type.Sliced);
        panel.raycastTarget = true;
        Situar(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f),
               new Vector2(1280f, 700f));

        var objetivo = Texto("Objetivo", panel.transform,
            "Pilotas al <b>Globulo Rojo A1</b> por el interior del torrente sanguineo. " +
            "Elimina los parasitos, recoge capsulas de medicina y alcanza la camara " +
            "cardiaca antes de quedarte sin oxigeno.",
            26, TextAnchor.UpperLeft, Crema);
        Situar(objetivo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -34f),
               new Vector2(1180f, 100f));

        var rotulo = Texto("RotuloControles", panel.transform, "CONTROLES", 24,
                           TextAnchor.MiddleLeft, Cian, FontStyle.Bold);
        Situar(rotulo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -150f),
               new Vector2(1180f, 34f));

        string[,] controles =
        {
            { "Flechas", "Desplazar la nave por el conducto", "ic_nave" },
            { "Re Pag / Av Pag", "Subir y bajar dentro de la vena", "ic_oxigeno" },
            { "D", "Disparar contra los parasitos", "ic_disparo" },
            { "S", "Saltar / esquivar un tentaculo", "ic_escudo" },
            { "B", "Aplicar una dosis de medicina", "ic_dosis" },
            { "P o Esc", "Abrir el menu de pausa", "ic_pausa" },
        };

        for (int i = 0; i < controles.GetLength(0); i++)
        {
            float y = -196f - i * 68f;

            var fila = Imagen("Fila" + i, panel.transform, Sp("panel_secundario"),
                              new Color(1f, 1f, 1f, i % 2 == 0 ? 0.5f : 0.25f),
                              Image.Type.Sliced);
            Situar(fila.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, y),
                   new Vector2(1180f, 60f));

            var ic = Imagen("Icono", fila.transform, Sp(controles[i, 2]), Cian);
            Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(20f, 0f),
                   new Vector2(34f, 34f));

            var tecla = Texto("Tecla", fila.transform, controles[i, 0], 26,
                              TextAnchor.MiddleLeft, Ambar, FontStyle.Bold);
            Situar(tecla.gameObject, new Vector2(0f, 0.5f), new Vector2(70f, 0f),
                   new Vector2(300f, 40f));

            var accion = Texto("Accion", fila.transform, controles[i, 1], 26,
                               TextAnchor.MiddleLeft, Crema);
            Situar(accion.gameObject, new Vector2(0f, 0.5f), new Vector2(380f, 0f),
                   new Vector2(760f, 40f));
        }

        var nota = Texto("Nota", panel.transform,
            "En el prototipo las teclas X, C, V y N simulan dano, puntaje, recogida " +
            "de dosis y recarga de oxigeno para demostrar los indicadores del HUD.",
            22, TextAnchor.UpperCenter, Apagado);
        Situar(nota.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 108f),
               new Vector2(1180f, 60f));

        var bVolver = Boton("BotonVolver", panel.transform, "VOLVER AL MENU",
                            "ic_atras", true);
        Situar(bVolver.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 28f),
               new Vector2(360f, 66f));

        var enlace = canvas.gameObject.AddComponent<PantallaSimple>();
        enlace.botonVolver = bVolver;

        GuardarEscena(escena, Navegador.ComoJugar);
    }

    // -------------------------------------------------------------------
    // Pantalla 4: creditos
    // -------------------------------------------------------------------

    private static void ConstruirCreditos()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                 NewSceneMode.Single);
        CamaraDeInterfaz();
        var canvas = CrearLienzo("LienzoCreditos");
        var raiz = canvas.transform;

        FondoDeMenu(raiz, 0.45f);

        var titulo = Texto("Titulo", raiz, "CREDITOS", 56, TextAnchor.UpperCenter,
                           Crema, FontStyle.Bold);
        Situar(titulo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -56f),
               new Vector2(900f, 70f));

        var panel = Imagen("PanelCreditos", raiz, Sp("panel_principal"), Color.white,
                           Image.Type.Sliced);
        panel.raycastTarget = true;
        Situar(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f),
               new Vector2(1120f, 700f));

        // Vista con desplazamiento: los creditos crecen mas que el panel.
        var scroll = DefaultControls.CreateScrollView(RecursosBase());
        scroll.name = "Desplazamiento";
        scroll.transform.SetParent(panel.transform, false);
        Situar(scroll, new Vector2(0.5f, 1f), new Vector2(0f, -28f),
               new Vector2(1040f, 540f));

        var sr = scroll.GetComponent<ScrollRect>();
        sr.horizontal = false;
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        var barra = scroll.transform.Find("Scrollbar Horizontal");
        if (barra != null) Object.DestroyImmediate(barra.gameObject);

        var barraV = scroll.transform.Find("Scrollbar Vertical");
        if (barraV != null)
        {
            var img = barraV.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);
            var deslizador = barraV.Find("Sliding Area/Handle").GetComponent<Image>();
            deslizador.color = Rojo;
        }

        var contenido = sr.content;
        contenido.sizeDelta = new Vector2(0f, 1020f);

        string texto =
            "<b>PARASITE INVASION</b>\n" +
            "Prototipo academico  -  version 0.3\n\n" +
            "<b>PROGRAMA DE FORMACION</b>\n" +
            "SENA  -  Desarrollo de Videojuegos y Entornos Interactivos\n" +
            "Ficha 3466174  -  Centro de Comercio y Servicio, Regional Tolima\n" +
            "Instructor: Andres Mosquera Copete\n" +
            "Evidencia GA2-220501087-AA2-EV01\n\n" +
            "<b>DISENO Y PROGRAMACION</b>\n" +
            "Carlos Botina\n\n" +
            "<b>MODELADO Y ANIMACION</b>\n" +
            "Piloto Globulo Rojo A1, Conducto Vena, Tentaculo Parasitario,\n" +
            "Capsula de Medicina, Dosis Maxima y Vida Extra\n" +
            "Modelados y animados para el proyecto\n\n" +
            "<b>RECURSOS DE TERCEROS</b>\n" +
            "Unity Asset Store  -  Dynamic Vertical Fog\n" +
            "Unity Asset Store  -  Power Up Particles\n" +
            "Unity UI Skin  -  sprites base de controles\n\n" +
            "<b>INTERFAZ GRAFICA</b>\n" +
            "Paneles, botones, barras, iconos, fondo y logotipo\n" +
            "generados para este prototipo\n\n" +
            "<b>SONIDO</b>\n" +
            "Efectos de disparo, dano, salto y desplazamiento\n" +
            "producidos para el proyecto\n\n" +
            "<b>MOTOR</b>\n" +
            "Unity 6000.2.7f1  -  Windows\n\n" +
            "Gracias por probar el prototipo.";

        var cuerpo = Texto("Texto", contenido, texto, 26, TextAnchor.UpperCenter, Crema);
        var rtCuerpo = Rt(cuerpo.gameObject);
        rtCuerpo.anchorMin = new Vector2(0f, 1f);
        rtCuerpo.anchorMax = new Vector2(1f, 1f);
        rtCuerpo.pivot = new Vector2(0.5f, 1f);
        rtCuerpo.anchoredPosition = new Vector2(0f, -20f);
        rtCuerpo.sizeDelta = new Vector2(-60f, 980f);
        cuerpo.lineSpacing = 1.15f;

        var bVolver = Boton("BotonVolver", panel.transform, "VOLVER AL MENU",
                            "ic_atras", true);
        Situar(bVolver.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 28f),
               new Vector2(360f, 66f));

        var enlace = canvas.gameObject.AddComponent<PantallaSimple>();
        enlace.botonVolver = bVolver;

        GuardarEscena(escena, Navegador.Creditos);
    }

    // -------------------------------------------------------------------
    // Pantalla 5: juego con HUD
    // -------------------------------------------------------------------

    private static void ConstruirJuego()
    {
        // Se parte de la escena de animaciones ya existente para que el HUD se
        // superponga al escenario real del videojuego y no a un fondo plano.
        var escena = EditorSceneManager.OpenScene("Assets/EscenaAnimaciones.unity",
                                                  OpenSceneMode.Single);

        if (Camera.main == null) CamaraDeInterfaz();

        // La camara de la escena de animaciones observaba el tunel desde lejos.
        // Se acerca al piloto para que el HUD se lea sobre una imagen de juego
        // reconocible y no sobre un fondo vacio.
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(-9.5f, 1.3f, 0.6f);
            Camera.main.transform.rotation = Quaternion.Euler(5f, 90f, 0f);
        }

        var canvas = CrearLienzo("LienzoHUD");
        var raiz = canvas.transform;

        // ---- Tinte de dano: cubre la pantalla y se enciende al recibir golpes ----
        var tinte = Imagen("TinteDano", raiz, null, new Color(0.85f, 0.12f, 0.2f, 0f));
        Estirar(tinte.gameObject);

        // ---- Bloque superior izquierdo: vida y oxigeno ----
        var bloqueIzq = Imagen("BloqueEstado", raiz, Sp("panel_hud"), Color.white,
                               Image.Type.Sliced);
        Situar(bloqueIzq.gameObject, new Vector2(0f, 1f), new Vector2(32f, -32f),
               new Vector2(460f, 152f));

        Text tVida, tOxigeno;
        var barraVida = Barra("BarraVida", bloqueIzq.transform, new Vector2(0f, 1f),
                              new Vector2(20f, -20f), new Vector2(420f, 34f),
                              "ic_vida", "barra_vida", Rojo, out tVida);
        var barraOxigeno = Barra("BarraOxigeno", bloqueIzq.transform,
                                 new Vector2(0f, 1f), new Vector2(20f, -64f),
                                 new Vector2(420f, 34f), "ic_oxigeno",
                                 "barra_oxigeno", Cian, out tOxigeno);
        tOxigeno.text = "74 %";

        var etiquetaPiloto = Texto("Piloto", bloqueIzq.transform,
                                   "PILOTO GLOBULO ROJO A1", 18,
                                   TextAnchor.LowerLeft, Apagado, FontStyle.Bold);
        Situar(etiquetaPiloto.gameObject, new Vector2(0f, 0f), new Vector2(22f, 12f),
               new Vector2(420f, 24f));

        // ---- Bloque superior derecho: contadores ----
        var bloqueDer = Imagen("BloqueContadores", raiz, Sp("panel_hud"), Color.white,
                               Image.Type.Sliced);
        Situar(bloqueDer.gameObject, new Vector2(1f, 1f), new Vector2(-32f, -32f),
               new Vector2(420f, 132f));

        Text tPuntaje = ContadorHud(bloqueDer.transform, new Vector2(20f, -22f),
                                    "ic_puntaje", "PUNTAJE", "01450", Ambar);
        Text tDosis = ContadorHud(bloqueDer.transform, new Vector2(20f, -76f),
                                  "ic_dosis", "DOSIS", "x 6", Cian);
        Text tTiempo = ContadorHud(bloqueDer.transform, new Vector2(230f, -22f),
                                   "ic_tiempo", "TIEMPO", "2:35", Crema);
        Text tAmenazas = ContadorHud(bloqueDer.transform, new Vector2(230f, -76f),
                                     "ic_parasito", "AMENAZAS", "08", Rojo);

        // ---- Nivel y boton de pausa ----
        var tNivel = Texto("Nivel", raiz, "NIVEL 1  -  Normal", 26,
                           TextAnchor.UpperCenter, Crema, FontStyle.Bold);
        Situar(tNivel.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -40f),
               new Vector2(560f, 36f));

        var bPausa = Boton("BotonPausa", raiz, "", "ic_pausa");
        Situar(bPausa.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -88f),
               new Vector2(66f, 60f));
        // Sin etiqueta el icono deja de ir a la izquierda y pasa al centro.
        Situar(bPausa.transform.Find("Icono").gameObject,
               new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));

        // ---- Retícula central ----
        var reticula = Imagen("Reticula", raiz, Sp("ic_disparo"),
                              new Color(1f, 1f, 1f, 0.35f));
        Situar(reticula.gameObject, new Vector2(0.5f, 0.5f), Vector2.zero,
               new Vector2(40f, 40f));

        // ---- Marco decorativo en las esquinas ----
        EsquinaHud(raiz, new Vector2(0f, 1f), new Vector2(76f, -76f), 0f);
        EsquinaHud(raiz, new Vector2(1f, 1f), new Vector2(-76f, -76f), -90f);
        EsquinaHud(raiz, new Vector2(1f, 0f), new Vector2(-76f, 76f), 180f);
        EsquinaHud(raiz, new Vector2(0f, 0f), new Vector2(76f, 76f), 90f);

        // ---- Recordatorio de controles ----
        var ayuda = Texto("Ayuda", raiz,
            "Flechas mover    D disparar    B usar dosis    P pausa",
            22, TextAnchor.LowerCenter, Apagado);
        Situar(ayuda.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 26f),
               new Vector2(1000f, 32f));

        var aviso = Aviso(raiz, new Vector2(0.5f, 0f), new Vector2(0f, 80f));

        // ---- Cuadro emergente: pausa ----
        RectTransform cPausa;
        var cuadroPausa = CuadroEmergente("CuadroPausa", raiz,
                                          new Vector2(680f, 620f), "JUEGO EN PAUSA",
                                          "ic_pausa", out cPausa, true, false);

        string[,] opcionesPausa =
        {
            { "BotonReanudar", "REANUDAR", "ic_play" },
            { "BotonReiniciar", "REINICIAR NIVEL", "ic_reiniciar" },
            { "BotonOpciones", "OPCIONES", "ic_config" },
            { "BotonAbandonar", "ABANDONAR PARTIDA", "ic_salir" },
        };

        var botonesPausa = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            botonesPausa[i] = Boton(opcionesPausa[i, 0], cPausa, opcionesPausa[i, 1],
                                    opcionesPausa[i, 2], i == 0);
            Situar(botonesPausa[i].gameObject, new Vector2(0.5f, 1f),
                   new Vector2(0f, -120f - i * 86f), new Vector2(560f, 72f));
        }

        var resumenPausa = Texto("Resumen", cPausa,
            "Puntaje 01450    Dosis 6    Tiempo 2:35", 24,
            TextAnchor.LowerCenter, Apagado);
        Situar(resumenPausa.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 34f),
               new Vector2(600f, 40f));

        cuadroPausa.primerElemento = botonesPausa[0];

        // ---- Cuadro emergente: confirmar abandono ----
        RectTransform cAbandono;
        var cuadroAbandono = CuadroEmergente("CuadroAbandono", raiz,
                                             new Vector2(800f, 360f),
                                             "ABANDONAR PARTIDA", "ic_advertencia",
                                             out cAbandono, true, false);

        var icAband = Imagen("IconoAviso", cAbandono, Sp("ic_advertencia"), Ambar);
        Situar(icAband.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -110f),
               new Vector2(64f, 64f));

        var textoAband = Texto("Pregunta", cAbandono,
            "Perderas el avance del nivel actual.\n¿Deseas volver al menu principal?",
            26, TextAnchor.UpperCenter, Crema);
        Situar(textoAband.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -190f),
               new Vector2(660f, 80f));

        var bAbandSi = Boton("BotonSi", cAbandono, "SI, ABANDONAR", "ic_salir",
                             false, 26);
        Situar(bAbandSi.gameObject, new Vector2(0.5f, 0f), new Vector2(-170f, 30f),
               new Vector2(320f, 66f));

        var bAbandNo = Boton("BotonNo", cAbandono, "SEGUIR JUGANDO", "ic_play",
                             true, 26);
        Situar(bAbandNo.gameObject, new Vector2(0.5f, 0f), new Vector2(170f, 30f),
               new Vector2(320f, 66f));

        cuadroAbandono.primerElemento = bAbandNo;

        // ---- Cuadro emergente: fin de partida ----
        RectTransform cDerrota;
        var cuadroDerrota = CuadroEmergente("CuadroDerrota", raiz,
                                            new Vector2(820f, 480f),
                                            "FIN DE LA PARTIDA", "ic_parasito",
                                            out cDerrota, true, false);

        var icDerrota = Imagen("Icono", cDerrota, Sp("ic_parasito"), Rojo);
        Situar(icDerrota.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -116f),
               new Vector2(72f, 72f));

        var resumenDerrota = Texto("Resumen", cDerrota,
            "El piloto no resistio la infeccion.\n\nPuntaje alcanzado: 01450\nDosis sin usar: 2",
            26, TextAnchor.UpperCenter, Crema);
        Situar(resumenDerrota.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -204f),
               new Vector2(720f, 160f));

        var bReintentar = Boton("BotonReintentar", cDerrota, "REINTENTAR",
                                "ic_reiniciar", true, 26);
        Situar(bReintentar.gameObject, new Vector2(0.5f, 0f), new Vector2(-175f, 30f),
               new Vector2(330f, 66f));

        var bSalirDerrota = Boton("BotonSalir", cDerrota, "MENU PRINCIPAL",
                                  "ic_atras", false, 26);
        Situar(bSalirDerrota.gameObject, new Vector2(0.5f, 0f), new Vector2(175f, 30f),
               new Vector2(330f, 66f));

        cuadroDerrota.primerElemento = bReintentar;

        // ---- Cuadro emergente: nivel completado ----
        RectTransform cVictoria;
        var cuadroVictoria = CuadroEmergente("CuadroVictoria", raiz,
                                             new Vector2(820f, 480f),
                                             "NIVEL COMPLETADO", "ic_check",
                                             out cVictoria, true, false);

        var icVictoria = Imagen("Icono", cVictoria, Sp("ic_check"), Cian);
        Situar(icVictoria.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -116f),
               new Vector2(72f, 72f));

        var resumenVictoria = Texto("Resumen", cVictoria,
            "Conducto despejado.\n\nPuntaje alcanzado: 03200\nDosis sin usar: 4",
            26, TextAnchor.UpperCenter, Crema);
        Situar(resumenVictoria.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -204f),
               new Vector2(720f, 160f));

        var bSiguiente = Boton("BotonSiguiente", cVictoria, "SIGUIENTE NIVEL",
                               "ic_play", true, 26);
        Situar(bSiguiente.gameObject, new Vector2(0.5f, 0f), new Vector2(-175f, 30f),
               new Vector2(330f, 66f));

        var bSalirVictoria = Boton("BotonSalir", cVictoria, "MENU PRINCIPAL",
                                   "ic_atras", false, 26);
        Situar(bSalirVictoria.gameObject, new Vector2(0.5f, 0f), new Vector2(175f, 30f),
               new Vector2(330f, 66f));

        cuadroVictoria.primerElemento = bSiguiente;

        // ---- Cableado ----
        var hud = canvas.gameObject.AddComponent<HUDJuego>();
        hud.barraVida = barraVida;
        hud.barraOxigeno = barraOxigeno;
        hud.textoVida = tVida;
        hud.textoOxigeno = tOxigeno;
        hud.textoDosis = tDosis;
        hud.textoPuntaje = tPuntaje;
        hud.textoTiempo = tTiempo;
        hud.textoNivel = tNivel;
        hud.textoAmenazas = tAmenazas;
        hud.aviso = aviso;
        hud.tinteDano = tinte;

        var control = canvas.gameObject.AddComponent<ControlPartida>();
        control.hud = hud;
        control.cuadroPausa = cuadroPausa;
        control.botonPausa = bPausa;
        control.botonReanudar = botonesPausa[0];
        control.botonReiniciarPausa = botonesPausa[1];
        control.botonOpcionesPausa = botonesPausa[2];
        control.botonAbandonar = botonesPausa[3];
        control.cuadroAbandono = cuadroAbandono;
        control.botonAbandonoSi = bAbandSi;
        control.botonAbandonoNo = bAbandNo;
        control.cuadroDerrota = cuadroDerrota;
        control.resumenDerrota = resumenDerrota;
        control.botonReintentar = bReintentar;
        control.botonSalirDerrota = bSalirDerrota;
        control.cuadroVictoria = cuadroVictoria;
        control.resumenVictoria = resumenVictoria;
        control.botonSiguiente = bSiguiente;
        control.botonSalirVictoria = bSalirVictoria;

        // Se guarda como escena nueva: la escena de animaciones original
        // permanece intacta como evidencia de la actividad anterior.
        GuardarEscena(escena, Navegador.Juego);
    }

    /// <summary>Contador del HUD: icono, rotulo pequeno y cifra grande.</summary>
    private static Text ContadorHud(Transform padre, Vector2 pos, string icono,
                                    string rotulo, string valor, Color color)
    {
        var raiz = Nodo("Contador" + rotulo, padre);
        Situar(raiz, new Vector2(0f, 1f), pos, new Vector2(190f, 48f));

        var ic = Imagen("Icono", raiz.transform, Sp(icono), color);
        Situar(ic.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0f),
               new Vector2(34f, 34f));

        var rot = Texto("Rotulo", raiz.transform, rotulo, 16, TextAnchor.UpperLeft,
                        Apagado, FontStyle.Bold);
        Situar(rot.gameObject, new Vector2(0f, 1f), new Vector2(44f, -2f),
               new Vector2(140f, 20f));

        var cifra = Texto("Valor", raiz.transform, valor, 28, TextAnchor.LowerLeft,
                          Crema, FontStyle.Bold);
        Situar(cifra.gameObject, new Vector2(0f, 0f), new Vector2(44f, 0f),
               new Vector2(140f, 30f));

        return cifra;
    }

    private static void EsquinaHud(Transform padre, Vector2 ancla, Vector2 pos,
                                   float giro)
    {
        var ic = Imagen("Esquina", padre, Sp("marco_hud"), new Color(1f, 1f, 1f, 0.6f));
        var rt = Rt(ic.gameObject);
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120f, 120f);
        rt.localRotation = Quaternion.Euler(0f, 0f, giro);
    }
}
