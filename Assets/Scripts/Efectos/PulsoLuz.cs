using UnityEngine;

/// <summary>
/// Hace "respirar" una luz subiendo y bajando su intensidad de forma suave.
///
/// Se usa en las luces puntuales de bioluminiscencia de la vena para que el
/// tejido parezca vivo. No responde al jugador: es solo ambientacion, como
/// pidio el instructor para esta fase (la interactividad llega con el demo).
/// </summary>
[RequireComponent(typeof(Light))]
public class PulsoLuz : MonoBehaviour
{
    [Tooltip("Cuanto varia la intensidad respecto a la base (0.3 = +-30 %).")]
    [Range(0f, 1f)] public float amplitud = 0.3f;

    [Tooltip("Pulsos por segundo.")]
    public float frecuencia = 0.8f;

    [Tooltip("Desfase en segundos para que no todas las luces pulsen a la vez.")]
    public float desfase = 0f;

    private Light luz;
    private float intensidadBase;

    void Awake()
    {
        luz = GetComponent<Light>();
        intensidadBase = luz.intensity;
    }

    void Update()
    {
        float onda = Mathf.Sin((Time.time + desfase) * frecuencia * 2f * Mathf.PI);
        luz.intensity = intensidadBase * (1f + amplitud * onda);
    }
}
