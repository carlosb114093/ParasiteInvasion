using UnityEngine;

/// <summary>
/// Control del Piloto Globulo Rojo A1 durante la partida.
///
/// Reemplaza en la escena de juego al AnimationController de la evidencia de
/// animaciones: conserva el mismo desplazamiento por el conducto y las mismas
/// animaciones, y anade lo que hace falta para jugar de verdad: disparo,
/// esquive, recepcion de golpes con un breve periodo de invulnerabilidad y
/// bloqueo de los controles cuando la partida esta en pausa o ha terminado.
///
/// El piloto siempre mira hacia delante en la vena, que es de donde llegan
/// los parasitos: asi las flechas laterales sirven para esquivar sin perder
/// la linea de tiro. La posicion se lleva en coordenadas de pista (ver
/// PistaAnillo), de modo que en la vena en anillo el piloto puede dar
/// vueltas sin encontrar nunca un final.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ControlJugador : MonoBehaviour
{
    [Header("Referencias")]
    public HUDJuego hud;

    [Tooltip("Proyectil que dispara el piloto")]
    public Proyectil prefabProyectil;

    [Header("Desplazamiento")]
    [Tooltip("Unidades por segundo")]
    public float velocidad = 4f;

    [Tooltip("Correccion en grados si el modelo no mira hacia su eje Z")]
    public float ajusteGiroY = 0f;

    [Tooltip("Inclinacion maxima en grados al moverse de lado")]
    public float inclinacionLateral = 12f;

    [Header("Limites del conducto")]
    [Tooltip("Solo se usan si el conducto es recto (sin PistaAnillo)")]
    public float limiteTrasero = -11f;
    public float limiteFrontal = 13f;

    [Tooltip("Radio interior del conducto: impide atravesar la pared")]
    public float radioConducto = 1.8f;

    [Header("Disparo")]
    [Tooltip("Segundos entre dos disparos seguidos")]
    public float cadencia = 0.22f;

    [Tooltip("Punto de salida del disparo respecto al piloto")]
    public Vector3 bocaDisparo = new Vector3(0.6f, 0.5f, 0f);

    [Header("Esquive")]
    [Tooltip("Duracion del salto de esquive en segundos")]
    public float duracionEsquive = 0.55f;

    [Tooltip("Altura del salto de esquive")]
    public float alturaEsquive = 0.8f;

    public float recargaEsquive = 1.1f;

    [Header("Golpes")]
    [Tooltip("Segundos invulnerable despues de recibir un golpe")]
    public float invulnerabilidad = 1.3f;

    [Header("Sonidos")]
    public AudioClip sonidoDisparo;
    public AudioClip sonidoDano;
    public AudioClip sonidoSalto;
    public AudioClip sonidoRecoger;

    private Animator animator;
    private AudioSource fuente;
    private Renderer[] partes;

    private bool seMoviaAntes;
    private float esperaDisparo;
    private float tiempoEsquive = -1f;
    private float esperaEsquive;
    private float tiempoInvulnerable;
    private float desfaseVertical;
    private Vector3 pista;

    /// <summary>Posicion en coordenadas de pista (recorrido, altura, lateral).</summary>
    public Vector3 PosicionPista { get { return pista; } }

    /// <summary>Verdadero mientras dura el salto de esquive.</summary>
    public bool Esquivando { get { return tiempoEsquive >= 0f; } }

    /// <summary>No recibe dano durante el esquive ni justo despues de un golpe.</summary>
    public bool Invulnerable
    {
        get { return Esquivando || tiempoInvulnerable > 0f; }
    }

    /// <summary>Centro del cuerpo del piloto, al que apuntan los enemigos.</summary>
    public Vector3 Centro
    {
        get { return transform.position + Vector3.up * bocaDisparo.y; }
    }

    /// <summary>Centro del cuerpo en coordenadas de pista.</summary>
    public Vector3 CentroPista
    {
        get { return pista + Vector3.up * bocaDisparo.y; }
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        partes = GetComponentsInChildren<Renderer>();

        if (hud == null) hud = FindFirstObjectByType<HUDJuego>();

        pista = PistaAnillo.Pista(transform.position);
        transform.rotation = Orientacion(0f);
    }

    void Update()
    {
        // Con la pausa abierta Time.deltaTime vale cero, pero las pulsaciones
        // se siguen leyendo: hay que ignorarlas para no disparar en pausa.
        bool bloqueado = Time.timeScale <= 0f || (hud != null && hud.Terminada);
        if (bloqueado) return;

        Desplazar();
        Disparar();
        Esquivar();
        Parpadear();
    }

    // ------------------------------------------------------------------
    // Movimiento
    // ------------------------------------------------------------------

    private void Desplazar()
    {
        float avance = 0f, lateral = 0f, altura = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) avance = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) avance = -1f;
        // La camara mira hacia +X, de modo que su derecha es -Z.
        if (Input.GetKey(KeyCode.RightArrow)) lateral = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) lateral = 1f;
        if (Input.GetKey(KeyCode.PageUp)) altura = 1f;
        if (Input.GetKey(KeyCode.PageDown)) altura = -1f;

        Vector3 direccion = new Vector3(avance, altura, lateral);
        bool seMueveAhora = direccion.sqrMagnitude > 0.01f;

        // Todo el movimiento ocurre en coordenadas de pista; al final se
        // traslada al mundo siguiendo la curva de la vena.
        if (seMueveAhora)
            pista += direccion.normalized * velocidad * Time.deltaTime;

        pista = Confinar(pista);

        // El salto de esquive se suma encima de la altura que elige el jugador.
        desfaseVertical = 0f;
        if (Esquivando)
        {
            float t = tiempoEsquive / duracionEsquive;
            desfaseVertical = Mathf.Sin(t * Mathf.PI) * alturaEsquive;
        }

        transform.position = PistaAnillo.Mundo(pista + Vector3.up * desfaseVertical);

        // Ligera inclinacion hacia el lado al que se esquiva.
        Quaternion destino = Orientacion(-lateral * inclinacionLateral);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, destino,
                                                      240f * Time.deltaTime);

        if (seMueveAhora != seMoviaAntes && animator != null && !Esquivando)
        {
            animator.SetTrigger(seMueveAhora ? "Moverse" : "Idle");
            seMoviaAntes = seMueveAhora;
        }
    }

    private Quaternion Orientacion(float inclinacion)
    {
        return PistaAnillo.Orientacion(pista.x) *
               Quaternion.Euler(0f, ajusteGiroY, inclinacion);
    }

    /// <summary>
    /// Confina al piloto a la seccion circular del conducto. En la vena en
    /// anillo el recorrido es libre y da la vuelta; en el conducto recto se
    /// acota entre los dos limites.
    /// </summary>
    public Vector3 Confinar(Vector3 p)
    {
        var anillo = PistaAnillo.Actual;
        if (anillo != null) p.x = Mathf.Repeat(p.x, anillo.Circunferencia);
        else p.x = Mathf.Clamp(p.x, limiteTrasero, limiteFrontal);

        return PistaAnillo.ConfinarSeccion(p, radioConducto);
    }

    // ------------------------------------------------------------------
    // Disparo y esquive
    // ------------------------------------------------------------------

    private void Disparar()
    {
        esperaDisparo -= Time.deltaTime;

        bool pide = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.Space) ||
                    Input.GetMouseButton(0);
        if (!pide || esperaDisparo > 0f || prefabProyectil == null) return;

        esperaDisparo = cadencia;

        Vector3 salida = PistaAnillo.Mundo(pista + bocaDisparo +
                                           Vector3.up * desfaseVertical);
        Proyectil bala = Instantiate(prefabProyectil, salida, Quaternion.identity);
        bala.Lanzar(Vector3.right, true);

        if (animator != null) animator.SetTrigger("Disparar");
        Sonar(sonidoDisparo, 0.6f);
    }

    private void Esquivar()
    {
        esperaEsquive -= Time.deltaTime;

        if (Esquivando)
        {
            tiempoEsquive += Time.deltaTime;
            if (tiempoEsquive >= duracionEsquive) tiempoEsquive = -1f;
            return;
        }

        if (Input.GetKeyDown(KeyCode.S) && esperaEsquive <= 0f)
        {
            tiempoEsquive = 0f;
            esperaEsquive = recargaEsquive;
            if (animator != null) animator.SetTrigger("Saltar");
            Sonar(sonidoSalto, 0.8f);
        }
    }

    // ------------------------------------------------------------------
    // Golpes y premios
    // ------------------------------------------------------------------

    /// <summary>
    /// Aplica un golpe de un parasito, tentaculo o proyectil enemigo.
    /// Devuelve falso si el golpe no entro por estar invulnerable.
    /// </summary>
    public bool RecibirGolpe(float dano)
    {
        if (Invulnerable || (hud != null && hud.Terminada)) return false;

        tiempoInvulnerable = invulnerabilidad;
        if (hud != null) hud.RecibirDano(dano);
        if (animator != null) animator.SetTrigger("Dano");
        Sonar(sonidoDano, 1f);

#if UNITY_ANDROID || UNITY_IOS
        if (AjustesJuego.Vibracion) Handheld.Vibrate();
#endif
        return true;
    }

    public void SonarRecogida()
    {
        Sonar(sonidoRecoger, 0.8f);
    }

    /// <summary>El piloto parpadea mientras es invulnerable tras un golpe.</summary>
    private void Parpadear()
    {
        if (tiempoInvulnerable <= 0f) return;

        tiempoInvulnerable -= Time.deltaTime;
        bool visible = tiempoInvulnerable <= 0f ||
                       Mathf.Repeat(tiempoInvulnerable, 0.16f) > 0.08f;

        foreach (var r in partes)
            if (r != null) r.enabled = visible;
    }

    private void Sonar(AudioClip clip, float volumen)
    {
        if (clip == null || fuente == null) return;
        fuente.PlayOneShot(clip, volumen * AjustesJuego.VolumenEfectos);
    }
}
