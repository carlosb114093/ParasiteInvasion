using System.Collections;
using UnityEngine;

/// <summary>
/// Sincroniza el sonido con el sistema de particulas del mismo objeto.
///
/// El AudioSource no conoce el Start Delay del sistema de particulas: si se
/// dejara en Play On Awake sonaria antes de que aparezcan las particulas.
/// Este componente espera el mismo retardo y reproduce el clip cada vez que
/// arranca un ciclo de emision.
/// </summary>
[RequireComponent(typeof(ParticleSystem), typeof(AudioSource))]
public class SonidoSistemaParticulas : MonoBehaviour
{
    private ParticleSystem sistema;
    private AudioSource fuente;

    IEnumerator Start()
    {
        sistema = GetComponent<ParticleSystem>();
        fuente = GetComponent<AudioSource>();

        var principal = sistema.main;

        // Mismo tiempo de activacion que las particulas.
        yield return new WaitForSeconds(principal.startDelay.constant);

        while (true)
        {
            fuente.Play();

            if (!principal.loop)
                yield break;

            // Un sonido por cada ciclo del sistema (Duration).
            yield return new WaitForSeconds(principal.duration);
        }
    }
}
