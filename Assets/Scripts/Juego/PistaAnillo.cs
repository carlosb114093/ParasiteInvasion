using UnityEngine;

/// <summary>
/// Vena en forma de anillo: traduce entre el mundo y las coordenadas de
/// pista del nivel.
///
/// En la pista cada punto se describe con tres numeros:
///   x = recorrido a lo largo del anillo (s), en unidades;
///   y = altura dentro del tubo;
///   z = desplazamiento lateral (positivo hacia la izquierda del piloto,
///       que es el lado exterior del anillo).
/// Asi el resto de scripts razona como si el conducto fuera recto e
/// infinito, y esta clase lo dobla en un circulo. El recorrido da la vuelta
/// al llegar a la circunferencia, de modo que no hay principio ni final.
///
/// El anillo esta en el plano horizontal y su centro es la posicion de este
/// objeto; el punto s = 0 queda a "radioAnillo" de distancia en el eje Z.
/// Si la escena no tiene PistaAnillo, las funciones dejan las coordenadas
/// tal cual y todo se comporta como el conducto recto original.
/// </summary>
public class PistaAnillo : MonoBehaviour
{
    [Tooltip("Radio del anillo medido hasta el centro del tubo")]
    public float radioAnillo = 32f;

    [Tooltip("Radio interior del tubo de la vena")]
    public float radioTubo = 6f;

    private static PistaAnillo actual;

    /// <summary>Anillo de la escena abierta, o null si el conducto es recto.</summary>
    public static PistaAnillo Actual
    {
        get
        {
            if (actual == null) actual = FindFirstObjectByType<PistaAnillo>();
            return actual;
        }
    }

    void OnEnable() { actual = this; }
    void OnDisable() { if (actual == this) actual = null; }

    public float Circunferencia { get { return 2f * Mathf.PI * radioAnillo; } }

    // ------------------------------------------------------------------
    // Conversion de un punto
    // ------------------------------------------------------------------

    /// <summary>Punto del mundo que corresponde a unas coordenadas de pista.</summary>
    public Vector3 AMundo(Vector3 pista)
    {
        float a = pista.x / radioAnillo;
        Vector3 exterior = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        return transform.position + exterior * (radioAnillo + pista.z) +
               Vector3.up * pista.y;
    }

    /// <summary>Coordenadas de pista de un punto del mundo.</summary>
    public Vector3 APista(Vector3 mundo)
    {
        Vector3 p = mundo - transform.position;
        float a = Mathf.Atan2(p.x, p.z);
        float s = Mathf.Repeat(a * radioAnillo, Circunferencia);
        float lateral = new Vector2(p.x, p.z).magnitude - radioAnillo;
        return new Vector3(s, p.y, lateral);
    }

    /// <summary>Direccion de avance (tangente) en un punto del recorrido.</summary>
    public Vector3 Tangente(float s)
    {
        float a = s / radioAnillo;
        return new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
    }

    /// <summary>Distancia mas corta de s1 a s2 sobre el anillo, con signo.</summary>
    public float Diferencia(float desde, float hasta)
    {
        float c = Circunferencia;
        float d = Mathf.Repeat(hasta - desde, c);
        return d > c * 0.5f ? d - c : d;
    }

    // ------------------------------------------------------------------
    // Versiones que funcionan tambien sin anillo (conducto recto)
    // ------------------------------------------------------------------

    public static Vector3 Mundo(Vector3 pista)
    {
        var p = Actual;
        return p != null ? p.AMundo(pista) : pista;
    }

    public static Vector3 Pista(Vector3 mundo)
    {
        var p = Actual;
        return p != null ? p.APista(mundo) : mundo;
    }

    public static Vector3 Avance(float s)
    {
        var p = Actual;
        return p != null ? p.Tangente(s) : Vector3.right;
    }

    /// <summary>Vector de pista que va de un punto a otro por el camino corto.</summary>
    public static Vector3 Hacia(Vector3 desdePista, Vector3 hastaPista)
    {
        Vector3 d = hastaPista - desdePista;
        var p = Actual;
        if (p != null) d.x = p.Diferencia(desdePista.x, hastaPista.x);
        return d;
    }

    /// <summary>Rotacion que mira hacia delante por el anillo con la cabeza arriba.</summary>
    public static Quaternion Orientacion(float s)
    {
        return Quaternion.LookRotation(Avance(s), Vector3.up);
    }

    /// <summary>Mantiene la seccion (y, z) dentro de un circulo de radio dado.</summary>
    public static Vector3 ConfinarSeccion(Vector3 pista, float radio)
    {
        Vector2 seccion = new Vector2(pista.y, pista.z);
        if (seccion.magnitude > radio)
        {
            seccion = seccion.normalized * radio;
            pista.y = seccion.x;
            pista.z = seccion.y;
        }
        return pista;
    }
}
