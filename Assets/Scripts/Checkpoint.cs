// Checkpoint.cs
// -----------------------------------------------------------------------------
// Trigger que, ao ser tocado pela primeira vez, atualiza o ponto de respawn
// do jogador. Reduz drasticamente a fric\u00e7\u00e3o de morrer no fim de uma fase
// longa (problema cl\u00e1ssico observado na fase 3 antes desta mudan\u00e7a).
//
// Decisões:
//  - Trigger \u00fanico: ap\u00f3s ativar, marca _used = true e troca a cor pra dar
//    feedback visual de "j\u00e1 foi pego".
//  - N\u00e3o persiste entre mortes: a posi\u00e7\u00e3o de respawn fica no PlayerController
//    (SetSpawn), ent\u00e3o continua valendo at\u00e9 trocar de fase.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        private bool _used;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used) return;
            var player = other.GetComponent<PlayerController2D>();
            if (player == null) return;

            _used = true;
            // Spawna no ch\u00e3o, n\u00e3o flutuando no checkpoint.
            player.SetSpawn(transform.position + new Vector3(0f, 0.1f, 0f));
            SfxPlayer.Instance?.PlayCoin(); // som de "ativado" reaproveitado
            if (_sr != null) _sr.color = new Color(0.4f, 1f, 0.4f);
        }
    }
}
