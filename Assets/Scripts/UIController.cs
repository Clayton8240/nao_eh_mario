// UIController.cs
// -----------------------------------------------------------------------------
// Toda a interface (HUD, Menu, Game Over, Vitoria) é construida AQUI em código,
// sem usar prefab nenhum. Eu sei que normalmente a gente arrasta uns Canvas
// no editor, mas pra esse protótipo decidi fazer 100% por código pq:
//   1. O professor não precisa montar nada na cena.
//   2. Posso entregar só os scripts e ja funciona.
//   3. É bom exercicio pra entender como o uGUI realmente funciona por dentro.
//
// Estrutura criada:
//   Canvas (ScreenSpaceOverlay)
//     - MenuPanel       (mostrado em State.Menu)
//     - HudPanel        (mostrado em State.Playing) -> Score, Recorde, Moedas, Vidas
//     - GameOverPanel   (mostrado em State.GameOver)
//     - VictoryPanel    (mostrado em State.Victory)
//
// O OnStateChanged liga/desliga esses paineis conforme o estado do GameManager.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace NaoEMario
{
    public class UIController : MonoBehaviour
    {
        // Refs pros painéis e textos. Tudo criado em runtime.
        private Canvas _canvas;
        private GameObject _menuPanel, _hudPanel, _gameOverPanel, _victoryPanel;

        // Textos do HUD
        private Text _scoreText, _livesText, _coinsText, _highScoreHudText;

        // Texto de recorde no menu
        private Text _menuHighScoreText;

        // Textos da tela de Game Over
        private Text _finalScoreText, _finalHighText;

        // Textos da tela de Vitória
        private Text _victoryScoreText, _victoryHighText, _victoryCoinsText;

        private void Start()
        {
            // Constroi tudo em ordem
            BuildCanvas();
            BuildMenu();
            BuildHud();
            BuildGameOver();
            BuildVictory();

            // Se inscreve nos eventos do GameManager (Observer pattern)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged += RefreshScore;
                GameManager.Instance.OnLivesChanged += RefreshLives;
                GameManager.Instance.OnStateChanged += OnStateChanged;
                GameManager.Instance.OnScorePopup += SpawnScorePopup;
                // Atualiza visual ja com o estado atual
                OnStateChanged(GameManager.Instance.CurrentState);
            }
        }

        // SEMPRE desinscrever no OnDestroy. Senão da memory leak ou
        // tentativa de chamar em objeto destruido (NullReferenceException).
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= RefreshScore;
                GameManager.Instance.OnLivesChanged -= RefreshLives;
                GameManager.Instance.OnStateChanged -= OnStateChanged;
                GameManager.Instance.OnScorePopup -= SpawnScorePopup;
            }
        }

        // ---------- CONSTRUÇÃO DA UI ----------

        private void BuildCanvas()
        {
            // Cria o Canvas principal (modo Overlay = sempre por cima do jogo)
            var go = new GameObject("UICanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Scaler pra UI escalar com diferentes resoluções (1920x1080 de referencia)
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // EventSystem é obrigatorio pra botões funcionarem!
            // Já uso o módulo do New Input System pq o projeto ta nele.
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }
        }

        // Helper: cria um painel que ocupa a tela toda com cor de fundo
        private GameObject MakePanel(string name, Color bg)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(_canvas.transform, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            // Anchor stretch (ocupa o canvas inteiro)
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        // Helper: cria um Text com configuração completa em uma linha só
        // Usei muito esse helper pra não repetir 50x as mesmas 8 propriedades.
        private Text MakeText(Transform parent, string txt, int size, Vector2 anchor,
                              Vector2 pos, Vector2 sz,
                              TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = txt;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            // LegacyRuntime.ttf = a fonte built-in do Unity (Arial substituto).
            // Se eu usasse Arial direto não funcionaria nas versões mais novas.
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sz;
            return t;
        }

        // Helper: cria um Button com label e callback
        private Button MakeButton(Transform parent, string label, Vector2 pos,
                                  System.Action onClick)
        {
            var go = new GameObject("Button_" + label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f, 1f);  // cinza escuro
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360, 80);

            // Texto centralizado dentro do botão
            MakeText(go.transform, label, 36, new Vector2(0.5f, 0.5f),
                     Vector2.zero, new Vector2(360, 80));

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            // onClick com lambda: toca o som de click + executa a ação passada
            btn.onClick.AddListener(() =>
            {
                SfxPlayer.Instance?.PlayClick();
                onClick?.Invoke();
            });
            return btn;
        }

        // ---------- TELAS ----------

        private void BuildMenu()
        {
            _menuPanel = MakePanel("MenuPanel", new Color(0.07f, 0.08f, 0.12f, 1f));
            MakeText(_menuPanel.transform, "NÃO É MÁRIO", 100,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1200, 140));
            MakeText(_menuPanel.transform, "Grey Box Prototype - M1", 36,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(800, 60));
            MakeText(_menuPanel.transform, "Setas/A,D para mover  |  Espaço/W para pular", 28,
                     new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1200, 40));

            // Recorde no menu (amarelo, pra dar destaque)
            _menuHighScoreText = MakeText(_menuPanel.transform, "Recorde: 0", 36,
                                          new Vector2(0.5f, 0.5f), new Vector2(0, 50),
                                          new Vector2(800, 50));
            _menuHighScoreText.color = new Color(1f, 0.85f, 0.2f);

            MakeButton(_menuPanel.transform, "JOGAR",  new Vector2(0, -20),
                       () => GameBootstrap.Instance.StartGame());
            MakeButton(_menuPanel.transform, "SAIR",   new Vector2(0, -120), () =>
            {
                // No editor a gente não pode chamar Application.Quit() (não fecha nada),
                // por isso essa diretiva de pré-processador. Aprendi no Stack Overflow.
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        private void BuildHud()
        {
            // Painel do HUD é só um container vazio (sem fundo, deixa ver o jogo)
            _hudPanel = new GameObject("HudPanel", typeof(RectTransform));
            _hudPanel.transform.SetParent(_canvas.transform, false);
            var rt = _hudPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Canto superior esquerdo: Score + Recorde + Moedas
            _scoreText = MakeText(_hudPanel.transform, "Score: 0", 48,
                new Vector2(0, 1), new Vector2(40, -40), new Vector2(600, 60),
                TextAnchor.UpperLeft);
            _highScoreHudText = MakeText(_hudPanel.transform, "Recorde: 0", 28,
                new Vector2(0, 1), new Vector2(40, -100), new Vector2(600, 40),
                TextAnchor.UpperLeft);
            _highScoreHudText.color = new Color(1f, 0.85f, 0.2f);
            _coinsText = MakeText(_hudPanel.transform, "Moedas: 0", 32,
                new Vector2(0, 1), new Vector2(40, -140), new Vector2(600, 40),
                TextAnchor.UpperLeft);
            _coinsText.color = new Color(1f, 0.95f, 0.3f);

            // Canto superior direito: Vidas
            _livesText = MakeText(_hudPanel.transform, "Vidas: 3", 48,
                new Vector2(1, 1), new Vector2(-40, -40), new Vector2(600, 60),
                TextAnchor.UpperRight);
        }

        private void BuildGameOver()
        {
            _gameOverPanel = MakePanel("GameOverPanel", new Color(0.15f, 0.05f, 0.05f, 0.9f));
            MakeText(_gameOverPanel.transform, "GAME OVER", 120,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1200, 160));
            _finalScoreText = MakeText(_gameOverPanel.transform, "Score: 0", 56,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(800, 80));
            _finalHighText = MakeText(_gameOverPanel.transform, "Recorde: 0", 36,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(800, 60));
            _finalHighText.color = new Color(1f, 0.85f, 0.2f);
            MakeButton(_gameOverPanel.transform, "TENTAR DE NOVO", new Vector2(0, -60),
                       () => GameBootstrap.Instance.StartGame());
            MakeButton(_gameOverPanel.transform, "MENU", new Vector2(0, -160),
                       () => GameBootstrap.Instance.GoToMenu());
        }

        private void BuildVictory()
        {
            _victoryPanel = MakePanel("VictoryPanel", new Color(0.05f, 0.15f, 0.05f, 0.9f));
            MakeText(_victoryPanel.transform, "VITÓRIA!", 120,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(1200, 160));
            _victoryScoreText = MakeText(_victoryPanel.transform, "Score final: 0", 56,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(800, 80));
            _victoryCoinsText = MakeText(_victoryPanel.transform, "Moedas: 0", 36,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 60));
            _victoryHighText = MakeText(_victoryPanel.transform, "Recorde: 0", 36,
                     new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(800, 60));
            _victoryHighText.color = new Color(1f, 0.85f, 0.2f);
            MakeButton(_victoryPanel.transform, "JOGAR DE NOVO", new Vector2(0, -60),
                       () => GameBootstrap.Instance.StartGame());
            MakeButton(_victoryPanel.transform, "MENU", new Vector2(0, -160),
                       () => GameBootstrap.Instance.GoToMenu());
        }

        // ---------- ATUALIZAÇÃO DOS TEXTOS ----------

        // Toda vez que o score muda eu atualizo TODOS os textos de score
        // (HUD, menu, gameover, vitoria). Mais simples do que rastrear quem
        // ta visivel, e nao impacta perf.
        private void RefreshScore()
        {
            var gm = GameManager.Instance;
            int s = gm.Score;
            int hs = gm.HighScore;
            int coins = gm.CoinsCollected;
            if (_scoreText != null)         _scoreText.text         = $"Score: {s}";
            if (_highScoreHudText != null)  _highScoreHudText.text  = $"Recorde: {hs}";
            if (_coinsText != null)         _coinsText.text         = $"Moedas: {coins}";
            if (_menuHighScoreText != null) _menuHighScoreText.text = $"Recorde: {hs}";
            if (_finalScoreText != null)    _finalScoreText.text    = $"Score: {s}";
            if (_finalHighText != null)     _finalHighText.text     = $"Recorde: {hs}";
            if (_victoryScoreText != null)  _victoryScoreText.text  = $"Score final: {s}";
            if (_victoryCoinsText != null)  _victoryCoinsText.text  = $"Moedas: {coins}";
            if (_victoryHighText != null)   _victoryHighText.text   = $"Recorde: {hs}";
        }

        private void RefreshLives()
        {
            if (_livesText != null) _livesText.text = $"Vidas: {GameManager.Instance.Lives}";
        }

        // Cria o "+10" amarelo na tela na posição da moeda/inimigo
        // A magica é o WorldToScreenPoint que converte a coordenada do mundo
        // pra posição em pixel na tela.
        private void SpawnScorePopup(int amount, Vector3 worldPos)
        {
            if (Camera.main == null) return;
            var go = new GameObject("ScorePopup", typeof(Text));
            go.transform.SetParent(_canvas.transform, false);
            var t = go.GetComponent<Text>();
            t.text = "+" + amount;
            t.fontSize = 36;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.95f, 0.3f);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(120, 50);
            var screen = Camera.main.WorldToScreenPoint(worldPos);
            rt.position = screen;
            // ScorePopup faz o resto (subir + desaparecer + auto-destruir)
            go.AddComponent<ScorePopup>();
        }

        // Liga só o painel do estado atual e desliga o resto
        private void OnStateChanged(GameManager.State s)
        {
            _menuPanel.SetActive(s == GameManager.State.Menu);
            _hudPanel.SetActive(s == GameManager.State.Playing);
            _gameOverPanel.SetActive(s == GameManager.State.GameOver);
            _victoryPanel.SetActive(s == GameManager.State.Victory);
            // Refresh pra textos das telas mostrarem valores corretos ao abrir
            RefreshScore();
            RefreshLives();
        }
    }
}
