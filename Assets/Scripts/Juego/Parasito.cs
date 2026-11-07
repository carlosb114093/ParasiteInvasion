using System;
using UnityEngine;

/// <summary>
/// Enemigo comun del torrente sanguineo.
///
/// Un mismo componente sirve para las tres variantes del nivel cambiando sus
/// valores en el prefab:
///   - Larva: lenta y resistente, embiste al piloto.
///   - Espora: rapida y fragil, llega en grupo.
///   - Escupidor: se queda a distancia y lanza proyectiles.
/// Al morir suma puntaje, libera algo de oxigeno y avisa al generador de
/// oleadas para que lleve la cuenta de amenazas.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Parasito : MonoBehaviour
{
    [Header("Resistencia y premio")]
    public float vida = 3f;
    public int puntos = 150;

    [Tooltip("Oxigeno que libera al ser eliminado")]
    public float oxigenoAlMorir = 6f;

    [Header("Movimiento")]
    public float velocidad = 1.6f;

    [Tooltip("Amplitud del vaiven lateral mientras avanza")]
    public float vaiven = 0.6f;

    [Tooltip("Distancia a la que se detiene; cero para embestir")]
    public float distanciaPreferida = 0f;

    [Header("Ataque")]
    public float danoContacto = 12f;

    [Tooltip("Proyectil que escupe; vacio si solo embiste")]
    public Proyectil prefabProyectil;

    public float cadenciaDisparo = 2.6f;
    public float danoDisparo = 8f;

    [Header("Aspecto")]
    [Tooltip("Pieza que late; si esta vacia late el objeto entero")]
    public Transform cuerpo;

    public Color colorEstallido = new Color(0.6f, 1f, 0.3f);
    public Estallido efectoMuerte;

    /// <summary>Se dispara una sola vez cuando el parasito es eliminado.</summary>
    public event Action<Parasito> AlMorir;

    public bool Muerto { get; private set; }

    private ControlJugador jugador;
    private HUDJuego hud;
    private float radioConducto = 1.8f;
    private float multiplicador = 1f;
    private float fase;
    private float esperaDisparo;
    private float retroceso;
    private float golpeVisual;
    private Vector3 escalaCuerpo;
    private Vector3 pista;
    private float sentidoRetroceso = 1f;

    /// <summary>Lo llama el generador al crear el parasito.</summary>
    public void Preparar(ControlJugador objetivo, HUDJuego indicadores,
                         float radio, float dificultad)
    {
        jugador = objetivo;
        hud = indicadores;
        radioConducto = radio;
        multiplicador = dificultad;
        pista = PistaAnillo.Pista(transform.position);
    }

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (cuerpo == null) cuerpo = transform;
        escalaCuerpo = cuerpo.localScale;
        fase = UnityEngine.Random.value * 10f;
        esperaDisparo = cadenciaDisparo * UnityEngine.Random.Range(0.6f, 1.2f);
    }

    void Update()
    {
        if (Muerto || jugador == null) return;
        if (hud != null && hud.Terminada) return;

        fase += Time.deltaTime;
        Moverse();
        Escupir();
        Latir();
    }

    /// <summary>
    /// Persigue al piloto en coordenadas de pista, por el camino mas corto
    /// alrededor del anillo, con un vaiven para que no llegue en linea recta.
    /// </summary>
    private void Moverse()
    {
        Vector3 aJugador = PistaAnillo.Hacia(pista, jugador.CentroPista);
        float distancia = aJugador.magnitude;
        Vector3 rumbo = distancia > 0.01f ? aJugador / distancia : Vector3.left;

        float rapidez = velocidad * multiplicador;
        if (distanciaPreferida > 0f && distancia < distanciaPreferida) rapidez = 0f;

        Vector3 lado = Vector3.Cross(rumbo, Vector3.right);
        if (lado.sqrMagnitude < 0.01f) lado = Vector3.forward;
        lado = (lado.normalized * Mathf.Sin(fase * 2.2f) +
                Vector3.up * Mathf.Cos(fase * 1.7f) * 0.5f) * vaiven;

        Vector3 paso = (rumbo * rapidez + lado) * Time.deltaTime;

        // Tras golpear al piloto el parasito rebota, alejandose por la vena.
        if (retroceso > 0f)
        {
            paso = Vector3.right * sentidoRetroceso * 6f * Time.deltaTime;
            retroceso -= Time.deltaTime;
        }

        pista = PistaAnillo.ConfinarSeccion(pista + paso, radioConducto);
        var anillo = PistaAnillo.Actual;
        if (anillo != null) pista.x = Mathf.Repeat(pista.x, anillo.Circunferencia);
        transform.position = PistaAnillo.Mundo(pista);

        Vector3 mira = jugador.Centro - transform.position;
        if (mira.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(mira), 4f * Time.deltaTime);
    }

    private void Escupir()
    {
        if (prefabProyectil == null) return;

        esperaDisparo -= Time.deltaTime;
        if (esperaDisparo > 0f) return;
        esperaDisparo = cadenciaDisparo / multiplicador;

        Vector3 sentido = PistaAnillo.Hacia(pista, jugador.CentroPista).normalized;
        Proyectil bala = Instantiate(prefabProyectil,
                                     PistaAnillo.Mundo(pista + sentido * 0.8f),
                                     Quaternion.identity);
        bala.dano = danoDisparo * multiplicador;
        bala.Lanzar(sentido, false);
    }

    /// <summary>Latido del cuerpo y pequeno salto al recibir un disparo.</summary>
    private void Latir()
    {
        golpeVisual = Mathf.MoveTowards(golpeVisual, 0f, Time.deltaTime * 5f);
        float latido = 1f + Mathf.Sin(fase * 6f) * 0.06f + golpeVisual * 0.25f;
        cuerpo.localScale = escalaCuerpo * latido;
    }

    void OnTriggerStay(Collider otro)
    {
        if (Muerto || retroceso > 0f) return;

        var piloto = otro.GetComponentInParent<ControlJugador>();
        if (piloto == null) return;

        // Rebota aunque el golpe no entre (piloto invulnerable), para que no
        // se quede pegado.
        piloto.RecibirGolpe(danoContacto * multiplicador);
        retroceso = 1.2f;
        sentidoRetroceso = PistaAnillo.Hacia(piloto.PosicionPista, pista).x >= 0f ? 1f : -1f;
    }

    public void RecibirDisparo(float dano)
    {
        if (Muerto) return;

        vida -= dano;
        golpeVisual = 1f;
        if (vida <= 0f) Morir();
    }

    private void Morir()
    {
        Muerto = true;

        if (hud != null)
        {
            hud.SumarPuntaje(puntos);
            hud.RecuperarOxigeno(oxigenoAlMorir);
        }

        if (efectoMuerte != null)
            Estallido.Crear(efectoMuerte, transform.position, 1f, colorEstallido);

        if (AlMorir != null) AlMorir(this);
        Destroy(gameObject);
    }
}
