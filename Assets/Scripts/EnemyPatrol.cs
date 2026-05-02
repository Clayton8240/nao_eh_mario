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
        public Sprite[] frames; // frames de animação (idle/anda alternando)
        public float frameDuration = 0.25f;

        // Quando true, o inimigo inverte a direção ao chegar na borda da plataforma
        // (em vez de cair em buracos sozinho — antes virava ameaa nula).
        public bool edgeCheck = true;
        public LayerMask edgeMask = ~0;

        // Tipo: caranguejo é rápido + cai de bordas; slime é lento + 2 hits.
        // Definido pelo GameBootstrap com base no tile do atlas.
        public bool isCrab = false;

        // Range de distância horizontal em que o inimigo percebe o player e persegue.
        public float alertRange = 6f;

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private float _origin;   // X inicial, base da patrulha
        private int _dir = 1;    // 1 = direita, -1 = esquerda
        private bool _alive = true;
        private float _animTimer;
        private int _frameIdx;
        // Multi-hit (slime aguenta 2 tiros; crab morre em 1).
        private int _maxHits;
        private int _hitCount;
        private float _flashUntil;
        // Referência ao player para lógica de perseguição.
        private Transform _playerTransform;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _rb.freezeRotation = true;
            _rb.gravityScale = 3f;
            _origin = transform.position.x;

            // Configurações por tipo (set pelo GameBootstrap antes do Awake não é
            // garantido, por isso usamos Start para aplicar as diferenças).
            // Aqui só inicializamos os padrões.
        }

        private void Start()
        {
            // Carrega o player UMA vez (barato, evita GetComponent todo frame).
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = FindFirstObjectByType<PlayerController2D>()?.gameObject;
            if (playerObj != null) _playerTransform = playerObj.transform;

            // Aplica diferenças de tipo após GameBootstrap ter configurado isCrab.
            if (isCrab)
            {
                speed = speed > 2.1f ? speed : 3.2f; // caranguejo rápido
                edgeCheck = false;  // crab cai de bordas — ameaa zonal
                _maxHits = 1;       // morre em 1 tiro
            }
            else
            {
                _maxHits = 2;       // slime aguenta 2 tiros antes de morrer
            }

            // Modificador FasterEnemies: todos os inimigos +30% de velocidade.
            if (GameManager.Instance?.ActiveModifier == GameManager.Modifier.FasterEnemies)
                speed *= 1.3f;
        }

        private void Update()
        {
            if (!_alive) return;
            if (frames == null || frames.Length < 2 || _sr == null) return;

            _animTimer += Time.deltaTime;
            if (_animTimer >= frameDuration)
            {
                _animTimer = 0f;
                _frameIdx = (_frameIdx + 1) % frames.Length;
                _sr.sprite = frames[_frameIdx];
            }

            // Flash vermelho quando toma hit mas sobrevive (slime 1º hit).
            _sr.color = Time.time < _flashUntil ? Color.red : Color.white;
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

            // Edge-check: se não tem chão à frente, inverte antes de cair.
            // Faz um OverlapBox bem fininho um pouco à frente do pé do inimigo.
            if (edgeCheck)
            {
                var col = GetComponent<Collider2D>();
                if (col != null)
                {
                    var b = col.bounds;
                    Vector2 probe = new Vector2(
                        b.center.x + _dir * (b.extents.x + 0.05f),
                        b.min.y - 0.10f);
                    var hit = Physics2D.OverlapBox(probe, new Vector2(0.08f, 0.08f), 0f, edgeMask);
                    // Ignora a si próprio e outros inimigos como "chão".
                    bool grounded = hit != null
                                    && hit.GetComponent<EnemyPatrol>() == null
                                    && hit.GetComponent<PlayerController2D>() == null;
                    if (!grounded)
                    {
                        _dir = -_dir;
                        _origin = transform.position.x;
                    }
                }
            }

            // Espelha o sprite conforme a direção (esquerda = flipX)
            if (_sr != null) _sr.flipX = _dir < 0;

            // Chase override: quando o player está dentro do alertRange,
            // o inimigo se orienta e acelera em direção a ele.
            float chaseSpeed = speed;
            if (_playerTransform != null)
            {
                float dx = _playerTransform.position.x - transform.position.x;
                float absDx = Mathf.Abs(dx);
                if (absDx < alertRange && absDx > 0.4f)
                {
                    _dir = dx > 0f ? 1 : -1;
                    chaseSpeed = speed * 1.8f;
                    if (_sr != null) _sr.flipX = _dir < 0;
                }
            }

            // Mantém Y pra não anular a queda da gravidade
            _rb.linearVelocity = new Vector2(_dir * chaseSpeed, _rb.linearVelocity.y);
        }

        // Se bater de lado em algo (parede, plataforma) inverte a direção
        // pra não ficar travado. Ignora player e contatos vindos de cima/baixo.
        private void OnCollisionStay2D(Collision2D c)
        {
            if (!_alive) return;
            if (c.collider.GetComponent<PlayerController2D>() != null) return;
            if (c.collider.GetComponent<EnemyPatrol>() != null) return;

            foreach (var contact in c.contacts)
            {
                // Contato lateral: normal aponta horizontalmente
                if (Mathf.Abs(contact.normal.x) > 0.7f)
                {
                    // Inverte se a normal contraria a direção atual
                    if ((contact.normal.x > 0 && _dir < 0) ||
                        (contact.normal.x < 0 && _dir > 0))
                    {
                        _dir = -_dir;
                        // Reposiciona a origem da patrulha para o ponto atual,
                        // assim o inimigo continua patrulhando a partir daqui.
                        _origin = transform.position.x;
                    }
                    return;
                }
            }
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
                    // Stomp = 2x pontos base + multiplicador de combo aéreo.
                    int chain      = player.AirKillChain;
                    int multiplier = Mathf.Min(chain + 1, 5); // cap 5x
                    GameManager.Instance?.AddScore(150 * multiplier, transform.position);
                    // Modificador StompGivesAmmo: stomp recarrega 2 balas.
                    if (GameManager.Instance?.ActiveModifier == GameManager.Modifier.StompGivesAmmo)
                        player.AddAmmo(2);
                    player.IncrementAirKillChain();
                    player.Bounce(22f);  // quique alto = encadeamento de stompadas possível
                    return;
                }
            }

            // Se chegou aqui, foi colisão de lado: aplica regra de dano da arma.
            player.TakeEnemyDamage();
        }

        private void Stomped()
        {
            _alive = false;
            SfxPlayer.Instance?.PlayStomp();
            // Espera um frame pequeno pra física aplicar o quique antes de sumir
            Destroy(gameObject, 0.05f);
        }

        public void HitByProjectile()
        {
            if (!_alive) return;
            _hitCount++;
            if (_hitCount < _maxHits)
            {
                // Primeiro hit num inimigo multi-hit (slime): sobrevive, pisca vermelho.
                _flashUntil = Time.time + 0.35f;
                // Inverte direção — feedback visual + comportamento reativo.
                _dir = -_dir;
                _origin = transform.position.x;
                return;
            }
            _alive = false;
            SfxPlayer.Instance?.PlayStomp();
            GameManager.Instance?.AddScore(50, transform.position);
            Destroy(gameObject);
        }
    }
}
