// EnemyPatrol.cs
// -----------------------------------------------------------------------------
// IA simples: o inimigo anda de um lado pro outro entre dois pontos
// (origem - patrolRange) e (origem + patrolRange).
//
// Comportamento de combate (clássico estilo Mario):
//  - Player encosta de LADO -> player morre.
//  - Player cai de CIMA (stomp) -> inimigo morre, player ganha um quique e pontos.
//
// Pra saber se foi de cima ou de lado eu olho a normal do contato. Se a normal
// aponta pra BAIXO (y < 0) significa que o inimigo foi atingido de CIMA, ou seja,
// foi stompado. Achei meio confuso isso no começo mas depois entendi:
// a normal aponta DO inimigo PRO player.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyPatrol : MonoBehaviour
    {
        public float speed = 2f;
        public float patrolRange = 3f;

        private Rigidbody2D _rb;
        private float _origin;   // X inicial, base da patrulha
        private int _dir = 1;    // 1 = direita, -1 = esquerda
        private bool _alive = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.gravityScale = 3f;
            _origin = transform.position.x;
        }

        // FixedUpdate é o lugar certo pra mexer em física (Rigidbody)
        private void FixedUpdate()
        {
            if (!_alive) return;

            float x = transform.position.x;

            // Se passou do limite à direita, vira pra esquerda
            if (x > _origin + patrolRange) _dir = -1;
            // Idem pra esquerda
            else if (x < _origin - patrolRange) _dir = 1;

            // Mantém Y pra não anular a queda da gravidade
            _rb.linearVelocity = new Vector2(_dir * speed, _rb.linearVelocity.y);
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (!_alive) return;

            // Só me importo se quem bateu é o player
            var player = c.collider.GetComponent<PlayerController2D>();
            if (player == null) return;

            // Vê todos os pontos de contato (pode ter mais de um)
            foreach (var contact in c.contacts)
            {
                // normal.y < -0.5 = veio de cima (stomp!)
                if (contact.normal.y < -0.5f)
                {
                    Stomped();
                    player.Bounce(10f);  // quique pro player
                    GameManager.Instance?.AddScore(50, transform.position);
                    return;
                }
            }

            // Se chegou aqui, foi colisão de lado: player morre
            player.Kill();
        }

        private void Stomped()
        {
            _alive = false;
            SfxPlayer.Instance?.PlayStomp();
            // Espera um frame pequeno pra física aplicar o quique antes de sumir
            Destroy(gameObject, 0.05f);
        }
    }
}
