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

    public class LevelData
    {
        public string name;
        public int length;
        public List<GroundSeg> ground = new List<GroundSeg>();
        public List<Platform> platforms = new List<Platform>();
        public List<Vector2> coins = new List<Vector2>();
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
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

        // ---------- FASE 1: tutorial / curta e tranquila ----------
        private static LevelData BuildLevel1()
        {
            var lv = new LevelData { name = "Floresta Calma", length = 50 };

            // Chão quase contínuo, só 1 buraco
            lv.ground.Add(new GroundSeg { startX = 0,  length = 22 });
            lv.ground.Add(new GroundSeg { startX = 26, length = 24 });

            lv.platforms.Add(new Platform { pos = new Vector2(8,  2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(14, 4.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(30, 3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(40, 2.5f), size = new Vector2(3, 0.5f) });

            lv.coins.AddRange(new[]
            {
                new Vector2(8,  4f),  new Vector2(14, 5.5f), new Vector2(20, 1.5f),
                new Vector2(30, 4.5f), new Vector2(40, 4f),  new Vector2(45, 1.5f),
            });

            // Só 1 inimigo (slime fofo) pra introduzir a mecânica
            lv.enemies.Add(new EnemySpawn
            {
                pos = new Vector2(35, 0.5f),
                patrolRange = 2.5f,
                spriteTile = SpriteLibrary.TILE_ENEMY_SLIME
            });

            return lv;
        }

        // ---------- FASE 2: média ----------
        private static LevelData BuildLevel2()
        {
            var lv = new LevelData { name = "Caverna Saltitante", length = 65 };

            lv.ground.Add(new GroundSeg { startX = 0,  length = 18 });
            lv.ground.Add(new GroundSeg { startX = 22, length = 14 });
            lv.ground.Add(new GroundSeg { startX = 40, length = 10 });
            lv.ground.Add(new GroundSeg { startX = 54, length = 11 });

            lv.platforms.Add(new Platform { pos = new Vector2(8,  2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(13, 4.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(20, 1.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(28, 3.0f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(35, 4.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(45, 2.5f), size = new Vector2(3, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(50, 4.0f), size = new Vector2(3, 0.5f) });

            lv.coins.AddRange(new[]
            {
                new Vector2(8,  4f),   new Vector2(13, 5.5f), new Vector2(15, 5.5f),
                new Vector2(20, 3f),   new Vector2(28, 4.5f), new Vector2(35, 6f),
                new Vector2(42, 1.5f), new Vector2(45, 4f),   new Vector2(50, 5.5f),
                new Vector2(58, 1.5f),
            });

            lv.enemies.Add(new EnemySpawn { pos = new Vector2(12, 0.5f), patrolRange = 2.5f, spriteTile = SpriteLibrary.TILE_ENEMY_SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(28, 0.5f), patrolRange = 3f,   spriteTile = SpriteLibrary.TILE_ENEMY_CRAB });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(45, 0.5f), patrolRange = 4f,   spriteTile = SpriteLibrary.TILE_ENEMY_SLIME });

            return lv;
        }

        // ---------- FASE 3: difícil / chefe sem chefe rs ----------
        private static LevelData BuildLevel3()
        {
            var lv = new LevelData { name = "Pulo Final", length = 80 };

            // Mais buracos e segmentos curtos = exige precisão de pulo
            lv.ground.Add(new GroundSeg { startX = 0,  length = 12 });
            lv.ground.Add(new GroundSeg { startX = 16, length = 6 });
            lv.ground.Add(new GroundSeg { startX = 26, length = 8 });
            lv.ground.Add(new GroundSeg { startX = 38, length = 5 });
            lv.ground.Add(new GroundSeg { startX = 47, length = 8 });
            lv.ground.Add(new GroundSeg { startX = 59, length = 6 });
            lv.ground.Add(new GroundSeg { startX = 69, length = 11 });

            // Plataformas mais alto / mais espalhadas
            lv.platforms.Add(new Platform { pos = new Vector2(7,  3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(13, 4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(20, 3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(31, 4.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(36, 5.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(43, 3.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(52, 5.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(57, 3.0f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(64, 4.5f), size = new Vector2(2, 0.5f) });
            lv.platforms.Add(new Platform { pos = new Vector2(72, 3.0f), size = new Vector2(2, 0.5f) });

            lv.coins.AddRange(new[]
            {
                new Vector2(7,  4.5f), new Vector2(13, 6f),   new Vector2(20, 4.5f),
                new Vector2(28, 1.5f), new Vector2(31, 5.5f), new Vector2(36, 7f),
                new Vector2(43, 5f),   new Vector2(50, 1.5f), new Vector2(52, 6.5f),
                new Vector2(57, 4.5f), new Vector2(64, 6f),   new Vector2(72, 4.5f),
                new Vector2(75, 1.5f),
            });

            lv.enemies.Add(new EnemySpawn { pos = new Vector2(10, 0.5f), patrolRange = 2f,   spriteTile = SpriteLibrary.TILE_ENEMY_CRAB });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(28, 0.5f), patrolRange = 3f,   spriteTile = SpriteLibrary.TILE_ENEMY_SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(40, 0.5f), patrolRange = 1.5f, spriteTile = SpriteLibrary.TILE_ENEMY_CRAB });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(50, 0.5f), patrolRange = 3f,   spriteTile = SpriteLibrary.TILE_ENEMY_SLIME });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(62, 0.5f), patrolRange = 2f,   spriteTile = SpriteLibrary.TILE_ENEMY_CRAB });
            lv.enemies.Add(new EnemySpawn { pos = new Vector2(72, 0.5f), patrolRange = 3f,   spriteTile = SpriteLibrary.TILE_ENEMY_SLIME });

            return lv;
        }
    }
}
