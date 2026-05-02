// Projectile.cs
// -----------------------------------------------------------------------------
// Projétil simples disparado pelo player quando está com arma.
// Destrói inimigos no impacto, some ao bater no cenário e expira por tempo.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    // Apenas Rigidbody2D e SpriteRenderer são auto-adicionados.
    // CircleCollider2D é criado manualmente no Awake para evitar
    // NullReferenceException causado por RequireComponent com tipo abstrato.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private CircleCollider2D _col;
        private SpriteRenderer _sr;

        private int _dir = 1;
        private float _speed = 14f;
        private Collider2D _owner;

        private static Sprite _whiteSprite;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();

            // Adiciona o CircleCollider2D diretamente (tipo concreto) para evitar
            // NullRef que ocorre com RequireComponent(typeof(Collider2D)) abstrato.
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.isTrigger = true;
            _col.radius = 0.16f;

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Bala: usa o sprite correto do atlas (tile 44).
            var bulletSprite = SpriteLibrary.Get(SpriteLibrary.TILE_BULLET);
            _sr.sprite = bulletSprite != null ? bulletSprite : WhiteSprite();
            _sr.color  = Color.white;
            transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            _sr.sortingOrder = 8;
        }

        public void Launch(int dir, float speed, float lifetime, Collider2D owner)
        {
            _dir = dir >= 0 ? 1 : -1;
            _speed = Mathf.Max(0.1f, speed);
            _owner = owner;

            // Espelha o sprite quando atira para a esquerda
            if (_sr != null) _sr.flipX = _dir < 0;

            if (_owner != null)
            {
                Physics2D.IgnoreCollision(_owner, _col, true);
            }

            _rb.linearVelocity = new Vector2(_dir * _speed, 0f);
            Destroy(gameObject, Mathf.Max(0.05f, lifetime));
        }

        private void Update()
        {
            _rb.linearVelocity = new Vector2(_dir * _speed, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (_owner != null && other == _owner) return;

            // Ignora triggers de gameplay para não quebrar fluxo de fase.
            if (other.GetComponent<Goal>() != null) return;
            if (other.GetComponent<KillZone>() != null) return;
            if (other.GetComponent<Coin>() != null) return;
            if (other.GetComponent<WeaponPickup>() != null) return;
            if (other.GetComponent<PlayerController2D>() != null) return;

            var enemy = other.GetComponent<EnemyPatrol>();
            if (enemy != null)
            {
                enemy.HitByProjectile();
                Destroy(gameObject);
                return;
            }

            // Colidiu com cenário/obstáculo: remove o projétil.
            Destroy(gameObject);
        }

        private static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
            return _whiteSprite;
        }
    }
}
