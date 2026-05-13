package com.bbb.editor.model;

import java.util.ArrayList;
import java.util.List;

/**
 * Dados de uma fase — espelha exatamente a classe LevelData do C#.
 * Os nomes dos campos DEVEM bater com os do C# para o Unity desserializar.
 */
public class LevelData {
    public String name    = "Nova Fase";
    public int    length  = 80;

    public Vec2    spawn          = new Vec2(1.5f, 2f);
    public boolean hasWeaponPickup = false;
    public Vec2    weaponPickup   = new Vec2(0f, 1.2f);

    public List<GroundSeg>   ground                = new ArrayList<>();
    public List<Platform>    platforms              = new ArrayList<>();
    public List<Vec2>        coins                  = new ArrayList<>();
    public List<EnemySpawn>  enemies                = new ArrayList<>();
    public List<Vec2>        checkpoints            = new ArrayList<>();
    public List<Platform>    disappearingPlatforms  = new ArrayList<>();
    public List<Vec2>        secretCoins            = new ArrayList<>();
    public List<Decoration>  decorations            = new ArrayList<>();

    public LevelData() {}

    /** Cria uma fase vazia com chão inicial. */
    public static LevelData createEmpty(String name, int length) {
        LevelData lv = new LevelData();
        lv.name   = name;
        lv.length = length;
        lv.ground.add(new GroundSeg(0, length));
        return lv;
    }
}
