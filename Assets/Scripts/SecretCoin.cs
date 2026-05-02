// SecretCoin.cs
// -----------------------------------------------------------------------------
// Moeda secreta (estrela dourada). Ao coletar, chama GameManager.CollectSecretCoin
// em vez de AddScore diretamente — para rastrear o total de segredos encontrados.
// Vale 150 pontos + notifica a UI.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Collider2D))]
    public class SecretCoin : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;

            GameManager.Instance?.CollectSecretCoin(transform.position);
            SfxPlayer.Instance?.PlayCoin();
            Destroy(gameObject);
        }
    }
}
