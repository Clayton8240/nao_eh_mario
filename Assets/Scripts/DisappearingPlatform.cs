// DisappearingPlatform.cs
// -----------------------------------------------------------------------------
// Plataforma que some após o player pousar nela.
// Comportamento:
//   1. Player pisa → plataforma começa a piscar (0.6s)
//   2. Plataforma desaparece (collider e visual desligados)
//   3. Após 2s, plataforma reaparece
//
// O visual (SpriteRenderer do GameObject separado) é passado pelo Bootstrap.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DisappearingPlatform : MonoBehaviour
    {
        // Referência ao objeto visual (o MakeSpriteObject do Bootstrap),
        // preenchida pelo GameBootstrap ao criar a plataforma.
        [HideInInspector] public GameObject visual;

        public float warningTime  = 0.6f; // tempo piscando antes de sumir
        public float hiddenTime   = 2.0f; // tempo invisível/sólido-off

        private BoxCollider2D _col;
        private SpriteRenderer _sr;
        private bool _triggered;

        private void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            // O collider já chega configurado (fino, ancorado ao topo) pelo GameBootstrap.
        }

        // OnCollisionEnter2D detecta o player pousando em cima.
        private void OnCollisionEnter2D(Collision2D c)
        {
            if (_triggered) return;
            if (c.collider.GetComponent<PlayerController2D>() == null) return;

            // Só dispara se o player veio de cima (contact normal apontando para baixo).
            foreach (var contact in c.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    _triggered = true;
                    StartCoroutine(DisappearRoutine());
                    return;
                }
            }
        }

        private IEnumerator DisappearRoutine()
        {
            if (visual != null) _sr = visual.GetComponent<SpriteRenderer>();

            // Fase 1: pisca (flicker) por warningTime
            float end = Time.time + warningTime;
            while (Time.time < end)
            {
                bool visible = Mathf.Repeat(Time.time * 14f, 1f) > 0.5f;
                if (_sr != null) _sr.enabled = visible;
                yield return null;
            }

            // Fase 2: some — desliga visual e collider
            if (_sr != null)   _sr.enabled = false;
            if (_col != null)  _col.enabled = false;

            yield return new WaitForSeconds(hiddenTime);

            // Fase 3: reaparece
            if (_sr != null)   _sr.enabled = true;
            if (_col != null)  _col.enabled = true;
            _triggered = false;
        }
    }
}
