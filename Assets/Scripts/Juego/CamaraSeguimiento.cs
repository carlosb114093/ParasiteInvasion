using UnityEngine;

/// <summary>
/// Camara en tercera persona que sigue al piloto desde atras.
///
/// Se mantiene dentro del conducto (solo acompana a medias los movimientos
/// laterales y verticales) para no atravesar la pared, y suaviza el
/// seguimiento para que los esquives no sacudan la imagen.
/// </summary>
public class CamaraSeguimiento : MonoBehaviour
{
    public Transform objetivo;

    [Tooltip("Distancia por detras del piloto")]
    public float distancia = 8.5f;

    [Tooltip("Altura sobre el piloto")]
    public float altura = 2.6f;

    [Tooltip("Cuanto acompana la camara el movimiento lateral y vertical (0 a 1)")]
    [Range(0f, 1f)] public float acompanamiento = 0.6f;

    [Tooltip("Distancia por delante del piloto a la que mira")]
    public float mirarAdelante = 10f;

    public float suavizado = 6f;

    private Vector3 camaraPista;
    private bool iniciada;

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Se calcula en coordenadas de pista para que en la vena en anillo la
        // camara siga la curva por detras del piloto y nunca atraviese la
        // pared interior del anillo.
        Vector3 p = PistaAnillo.Pista(objetivo.position);
        Vector3 destino = new Vector3(p.x - distancia,
                                      p.y * acompanamiento + altura,
                                      p.z * acompanamiento);

        if (!iniciada)
        {
            camaraPista = destino;
            iniciada = true;
        }

        float t = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        camaraPista += PistaAnillo.Hacia(camaraPista, destino) * t;
        var anillo = PistaAnillo.Actual;
        if (anillo != null) camaraPista.x = Mathf.Repeat(camaraPista.x, anillo.Circunferencia);

        transform.position = PistaAnillo.Mundo(camaraPista);

        Vector3 foco = PistaAnillo.Mundo(new Vector3(p.x + mirarAdelante,
                                                     p.y * 0.5f + altura * 0.4f,
                                                     p.z * 0.5f));
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(foco - transform.position, Vector3.up), t);
    }
}
