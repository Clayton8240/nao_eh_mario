// GameBootstrap.cs
// -----------------------------------------------------------------------------
// Esse é o ponto de entrada do jogo. Ele monta a CENA INTEIRA por código.
//
// Por que fazer assim? Porque pra entregar o protótipo eu queria que bastasse
// adicionar UM unico componente em UM GameObject vazio e pronto, jogo funcional.
// Sem ter que arrastar 20 prefabs, configurar 8 referências no Inspector, etc.
//
// O que ele cria em runtime:
//   - GameManager (singleton de score/vidas/estado/fase)
//   - SfxPlayer (singleton de sons)
//   - Câmera ortográfica + script de follow
//   - Canvas/UI (delega pro UIController)
//   - O nível CORRENTE (3 fases no LevelLibrary)
//   - O Player (coelho azul do Kenney)
//
// Mudanças do BBB (Blue Bunny Blaster):
//   - Multi-fase: 3 fases dentro de LevelLibrary, GameManager.CurrentLevel.
//   - Sprites: usa o pack Kenney Pixel Line Platformer via SpriteLibrary.
//   - Quando o player toca a meta, o GameManager incrementa o nivel e o
//     Bootstrap reconstroi o cenario pra próxima fase.
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
            // Pre-carrega fase 1 só pra ter algo no fundo do menu.
            // O StartGame depois reconstroi tudo do zero.
            BuildLevel(LevelLibrary.Get(1));
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
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f); // azul céu igual o pack Kenney
            cam.transform.position = new Vector3(0, 2, -10);      // -10 em Z (padrao 2D)

            // Adiciona o script de follow
            _cameraFollow = cam.GetComponent<CameraFollow2D>();
            if (_cameraFollow == null)
                _cameraFollow = cam.gameObject.AddComponent<CameraFollow2D>();
            _cameraFollow.minX = 0f;
            // maxX é setado em BuildLevel pq depende do tamanho da fase
        }

        private void BuildUI()
        {
            var go = new GameObject("[UI]");
            go.AddComponent<UIController>();
        }

        // ---------- HELPERS DE CONSTRUÇÃO ----------

        // Cache do sprite branco (usado só em coisas sem sprite específico,
        // tipo a haste da bandeira ou fallback se o tilemap não carregar).
        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
            return _whiteSprite;
        }

        // Cria um GameObject com sprite do tilemap. Se o tile não existir
        // (por algum motivo o Resources não carregou) usa sprite branco com
        // a cor de fallback, pra pelo menos enxergar onde está.
        private GameObject MakeSpriteObject(string name, Vector2 pos, Vector2 size,
                                            int tileIndex, Transform parent,
                                            bool addCollider = true,
                                            bool tiled = false,
                                            Color? tintIfFallback = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            var tile = SpriteLibrary.Get(tileIndex);
            if (tile != null)
            {
                sr.sprite = tile;
                if (tiled)
                {
                    // drawMode Tiled faz o sprite repetir pelo tamanho do RectTransform.
                    // Nesse modo NÃO uso transform.localScale, uso sr.size.
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = size;
                }
                else
                {
                    // Sprite normal: ajusto a escala. Como o sprite tem ppu=16 e é 16px,
                    // ele já ocupa 1 unit. Multiplicando por size, fica em "size" units.
                    go.transform.localScale = new Vector3(size.x, size.y, 1f);
                }
            }
            else
            {
                // Fallback: sprite branco colorido (caso o tilemap não tenha carregado)
                sr.sprite = WhiteSprite();
                sr.color = tintIfFallback ?? Color.magenta;
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            if (addCollider)
            {
                var col = go.AddComponent<BoxCollider2D>();
                if (tiled) col.size = size; // collider precisa cobrir o tamanho tileado
            }
            return go;
        }

        // ---------- CONSTRUÇÃO DO NIVEL ----------

        // Constroi o cenario inteiro a partir de um LevelData.
        // Limpa o anterior (se existir) antes de reconstruir.
        public void BuildLevel(LevelData lv)
        {
            if (_levelRoot != null) Destroy(_levelRoot);

            _levelRoot = new GameObject("[Level] " + lv.name);

            // Atualiza limite da camera pelo tamanho da fase
            if (_cameraFollow != null) _cameraFollow.maxX = lv.length - 8f;

            // ----- CHÃO -----
            foreach (var g in lv.ground)
            {
                BuildGroundSegment(g.startX, g.length);
            }

            // ----- PLATAFORMAS NO AR -----
            foreach (var p in lv.platforms)
            {
                MakeSpriteObject("Platform", p.pos, p.size,
                    SpriteLibrary.TILE_PLATFORM_MID, _levelRoot.transform,
                    tiled: true,
                    tintIfFallback: new Color(0.55f, 0.45f, 0.35f));
            }

            // ----- MOEDAS -----
            foreach (var c in lv.coins) SpawnCoin(c);

            // ----- INIMIGOS -----
            foreach (var e in lv.enemies) SpawnEnemy(e);

            // ----- KILL ZONE -----
            // Trigger gigante embaixo do mapa pra detectar quedas no abismo
            var kz = new GameObject("KillZone");
            kz.transform.SetParent(_levelRoot.transform, false);
            kz.transform.position = new Vector2(lv.length * 0.5f, killY);
            var kzCol = kz.AddComponent<BoxCollider2D>();
            kzCol.size = new Vector2(lv.length * 2f, 2f);
            kzCol.isTrigger = true;
            kz.AddComponent<KillZone>();

            // ----- META (bandeira) -----
            // Uso a "seta amarela" tile 43 como bandeira em cima de uma haste branca.
            float goalX = lv.length - 2f;

            // Haste (visual, sem collider)
            var pole = new GameObject("GoalPole");
            pole.transform.SetParent(_levelRoot.transform, false);
            pole.transform.position = new Vector2(goalX, 1.5f);
            pole.transform.localScale = new Vector3(0.1f, 3f, 1f);
            var poleSr = pole.AddComponent<SpriteRenderer>();
            poleSr.sprite = WhiteSprite();
            poleSr.color = new Color(0.9f, 0.9f, 0.9f);

            // Bandeirinha (sprite do tile + trigger pra detectar player)
            var flag = MakeSpriteObject("Goal", new Vector2(goalX, 3f),
                new Vector2(1f, 1f), SpriteLibrary.TILE_FLAG,
                _levelRoot.transform, addCollider: false,
                tintIfFallback: new Color(0.95f, 0.85f, 0.2f));
            // Box trigger maior pra ficar facil de "tocar" a meta (cobre a haste toda)
            var goalCol = flag.AddComponent<BoxCollider2D>();
            goalCol.size = new Vector2(1.2f, 3.5f);
            goalCol.offset = new Vector2(0f, -1f);
            goalCol.isTrigger = true;
            flag.AddComponent<Goal>();

            // Onde o player nasce (definido pela fase)
            _spawnPoint = new Vector3(lv.spawn.x, lv.spawn.y, 0f);
        }

        private void BuildGroundSegment(float startX, float length)
        {
            // Topo (com grama) -- 1 unidade de altura no Y=-0.5
            MakeSpriteObject("GroundTop",
                new Vector2(startX + length * 0.5f, -0.5f),
                new Vector2(length, 1f),
                SpriteLibrary.TILE_GROUND_GRASS,
                _levelRoot.transform,
                tiled: true,
                tintIfFallback: new Color(0.4f, 0.6f, 0.3f));

            // Camada de terra abaixo (decoração visual, sem collider novo
            // pq o de cima ja segura o player).
            var dirt = MakeSpriteObject("GroundDirt",
                new Vector2(startX + length * 0.5f, -2f),
                new Vector2(length, 2f),
                SpriteLibrary.TILE_GROUND_SOLID,
                _levelRoot.transform,
                addCollider: false,
                tiled: true,
                tintIfFallback: new Color(0.45f, 0.32f, 0.22f));
            dirt.GetComponent<SpriteRenderer>().sortingOrder = -1;
        }

        private void SpawnCoin(Vector2 pos)
        {
            var go = MakeSpriteObject("Coin", pos, new Vector2(0.6f, 0.6f),
                SpriteLibrary.TILE_COIN, _levelRoot.transform,
                addCollider: false,
                tintIfFallback: new Color(1f, 0.85f, 0.1f));
            // Trigger circular pra ser mais "moedal"
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            go.AddComponent<Coin>();
        }

        private void SpawnEnemy(EnemySpawn def)
        {
            var go = MakeSpriteObject("Enemy", def.pos, new Vector2(0.9f, 0.9f),
                def.spriteTile, _levelRoot.transform,
                tintIfFallback: new Color(0.85f, 0.2f, 0.2f));
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            var e = go.AddComponent<EnemyPatrol>();
            e.patrolRange = def.patrolRange;
        }

        // ---------- PLAYER ----------

        private void BuildPlayer()
        {
            _player = new GameObject("Player");
            _player.layer = PLAYER_LAYER; // pra ser ignorado pelo BoxCast de chão
            _player.transform.position = _spawnPoint;
            // Coelhinho do Kenney é quadrado 16x16 = 1 unit. Boto 1.1 pra ficar "fofo"
            _player.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            var sr = _player.AddComponent<SpriteRenderer>();
            var bunny = SpriteLibrary.Get(SpriteLibrary.TILE_PLAYER);
            if (bunny != null)
            {
                sr.sprite = bunny;
                // sem tint - o coelho ja é azul \o/
            }
            else
            {
                sr.sprite = WhiteSprite();
                sr.color = new Color(0.2f, 0.5f, 0.95f); // azul fallback
            }
            sr.sortingOrder = 5; // desenha por cima de outros sprites

            // Collider menor que o sprite pra dar uma "folguinha" nas colisoes
            var box = _player.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.7f, 0.95f);

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

        // ---------- COMANDOS DA UI / FLUXO DE FASES ----------

        // Botão "JOGAR" / "TENTAR DE NOVO" / "JOGAR DE NOVO".
        // Sempre começa da fase 1.
        public void StartGame()
        {
            GameManager.Instance.StartNewGame();   // zera score/vidas + level=1
            BuildLevel(LevelLibrary.Get(GameManager.Instance.CurrentLevel));
            RespawnPlayerForCurrentLevel();
        }

        // Chamado pelo Goal quando o jogador completa uma fase intermediária.
        // O GameManager ja foi avisado (e ja incrementou CurrentLevel) antes disso.
        public void LoadCurrentLevel()
        {
            BuildLevel(LevelLibrary.Get(GameManager.Instance.CurrentLevel));
            RespawnPlayerForCurrentLevel();
        }

        private void RespawnPlayerForCurrentLevel()
        {
            _player.SetActive(true);
            var ctrl = _player.GetComponent<PlayerController2D>();
            ctrl.SetSpawn(_spawnPoint);
            ctrl.ResetForNewGame();
            // Realinha a camera pro novo spawn (senão o lerp leva 1s pra alcançar)
            if (_cameraFollow != null)
            {
                _cameraFollow.transform.position =
                    new Vector3(_spawnPoint.x + 2f, _spawnPoint.y + 1.5f, -10f);
            }
        }

        // Botão "MENU"
        public void GoToMenu()
        {
            // Cancela qualquer Invoke pendente (ex: Respawn agendado depois de uma morte)
            // antes de desativar. Sem isso, o Respawn pode reativar o collider enquanto
            // o player está no menu, o que não quebra o jogo mas é um bug silencioso.
            _player.GetComponent<PlayerController2D>()?.CancelInvoke();
            _player.SetActive(false);
            GameManager.Instance.SetState(GameManager.State.Menu);
        }
    }
}
