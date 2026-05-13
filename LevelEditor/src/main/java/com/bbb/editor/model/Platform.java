package com.bbb.editor.model;

/**
 * Plataforma no ar. Espelha o struct Platform do C#.
 * pos = centro da plataforma (convenção Unity BoxCollider2D).
 * size = largura e altura em units.
 */
public class Platform {
    public Vec2 pos;
    public Vec2 size;

    public Platform() {
        pos  = new Vec2();
        size = new Vec2(3f, 0.5f);
    }

    public Platform(Vec2 pos, Vec2 size) {
        this.pos  = pos;
        this.size = size;
    }

    /** Extremo esquerdo. */
    public float left()   { return pos.x - size.x / 2f; }
    /** Extremo direito. */
    public float right()  { return pos.x + size.x / 2f; }
    /** Topo (y mais alto). */
    public float top()    { return pos.y + size.y / 2f; }
    /** Base (y mais baixo). */
    public float bottom() { return pos.y - size.y / 2f; }
}
