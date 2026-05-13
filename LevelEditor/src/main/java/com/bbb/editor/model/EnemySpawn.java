package com.bbb.editor.model;

/** Spawn de inimigo. Espelha o struct EnemySpawn do C#. */
public class EnemySpawn {
    public Vec2  pos;
    public float patrolRange;
    public int   spriteTile;

    public EnemySpawn() {
        pos         = new Vec2();
        patrolRange = 2f;
        spriteTile  = EnemyType.SLIME.tileId;
    }

    public EnemySpawn(Vec2 pos, float patrolRange, EnemyType type) {
        this.pos         = pos;
        this.patrolRange = patrolRange;
        this.spriteTile  = type.tileId;
    }

    public EnemyType type() {
        return EnemyType.fromTileId(spriteTile);
    }
}
