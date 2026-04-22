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
        private const string PREF_HIGHSCORE = "naoemario.highscore";

        // Propriedades com get publico e set privado pra ninguem mexer de fora.
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int Lives { get; private set; } = 3;     // começa com 3 vidas
        public int CoinsCollected { get; private set; }

        // Enum pra deixar claro os estados do jogo (loop de jogo: Menu->Playing->GameOver/Victory)
        public enum State { Menu, Playing, GameOver, Victory }
        public State CurrentState { get; private set; } = State.Menu;

        // Eventos (Observer pattern). Quando algo muda eu disparo isso e a UI escuta.
        public event System.Action OnScoreChanged;
        public event System.Action OnLivesChanged;
        public event System.Action<State> OnStateChanged;
        // Esse aqui carrega a posição no mundo pra UI fazer um "+10" subir na tela.
        public event System.Action<int, Vector3> OnScorePopup;

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
            Score += amount;
            if (isCoin) CoinsCollected++;

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
            OnLivesChanged?.Invoke();

            // Se zerou as vidas, vai pra tela de Game Over
            if (Lives <= 0) SetState(State.GameOver);
        }

        // Troca o estado e avisa (a UI usa isso pra ligar/desligar painéis)
        public void SetState(State s)
        {
            CurrentState = s;
            OnStateChanged?.Invoke(s);
        }

        // Resetar tudo pra começar partida nova
        public void StartNewGame()
        {
            Score = 0;
            Lives = 3;
            CoinsCollected = 0;
            OnScoreChanged?.Invoke();
            OnLivesChanged?.Invoke();
            SetState(State.Playing);
        }

        public void Win()
        {
            // Bônus por terminar com vidas restantes (incentivo a não morrer)
            // 100 pontos por vida que sobrou.
            if (Lives > 0) AddScoreInternal(Lives * 100, false);
            SetState(State.Victory);
        }
    }
}
