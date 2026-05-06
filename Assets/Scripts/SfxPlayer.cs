// SfxPlayer.cs
// -----------------------------------------------------------------------------
// Player de efeitos sonoros e música de fundo.
//
// SFX carregados de Assets/Resources/sfx/ via Resources.Load:
//   sfx_coin        → moeda coletada
//   sfx_death       → player morreu
//   sfx_enemy_kill  → inimigo derrotado (stomp ou tiro)
//   sfx_victory     → fase concluída / vitória
//   sfx_gun         → disparo
//
// O pulo ainda usa beep gerado em código (sem arquivo de som dedicado).
//
// BGM: dois AudioSources separados (um pra SFX, um pra música) pra não
// cortar a música quando toca um efeito sonoro.
//   bgm_menu  → toca no estado Menu
//   bgm_game  → toca durante o jogo
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    public class SfxPlayer : MonoBehaviour
    {
        // Singleton (igual o GameManager)
        public static SfxPlayer Instance { get; private set; }

        // AudioSource de efeitos (one-shot, não faz loop)
        private AudioSource _sfxSource;
        // AudioSource de música de fundo (loop, volume menor)
        private AudioSource _bgmSource;

        // Clips de efeitos sonoros (carregados de Resources/sfx/)
        private AudioClip _jump, _coin, _hurt, _stomp, _win, _click, _shoot;

        // Clips de música de fundo
        private AudioClip _bgmMenu, _bgmGame;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource de SFX
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            // AudioSource de BGM (loop, volume reduzido pra não abafar os SFX)
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = 0.35f;

            // Carrega os clips de arquivo (retorna null se não existir, sem travar)
            _coin   = Resources.Load<AudioClip>("sfx/sfx_coin");
            _hurt   = Resources.Load<AudioClip>("sfx/sfx_death");
            _stomp  = Resources.Load<AudioClip>("sfx/sfx_enemy_kill");
            _win    = Resources.Load<AudioClip>("sfx/sfx_victory");
            _shoot  = Resources.Load<AudioClip>("sfx/sfx_gun");
            _bgmMenu = Resources.Load<AudioClip>("sfx/bgm_menu");
            _bgmGame = Resources.Load<AudioClip>("sfx/bgm_game");

            // Pulo ainda usa beep gerado em código (sem arquivo dedicado)
            _jump  = MakeBeep(660f, 0.10f, 0.5f);
            _click = MakeBeep(440f, 0.05f, 0.4f);

            // Escuta mudanças de estado do jogo pra trocar a BGM automaticamente
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void Start()
        {
            // Garante inscrição caso o GameManager seja criado depois do SfxPlayer
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                // Aplica a BGM do estado inicial (Menu)
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        // Troca a BGM automaticamente conforme o estado do jogo muda
        private void HandleStateChanged(GameManager.State state)
        {
            switch (state)
            {
                case GameManager.State.Menu:
                    PlayBgm(_bgmMenu);
                    break;
                case GameManager.State.Playing:
                    PlayBgm(_bgmGame);
                    break;
                case GameManager.State.GameOver:
                case GameManager.State.Victory:
                    StopBgm();
                    break;
                // Paused: mantém a BGM atual sem mudar
            }
        }

        // Troca a música de fundo; ignora se o clip já está tocando
        private void PlayBgm(AudioClip clip)
        {
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void StopBgm() => _bgmSource.Stop();

        // Gera um beep sintético (mantido só pro pulo e clique de UI)
        private static AudioClip MakeBeep(float freq, float duration, float volume)
        {
            int sampleRate  = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create($"beep_{freq}", sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t   = i / (float)sampleRate;
                float env = Mathf.Min(1f, t * 20f) * Mathf.Min(1f, (duration - t) * 20f);
                data[i]   = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        // Toca um clip de efeito sonoro (sem cortar a BGM)
        private void PlaySfx(AudioClip clip)
        {
            if (clip != null) _sfxSource.PlayOneShot(clip);
        }

        // API pública — mesmos nomes que antes pra não quebrar nenhum outro script
        public void PlayJump()  => PlaySfx(_jump);
        public void PlayCoin()  => PlaySfx(_coin);
        public void PlayHurt()  => PlaySfx(_hurt);
        public void PlayStomp() => PlaySfx(_stomp);
        public void PlayWin()   => PlaySfx(_win);
        public void PlayClick() => PlaySfx(_click);
        public void PlayShoot() => PlaySfx(_shoot);
    }
}
