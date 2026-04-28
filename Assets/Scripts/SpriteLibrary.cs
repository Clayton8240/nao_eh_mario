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

        // Constantes pra deixar o código que usa mais legível
        // (mapeei na unha olhando a imagem Preview.png)
        public const int TILE_PLAYER       = 41; // coelho azul parado
        public const int TILE_COIN         = 44; // moeda amarela
        public const int TILE_GROUND_GRASS = 4;  // grama com terra (row 0, col 4)
        public const int TILE_GROUND_SOLID = 14; // terra sem grama (row 1, col 4)
        // ATENÇÃO: TILE_PLATFORM_MID DEVE ser diferente de TILE_GROUND_GRASS (4)!
        // Index 24 = row 2, col 4 do tilemap (bloco de pedra/madeira).
        // Se ficar errado visualmente, ajuste aqui pro índice certo do tilemap.
        public const int TILE_PLATFORM_MID = 24; // plataforma flutuante (row 2, col 4)
        public const int TILE_FLAG         = 43; // setinha amarela = bandeira/meta
        public const int TILE_ENEMY_SLIME  = 53;
        public const int TILE_ENEMY_CRAB   = 55;

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
                    // pixelsPerUnit = TILE_SIZE faz cada tile ocupar exatamente 1 unidade do mundo
                    _cache[index] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), TILE_SIZE);
                }
            }
        }
    }
}
