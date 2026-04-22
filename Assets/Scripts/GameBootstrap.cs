// GameBootstrap.cs
// -----------------------------------------------------------------------------
// Esse é o ponto de entrada do jogo. Ele monta a CENA INTEIRA por código.
//
// Por que fazer assim? Porque pra entregar o protótipo M1 eu queria que bastasse
// adicionar UM unico componente em UM GameObject vazio e pronto, jogo funcional.
// Sem ter que arrastar 20 prefabs, configurar 8 referências no Inspector, etc.
//
// O que ele cria em runtime:
//   - GameManager (singleton de score/vidas/estado)
//   - SfxPlayer (singleton de sons)
//   - Câmera ortográfica + script de follow
//   - Canvas/UI (delega pro UIController)
//   - O nível: chão, plataformas, moedas, inimigos, kill zone, meta
//   - O Player
//
// Dá pra evoluir isso pra ler de um JSON ou ScriptableObject, mas pro escopo
// do M1 ta ok hardcoded.
//
// [DefaultExecutionOrder(-100)] = roda ANTES dos outros scripts. Importante
// porque outros scripts dependem de GameManager.Instance existir.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [Header("Layout")]
        public int levelLength = 60;     // largura total do nivel em unidades Unity
        public float killY = -8f;        // Y abaixo do qual o player morre

        // Truque pra não precisar criar layer custom: uso a layer 2 (IgnoreRaycast)
        // pro player. Ai o BoxCast de chão ignora ele mesmo.
        private const int PLAYER_LAYER = 2;
        private LayerMask _groundMask;

        private GameObject _player;
        private GameObject _levelRoot;       // pai de tudo do nivel (pra destruir tudo de uma vez no restart)
        private CameraFollow2D _cameraFollow;
        private Vector3 _spawnPoint;

        private void Awake()
        {
            Instance = this;

            // groundMask = TUDO menos a layer do player (e a 5 que é a UI por padrão)
            // O ~ inverte a mascara (bitwise NOT). Se ligo bits do que NÃO é chão,
            // depois inverto e fica só o que É chão.
            _groundMask = ~((1 << PLAYER_LAYER) | (1 << 5));

            EnsureManagers();
            BuildCameraIfNeeded();
            BuildUI();
            BuildLevel();
            BuildPlayer();

            // Começa no menu (player desabilitado nesse estado)
            GameManager.Instance.SetState(GameManager.State.Menu);
        }

        // Cria os singletons se não existirem (defensivo)
        private void EnsureManagers()
        {
            if (GameManager.Instance == null)
            {
                var go = new GameObject("[GameManager]");
                go.AddComponent<GameManager>();
            }
            if (SfxPlayer.Instance == null)
            {
                var go = new GameObject("[SfxPlayer]");
                go.AddComponent<SfxPlayer>();
            }
        }

        private void BuildCameraIfNeeded()
        {
            // Se a cena já tem Main Camera (vinda do template) eu reaproveito
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera",
                                        typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            // Configurações 2D
            cam.orthographic = true;       // sem perspectiva
            cam.orthographicSize = 6f;     // metade da altura visível em units
            cam.backgroundColor = new Color(0.45f, 0.65f, 0.85f); // azul céu
            cam.transform.position = new Vector3(0, 2, -10);      // -10 em Z (padrao 2D)

            // Adiciona o script de follow
            _cameraFollow = cam.GetComponent<CameraFollow2D>();
            if (_cameraFollow == null)
                _cameraFollow = cam.gameObject.AddComponent<CameraFollow2D>();
            _cameraFollow.minX = 0f;
            _cameraFollow.maxX = levelLength - 8f;
        }

        private void BuildUI()
        {
            var go = new GameObject("[UI]");
            go.AddComponent<UIController>();
        }

        // ---------- HELPERS DE CONSTRUÇÃO ----------

        // Cache do sprite branco. Reutilizo pra tudo (chão, player, moeda, etc).
        // A cor sai do SpriteRenderer.color, não do sprite em si.
        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
            return _whiteSprite;
        }

        // Cria um "blocão" colorido com sprite branco + collider (opcional)
        // Praticamente todo objeto visual do jogo é feito disso.
        private GameObject MakeBlock(string name, Vector2 pos, Vector2 size,
                                     Color color, Transform parent,
                                     bool addCollider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            // Escala = tamanho do bloco em units (sprite branco é 1x1)
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.color = color;
            if (addCollider) go.AddComponent<BoxCollider2D>();
            return go;
        }

        // ---------- CONSTRUÇÃO DO NIVEL ----------

        private void BuildLevel()
        {
            // Tudo do nivel fica embaixo desse pai. Pra reiniciar é so destruir ele.
            _levelRoot = new GameObject("[Level]");

            // Chão dividido em segmentos com lacunas (buracos pra cair)
            var groundColor = new Color(0.35f, 0.28f, 0.22f); // marrom
            BuildGroundSegment(0, 18, groundColor);
            BuildGroundSegment(22, 14, groundColor);   // gap de 4 units
            BuildGroundSegment(40, 20, groundColor);   // gap de 4 units

            // Plataformas no ar
            var platColor = new Color(0.55f, 0.45f, 0.35f);
            MakeBlock("Platform", new Vector2(8,  2.5f), new Vector2(3, 0.5f), platColor, _levelRoot.transform);
            MakeBlock("Platform", new Vector2(13, 4.0f), new Vector2(3, 0.5f), platColor, _levelRoot.transform);
            MakeBlock("Platform", new Vector2(20, 1.5f), new Vector2(2, 0.5f), platColor, _levelRoot.transform);
            MakeBlock("Platform", new Vector2(28, 3.0f), new Vector2(3, 0.5f), platColor, _levelRoot.transform);
            MakeBlock("Platform", new Vector2(35, 4.5f), new Vector2(3, 0.5f), platColor, _levelRoot.transform);
            MakeBlock("Platform", new Vector2(45, 2.5f), new Vector2(3, 0.5f), platColor, _levelRoot.transform);
            MakeBlock("Platform", new Vector2(50, 4.0f), new Vector2(3, 0.5f), platColor, _levelRoot.transform);

            // Moedas espalhadas
            SpawnCoin(new Vector2(8,  4f));
            SpawnCoin(new Vector2(13, 5.5f));
            SpawnCoin(new Vector2(15, 5.5f));
            SpawnCoin(new Vector2(20, 3f));
            SpawnCoin(new Vector2(28, 4.5f));
            SpawnCoin(new Vector2(35, 6f));
            SpawnCoin(new Vector2(42, 1.5f));
            SpawnCoin(new Vector2(45, 4f));
            SpawnCoin(new Vector2(50, 5.5f));
            SpawnCoin(new Vector2(55, 1.5f));

            // Inimigos
            SpawnEnemy(new Vector2(12, 0.5f), 2.5f);
            SpawnEnemy(new Vector2(28, 0.5f), 3f);
            SpawnEnemy(new Vector2(45, 0.5f), 4f);

            // KillZone (trigger gigante embaixo do mapa pra detectar quedas)
            var kz = new GameObject("KillZone");
            kz.transform.SetParent(_levelRoot.transform, false);
            kz.transform.position = new Vector2(levelLength * 0.5f, killY);
            var kzCol = kz.AddComponent<BoxCollider2D>();
            kzCol.size = new Vector2(levelLength * 2f, 2f);
            kzCol.isTrigger = true;
            kz.AddComponent<KillZone>();

            // Meta (bandeira) no fim do nivel
            var goal = MakeBlock("Goal",
                new Vector2(levelLength - 2f, 1.5f),
                new Vector2(1f, 3f),
                new Color(0.95f, 0.85f, 0.2f),
                _levelRoot.transform,
                addCollider: false);
            // Usa BoxCollider trigger ao invés do solido (pra atravessar)
            var goalCol = goal.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            goal.AddComponent<Goal>();

            // Onde o player nasce
            _spawnPoint = new Vector3(1.5f, 2f, 0f);
        }

        private void BuildGroundSegment(float startX, float length, Color color)
        {
            // Coloco o pivot no meio do segmento (por isso startX + length/2)
            MakeBlock("Ground",
                new Vector2(startX + length * 0.5f, -0.5f),
                new Vector2(length, 1f),
                color,
                _levelRoot.transform);
        }

        private void SpawnCoin(Vector2 pos)
        {
            var go = MakeBlock("Coin", pos, new Vector2(0.4f, 0.4f),
                new Color(1f, 0.85f, 0.1f), _levelRoot.transform, addCollider: false);
            // Circle pra ficar mais "moedal" hahaha
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            go.AddComponent<Coin>();
        }

        private void SpawnEnemy(Vector2 pos, float patrolRange)
        {
            var go = MakeBlock("Enemy", pos, new Vector2(0.8f, 0.8f),
                new Color(0.85f, 0.2f, 0.2f), _levelRoot.transform);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            var e = go.AddComponent<EnemyPatrol>();
            e.patrolRange = patrolRange;
        }

        // ---------- PLAYER ----------

        private void BuildPlayer()
        {
            _player = new GameObject("Player");
            _player.layer = PLAYER_LAYER; // pra ser ignorado pelo BoxCast de chão
            _player.transform.position = _spawnPoint;
            // Escala 0.7 x 1.1 = retangulo levemente alto (tipo pessoa)
            _player.transform.localScale = new Vector3(0.7f, 1.1f, 1f);

            var sr = _player.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.color = new Color(0.2f, 0.5f, 0.95f); // azul
            sr.sortingOrder = 5; // desenha por cima de outros sprites

            _player.AddComponent<BoxCollider2D>();
            var rb = _player.AddComponent<Rigidbody2D>();
            // Continuous evita que ele atravesse o chão em velocidade alta (tunneling)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var ctrl = _player.AddComponent<PlayerController2D>();
            ctrl.groundMask = _groundMask;
            ctrl.SetSpawn(_spawnPoint);

            // Câmera ja olhando pro player
            _cameraFollow.target = _player.transform;
            _cameraFollow.transform.position =
                new Vector3(_spawnPoint.x + 2f, _spawnPoint.y + 1.5f, -10f);

            // Desativa enquanto está no menu
            _player.SetActive(false);
        }

        // ---------- COMANDOS DA UI ----------

        // Botão "JOGAR" / "TENTAR DE NOVO" / "JOGAR DE NOVO"
        public void StartGame()
        {
            // Reconstroi o nivel inteiro (limpa moedas pegas, inimigos mortos, etc)
            if (_levelRoot != null) Destroy(_levelRoot);
            BuildLevel();

            _player.SetActive(true);
            var ctrl = _player.GetComponent<PlayerController2D>();
            ctrl.SetSpawn(_spawnPoint);
            ctrl.ResetForNewGame();

            GameManager.Instance.StartNewGame();
        }

        // Botão "MENU"
        public void GoToMenu()
        {
            _player.SetActive(false);
            GameManager.Instance.SetState(GameManager.State.Menu);
        }
    }
}
