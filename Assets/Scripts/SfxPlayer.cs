// SfxPlayer.cs
// -----------------------------------------------------------------------------
// Player de efeitos sonoros. Como pro M1 a gente não pode (ou não quer ainda)
// baixar audio da Asset Store, eu gerei os SFX no código mesmo, na unha,
// usando uma onda senoidal (a famosa sin(2*pi*f*t) que vimos em Sinais).
//
// Cada som é só um "beep" com:
//   - frequencia (f): grave = numero baixo, agudo = numero alto
//   - duração: em segundos
//   - volume
//
// Coloquei tambem um "envelope" linear pra evitar aquele click chato no começo
// e no fim quando o som corta de seco (descontinuidade na onda).
//
// Pros próximos milestones (M2/M3) é só substituir esses AudioClip por arquivos
// de verdade (.wav/.ogg) baixados da Asset Store ou do Itch.io.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    public class SfxPlayer : MonoBehaviour
    {
        // Singleton (igual o GameManager)
        public static SfxPlayer Instance { get; private set; }

        private AudioSource _source;

        // Cada AudioClip é um som diferente (gerado no Awake)
        private AudioClip _jump, _coin, _hurt, _stomp, _win, _click;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cria um AudioSource em runtime (não precisa configurar no editor)
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            // Gero todos os sons aqui. Os numeros foram tentativa e erro até soar legal.
            _jump  = MakeBeep(660f, 0.10f, 0.5f);  // Mi5 mais ou menos
            _coin  = MakeBeep(990f, 0.08f, 0.4f);  // bem agudo, parece moeda
            _hurt  = MakeBeep(160f, 0.20f, 0.6f);  // grave e longo
            _stomp = MakeBeep(220f, 0.10f, 0.6f);
            _win   = MakeBeep(880f, 0.40f, 0.5f);
            _click = MakeBeep(440f, 0.05f, 0.4f);  // Lá4
        }

        // Função que constrói um AudioClip do zero, sample por sample.
        private static AudioClip MakeBeep(float freq, float duration, float volume)
        {
            int sampleRate = 44100; // taxa de amostragem padrão de áudio (CD quality)
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);

            // AudioClip vazio que vamos preencher
            var clip = AudioClip.Create($"beep_{freq}", sampleCount, 1, sampleRate, false);

            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                // tempo em segundos do sample atual
                float t = i / (float)sampleRate;

                // Envelope simples: cresce no inicio (ataque) e decresce no fim (release).
                // Isso evita o "click" desagradável.
                // O 20f é a velocidade do attack/release (escolhi no chute).
                float env = Mathf.Min(1f, t * 20f) * Mathf.Min(1f, (duration - t) * 20f);

                // Onda senoidal: amplitude * sin(2*pi*f*t)
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        // Funções publicas que outros scripts chamam (tipo SfxPlayer.Instance.PlayCoin())
        // Usei "expression-bodied members" (=>) só pra ficar mais curto.
        public void PlayJump()  => _source.PlayOneShot(_jump);
        public void PlayCoin()  => _source.PlayOneShot(_coin);
        public void PlayHurt()  => _source.PlayOneShot(_hurt);
        public void PlayStomp() => _source.PlayOneShot(_stomp);
        public void PlayWin()   => _source.PlayOneShot(_win);
        public void PlayClick() => _source.PlayOneShot(_click);
    }
}
