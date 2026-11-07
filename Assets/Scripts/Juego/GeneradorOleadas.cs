using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reglas del Nivel 1 (Vena Principal).
///
/// Lanza los parasitos por oleadas por delante del piloto, lleva la cuenta
/// de amenazas en el HUD y, cuando no queda ninguna, abre el paso a la
/// camara cardiaca un poco mas adelante. El nivel se completa al cruzar ese
/// anillo de luz.
/// </summary>
public class GeneradorOleadas : MonoBehaviour
{
    [Serializable]
    public class Grupo
    {
        public Parasito prefab;
        public int cantidad = 3;
    }

    [Serializable]
    public class Oleada
    {
        public string nombre = "Oleada";
        public List<Grupo> grupos = new List<Grupo>();

        [Tooltip("Segundos entre la aparicion de dos parasitos")]
        public float intervalo = 1.4f;
    }

    [Header("Referencias")]
    public HUDJuego hud;
    public ControlJugador jugador;

    [Tooltip("Anillo de salida hacia la camara cardiaca; arranca apagado")]
    public GameObject salida;

    [Header("Oleadas")]
    public List<Oleada> oleadas = new List<Oleada>();

    [Tooltip("Pausa antes de la primera oleada y entre oleadas")]
    public float descanso = 3f;

    [Header("Zona de aparicion")]
    [Tooltip("En la vena en anillo: distancia por delante del piloto. En el " +
             "conducto recto: margen por delante del limite frontal.")]
    public float margenAparicion = 26f;

    [Tooltip("Distancia por delante del piloto a la que se abre la salida")]
    public float distanciaSalida = 22f;

    [Tooltip("Fraccion del radio del conducto donde pueden aparecer")]
    [Range(0.1f, 1f)] public float dispersion = 0.75f;

    private readonly List<Parasito> vivos = new List<Parasito>();
    private bool salidaAbierta;
    private float metaSalida;

    /// <summary>Multiplicador de velocidad y dano segun la dificultad elegida.</summary>
    public static float Dificultad
    {
        get
        {
            float[] valores = { 0.75f, 1f, 1.3f };
            return valores[Mathf.Clamp(AjustesJuego.Dificultad, 0, 2)];
        }
    }

    public int TotalParasitos
    {
        get
        {
            int total = 0;
            foreach (var o in oleadas)
                foreach (var g in o.grupos)
                    if (g.prefab != null) total += g.cantidad;
            return total;
        }
    }

    void Start()
    {
        if (salida != null) salida.SetActive(false);
        StartCoroutine(Partida());
    }

    private IEnumerator Partida()
    {
        // Se espera un fotograma para que el HUD termine su propio Start.
        yield return null;
        if (hud != null) hud.ConfigurarAmenazas(TotalParasitos);

        for (int i = 0; i < oleadas.Count; i++)
        {
            yield return new WaitForSeconds(descanso);

            if (hud != null)
                hud.Avisar("OLEADA " + (i + 1) + " DE " + oleadas.Count +
                           "  -  " + oleadas[i].nombre);

            yield return StartCoroutine(Lanzar(oleadas[i]));

            // La siguiente oleada no llega hasta limpiar la actual.
            while (vivos.Count > 0) yield return null;
        }

        AbrirSalida();
    }

    private IEnumerator Lanzar(Oleada oleada)
    {
        // Se intercalan los grupos para que la oleada llegue mezclada.
        var cola = new List<Parasito>();
        int mayor = 0;
        foreach (var g in oleada.grupos) mayor = Mathf.Max(mayor, g.cantidad);
        for (int n = 0; n < mayor; n++)
            foreach (var g in oleada.grupos)
                if (g.prefab != null && n < g.cantidad) cola.Add(g.prefab);

        foreach (var prefab in cola)
        {
            if (hud != null && hud.Terminada) yield break;
            Aparecer(prefab);
            yield return new WaitForSeconds(oleada.intervalo / Dificultad);
        }
    }

    private void Aparecer(Parasito prefab)
    {
        float radio = jugador != null ? jugador.radioConducto : 1.8f;
        float x;
        if (PistaAnillo.Actual != null)
            x = (jugador != null ? jugador.PosicionPista.x : 0f) + margenAparicion;
        else
            x = (jugador != null ? jugador.limiteFrontal : 13f) + margenAparicion;

        Vector2 disco = UnityEngine.Random.insideUnitCircle * radio * dispersion;
        Vector3 punto = PistaAnillo.Mundo(new Vector3(x, disco.x, disco.y));

        Parasito p = Instantiate(prefab, punto,
                                 Quaternion.LookRotation(-PistaAnillo.Avance(x)));
        p.Preparar(jugador, hud, radio, Dificultad);
        p.AlMorir += Retirar;
        vivos.Add(p);
    }

    private void Retirar(Parasito p)
    {
        vivos.Remove(p);
    }

    private void AbrirSalida()
    {
        salidaAbierta = true;
        if (salida != null)
        {
            // En el anillo la salida aparece delante del piloto, perpendicular
            // a la vena. El disco del anillo de luz mira hacia su eje X local.
            if (PistaAnillo.Actual != null && jugador != null)
            {
                float s = jugador.PosicionPista.x + distanciaSalida;
                salida.transform.position = PistaAnillo.Mundo(new Vector3(s, 0f, 0f));
                salida.transform.rotation = PistaAnillo.Orientacion(s) *
                                            Quaternion.Euler(0f, -90f, 0f);
                metaSalida = s;
            }
            salida.SetActive(true);
        }
        if (hud != null)
            hud.Avisar("Conducto despejado: cruza el anillo hacia la camara cardiaca");
    }

    void Update()
    {
        if (!salidaAbierta || jugador == null || hud == null || hud.Terminada) return;

        var anillo = PistaAnillo.Actual;
        if (anillo != null)
        {
            float faltan = anillo.Diferencia(jugador.PosicionPista.x, metaSalida);
            if (Mathf.Abs(faltan) < 0.8f) hud.CompletarNivel();
            return;
        }

        float meta = salida != null ? salida.transform.position.x : jugador.limiteFrontal;
        if (jugador.transform.position.x >= meta - 0.6f) hud.CompletarNivel();
    }
}
