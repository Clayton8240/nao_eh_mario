package com.bbb.editor.model;

/** Decoração visual sem colisão. Espelha o struct Decoration do C#. */
public class Decoration {
    public Vec2  pos;
    public int   tileIndex;
    public float scale       = 1f;
    public int   heightTiles = 1;
    // só usados quando heightTiles > 1 (ex: árvore 3-tiles)
    public int   topTileIndex;
    public int   midTileIndex;
    public int   botTileIndex;

    public Decoration() {
        pos = new Vec2();
    }

    public Decoration(Vec2 pos, int tileIndex) {
        this.pos       = pos;
        this.tileIndex = tileIndex;
    }
}
