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

        // Garante que só dispara uma vez (player pode triggar varios frames seguidos)
        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (other.GetComponent<PlayerController2D>() == null) return;
            _triggered = true;

            SfxPlayer.Instance?.PlayWin();
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool wasLast = gm.CurrentLevel >= gm.TotalLevels;
            gm.CompleteLevel(); // incrementa CurrentLevel ou seta Victory

            // Se NÃO era a última, manda o Bootstrap reconstruir a próxima fase
            if (!wasLast && GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.LoadCurrentLevel();
            }
        }
    }
}
