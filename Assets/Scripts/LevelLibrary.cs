// LevelLibrary.cs
// -----------------------------------------------------------------------------
// Definição estática das 3 fases do BBB. Cada fase tem:
//   - nome
//   - comprimento total (em units)
//   - segmentos de chão (com gaps no meio)
//   - plataformas no ar
//   - moedas
//   - inimigos (com tipo de sprite)
//
// Coloquei tudo aqui pra ser fácil ajustar valores sem mexer no Bootstrap.
// Se um dia precisar de mais fases é só adicionar item no array.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace NaoEMario
{
    [System.Serializable]
    public struct GroundSeg { public float startX; public float length; }

    [System.Serializable]
    public struct Platform { public Vector2 pos; public Vector2 size; }

    [System.Serializable]
    public struct EnemySpawn
    {
        public Vector2 pos;
        public float patrolRange;
        public int spriteTile; // SpriteLibrary.TILE_ENEMY_*
    }

    // Decoração puramente visual: sprite renderizado sem colisão.
    // Use heightTiles>1 para empílhar tiles (útil para árvores 3-tiles).
    [System.Serializable]
    public struct Decoration
    {
        public Vector2 pos;
        public int tileIndex;
        public float scale;     // 1 = 1 unit; 0 deixa como 1
        public int heightTiles; // 1 = single tile; >1 empilha (ex.: árvore alta)
        public int topTileIndex; public int midTileIndex; public int botTileIndex; // só quando heightTiles>1
    }

    public class LevelData
    {
        public string name;
        public int length;
        public List<GroundSeg> ground = new List<GroundSeg>();
        public List<Platform> platforms = new List<Platform>();
        public List<Vector2> coins = new List<Vector2>();
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
        // Checkpoints intermediários: ao tocar, atualizam o respawn do player.
        public List<Vector2> checkpoints = new List<Vector2>();
        // Plataformas que desaparecem após o player pousar nelas (fase 3).
        public List<Platform> disappearingPlatforms = new List<Platform>();
        // Moedas secretas: posicionadas fora do caminho óbvio (1 por fase).
        public List<Vector2> secretCoins = new List<Vector2>();
        // Decorações de cenário (flores, arbustos, fences, árvores).
        public List<Decoration> decorations = new List<Decoration>();
        public bool hasWeaponPickup;
        public Vector2 weaponPickup;
        public Vector2 spawn = new Vector2(1.5f, 2f);
    }

    public static class LevelLibrary
    {
        private static LevelData[] _levels;

        public static int Count
        {
            get
            {
                EnsureBuilt();
                return _levels.Length;
            }
        }

        public static LevelData Get(int oneBasedIndex)
        {
            EnsureBuilt();
            int i = Mathf.Clamp(oneBasedIndex - 1, 0, _levels.Length - 1);
            return _levels[i];
        }

        private static void EnsureBuilt()
        {
            if (_levels != null) return;
            _levels = new LevelData[]
            {
                BuildLevel1(),
                BuildLevel2(),
                BuildLevel3(),
            };
        }

        // Aliases de tile pra deixar a leitura das listas mais curta.
        private const int SLIME = SpriteLibrary.TILE_ENEMY_SLIME;
        private const int CRAB  = SpriteLibrary.TILE_ENEMY_CRAB;

        // Helpers para construir decorações de forma compacta.
        private static Decoration Deco(float x, float y, int tile, float scale = 1f)
            => new Decoration { pos = new Vector2(x, y), tileIndex = tile, scale = scale, heightTiles = 1 };
        private static Decoration Tree(float x, float groundY = 0.5f)
            => new Decoration
            {
                pos = new Vector2(x, groundY),
                heightTiles = 3,
                botTileIndex = SpriteLibrary.TILE_DECO_TREE_BOT,
                midTileIndex = SpriteLibrary.TILE_DECO_TREE_MID,
                topTileIndex = SpriteLibrary.TILE_DECO_TREE_TOP,
                scale = 1f,
            };

        // ---------- FASE 1: tutorial estendido / Floresta Calma ----------
        // Comprimento ~120 units (~2x do original). Ritmo: introduz pulos e
        // stomp na primeira metade, e tiro na segunda (depois do pickup).
        private static LevelData BuildLevel1()
        {
            var lv = new LevelData { name = "Floresta Calma", length = 120 };

            // Chão majoritariamente contínuo, com poucos buracos largos
            // (jogador ainda está aprendendo pulo).
            lv.ground.Add(new GroundSeg { startX = 0,   length = 24 });
            lv.ground.Add(new GroundSeg { startX = 28,  length = 22 });
            lv.ground.Add(new GroundSeg { startX = 54,  length = 20 });
            lv.ground.Add(new GroundSeg { startX = 78,  length = 18 });
            lv.ground.Add(new GroundSeg { startX = 100, length = 20 });

            // Plataformas baixas, espaçadas — convidam a explorar mas não punem.
            lv.platforms.Add(new Platform { pos = new Vector2(8,   2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(14,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(20,  2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(33,  3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(40,  4.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(46,  3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(58,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(65,  2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(82,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(88,  4.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(94,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(106, 3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(113, 4.0f), size = new Vector2(3, 0.5f) });

            lv.coins.AddRange(new[]
            {
                // Linha sobre o chão (pickup quase passivo enquanto corre).
                new Vector2(8,   4f),   new Vector2(14, 5f),   new Vector2(20, 4f),
                // Arcos sobre os GAPS — o jogador passa por elas durante o pulo.
                new Vector2(25,  3.5f), new Vector2(26, 4.0f), new Vector2(27, 3.5f),
                new Vector2(33,  4.5f), new Vector2(40, 5.5f), new Vector2(46, 4.5f),
                new Vector2(51,  3.5f), new Vector2(52, 4.0f), new Vector2(53, 3.5f),
                new Vector2(58,  5f),   new Vector2(65, 4f),
                new Vector2(75,  3.5f), new Vector2(76, 4.0f), new Vector2(77, 3.5f),
                new Vector2(82,  5f),   new Vector2(88, 6f),   new Vector2(94, 5f),
                new Vector2(97,  3.5f), new Vector2(98, 4.0f), new Vector2(99, 3.5f),
                new Vector2(106, 4.5f), new Vector2(113, 5.5f),new Vector2(118, 4f),
            });

            lv.enemies.Add(new EnemySpawn { pos = new Vector2(11,  0.5f), patrolRange = 2f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(20,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(36,  0.5f), patrolRange = 2.5f, spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(45,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(60,  0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(70,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(84,  0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(92,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(108, 0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(115, 0.5f), patrolRange = 2f,   spriteTile = CRAB  });

            lv.hasWeaponPickup = true;
            lv.weaponPickup = new Vector2(50f, 1.2f);

            // Mesmo na fase 1 ajuda ter 1 checkpoint pra não refazer o caminho todo.
            lv.checkpoints.Add(new Vector2(60f, 1f));

            // Decorações de cenário: árvores, flores, fences ao longo do chão.
            // Tudo y=0.5 (em cima do topo do chão).
            lv.decorations.Add(Tree(3f));
            lv.decorations.Add(Deco(5f,   0.5f, SpriteLibrary.TILE_DECO_TULIPS));
            lv.decorations.Add(Deco(17f,  0.5f, SpriteLibrary.TILE_DECO_BUSH));
            lv.decorations.Add(Deco(19f,  0.5f, SpriteLibrary.TILE_DECO_TULIP));
            lv.decorations.Add(Deco(22f,  0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Deco(23f,  0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Tree(30f));
            lv.decorations.Add(Deco(38f,  0.5f, SpriteLibrary.TILE_DECO_PLANT));
            lv.decorations.Add(Deco(43f,  0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_BIG));
            lv.decorations.Add(Deco(44f,  0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_SMALL));
            lv.decorations.Add(Deco(48f,  0.5f, SpriteLibrary.TILE_DECO_FENCE_BROKEN));
            lv.decorations.Add(Deco(49f,  0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Tree(57f));
            lv.decorations.Add(Deco(63f,  0.5f, SpriteLibrary.TILE_DECO_TULIPS));
            lv.decorations.Add(Deco(67f,  0.5f, SpriteLibrary.TILE_DECO_PLANT_SMALL));
            lv.decorations.Add(Deco(73f,  0.5f, SpriteLibrary.TILE_DECO_BUSH));
            lv.decorations.Add(Deco(80f,  0.5f, SpriteLibrary.TILE_DECO_TULIP));
            lv.decorations.Add(Tree(85f));
            lv.decorations.Add(Deco(93f,  0.5f, SpriteLibrary.TILE_DECO_PLANT));
            lv.decorations.Add(Deco(95f,  0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Deco(102f, 0.5f, SpriteLibrary.TILE_DECO_BUSH));
            lv.decorations.Add(Deco(105f, 0.5f, SpriteLibrary.TILE_DECO_TULIPS));
            lv.decorations.Add(Deco(110f, 0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_BIG));
            lv.decorations.Add(Tree(116f));

            return lv;
        }

        // ---------- FASE 2: média / Caverna Saltitante ----------
        // Comprimento ~160 units. Mais buracos médios, plataformas variando
        // de altura, dois trechos com grupos de inimigos.
        private static LevelData BuildLevel2()
        {
            var lv = new LevelData { name = "Caverna Saltitante", length = 160 };

            lv.ground.Add(new GroundSeg { startX = 0,   length = 18 });
            lv.ground.Add(new GroundSeg { startX = 22,  length = 14 });
            lv.ground.Add(new GroundSeg { startX = 40,  length = 12 });
            lv.ground.Add(new GroundSeg { startX = 56,  length = 10 });
            lv.ground.Add(new GroundSeg { startX = 70,  length = 14 });
            lv.ground.Add(new GroundSeg { startX = 88,  length = 10 });
            lv.ground.Add(new GroundSeg { startX = 102, length = 12 });
            lv.ground.Add(new GroundSeg { startX = 118, length = 10 });
            lv.ground.Add(new GroundSeg { startX = 132, length = 12 });
            lv.ground.Add(new GroundSeg { startX = 148, length = 12 });

            lv.platforms.Add(new Platform { pos = new Vector2(8,   2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(13,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(20,  1.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(28,  3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(35,  4.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(45,  2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(50,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(60,  4.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(67,  3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(75,  3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(85,  4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(92,  3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(100, 4.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(108, 3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(116, 4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(124, 3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(130, 4.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(140, 3.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(146, 4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(154, 3.0f), size = new Vector2(3, 0.5f) });

            lv.coins.AddRange(new[]
            {
                new Vector2(8,   4f),   new Vector2(13, 5f),   new Vector2(15, 5f),
                new Vector2(20,  3f),   new Vector2(28, 4.5f), new Vector2(35, 5.5f),
                // Arco sobre o gap 36-40
                new Vector2(37,  4.0f), new Vector2(38, 4.5f), new Vector2(39, 4.0f),
                new Vector2(45,  4f),   new Vector2(50, 5f),
                // Arco sobre o gap 52-56
                new Vector2(53,  4.0f), new Vector2(54, 4.5f), new Vector2(55, 4.0f),
                new Vector2(60,  5.5f), new Vector2(67, 4.5f),
                // Arco sobre o gap 66-70
                new Vector2(68,  4.0f), new Vector2(69, 4.5f),
                new Vector2(75,  5f),   new Vector2(80, 4.5f), new Vector2(85, 6f),
                // Arco sobre o gap 84-88 já incluído acima (85)
                new Vector2(92,  4.5f), new Vector2(95, 4.5f), new Vector2(100, 5.5f),
                new Vector2(108, 5f),   new Vector2(116, 6f),  new Vector2(120, 4.5f),
                new Vector2(124, 4.5f), new Vector2(130, 5.5f),new Vector2(140, 5f),
                new Vector2(146, 6f),   new Vector2(154, 4.5f),new Vector2(157, 4f),
            });

            lv.enemies.Add(new EnemySpawn { pos = new Vector2(12,  0.5f), patrolRange = 2.5f, spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(28,  0.5f), patrolRange = 3f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(45,  0.5f), patrolRange = 4f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(60,  0.5f), patrolRange = 3f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(75,  0.5f), patrolRange = 4f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(92,  0.5f), patrolRange = 3f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(106, 0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(122, 0.5f), patrolRange = 3f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(138, 0.5f), patrolRange = 4f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(152, 0.5f), patrolRange = 3f,   spriteTile = CRAB  });

            lv.hasWeaponPickup = true;
            lv.weaponPickup = new Vector2(33f, 3.7f);

            // Checkpoints distribuídos a cada ~50 units.
            lv.checkpoints.Add(new Vector2(56f,  1f));
            lv.checkpoints.Add(new Vector2(102f, 1f));

            // Decorações: caverna fofa com flores, fences e arbustos.
            lv.decorations.Add(Deco(4f,   0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_BIG));
            lv.decorations.Add(Deco(5f,   0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_SMALL));
            lv.decorations.Add(Deco(15f,  0.5f, SpriteLibrary.TILE_DECO_PLANT));
            lv.decorations.Add(Tree(24f));
            lv.decorations.Add(Deco(31f,  0.5f, SpriteLibrary.TILE_DECO_TULIPS));
            lv.decorations.Add(Deco(41f,  0.5f, SpriteLibrary.TILE_DECO_BUSH));
            lv.decorations.Add(Deco(51f,  0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Deco(58f,  0.5f, SpriteLibrary.TILE_DECO_TULIP));
            lv.decorations.Add(Deco(63f,  0.5f, SpriteLibrary.TILE_DECO_PLANT_SMALL));
            lv.decorations.Add(Tree(72f));
            lv.decorations.Add(Deco(82f,  0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_BIG));
            lv.decorations.Add(Deco(89f,  0.5f, SpriteLibrary.TILE_DECO_FENCE_BROKEN));
            lv.decorations.Add(Deco(90f,  0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Deco(96f,  0.5f, SpriteLibrary.TILE_DECO_BUSH));
            lv.decorations.Add(Deco(104f, 0.5f, SpriteLibrary.TILE_DECO_TULIPS));
            lv.decorations.Add(Tree(112f));
            lv.decorations.Add(Deco(120f, 0.5f, SpriteLibrary.TILE_DECO_PLANT));
            lv.decorations.Add(Deco(126f, 0.5f, SpriteLibrary.TILE_DECO_TULIP));
            lv.decorations.Add(Deco(134f, 0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Deco(135f, 0.5f, SpriteLibrary.TILE_DECO_FENCE));
            lv.decorations.Add(Deco(142f, 0.5f, SpriteLibrary.TILE_DECO_BUSH));
            lv.decorations.Add(Tree(150f));
            lv.decorations.Add(Deco(157f, 0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_BIG));

            // Moeda secreta: plataforma alta isolada no meio do gap maior (x=85, y=4.5),
            // o jogador precisa chegar sem cair no buraco abaixo.
            lv.secretCoins.Add(new Vector2(85f, 6.8f));

            return lv;
        }

        // ---------- FASE 3: difícil / Pulo Final ----------
        // Comprimento ~210 units. Buracos curtos e plataformas altas exigem
        // precisão; presença mais densa de inimigos e checkpoints frequentes.
        private static LevelData BuildLevel3()
        {
            var lv = new LevelData { name = "Pulo Final", length = 210 };

            // Segmentos curtos com gaps consistentes.
            lv.ground.Add(new GroundSeg { startX = 0,   length = 12 });
            lv.ground.Add(new GroundSeg { startX = 16,  length = 6  });
            lv.ground.Add(new GroundSeg { startX = 26,  length = 8  });
            lv.ground.Add(new GroundSeg { startX = 38,  length = 5  });
            lv.ground.Add(new GroundSeg { startX = 47,  length = 8  });
            lv.ground.Add(new GroundSeg { startX = 59,  length = 6  });
            lv.ground.Add(new GroundSeg { startX = 69,  length = 11 });
            lv.ground.Add(new GroundSeg { startX = 84,  length = 6  });
            lv.ground.Add(new GroundSeg { startX = 94,  length = 8  });
            lv.ground.Add(new GroundSeg { startX = 106, length = 5  });
            lv.ground.Add(new GroundSeg { startX = 115, length = 7  });
            lv.ground.Add(new GroundSeg { startX = 126, length = 6  });
            lv.ground.Add(new GroundSeg { startX = 136, length = 9  });
            lv.ground.Add(new GroundSeg { startX = 149, length = 6  });
            lv.ground.Add(new GroundSeg { startX = 159, length = 8  });
            lv.ground.Add(new GroundSeg { startX = 171, length = 5  });
            lv.ground.Add(new GroundSeg { startX = 180, length = 9  });
            lv.ground.Add(new GroundSeg { startX = 193, length = 17 });

            lv.platforms.Add(new Platform { pos = new Vector2(7,   3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(13,  4.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(20,  3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(31,  3.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(36,  4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(43,  3.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(52,  4.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(57,  3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(64,  4.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(72,  3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(80,  4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(88,  3.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(96,  4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(104, 3.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(112, 4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(120, 3.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(128, 4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(135, 3.0f), size = new Vector2(2, 0.5f) });
            // x=142: plataforma desaparecedora (não adicionar aqui, fica em disappearingPlatforms)
            lv.platforms.Add(new Platform { pos = new Vector2(150, 3.5f), size = new Vector2(2, 0.5f) });
            // x=158: plataforma desaparecedora
            lv.platforms.Add(new Platform { pos = new Vector2(166, 3.5f), size = new Vector2(2, 0.5f) });
            // x=174: plataforma desaparecedora
            lv.platforms.Add(new Platform { pos = new Vector2(182, 3.5f), size = new Vector2(2, 0.5f) });
            // x=190: plataforma desaparecedora
            lv.platforms.Add(new Platform { pos = new Vector2(200, 3.5f), size = new Vector2(2, 0.5f) });

            lv.coins.AddRange(new[]
            {
                new Vector2(7,   4.5f), new Vector2(13, 5.5f), new Vector2(20, 4.5f),
                // Arco sobre gap 22-26
                new Vector2(23,  4.0f), new Vector2(24, 4.5f), new Vector2(25, 4.0f),
                new Vector2(31,  5f),   new Vector2(36, 6f),
                // Arco sobre gap 34-38
                new Vector2(35,  4.5f), new Vector2(36, 5.0f),
                new Vector2(43,  5f),
                // Arco sobre gap 43-47
                new Vector2(44,  4.5f), new Vector2(45, 5.0f), new Vector2(46, 4.5f),
                new Vector2(52,  5.5f), new Vector2(57, 4.5f), new Vector2(64, 5.5f),
                new Vector2(72,  4.5f),
                // Arcos sobre os gaps consecutivos da segunda metade
                new Vector2(81,  5.0f), new Vector2(82, 5.5f), new Vector2(83, 5.0f),
                new Vector2(80,  6f),   new Vector2(88, 5f),
                new Vector2(91,  5.0f), new Vector2(92, 5.5f), new Vector2(93, 5.0f),
                new Vector2(96,  6f),   new Vector2(104, 5f),
                new Vector2(112, 6f),   new Vector2(113, 5.5f),new Vector2(120, 5f),
                new Vector2(123, 5.0f), new Vector2(124, 5.5f),new Vector2(125, 5.0f),
                new Vector2(128, 6f),   new Vector2(135, 4.5f),new Vector2(142, 6f),
                new Vector2(146, 5.0f), new Vector2(147, 5.5f),
                new Vector2(150, 5f),   new Vector2(158, 6f),  new Vector2(166, 5f),
                new Vector2(168, 5.0f), new Vector2(169, 5.5f),
                new Vector2(174, 6f),   new Vector2(182, 5f),  new Vector2(190, 6f),
                new Vector2(200, 5f),   new Vector2(205, 4f),
            });

            lv.enemies.Add(new EnemySpawn { pos = new Vector2(10,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(28,  0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(40,  0.5f), patrolRange = 1.5f, spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(50,  0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(62,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(72,  0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(86,  0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(96,  0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(108, 0.5f), patrolRange = 1.5f, spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(117, 0.5f), patrolRange = 2f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(128, 0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(138, 0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(151, 0.5f), patrolRange = 2f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(161, 0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(173, 0.5f), patrolRange = 1.5f, spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(183, 0.5f), patrolRange = 3f,   spriteTile = SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(196, 0.5f), patrolRange = 3f,   spriteTile = CRAB  });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(204, 0.5f), patrolRange = 3f,   spriteTile = SLIME });

            lv.hasWeaponPickup = true;
            lv.weaponPickup = new Vector2(56f, 3.8f);

            // Checkpoints frequentes (~40 units) na fase mais punitiva.
            lv.checkpoints.Add(new Vector2(48f,  1f));
            lv.checkpoints.Add(new Vector2(85f,  1f));
            lv.checkpoints.Add(new Vector2(137f, 1f));
            lv.checkpoints.Add(new Vector2(181f, 1f));

            // Decorações escassas — pra reforar o tom desolado/difícil da fase.
            lv.decorations.Add(Tree(2f));
            lv.decorations.Add(Deco(8f,   0.5f, SpriteLibrary.TILE_DECO_FENCE_BROKEN));
            lv.decorations.Add(Deco(18f,  0.5f, SpriteLibrary.TILE_DECO_PLANT_SMALL));
            lv.decorations.Add(Deco(30f,  0.5f, SpriteLibrary.TILE_DECO_FENCE_BROKEN));
            lv.decorations.Add(Deco(48f,  0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_BIG));
            lv.decorations.Add(Tree(70f));
            lv.decorations.Add(Deco(76f,  0.5f, SpriteLibrary.TILE_DECO_PLANT));
            lv.decorations.Add(Deco(78f,  0.5f, SpriteLibrary.TILE_DECO_FENCE_BROKEN));
            lv.decorations.Add(Deco(99f,  0.5f, SpriteLibrary.TILE_DECO_TULIP));
            lv.decorations.Add(Deco(120f, 0.5f, SpriteLibrary.TILE_DECO_FENCE_BROKEN));
            lv.decorations.Add(Deco(140f, 0.5f, SpriteLibrary.TILE_DECO_PLANT_SMALL));
            lv.decorations.Add(Deco(143f, 0.5f, SpriteLibrary.TILE_DECO_MUSHROOM_SMALL));
            lv.decorations.Add(Tree(165f));
            lv.decorations.Add(Deco(186f, 0.5f, SpriteLibrary.TILE_DECO_PLANT));
            lv.decorations.Add(Tree(196f));
            lv.decorations.Add(Deco(202f, 0.5f, SpriteLibrary.TILE_DECO_TULIPS));
            lv.decorations.Add(Deco(207f, 0.5f, SpriteLibrary.TILE_DECO_BUSH));
            // Moeda secreta: em cima da plataforma mais alta do trecho final,
            // requer pulo duplo (plataforma 88 y=4.5 -> plataforma 94 y=3.5 -> moeda acima).
            lv.secretCoins.Add(new Vector2(89f, 6.5f));
            // Plataformas desaparecedoras: segunda metade da fase 3 (a partir de x=130).
            // Identificadas visualmente pelo sprite laranja.
            lv.disappearingPlatforms.Add(new Platform { pos = new Vector2(142, 4.5f), size = new Vector2(2, 0.5f) });
            lv.disappearingPlatforms.Add(new Platform { pos = new Vector2(158, 4.5f), size = new Vector2(2, 0.5f) });
            lv.disappearingPlatforms.Add(new Platform { pos = new Vector2(174, 4.5f), size = new Vector2(2, 0.5f) });
            lv.disappearingPlatforms.Add(new Platform { pos = new Vector2(190, 4.5f), size = new Vector2(2, 0.5f) });

            // Moeda secreta: requer subir pela pilha de plataformas no trecho inicial
            // sem cair no gap, acima da plataforma x=36, y=4.5.
            lv.secretCoins.Add(new Vector2(36f, 7.0f));

            return lv;
        }
    }
}
