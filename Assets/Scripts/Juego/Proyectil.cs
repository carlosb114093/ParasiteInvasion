using UnityEngine;

/// <summary>
/// Disparo en linea recta, tanto del piloto como de los parasitos
/// escupidores. "Recto" en coordenadas de pista: en la vena en anillo el
/// disparo sigue la curva del conducto en lugar de salirse por la pared.
///
/// Avanza a velocidad constante, desaparece al cabo de unos segundos o al
/// tocar la pared del conducto y, al chocar con un blanco del bando
/// contrario, le resta vida. Lleva un Rigidbody cinematico para que Unity le
/// envie los avisos de colision aunque se mueva por codigo.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Proyectil : MonoBehaviour
{
    public float velocidad = 18f;
    public float dano = 1f;

    [Tooltip("Segundos antes de desaparecer si no choca con nada")]
    public float duracion = 2.2f;

    [Tooltip("Radio del conducto: el disparo se apaga al tocar la pared")]
    public float radioConducto = 2.1f;

    [Tooltip("Estallido al impactar (opcional)")]
    public Estallido efectoImpacto;

    private Vector3 direccion = Vector3.right;
    private bool delJugador = true;
    private float vida;
    private Vector3 pista;

    /// <summary>
    /// Indica el sentido (en coordenadas de pista) y el bando antes del
    /// primer fotograma. El punto de partida es la posicion actual.
    /// </summary>
    public void Lanzar(Vector3 sentidoPista, bool disparadoPorJugador)
    {
        direccion = sentidoPista.normalized;
        delJugador = disparadoPorJugador;
        pista = PistaAnillo.Pista(transform.position);
        Orientar();
    }

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        pista += direccion * velocidad * Time.deltaTime;
        transform.position = PistaAnillo.Mundo(pista);
        Orientar();

        vida += Time.deltaTime;
        Vector2 seccion = new Vector2(pista.y, pista.z);
        if (vida > duracion || seccion.magnitude > radioConducto) Destroy(gameObject);
    }

    /// <summary>La estela queda alineada con el avance sobre la curva.</summary>
    private void Orientar()
    {
        Vector3 adelante = PistaAnillo.Mundo(pista + direccion * 0.1f) - transform.position;
        if (adelante.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.LookRotation(adelante);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (delJugador)
        {
            var parasito = otro.GetComponentInParent<Parasito>();
            if (parasito == null || parasito.Muerto) return;
            parasito.RecibirDisparo(dano);
            Impactar();
        }
        else
        {
            var jugador = otro.GetComponentInParent<ControlJugador>();
            if (jugador == null) return;
            jugador.RecibirGolpe(dano);
            Impactar();
        }
    }

    private void Impactar()
    {
        if (efectoImpacto != null)
            Estallido.Crear(efectoImpacto, transform.position, 0.4f);
        Destroy(gameObject);
    }
}
