// SpriteLibrary.cs
// -----------------------------------------------------------------------------
// Carrega o tilemap do Kenney (Pixel Line Platformer) e fatia em sprites
// individuais de 16x16. Acesso por índice (0 a 59).
//
// Layout do tilemap (10 colunas x 6 linhas):
//   index = row * 10 + col
//
// Tiles que uso (mapeei olhando o Preview.png do pack):
//   - Player: linha 4 = coelho azul
//   - Inimigos: linha 5 = slime, caranguejo, etc.
//   - Linha 0-2: chão/grama/céu
//   - Linha 3: vegetação/decoração
//   - Linha 4 col 4: moeda amarela
//
// O tilemap é colocado em Assets/Resources/ pra eu poder carregar via
// Resources.Load em runtime sem precisar configurar nada no editor.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    public static class SpriteLibrary
    {
        public const int TILE_SIZE = 16;
        public const int COLS = 10;
        public const int ROWS = 6;

        // Tiles de fundo / céu
        public const int TILE_BG_SKY   = 0;  // céu liso (sem nuvens) -- base tileada
        public const int TILE_BG_CLOUD = 11; // nuvenzinha individual -- decoração esparsa

        // Terreno
        public const int TILE_GROUND_GRASS = 4;  // grama com terra (chão)
        public const int TILE_GROUND_SOLID = 14; // terra sem grama
        public const int TILE_PLATFORM_MID = 20; // plataforma de grama flutuante (linha 2, col 0)

        // Itens (atlas Kenney não tem moeda dedicada)
        public const int TILE_COIN          = 51; // "moeda" (visual aproximado, não usado — a moeda é gerada em código)
        public const int TILE_FLAG          = 43; // seta amarela — usada como bandeira da meta
        public const int TILE_WEAPON_PICKUP = 50; // ícone de arma vermelha

        // Decorações (sprites do atlas que estavam sem uso). Todas são
        // 1-tile, ficam em cima do chão sem colisão, só pra dar vida visual.
        public const int TILE_DECO_MUSHROOM_BIG   = 30;
        public const int TILE_DECO_MUSHROOM_SMALL = 31;
        public const int TILE_DECO_TULIPS         = 32; // 2 tulipas
        public const int TILE_DECO_TULIP          = 33; // 1 tulipa
        public const int TILE_DECO_PLANT          = 34; // grama dupla
        public const int TILE_DECO_PLANT_SMALL    = 35; // grama simples
        public const int TILE_DECO_FENCE          = 36;
        public const int TILE_DECO_FENCE_BROKEN   = 37;
        public const int TILE_DECO_BUSH           = 38; // arbusto/copinha (árvore pequena)
        // Para uma "árvore alta" (3 tiles de altura) empilho: TOP, MID, BOT.
        public const int TILE_DECO_TREE_TOP       = 39;
        public const int TILE_DECO_TREE_MID       = 49;
        public const int TILE_DECO_TREE_BOT       = 59;

        // Inimigos (cada um tem 2 frames consecutivos no atlas)
        public const int TILE_ENEMY_SLIME  = 53;
        public const int TILE_ENEMY_SLIME2 = 54;
        public const int TILE_ENEMY_CRAB   = 55;
        public const int TILE_ENEMY_CRAB2  = 56;

        // Coelho SEM arma: 45 = idle/pulo  |  46 = corrida
        public const int TILE_PLAYER      = 45; // sprite inicial (desarmado)
        public const int TILE_PLAYER_IDLE = 45;
        public const int TILE_PLAYER_RUN1 = 46;
        public const int TILE_PLAYER_JUMP = 45;

        // Coelho COM arma: 41 = idle  |  40 = corrida  |  42 = pulo
        public const int TILE_PLAYER_IDLE_ARMED = 41;
        public const int TILE_PLAYER_RUN1_ARMED = 40;
        public const int TILE_PLAYER_JUMP_ARMED = 42;

        // Projétil
        public const int TILE_BULLET = 44; // tiro em voo

        private static Sprite[] _cache;
        private static bool _loaded;

        public static Sprite Get(int index)
        {
            EnsureLoaded();
            if (_cache == null) return null;
            if (index < 0 || index >= _cache.Length) return null;
            return _cache[index];
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            // Carrega a textura da pasta Resources/
            // (sem precisar configurar nada, o Unity acha sozinho)
            var tex = Resources.Load<Texture2D>("bbb_tilemap");
            if (tex == null)
            {
                Debug.LogError("[SpriteLibrary] Não achei bbb_tilemap em Assets/Resources/");
                return;
            }

            // IMPORTANTE: filtro Point pra não embaçar o pixel art
            tex.filterMode = FilterMode.Point;

            int total = COLS * ROWS;
            _cache = new Sprite[total];

            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLS; col++)
                {
                    // O Unity usa coords de baixo pra cima (Y invertido em relação à imagem)
                    int x = col * TILE_SIZE;
                    int y = (ROWS - 1 - row) * TILE_SIZE;

                    int index = row * COLS + col;
                    var rect = new Rect(x, y, TILE_SIZE, TILE_SIZE);
                    // pixelsPerUnit = TILE_SIZE faz cada tile ocupar exatamente 1 unidade do mundo.
                    // FullRect é obrigatório pra drawMode=Tiled funcionar sem warning.
                    _cache[index] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), TILE_SIZE,
                                                  0, SpriteMeshType.FullRect);
                }
            }
        }
    }
}
