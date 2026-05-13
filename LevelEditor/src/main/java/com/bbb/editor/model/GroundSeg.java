package com.bbb.editor.model;

/** Segmento de chão contínuo. Espelha o struct GroundSeg do C#. */
public class GroundSeg {
    public float startX;
    public float length;

    public GroundSeg() {}

    public GroundSeg(float startX, float length) {
        this.startX = startX;
        this.length = length;
    }
}
