package com.bbb.editor.model;

/**
 * Tipos de inimigo com os IDs de tile do SpriteLibrary.cs.
 * SLIME=53, SLIME2=54, CRAB=55, CRAB2=56.
 */
public enum EnemyType {
    SLIME ("Slime",      53),
    SLIME2("Slime 2",    54),
    CRAB  ("Caranguejo", 55),
    CRAB2 ("Caranguejo 2", 56);

    public final String label;
    public final int tileId;

    EnemyType(String label, int tileId) {
        this.label  = label;
        this.tileId = tileId;
    }

    public static EnemyType fromTileId(int id) {
        for (EnemyType t : values()) {
            if (t.tileId == id) return t;
        }
        return SLIME;
    }

    @Override
    public String toString() { return label; }
}
