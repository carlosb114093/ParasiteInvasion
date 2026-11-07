using UnityEngine;

/// <summary>
/// Tentaculo parasitario anclado a la pared del conducto.
///
/// Se balancea de forma continua y golpea al piloto si lo toca. Se puede
/// evitar rodeandolo o saltando con S, que deja al piloto invulnerable
/// mientras dura el salto.
/// </summary>
public class TentaculoObstaculo : MonoBehaviour
{
    public float dano = 6f;

    [Tooltip("Grados que oscila a cada lado")]
    public float amplitud = 18f;

    [Tooltip("Oscilaciones por segundo")]
    public float frecuencia = 0.45f;

    private Quaternion reposo;
    private Vector3 eje = Vector3.right;
    private float fase;

    void Start()
    {
        reposo = transform.rotation;
        fase = Random.value * 6f;

        // Oscila de lado a lado de la vena: gira alrededor de la direccion
        // de avance en el punto donde esta anclado.
        eje = PistaAnillo.Avance(PistaAnillo.Pista(transform.position).x);
    }

    void Update()
    {
        fase += Time.deltaTime * frecuencia * Mathf.PI * 2f;
        transform.rotation = Quaternion.AngleAxis(Mathf.Sin(fase) * amplitud, eje) *
                             reposo;
    }

    void OnTriggerStay(Collider otro)
    {
        var jugador = otro.GetComponentInParent<ControlJugador>();
        if (jugador != null) jugador.RecibirGolpe(dano * GeneradorOleadas.Dificultad);
    }
}
