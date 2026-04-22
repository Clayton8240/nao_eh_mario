// ScorePopup.cs
// -----------------------------------------------------------------------------
// Aquele textinho "+10" amarelo que sobe e some quando pega moeda ou pisa em
// inimigo. É um feedback visual rápido pro player saber que pontuou.
//
// Funcionamento: a cada frame eu subo um pouquinho na tela e diminuo a opacidade
// (alpha do canal de cor). Quando o tempo total passa do "duration", destruo.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace NaoEMario
{
    [RequireComponent(typeof(Text))]
    public class ScorePopup : MonoBehaviour
    {
        public float duration = 0.8f;   // tempo total de vida do popup
        public float riseSpeed = 60f;   // pixels por segundo subindo

        private Text _text;
        private float _t;               // tempo decorrido
        private Color _initial;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _initial = _text.color;
        }

        private void Update()
        {
            _t += Time.deltaTime;

            // Sobe um pouco
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            // Diminui o alpha linearmente até 0
            // Clamp01 garante valor entre 0 e 1
            float a = 1f - Mathf.Clamp01(_t / duration);
            _text.color = new Color(_initial.r, _initial.g, _initial.b, a);

            if (_t >= duration) Destroy(gameObject);
        }
    }
}
