package com.bbb.editor.model;

/** Equivalente ao Vector2 do Unity. Campos x/y públicos para o Gson serializar. */
public class Vec2 {
    public float x;
    public float y;

    public Vec2() {}

    public Vec2(float x, float y) {
        this.x = x;
        this.y = y;
    }

    @Override
    public String toString() {
        return "(" + x + ", " + y + ")";
    }
}
