// Goal.cs
// -----------------------------------------------------------------------------
// Bandeira / meta no fim da fase. Encostou nela = vitória.
// Bem simples mesmo, só dispara o estado de Vitória no GameManager.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Collider2D))]
    public class Goal : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            SfxPlayer.Instance?.PlayWin();
            GameManager.Instance?.Win();
        }
    }
}
