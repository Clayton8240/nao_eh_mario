// KillZone.cs
// -----------------------------------------------------------------------------
// "Chão" invisivel lá embaixo do mapa. Se o player cair num buraco, eventualmente
// vai entrar nesse trigger e morre.
//
// Sem isso o player cairia pra sempre e a gente teria que ficar esperando.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponent<PlayerController2D>();
            if (p != null) p.Kill();
        }
    }
}
