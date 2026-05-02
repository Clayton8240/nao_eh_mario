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

using System.Collections;
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
        public float jumpForce = 16f; // gravityScale=3 -> apice ~4.35 units
        public float groundCheckDistance = 0.08f;
        public float shootCooldown = 0.18f;
        public float projectileSpeed = 14f;
        public float projectileLifetime = 1.4f;
        public LayerMask groundMask;  // máscara de quais layers contam como "chão"

        // Quality-of-life moderno (padrão Celeste/Hollow Knight):
        //  - coyoteTime: ainda dá pra pular um instante depois de sair do chão
        //  - jumpBuffer: pular um pouquinho ANTES de tocar o chão já vale
        //  - variableJumpCut: soltar o pulo cedo encurta a altura (controle fino)
        public float coyoteTime = 0.10f;
        public float jumpBuffer = 0.10f;
        [Range(0.1f, 1f)] public float variableJumpCut = 0.5f;

        // Munição finita: a arma vira recurso ao invés de spam infinito.
        public int maxAmmo = 5;
        public int ammoPerPickup = 3;

        // Refs de componentes (cache no Awake pra não chamar GetComponent toda hora)
        private Rigidbody2D _rb;
        private Collider2D _col;
        private SpriteRenderer _sr;

        private bool _grounded;     // true quando o pé tá tocando algo
        private bool _alive = true; // false quando morre, pra travar o input
        private bool _hasWeapon;
        private int _ammo;
        private float _nextDamageTime;
        private float _nextShootTime;
        private float _animTimer;
        private int _runFrame;
        private int _facingDir = 1;
        private int _airKillChain;

        // Janelas de tempo pra coyote/buffer
        private float _lastGroundedTime = -999f;
        private float _lastJumpPressTime = -999f;
        private bool _jumpHeld;

        private Vector3 _spawnPoint; // pra onde respawnar

        public bool HasWeapon => _hasWeapon;
        public int Ammo => _ammo;
        public int MaxAmmo => maxAmmo;
        public bool IsGrounded => _grounded;
        // Contador de kills aéreas encadeadas (combo stomp).
        public int AirKillChain => _airKillChain;
        public void IncrementAirKillChain() => _airKillChain++;
        public void ResetAirKillChain()     => _airKillChain = 0;

        // Eventos pra UI/inimigos reagirem a mudanças de estado.
        public event System.Action OnAmmoChanged;
        // Disparado quando o hit absorvido pela arma a destrói.
        public event System.Action OnWeaponLost;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _sr = GetComponent<SpriteRenderer>();

            // Configurações fixas do Rigidbody
            _rb.freezeRotation = true; // não rotaciona ao colidir
            _rb.gravityScale = 3f;     // gravidade mais forte que o padrão
                                       // (sensação melhor pra plataforma 2D)
        }

        private void Update()
        {
            if (!_alive)
            {
                UpdateVisual();
                return;
            }

            if (GameManager.Instance == null
                || GameManager.Instance.CurrentState != GameManager.State.Playing)
            {
                UpdateVisual();
                return;
            }

            // Pega o teclado atual do New Input System
            var kb = Keyboard.current;
            if (kb == null) return; // se ninguém tem teclado, não faz nada (ex: rodando em mobile)

            // Eixo horizontal feito na unha (esquerda = -1, direita = +1, parado = 0)
            float h = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;

            // Aplica velocidade horizontal mantendo a vertical (pra não cancelar o pulo!)
            _rb.linearVelocity = new Vector2(h * moveSpeed, _rb.linearVelocity.y);
            if (h > 0.01f) _facingDir = 1;
            else if (h < -0.01f) _facingDir = -1;

            // Atualiza se tá no chão antes de testar pulo
            CheckGround();
            if (_grounded) _lastGroundedTime = Time.time;

            // wasPressedThisFrame = só dispara no FRAME do clique (não enquanto segura)
            bool jumpPressed = kb.spaceKey.wasPressedThisFrame
                            || kb.wKey.wasPressedThisFrame
                            || kb.upArrowKey.wasPressedThisFrame;

            // Mantém o press registrado por jumpBuffer segundos.
            if (jumpPressed) _lastJumpPressTime = Time.time;

            // Está segurando o botão de pulo? (pra variable jump height)
            _jumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;

            // Pulo só dispara se houve press recente (buffer) E o player
            // está/estava no chão recentemente (coyote).
            bool bufferedJump = (Time.time - _lastJumpPressTime) <= jumpBuffer;
            bool canCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
            if (bufferedJump && canCoyote)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                SfxPlayer.Instance?.PlayJump();
                // Consome ambas janelas pra não pular 2x no mesmo press.
                _lastJumpPressTime = -999f;
                _lastGroundedTime = -999f;
            }

            // Variable jump: se soltou o botão durante a subida, corta o impulso.
            if (!_jumpHeld && _rb.linearVelocity.y > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x,
                                                 _rb.linearVelocity.y * variableJumpCut);
            }

            bool shootPressed = kb.zKey.wasPressedThisFrame
                             || kb.jKey.wasPressedThisFrame
                             || kb.kKey.wasPressedThisFrame
                             || kb.leftCtrlKey.wasPressedThisFrame;
            if (shootPressed)
            {
                TryShoot();
            }

            UpdateVisual();
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
            bool wasGrounded = _grounded;
            _grounded = hit.collider != null;
            // Ao pousar: reseta o contador de kills aéreas (combo stomp).
            if (!wasGrounded && _grounded) _airKillChain = 0;
        }

        // Chamado por inimigo ou kill zone
        public void Kill()
        {
            if (!_alive) return; // ja morreu, evita perder vida 2x no mesmo frame
            _alive = false;
            SfxPlayer.Instance?.PlayHurt();
            // Hit-stop: congela o frame por 0.1s — dá peso à morte.
            StartCoroutine(HitStopCoroutine());
            GameManager.Instance?.LoseLife();

            // Pequeno knockback pra cima pra dar feedback visual
            _rb.linearVelocity = new Vector2(0f, 8f);
            // Desabilita o collider pra não bater em mais inimigo enquanto cai
            _col.enabled = false;

            // Se ainda tem vidas, agenda o respawn (0.5s: o 0.1s do hit-stop + 0.4s jogável).
            if (GameManager.Instance != null && GameManager.Instance.Lives > 0)
            {
                Invoke(nameof(Respawn), 0.5f);
            }
        }

        private IEnumerator HitStopCoroutine()
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.10f);
            Time.timeScale = 1f;
        }

        public void GiveWeapon()
        {
            // Modificador NoStartAmmo: primeiro pickup não concede munição.
            bool skipAmmo = !_hasWeapon &&
                GameManager.Instance?.ActiveModifier == GameManager.Modifier.NoStartAmmo;
            _hasWeapon = true;
            if (!skipAmmo)
                _ammo = Mathf.Min(maxAmmo, _ammo + ammoPerPickup);
            OnAmmoChanged?.Invoke();
            UpdateVisual(force: true);
        }

        // Concedido pelo GameManager quando o jogador acumula 100 moedas.
        public void AddAmmo(int amount)
        {
            if (!_hasWeapon) return; // sem arma, balas vão pro vazio
            _ammo = Mathf.Min(maxAmmo, _ammo + amount);
            OnAmmoChanged?.Invoke();
        }

        public void TakeEnemyDamage()
        {
            if (!_alive) return;
            if (Time.time < _nextDamageTime) return;

            // Com arma: perde a arma e sobrevive ao hit.
            if (_hasWeapon)
            {
                _hasWeapon = false;
                _ammo = 0;
                OnAmmoChanged?.Invoke();
                OnWeaponLost?.Invoke();  // avisa a UI pra dar feedback visual
                _nextDamageTime = Time.time + 0.9f;
                SfxPlayer.Instance?.PlayHurt();
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.25f, 8f);
                UpdateVisual(force: true);
                return;
            }

            // Sem arma: dano é fatal.
            Kill();
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
            ResetForLevel(keepWeapon: false);
        }

        // Reset entre fases preservando o estado da arma conquistada antes.
        public void ResetForLevel(bool keepWeapon)
        {
            CancelInvoke();
            _alive = true;
            if (!keepWeapon)
            {
                _hasWeapon = false;
                _ammo = 0;
            }
            OnAmmoChanged?.Invoke();
            _nextDamageTime = 0f;
            _nextShootTime = 0f;
            _col.enabled = true;
            _rb.linearVelocity = Vector2.zero;
            transform.position = _spawnPoint;
            _animTimer = 0f;
            _runFrame = 0;
            _facingDir = 1;
            _lastGroundedTime = -999f;
            _lastJumpPressTime = -999f;
            UpdateVisual(force: true);
        }

        private void TryShoot()
        {
            if (!_hasWeapon) return;
            if (_ammo <= 0) return; // sem munição
            if (Time.time < _nextShootTime) return;

            _nextShootTime = Time.time + shootCooldown;
            _ammo--;
            OnAmmoChanged?.Invoke();

            // Projétil sai à frente do coelho na altura do peito.
            Vector3 bulletPos = transform.position + new Vector3(0.7f * _facingDir, 0.05f, 0f);
            var go = new GameObject("Projectile");
            go.transform.position = bulletPos;
            var projectile = go.AddComponent<Projectile>();
            projectile.Launch(_facingDir, projectileSpeed, projectileLifetime, _col);
            SfxPlayer.Instance?.PlayShoot();
        }

        private void UpdateVisual(bool force = false)
        {
            if (_sr == null) return;

            // Pisca durante a janela de invulnerabilidade após perder a arma.
            if (Time.time < _nextDamageTime)
                _sr.enabled = Mathf.Repeat(Time.time * 20f, 1f) > 0.5f;
            else
                _sr.enabled = true;

            // Sem tint: a troca de sprite comunica o estado da arma visualmente.
            _sr.color = Color.white;

            // Flip horizontal conforme direção de movimento.
            float vx = _rb.linearVelocity.x;
            if (Mathf.Abs(vx) > 0.05f)
                _sr.flipX = vx < 0f;

            // Pulo / queda.
            bool airborne = Mathf.Abs(_rb.linearVelocity.y) > 0.15f && !_grounded;
            if (airborne)
            {
                _sr.sprite = SpriteLibrary.Get(_hasWeapon
                    ? SpriteLibrary.TILE_PLAYER_JUMP_ARMED
                    : SpriteLibrary.TILE_PLAYER_JUMP);
                return;
            }

            // Idle (parado).
            if (Mathf.Abs(vx) < 0.15f)
            {
                _sr.sprite = SpriteLibrary.Get(_hasWeapon
                    ? SpriteLibrary.TILE_PLAYER_IDLE_ARMED
                    : SpriteLibrary.TILE_PLAYER_IDLE);
                _animTimer = 0f;
                return;
            }

            // Corrida: alterna IDLE e RUN1 a cada 0.12 s (sem arma tem 2 frames; com arma tem 2).
            _animTimer += Time.deltaTime;
            if (force || _animTimer >= 0.12f)
            {
                _animTimer = 0f;
                _runFrame  = (_runFrame + 1) % 2;
            }

            if (_hasWeapon)
            {
                _sr.sprite = _runFrame == 0
                    ? SpriteLibrary.Get(SpriteLibrary.TILE_PLAYER_RUN1_ARMED)
                    : SpriteLibrary.Get(SpriteLibrary.TILE_PLAYER_IDLE_ARMED);
            }
            else
            {
                _sr.sprite = _runFrame == 0
                    ? SpriteLibrary.Get(SpriteLibrary.TILE_PLAYER_RUN1)
                    : SpriteLibrary.Get(SpriteLibrary.TILE_PLAYER_IDLE);
            }
        }
    }
}
