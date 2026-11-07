using UnityEngine;

/// <summary>
/// Premio flotante que el piloto recoge al tocarlo.
///
///   - Capsula de medicina: una dosis mas para curarse con la tecla B.
///   - Vida extra: recupera vida al instante.
///   - Dosis maxima: llena el oxigeno y suma dos dosis.
/// Gira y flota en su sitio para que se distinga del escenario.
/// </summary>
public class Recogible : MonoBehaviour
{
    public enum Tipo { CapsulaMedicina, VidaExtra, DosisMaxima }

    public Tipo tipo = Tipo.CapsulaMedicina;

    [Tooltip("Vida que devuelve la Vida extra")]
    public float vidaExtra = 35f;

    [Header("Animacion")]
    public float giro = 70f;
    public float flotacion = 0.15f;

    public Estallido efecto;
    public Color colorEfecto = new Color(0.4f, 1f, 0.9f);

    private Vector3 origen;
    private float fase;
    private bool recogido;

    void Start()
    {
        origen = transform.position;
        fase = Random.value * 6f;
    }

    void Update()
    {
        fase += Time.deltaTime;
        transform.Rotate(Vector3.up, giro * Time.deltaTime, Space.World);
        transform.position = origen + Vector3.up * Mathf.Sin(fase * 2f) * flotacion;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (recogido) return;

        var jugador = otro.GetComponentInParent<ControlJugador>();
        if (jugador == null || jugador.hud == null || jugador.hud.Terminada) return;

        recogido = true;
        HUDJuego hud = jugador.hud;

        switch (tipo)
        {
            case Tipo.CapsulaMedicina:
                hud.RecogerDosis();
                break;
            case Tipo.VidaExtra:
                hud.RecuperarVida(vidaExtra);
                hud.Avisar("Vida extra  +" + Mathf.RoundToInt(vidaExtra));
                break;
            case Tipo.DosisMaxima:
                hud.RecuperarOxigeno(hud.oxigenoMaximo);
                hud.RecogerDosis();
                hud.RecogerDosis();
                hud.Avisar("Dosis maxima: oxigeno lleno y +2 dosis");
                break;
        }

        jugador.SonarRecogida();
        if (efecto != null) Estallido.Crear(efecto, transform.position, 1f, colorEfecto);
        Destroy(gameObject);
    }
}
