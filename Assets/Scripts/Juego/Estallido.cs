using UnityEngine;

/// <summary>
/// Rafaga de particulas de un solo uso: impacto de disparo, parasito
/// eliminado o premio recogido.
///
/// El prefab se crea apagado; quien lo instancia puede tenirlo antes de
/// lanzarlo, y el objeto se destruye solo cuando terminan las particulas.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class Estallido : MonoBehaviour
{
    /// <summary>Instancia la rafaga con su color original.</summary>
    public static Estallido Crear(Estallido prefab, Vector3 posicion, float escala)
    {
        return Crear(prefab, posicion, escala, Color.clear);
    }

    /// <summary>
    /// Instancia la rafaga en una posicion. Un color transparente conserva
    /// el color con el que se construyo el prefab.
    /// </summary>
    public static Estallido Crear(Estallido prefab, Vector3 posicion, float escala,
                                  Color color)
    {
        if (prefab == null) return null;

        Estallido e = Instantiate(prefab, posicion, Quaternion.identity);
        e.transform.localScale = Vector3.one * escala;

        var ps = e.GetComponent<ParticleSystem>();
        if (color.a > 0f)
        {
            var main = ps.main;
            main.startColor = color;
        }
        ps.Play();

        var principal = ps.main;
        float total = principal.duration + principal.startLifetime.constantMax;
        Destroy(e.gameObject, total + 0.2f);
        return e;
    }
}
