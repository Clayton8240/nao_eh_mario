// PlayerController2D.cs
// -----------------------------------------------------------------------------
// Controlador do personagem. Movimentação lateral (A/D ou setas) e pulo (Espaço).
//
// Decisões que tomei:
//  - Usei Rigidbody2D dinâmico pq quero que a gravidade aja sozinha. Se eu
//    movesse pelo Transform direto ia ter que implementar gravidade na mão
//    (e ia ser pior).
//  - Setei freezeRotation pra ele não cair de ladinho quando bate em coisas.
//  - Pra detectar o chão eu faço um BoxCast pra baixo a partir do collider.
//    Achei que é mais confiavel que OnCollisionStay porque dá pra checar
//    EXATAMENTE no frame do pulo.
//  - Usei o NEW Input System (Keyboard.current). Não precisa configurar
//    Input Action Asset, só ler direto.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

namespace NaoEMario
{
    // RequireComponent: o Unity já adiciona esses componentes se faltarem.
    // Garante que o script nunca quebre por falta de Rigidbody/Collider.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        // Variáveis de tuning. Public pra dar pra mexer no Inspector se quiser.
        public float moveSpeed = 7f;
        public float jumpForce = 13f;
        public float groundCheckDistance = 0.08f;
        public LayerMask groundMask;  // máscara de quais layers contam como "chão"

        // Refs de componentes (cache no Awake pra não chamar GetComponent toda hora)
        private Rigidbody2D _rb;
        private Collider2D _col;

        private bool _grounded;     // true quando o pé tá tocando algo
        private bool _alive = true; // false quando morre, pra travar o input

        private Vector3 _spawnPoint; // pra onde respawnar

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            // Configurações fixas do Rigidbody
            _rb.freezeRotation = true; // não rotaciona ao colidir
            _rb.gravityScale = 3f;     // gravidade mais forte que o padrão
                                       // (sensação melhor pra plataforma 2D)
        }

        private void Update()
        {
            // Trava o input se tá morto ou se não estamos jogando (menu, gameover)
            if (!_alive) return;
            if (GameManager.Instance == null
                || GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            // Pega o teclado atual do New Input System
            var kb = Keyboard.current;
            if (kb == null) return; // se ninguém tem teclado, não faz nada (ex: rodando em mobile)

            // Eixo horizontal feito na unha (esquerda = -1, direita = +1, parado = 0)
            float h = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;

            // Aplica velocidade horizontal mantendo a vertical (pra não cancelar o pulo!)
            _rb.linearVelocity = new Vector2(h * moveSpeed, _rb.linearVelocity.y);

            // Atualiza se tá no chão antes de testar pulo
            CheckGround();

            // wasPressedThisFrame = só dispara no FRAME do clique (não enquanto segura)
            bool jumpPressed = kb.spaceKey.wasPressedThisFrame
                            || kb.wKey.wasPressedThisFrame
                            || kb.upArrowKey.wasPressedThisFrame;

            if (jumpPressed && _grounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                SfxPlayer.Instance?.PlayJump();
            }
        }

        // Faz um BoxCast bem fininho logo abaixo do collider
        // pra ver se tem chão.
        private void CheckGround()
        {
            var b = _col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + 0.02f);
            // 90% da largura do collider (margem pra não confundir com colisão lateral)
            Vector2 size   = new Vector2(b.size.x * 0.9f, 0.04f);
            var hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down,
                                        groundCheckDistance, groundMask);
            _grounded = hit.collider != null;
        }

        // Chamado por inimigo ou kill zone
        public void Kill()
        {
            if (!_alive) return; // ja morreu, evita perder vida 2x no mesmo frame
            _alive = false;
            SfxPlayer.Instance?.PlayHurt();
            GameManager.Instance?.LoseLife();

            // Pequeno knockback pra cima pra dar feedback visual
            _rb.linearVelocity = new Vector2(0f, 8f);
            // Desabilita o collider pra não bater em mais inimigo enquanto cai
            _col.enabled = false;

            // Se ainda tem vidas, agenda o respawn
            if (GameManager.Instance != null && GameManager.Instance.Lives > 0)
            {
                Invoke(nameof(Respawn), 1.0f);
            }
        }

        public void SetSpawn(Vector3 p) => _spawnPoint = p;

        private void Respawn()
        {
            transform.position = _spawnPoint;
            _rb.linearVelocity = Vector2.zero;
            _col.enabled = true;
            _alive = true;
        }

        // Usado quando pisa em inimigo: dá um pulinho extra
        public void Bounce(float force)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, force);
        }

        // Usado pelo GameBootstrap quando começa partida nova,
        // pra cancelar respawn pendente e zerar tudo.
        public void ResetForNewGame()
        {
            CancelInvoke();
            _alive = true;
            _col.enabled = true;
            _rb.linearVelocity = Vector2.zero;
            transform.position = _spawnPoint;
        }
    }
}
