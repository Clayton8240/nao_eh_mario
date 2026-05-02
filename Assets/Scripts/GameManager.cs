// GameManager.cs
// -----------------------------------------------------------------------------
// Esse aqui é o "cérebro" do jogo. Eu fiz ele como Singleton porque o professor
// falou que pra coisas que existem só uma vez (tipo gerenciador de score) o
// padrão Singleton é tranquilo de usar. Tem que tomar cuidado pra não virar
// "God Object", mas pra um protótipo de M1 acho que ta ok.
//
// Ele guarda:
//   - Score atual (zera a cada partida nova)
//   - HighScore (esse persiste em disco usando PlayerPrefs)
//   - Vidas
//   - Quantas moedas pegou
//   - O Estado atual do jogo (Menu / Jogando / GameOver / Vitoria)
//
// Quem precisa saber quando alguma dessas coisas muda se inscreve nos eventos
// (OnScoreChanged, OnLivesChanged, etc). Isso é o padrão Observer, deu pra
// usar bem aqui pra UI não ficar perguntando o tempo todo "mudou? mudou?".
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    public class GameManager : MonoBehaviour
    {
        // Singleton: a "instância única" do GameManager fica acessível por
        // qualquer script via GameManager.Instance.
        public static GameManager Instance { get; private set; }

        // Chave do PlayerPrefs (tipo um "localStorage" do Unity).
        // Coloquei prefixo pra não conflitar caso tenha outro joguinho na mesma máquina.
        // Mantive o prefixo antigo "naoemario" pra não resetar o recorde de quem ja jogou
        // o prototípo anterior (apenas mudou o título de exibição pra Blue Bunny Blaster).
        private const string PREF_HIGHSCORE = "naoemario.highscore";

        // Propriedades com get publico e set privado pra ninguem mexer de fora.
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int Lives { get; private set; } = 3;     // começa com 3 vidas
        public int CoinsCollected { get; private set; }
        // Por fase: mortes e score inicial (para cálculo de grade).
        public int LevelDeaths { get; private set; }
        private int _levelStartScore;
        // Moedas secretas (1 por fase, escondidas fora do caminho óbvio).
        public int SecretCoinsFound { get; private set; }

        // ----- Sistema de fases (BBB tem 3) -----
        // CurrentLevel é 1-based pq fica natural mostrar "Fase 1/3" pro jogador.
        public int CurrentLevel { get; private set; } = 1;
        public int TotalLevels => LevelLibrary.Count;

        // Enum pra deixar claro os estados do jogo (loop de jogo: Menu->Playing->GameOver/Victory)
        public enum State { Menu, Playing, GameOver, Victory, Paused }
        public State CurrentState { get; private set; } = State.Menu;

        // ===== MODIFICADORES DE PARTIDA (roguelite micro-modifier) =====
        // A cada nova partida, 1 modificador é sorteado e ativo durante toda a run.
        public enum Modifier
        {
            None,            // sem modificação (run limpa)
            FasterEnemies,   // inimigos 30% mais rápidos
            NoStartAmmo,     // primeiro pickup da arma não dá munição
            StompGivesAmmo,  // stompadas recarregam 2 balas
            DoubleCoinValue, // moedas valem o dobro
            ExtraLife,       // começa com 4 vidas
        }
        public Modifier ActiveModifier { get; private set; } = Modifier.None;

        // Para o sistema "100 moedas = 1 vida" funcionar com 25/50/75/...,
        // guardo qual é o próximo múltiplo de 100 que ainda não concedi vida.
        private int _nextLifeBonusAt = 100;
        // Lembra o estado anterior pra restaurar ao despausar.
        private State _stateBeforePause = State.Playing;

        // Eventos (Observer pattern). Quando algo muda eu disparo isso e a UI escuta.
        public event System.Action OnScoreChanged;
        public event System.Action OnLivesChanged;
        public event System.Action<State> OnStateChanged;
        // Esse aqui carrega a posição no mundo pra UI fazer um "+10" subir na tela.
        public event System.Action<int, Vector3> OnScorePopup;
        // Disparado quando muda de fase (UI atualiza o "Fase X/3")
        public event System.Action OnLevelChanged;
        // Disparado ao completar uma fase: grade ("S"/"A"/"B"/"C") + bônus concedido.
        public event System.Action<string, int> OnLevelCompleted;
        // Disparado uma vez ao iniciar uma nova partida com o modificador ativo.
        public event System.Action<Modifier> OnModifierActivated;
        // Disparado ao coletar uma moeda secreta.
        public event System.Action OnSecretCoinCollected;

        private void Awake()
        {
            // Garantir que só existe UM GameManager. Se já tem outro, eu me destruo.
            // (Aprendi do jeito difícil que sem isso, ao recarregar a cena, fica
            // dois GameManager e o evento dispara duas vezes.)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Não destrói entre cenas — assim o score "persiste durante a sessão"
            // como pede o requisito do M1.
            DontDestroyOnLoad(gameObject);

            // Carrega o recorde salvo no disco (default = 0 se não tem nada).
            HighScore = PlayerPrefs.GetInt(PREF_HIGHSCORE, 0);
        }

        // Sobrecarga: versão que dispara o popup "+10" na posição do mundo
        public void AddScore(int amount, Vector3 worldPos, bool isCoin = false)
        {
            AddScoreInternal(amount, isCoin);
            OnScorePopup?.Invoke(amount, worldPos);
        }

        // Versão "silenciosa", sem popup
        public void AddScore(int amount)
        {
            AddScoreInternal(amount, false);
        }

        // Lógica que de fato soma no score e salva o recorde.
        // Coloquei separado pra não duplicar código (DRY).
        private void AddScoreInternal(int amount, bool isCoin)
        {
            // Modificador DoubleCoinValue: moedas normais valem o dobro.
            if (isCoin && ActiveModifier == Modifier.DoubleCoinValue)
                amount *= 2;

            Score += amount;
            if (isCoin)
            {
                CoinsCollected++;
                // Estilo Mario clássico: a cada 100 moedas, ganha uma vida extra.
                if (CoinsCollected >= _nextLifeBonusAt)
                {
                    Lives++;
                    _nextLifeBonusAt += 100;
                    OnLivesChanged?.Invoke();
                    SfxPlayer.Instance?.PlayCoin();
                }
            }

            // Se quebrou o recorde, salva no PlayerPrefs (vai pro disco).
            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(PREF_HIGHSCORE, HighScore);
                PlayerPrefs.Save(); // força gravar agora (senão só salva ao fechar o app)
            }

            // Avisa quem tá escutando (UI atualiza o texto)
            OnScoreChanged?.Invoke();
        }

        public void LoseLife()
        {
            // Mathf.Max evita ficar com -1 vida (defensive programming bonitinho rs)
            Lives = Mathf.Max(0, Lives - 1);
            // Rastreia mortes da fase atual (para cálculo de grade).
            if (CurrentState == State.Playing) LevelDeaths++;
            OnLivesChanged?.Invoke();

            // Se zerou as vidas, vai pra tela de Game Over
            if (Lives <= 0) SetState(State.GameOver);
        }

        // Troca o estado e avisa (a UI usa isso pra ligar/desligar painéis)
        public void SetState(State s)
        {
            CurrentState = s;
            // Garantia: ao sair de Paused por qualquer caminho, restaura o tempo.
            if (s != State.Paused) Time.timeScale = 1f;
            OnStateChanged?.Invoke(s);
        }

        // Resetar tudo pra começar partida nova
        public void StartNewGame()
        {
            Score = 0;
            Lives = 3;
            CoinsCollected = 0;
            CurrentLevel = 1;            // sempre começa da primeira fase
            _nextLifeBonusAt = 100;
            LevelDeaths = 0;
            SecretCoinsFound = 0;
            _levelStartScore = 0;
            OnScoreChanged?.Invoke();
            OnLivesChanged?.Invoke();
            OnLevelChanged?.Invoke();

            // Sorteia o modificador da run (1–5; 0 = None reservado para run limpa).
            // Usa Random.Range(0, 6) pra dar 1/6 de chance de run sem modificador.
            ActiveModifier = (Modifier)Random.Range(0, 6);
            if (ActiveModifier == Modifier.ExtraLife) { Lives = 4; OnLivesChanged?.Invoke(); }
            OnModifierActivated?.Invoke(ActiveModifier);

            SetState(State.Playing);
        }

        // Pausa / despausa o jogo (Esc). Time.timeScale = 0 congela física e Update.
        public void TogglePause()
        {
            if (CurrentState == State.Paused)
            {
                Time.timeScale = 1f;
                SetState(_stateBeforePause);
            }
            else if (CurrentState == State.Playing)
            {
                _stateBeforePause = CurrentState;
                Time.timeScale = 0f;
                SetState(State.Paused);
            }
        }

        // Chamado pelo Goal quando o jogador toca a bandeira/meta.
        // Se ainda tem fase, avança; senão, vitória total.
        public void CompleteLevel()
        {
            // ----- Grade S/A/B/C -----
            int scoreGained = Score - _levelStartScore;
            string grade    = ComputeGrade(scoreGained, LevelDeaths);
            int gradeBonus  = grade == "S" ? 300 :
                              grade == "A" ? 150 :
                              grade == "B" ? 50  : 0;
            if (gradeBonus > 0) AddScoreInternal(gradeBonus, false);
            OnLevelCompleted?.Invoke(grade, gradeBonus);

            // Bônus por terminar a fase com vidas restantes
            // (50 por vida em fase intermediária, 100 na final = mais celebratório)
            bool isLast = CurrentLevel >= TotalLevels;
            int perLifeBonus = isLast ? 100 : 50;
            if (Lives > 0) AddScoreInternal(Lives * perLifeBonus, false);

            // Reseta rastreadores para a próxima fase.
            LevelDeaths      = 0;
            _levelStartScore = Score;

            if (isLast)
            {
                SetState(State.Victory);
            }
            else
            {
                CurrentLevel++;
                OnLevelChanged?.Invoke();
                // O GameBootstrap escuta OnLevelChanged via UI? Não, mais simples:
                // o Goal chama Bootstrap.LoadCurrentLevel() depois de chamar isso aqui.
            }
        }

        // Computa a grade com base no score ganho na fase e no número de mortes.
        private string ComputeGrade(int scoreGained, int deaths)
        {
            if (deaths == 0 && scoreGained >= 800) return "S";
            if (deaths <= 1 && scoreGained >= 500) return "A";
            if (deaths <= 3 && scoreGained >= 200) return "B";
            return "C";
        }

        // Moedas secretas: valor extra + notifica a UI.
        public void CollectSecretCoin(Vector3 worldPos)
        {
            SecretCoinsFound++;
            AddScore(150, worldPos); // moeda secreta vale muito mais
            OnSecretCoinCollected?.Invoke();
        }
    }
}