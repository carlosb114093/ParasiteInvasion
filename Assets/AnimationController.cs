using UnityEngine;

/// <summary>
/// Controla las animaciones del Piloto Globulo Rojo A1, su desplazamiento
/// por el interior del Conducto Vena y su orientacion hacia la direccion
/// en la que avanza.
///
/// Se coloca en el objeto contenedor (padre), no en el modelo animado.
/// El Animator escribe la posicion del modelo en cada fotograma porque los
/// clips traen curvas de raiz; al mover el padre, el desplazamiento del
/// jugador y la animacion del personaje no compiten entre si.
/// </summary>
public class AnimationController : MonoBehaviour
{
    [Header("Desplazamiento")]
    [Tooltip("Unidades por segundo")]
    public float velocidad = 4f;

    [Header("Giro")]
    [Tooltip("Grados por segundo que tarda en encarar la nueva direccion")]
    public float velocidadGiro = 540f;

    [Tooltip("Correccion en grados si el modelo no mira hacia su eje Z")]
    public float ajusteGiroY = 0f;

    [Header("Limites del conducto")]
    [Tooltip("Recorrido maximo a lo largo del tunel (eje X)")]
    public float limiteLongitudinal = 13f;

    [Tooltip("Radio interior del conducto: impide atravesar la pared")]
    public float radioConducto = 1.8f;

    private Animator animator;
    private bool seMoviaAntes;

    void Start()
    {
        // El Animator vive en el modelo hijo, no en este contenedor.
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("AnimationController: no se encontro un Animator en los hijos.");
    }

    void Update()
    {
        LeerAnimacionesManuales();
        Desplazar();
    }

    /// <summary>
    /// Teclas de demostracion: activan cada animacion de forma aislada.
    /// </summary>
    private void LeerAnimacionesManuales()
    {
        if (Input.GetKeyDown(KeyCode.I)) animator.SetTrigger("Idle");
        if (Input.GetKeyDown(KeyCode.M)) animator.SetTrigger("Moverse");
        if (Input.GetKeyDown(KeyCode.S)) animator.SetTrigger("Saltar");
        if (Input.GetKeyDown(KeyCode.D)) animator.SetTrigger("Disparar");
        if (Input.GetKeyDown(KeyCode.X)) animator.SetTrigger("Dano");
    }

    /// <summary>
    /// Desplaza al jugador con las flechas, lo orienta hacia donde avanza y
    /// encadena la animacion que corresponde: Moverse al andar, Idle al parar.
    /// </summary>
    private void Desplazar()
    {
        float avance = 0f;
        float lateral = 0f;
        float altura = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) avance = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) avance = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) lateral = 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) lateral = -1f;
        if (Input.GetKey(KeyCode.PageUp)) altura = 1f;
        if (Input.GetKey(KeyCode.PageDown)) altura = -1f;

        // El tunel se extiende sobre el eje X del mundo.
        Vector3 direccion = new Vector3(avance, altura, lateral);
        bool seMueveAhora = direccion.sqrMagnitude > 0.01f;

        if (seMueveAhora)
        {
            transform.position += direccion.normalized * velocidad * Time.deltaTime;
            ConfinarAlConducto();
            Orientar(direccion);
        }

        // Solo se dispara el trigger cuando cambia el estado, no cada fotograma.
        if (seMueveAhora != seMoviaAntes)
        {
            animator.SetTrigger(seMueveAhora ? "Moverse" : "Idle");
            seMoviaAntes = seMueveAhora;
        }
    }

    /// <summary>
    /// Encara progresivamente la direccion de avance. Solo se tiene en cuenta
    /// la componente horizontal: subir o bajar no debe inclinar al personaje.
    /// </summary>
    private void Orientar(Vector3 direccion)
    {
        Vector3 horizontal = new Vector3(direccion.x, 0f, direccion.z);

        // Si solo se pulsa subir o bajar no hay hacia donde girar.
        if (horizontal.sqrMagnitude < 0.001f) return;

        Quaternion destino = Quaternion.LookRotation(horizontal.normalized)
                           * Quaternion.Euler(0f, ajusteGiroY, 0f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, destino, velocidadGiro * Time.deltaTime);
    }

    /// <summary>
    /// Mantiene al jugador dentro del conducto: acota el recorrido a lo largo
    /// del tunel y lo confina a la seccion circular en el plano Y-Z.
    /// </summary>
    private void ConfinarAlConducto()
    {
        Vector3 p = transform.position;

        p.x = Mathf.Clamp(p.x, -limiteLongitudinal, limiteLongitudinal);

        Vector2 seccion = new Vector2(p.y, p.z);
        if (seccion.magnitude > radioConducto)
        {
            seccion = seccion.normalized * radioConducto;
            p.y = seccion.x;
            p.z = seccion.y;
        }

        transform.position = p;
    }
}
