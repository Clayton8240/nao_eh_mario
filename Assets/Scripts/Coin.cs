// Coin.cs
// -----------------------------------------------------------------------------
// Moeda coletável. Quando o player encosta, soma 10 no score, toca o som
// e se destrói.
//
// Detalhes:
//  - Uso Trigger ao invés de colisão sólida pq não quero que a moeda empurre
//    o player. Trigger detecta sobreposição mas não aplica força.
//  - O Reset() é chamado pelo Unity quando o componente é adicionado pela
//    primeira vez. Aproveito pra já marcar o collider como Trigger (qol).
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        public int value = 10;

        private void Reset()
        {
            // Garante que o collider é trigger ao adicionar o script
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Só conta se quem encostou é o player (e não outro inimigo, etc.)
            if (other.GetComponent<PlayerController2D>() == null) return;

            // Soma o score E avisa a UI pra spawnar o "+10" flutuante
            GameManager.Instance?.AddScore(value, transform.position, isCoin: true);
            SfxPlayer.Instance?.PlayCoin();

            // Adeus moeda
            Destroy(gameObject);
        }
    }
}
