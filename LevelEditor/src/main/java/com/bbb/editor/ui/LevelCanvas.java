package com.bbb.editor.ui;

import java.awt.AlphaComposite;
import java.awt.BasicStroke;
import java.awt.Color;
import java.awt.Composite;
import java.awt.Dimension;
import java.awt.Font;
import java.awt.GradientPaint;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.util.List;

import javax.swing.JPanel;

import com.bbb.editor.model.EnemySpawn;
import com.bbb.editor.model.EnemyType;
import com.bbb.editor.model.GroundSeg;
import com.bbb.editor.model.LevelData;
import com.bbb.editor.model.Platform;
import com.bbb.editor.model.Tool;
import com.bbb.editor.model.Vec2;

/**
 * Canvas principal do editor. Renderiza o nível e processa inputs de mouse.
 *
 * Sistema de coordenadas:
 *   Mundo (Unity): x → direita, y ↑ cima. y=0 é o chão.
 *   Tela (pixels):  x → direita, y ↓ baixo. GROUND_Y pixels do topo = y=0.
 *
 * Conversão:
 *   screenX(wx) = wx * PPU
 *   screenY(wy) = GROUND_Y - wy * PPU
 */
public class LevelCanvas extends JPanel {

    // ── constantes visuais ────────────────────────────────────────────────────
    static final int   PPU          = 48;   // pixels por unidade Unity
    static final int   GROUND_Y     = 576;  // y=0 em pixels (12 unidades de céu)
    static final int   CANVAS_H     = GROUND_Y + 4 * PPU; // + 4 unidades de subsolo = 768px
    static final float PLATFORM_H   = 0.5f; // altura padrão de plataforma (units)
    static final float SNAP         = 0.5f; // grade de snap (units)
    static final float DELETE_RANGE = 0.8f; // raio de remoção em units

    // ── estado ────────────────────────────────────────────────────────────────
    private LevelData    level;
    private Tool         currentTool  = Tool.GROUND;
    private EnemyType    enemyType    = EnemyType.SLIME;
    private float        cursorX, cursorY;
    private float        dragStartX, dragStartY;
    private boolean      isDragging;
    private final EditorWindow parent;

    // Cores
    private static final Color SKY_TOP    = new Color(100, 160, 220);
    private static final Color SKY_BOT    = new Color(160, 210, 240);
    private static final Color GROUND_COL = new Color(89, 67, 42);
    private static final Color GRASS_COL  = new Color(100, 160, 70);
    private static final Color PLAT_COL   = new Color(130, 110, 90);
    private static final Color PLAT_TOP   = new Color(160, 140, 110);
    private static final Color DISAPP_COL = new Color(70, 140, 180);
    private static final Color COIN_COL   = new Color(255, 215, 0);
    private static final Color SECRET_COL = new Color(220, 100, 220);
    private static final Color SPAWN_COL  = new Color(50, 220, 50);
    private static final Color CHECK_COL  = new Color(255, 180, 0);
    private static final Color WEAPON_COL = new Color(200, 60, 60);
    private static final Color GOAL_COL   = new Color(255, 80, 80);
    private static final Color GRID_COL   = new Color(255, 255, 255, 25);
    private static final Color GRID_MAJOR = new Color(255, 255, 255, 55);
    private static final Color RULER_BG   = new Color(30, 30, 30, 200);

    // ── construtor ────────────────────────────────────────────────────────────
    public LevelCanvas(EditorWindow parent) {
        this.parent = parent;
        setBackground(SKY_TOP);
        setFocusable(true);

        MouseAdapter ma = new MouseAdapter() {
            @Override public void mousePressed (MouseEvent e) { handlePressed(e);  }
            @Override public void mouseReleased(MouseEvent e) { handleReleased(e); }
            @Override public void mouseDragged (MouseEvent e) { handleDragged(e);  }
            @Override public void mouseMoved   (MouseEvent e) { handleMoved(e);    }
        };
        addMouseListener(ma);
        addMouseMotionListener(ma);

        addMouseWheelListener(e -> {
            if (currentTool == Tool.ENEMY && isDragging) {
                // ajusta patrol range do inimigo sendo editado
            }
        });

        KeyAdapter ka = new KeyAdapter() {
            @Override public void keyPressed(KeyEvent e) {
                for (Tool t : Tool.values()) {
                    if (t.hotkey != '\0' && Character.toUpperCase(e.getKeyChar()) == t.hotkey) {
                        parent.setTool(t);
                    }
                }
            }
        };
        addKeyListener(ka);
    }

    // ── API pública ───────────────────────────────────────────────────────────
    public void setLevel(LevelData level) {
        this.level = level;
        revalidate();
        repaint();
    }

    public LevelData getLevel() { return level; }

    public void setTool(Tool t)         { this.currentTool = t; repaint(); }
    public void setEnemyType(EnemyType t) { this.enemyType = t; }

    @Override
    public Dimension getPreferredSize() {
        int len = (level != null) ? level.length : 80;
        return new Dimension(len * PPU + 256, CANVAS_H);
    }

    // ── conversão de coordenadas ──────────────────────────────────────────────
    int sx(float wx) { return Math.round(wx * PPU); }
    int sy(float wy) { return GROUND_Y - Math.round(wy * PPU); }

    float wx(int sx) { return (float) sx / PPU; }
    float wy(int sy) { return (float) (GROUND_Y - sy) / PPU; }

    float snapV(float v) { return Math.round(v / SNAP) * SNAP; }

    // ── rendering ─────────────────────────────────────────────────────────────
    @Override
    protected void paintComponent(Graphics g) {
        super.paintComponent(g);
        if (level == null) return;

        Graphics2D g2 = (Graphics2D) g;
        g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING,   RenderingHints.VALUE_ANTIALIAS_ON);
        g2.setRenderingHint(RenderingHints.KEY_TEXT_ANTIALIASING, RenderingHints.VALUE_TEXT_ANTIALIAS_ON);

        int w = getWidth(), h = getHeight();

        // gradiente de céu
        GradientPaint sky = new GradientPaint(0, 0, SKY_TOP, 0, GROUND_Y, SKY_BOT);
        g2.setPaint(sky);
        g2.fillRect(0, 0, w, GROUND_Y);

        // subsolo
        g2.setColor(GROUND_COL);
        g2.fillRect(0, sy(0), w, h - sy(0));

        drawGrid(g2);
        drawGround(g2);
        drawDisappearingPlatforms(g2);
        drawPlatforms(g2);
        drawSecretCoins(g2);
        drawCoins(g2);
        drawEnemies(g2);
        drawCheckpoints(g2);
        drawWeaponPickup(g2);
        drawSpawn(g2);
        drawGoal(g2);
        drawDragPreview(g2);
        drawCursor(g2);
        drawRuler(g2);
    }

    private void drawGrid(Graphics2D g2) {
        int len = level.length;
        // linhas verticais
        for (float x = 0; x <= len + 0.1f; x += SNAP) {
            boolean major = (Math.round(x) == x);
            g2.setColor(major ? GRID_MAJOR : GRID_COL);
            g2.drawLine(sx(x), 0, sx(x), getHeight());
        }
        // linhas horizontais a cada 1 unit
        for (float y = -3; y <= 12; y++) {
            g2.setColor(y == 0 ? new Color(255,255,255,90) : GRID_COL);
            g2.drawLine(0, sy(y), getWidth(), sy(y));
        }
    }

    private void drawGround(Graphics2D g2) {
        for (GroundSeg seg : level.ground) {
            int x = sx(seg.startX);
            int y = sy(0);
            int w = (int)(seg.length * PPU);
            int underground = getHeight() - y;
            g2.setColor(GROUND_COL);
            g2.fillRect(x, y, w, underground);
            // grama
            g2.setColor(GRASS_COL);
            g2.fillRect(x, y, w, PPU / 6);
            // borda escura
            g2.setColor(GROUND_COL.darker());
            g2.drawRect(x, y, w - 1, underground - 1);
        }
    }

    private void drawPlatforms(Graphics2D g2) {
        for (Platform p : level.platforms) drawPlatform(g2, p, PLAT_COL, PLAT_TOP);
    }

    private void drawDisappearingPlatforms(Graphics2D g2) {
        for (Platform p : level.disappearingPlatforms) drawPlatform(g2, p, DISAPP_COL, DISAPP_COL.brighter());
    }

    private void drawPlatform(Graphics2D g2, Platform p, Color body, Color top) {
        int x = sx(p.left());
        int y = sy(p.top());
        int w = (int)(p.size.x * PPU);
        int h = Math.max((int)(p.size.y * PPU), 6);
        g2.setColor(body);
        g2.fillRect(x, y, w, h);
        g2.setColor(top);
        g2.fillRect(x, y, w, Math.max(h / 4, 3));
        g2.setColor(body.darker());
        g2.drawRect(x, y, w - 1, h - 1);
    }

    private void drawCoins(Graphics2D g2) {
        int r = PPU / 5;
        g2.setColor(COIN_COL);
        for (Vec2 c : level.coins) {
            int cx = sx(c.x), cy = sy(c.y);
            g2.fillOval(cx - r, cy - r, r * 2, r * 2);
            g2.setColor(COIN_COL.darker());
            g2.drawOval(cx - r, cy - r, r * 2, r * 2);
            g2.setColor(COIN_COL);
        }
    }

    private void drawSecretCoins(Graphics2D g2) {
        int r = PPU / 6;
        for (Vec2 c : level.secretCoins) {
            int cx = sx(c.x), cy = sy(c.y);
            // pontilhado para indicar que é secreta
            g2.setColor(SECRET_COL);
            g2.fillOval(cx - r, cy - r, r * 2, r * 2);
            g2.setColor(Color.WHITE);
            g2.setFont(new Font("SansSerif", Font.BOLD, 9));
            g2.drawString("S", cx - 4, cy + 4);
        }
    }

    private void drawEnemies(Graphics2D g2) {
        for (EnemySpawn e : level.enemies) {
            int ex = sx(e.pos.x), ey = sy(e.pos.y);
            int pr = (int)(e.patrolRange * PPU);
            // linha de patrulha
            g2.setColor(new Color(255, 80, 80, 80));
            g2.setStroke(new BasicStroke(1.5f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND,
                    0, new float[]{4, 4}, 0));
            g2.drawLine(ex - pr, ey, ex + pr, ey);
            g2.setStroke(new BasicStroke(1));
            // marcadores de limite
            g2.setColor(new Color(255, 80, 80, 150));
            g2.fillRect(ex - pr - 2, ey - PPU / 4, 4, PPU / 2);
            g2.fillRect(ex + pr - 2, ey - PPU / 4, 4, PPU / 2);
            // corpo do inimigo
            int sz = (int)(0.75f * PPU);
            EnemyType type = EnemyType.fromTileId(e.spriteTile);
            Color col = (type == EnemyType.SLIME || type == EnemyType.SLIME2)
                    ? new Color(80, 190, 80) : new Color(200, 90, 40);
            g2.setColor(col);
            g2.fillRoundRect(ex - sz / 2, ey - sz, sz, sz, 8, 8);
            g2.setColor(col.darker());
            g2.drawRoundRect(ex - sz / 2, ey - sz, sz, sz, 8, 8);
            // olhos
            g2.setColor(Color.WHITE);
            g2.fillOval(ex - sz / 4 - 3, ey - sz + sz / 3, 6, 6);
            g2.fillOval(ex + sz / 4 - 3, ey - sz + sz / 3, 6, 6);
            // label
            g2.setColor(Color.WHITE);
            g2.setFont(new Font("SansSerif", Font.BOLD, 9));
            g2.drawString(type.label, ex - 20, ey - sz - 2);
        }
    }

    private void drawCheckpoints(Graphics2D g2) {
        for (Vec2 cp : level.checkpoints) {
            int cx = sx(cp.x), cy = sy(cp.y);
            int flagH = PPU;
            g2.setColor(CHECK_COL);
            g2.fillRect(cx - 1, cy - flagH, 3, flagH);
            int[] fx = {cx + 2, cx + PPU / 2, cx + 2};
            int[] fy = {cy - flagH, cy - flagH + PPU / 4, cy - flagH + PPU / 2};
            g2.fillPolygon(fx, fy, 3);
        }
    }

    private void drawWeaponPickup(Graphics2D g2) {
        if (!level.hasWeaponPickup || level.weaponPickup == null) return;
        int wx2 = sx(level.weaponPickup.x), wy2 = sy(level.weaponPickup.y);
        g2.setColor(WEAPON_COL);
        int sz = PPU / 2;
        g2.fillRect(wx2 - sz / 2, wy2 - sz / 2, sz, sz);
        g2.setColor(Color.WHITE);
        g2.setFont(new Font("SansSerif", Font.BOLD, 9));
        g2.drawString("ARM", wx2 - 10, wy2 + 3);
    }

    private void drawSpawn(Graphics2D g2) {
        if (level.spawn == null) return;
        int sx2 = sx(level.spawn.x), sy2 = sy(level.spawn.y);
        g2.setColor(SPAWN_COL);
        int[] xp = {sx2, sx2 - PPU / 3, sx2 + PPU / 3};
        int[] yp = {sy2 - PPU, sy2, sy2};
        g2.fillPolygon(xp, yp, 3);
        g2.setColor(Color.WHITE);
        g2.setFont(new Font("SansSerif", Font.BOLD, 9));
        g2.drawString("SPAWN", sx2 - 14, sy2 - PPU - 3);
    }

    private void drawGoal(Graphics2D g2) {
        int goalX = sx(level.length - 1);
        int goalY = sy(0);
        int flagH = PPU * 2;
        g2.setColor(GOAL_COL);
        g2.setStroke(new BasicStroke(2));
        g2.drawLine(goalX, goalY, goalX, goalY - flagH);
        int[] fx = {goalX, goalX + PPU / 2, goalX};
        int[] fy = {goalY - flagH, goalY - flagH + PPU / 3, goalY - flagH + PPU * 2 / 3};
        g2.fillPolygon(fx, fy, 3);
        g2.setStroke(new BasicStroke(1));
        g2.setColor(Color.WHITE);
        g2.setFont(new Font("SansSerif", Font.BOLD, 10));
        g2.drawString("META", goalX + 4, goalY - flagH - 4);
    }

    private void drawDragPreview(Graphics2D g2) {
        if (!isDragging) return;
        float x1 = Math.min(dragStartX, cursorX);
        float x2 = Math.max(dragStartX, cursorX);
        float y2 = cursorY; // usado pela plataforma

        if (currentTool == Tool.GROUND) {
            int px = sx(x1), py = sy(0);
            int pw = (int)((x2 - x1) * PPU);
            g2.setColor(new Color(GRASS_COL.getRed(), GRASS_COL.getGreen(), GRASS_COL.getBlue(), 150));
            g2.fillRect(px, py, Math.max(pw, 1), PPU / 4);
            g2.setColor(new Color(GROUND_COL.getRed(), GROUND_COL.getGreen(), GROUND_COL.getBlue(), 150));
            g2.fillRect(px, py + PPU / 4, Math.max(pw, 1), 3 * PPU);
        } else if (currentTool == Tool.PLATFORM || currentTool == Tool.DISAPPEARING) {
            float cx = (x1 + x2) / 2f;
            Platform prev = new Platform(new Vec2(cx, y2), new Vec2(x2 - x1, PLATFORM_H));
            Color c = currentTool == Tool.PLATFORM ? PLAT_COL : DISAPP_COL;
            Composite old = g2.getComposite();
            g2.setComposite(AlphaComposite.getInstance(AlphaComposite.SRC_OVER, 0.6f));
            drawPlatform(g2, prev, c, c.brighter());
            g2.setComposite(old);
        }
    }

    private void drawCursor(Graphics2D g2) {
        int cx = sx(cursorX), cy = sy(cursorY);
        g2.setColor(new Color(255, 255, 255, 100));
        g2.drawLine(cx - 10, cy, cx + 10, cy);
        g2.drawLine(cx, cy - 10, cx, cy + 10);

        // preview de coin / secret / enemy / checkpoint / weapon
        int r = PPU / 4;
        Composite old = g2.getComposite();
        g2.setComposite(AlphaComposite.getInstance(AlphaComposite.SRC_OVER, 0.5f));
        switch (currentTool) {
            case COIN         -> { g2.setColor(COIN_COL);   g2.fillOval(cx - r, cy - r, r*2, r*2); }
            case SECRET_COIN  -> { g2.setColor(SECRET_COL); g2.fillOval(cx - r, cy - r, r*2, r*2); }
            case CHECKPOINT   -> { g2.setColor(CHECK_COL);  g2.fillRect(cx - 2, cy - r*3, 4, r*3); }
            case WEAPON       -> { g2.setColor(WEAPON_COL); g2.fillRect(cx - r, cy - r, r*2, r*2); }
            case SPAWN        -> { g2.setColor(SPAWN_COL);  g2.fillOval(cx - r, cy - r, r*2, r*2); }
            case ENEMY -> {
                int sz = PPU / 2;
                EnemyType type = parent.getSelectedEnemyType();
                Color col = (type == EnemyType.SLIME || type == EnemyType.SLIME2)
                        ? new Color(80, 190, 80) : new Color(200, 90, 40);
                g2.setColor(col);
                g2.fillRoundRect(cx - sz / 2, cy - sz, sz, sz, 6, 6);
            }
            case DELETE -> {
                g2.setColor(new Color(255, 80, 80));
                int d = (int)(DELETE_RANGE * PPU);
                g2.drawOval(cx - d, cy - d, d * 2, d * 2);
            }
            default -> {}
        }
        g2.setComposite(old);
    }

    private void drawRuler(Graphics2D g2) {
        int rulerH = 20;
        g2.setColor(RULER_BG);
        g2.fillRect(0, getHeight() - rulerH, getWidth(), rulerH);
        g2.setColor(new Color(180, 180, 180));
        g2.setFont(new Font("Monospaced", Font.PLAIN, 10));
        for (int x = 0; x <= level.length; x += 5) {
            int px = sx(x);
            g2.drawLine(px, getHeight() - rulerH, px, getHeight() - rulerH + 5);
            g2.drawString(String.valueOf(x), px + 2, getHeight() - 5);
        }
    }

    // ── mouse handlers ────────────────────────────────────────────────────────
    private void handlePressed(MouseEvent e) {
        requestFocusInWindow();
        if (level == null) return;
        float wx = snapV(wx(e.getX()));
        float wy = snapV(wy(e.getY()));

        if (e.getButton() == MouseEvent.BUTTON3 || currentTool == Tool.DELETE) {
            deleteNearest(wx, wy);
            parent.setModified(true);
            repaint();
            return;
        }

        switch (currentTool) {
            case GROUND, PLATFORM, DISAPPEARING -> {
                dragStartX = wx;
                dragStartY = wy;
                isDragging = true;
            }
            case COIN -> {
                level.coins.add(new Vec2(wx, wy));
                parent.setModified(true);
            }
            case SECRET_COIN -> {
                level.secretCoins.add(new Vec2(wx, wy));
                parent.setModified(true);
            }
            case CHECKPOINT -> {
                level.checkpoints.add(new Vec2(wx, wy));
                parent.setModified(true);
            }
            case WEAPON -> {
                level.hasWeaponPickup = true;
                level.weaponPickup    = new Vec2(wx, wy);
                parent.onWeaponPickupChanged(true);
                parent.setModified(true);
            }
            case SPAWN -> {
                level.spawn = new Vec2(wx, wy);
                parent.setModified(true);
            }
            case ENEMY -> {
                EnemySpawn es = new EnemySpawn(new Vec2(wx, wy), 2f, parent.getSelectedEnemyType());
                level.enemies.add(es);
                dragStartX = wx;
                isDragging = true; // drag ajusta patrol range
                parent.setModified(true);
            }
            default -> {}
        }
        repaint();
    }

    private void handleReleased(MouseEvent e) {
        if (!isDragging || level == null) return;
        float wx = snapV(wx(e.getX()));

        float x1 = Math.min(dragStartX, wx);
        float x2 = Math.max(dragStartX, wx);
        float width = x2 - x1;

        switch (currentTool) {
            case GROUND -> {
                if (width > 0.1f) {
                    level.ground.add(new GroundSeg(x1, width));
                    parent.setModified(true);
                }
            }
            case PLATFORM -> {
                if (width > 0.2f) {
                    Vec2 pos  = new Vec2((x1 + x2) / 2f, snapV(wy(e.getY())));
                    Vec2 size = new Vec2(width, PLATFORM_H);
                    level.platforms.add(new Platform(pos, size));
                    parent.setModified(true);
                }
            }
            case DISAPPEARING -> {
                if (width > 0.2f) {
                    Vec2 pos  = new Vec2((x1 + x2) / 2f, snapV(wy(e.getY())));
                    Vec2 size = new Vec2(width, PLATFORM_H);
                    level.disappearingPlatforms.add(new Platform(pos, size));
                    parent.setModified(true);
                }
            }
            case ENEMY -> {
                // ajusta patrol range do último inimigo adicionado
                if (!level.enemies.isEmpty()) {
                    EnemySpawn last = level.enemies.get(level.enemies.size() - 1);
                    float range = Math.abs(wx - last.pos.x);
                    if (range > 0.1f) last.patrolRange = range;
                    parent.setModified(true);
                }
            }
            default -> {}
        }

        isDragging = false;
        repaint();
    }

    private void handleDragged(MouseEvent e) {
        cursorX = snapV(wx(e.getX()));
        cursorY = snapV(wy(e.getY()));
        parent.updateStatusBar(cursorX, cursorY);
        repaint();
    }

    private void handleMoved(MouseEvent e) {
        cursorX = snapV(wx(e.getX()));
        cursorY = snapV(wy(e.getY()));
        parent.updateStatusBar(cursorX, cursorY);
        repaint();
    }

    // ── remoção ───────────────────────────────────────────────────────────────
    private void deleteNearest(float wx, float wy) {
        float best = DELETE_RANGE;
        String bestKind = null;
        int    bestIdx  = -1;

        bestIdx = nearestIdx(level.coins, wx, wy, best);
        if (bestIdx >= 0) { level.coins.remove(bestIdx); return; }

        bestIdx = nearestIdx(level.secretCoins, wx, wy, best);
        if (bestIdx >= 0) { level.secretCoins.remove(bestIdx); return; }

        bestIdx = nearestEnemyIdx(wx, wy, best);
        if (bestIdx >= 0) { level.enemies.remove(bestIdx); return; }

        bestIdx = nearestIdx(level.checkpoints, wx, wy, best);
        if (bestIdx >= 0) { level.checkpoints.remove(bestIdx); return; }

        bestIdx = nearestPlatformIdx(level.platforms, wx, wy, best * 2);
        if (bestIdx >= 0) { level.platforms.remove(bestIdx); return; }

        bestIdx = nearestPlatformIdx(level.disappearingPlatforms, wx, wy, best * 2);
        if (bestIdx >= 0) { level.disappearingPlatforms.remove(bestIdx); return; }

        bestIdx = nearestGroundIdx(wx, wy, best * 2);
        if (bestIdx >= 0) { level.ground.remove(bestIdx); }
    }

    private int nearestIdx(List<Vec2> list, float wx, float wy, float maxDist) {
        int best = -1;
        float bestD = maxDist;
        for (int i = 0; i < list.size(); i++) {
            Vec2 v = list.get(i);
            float d = dist(v.x, v.y, wx, wy);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private int nearestEnemyIdx(float wx, float wy, float maxDist) {
        int best = -1;
        float bestD = maxDist;
        for (int i = 0; i < level.enemies.size(); i++) {
            EnemySpawn es = level.enemies.get(i);
            float d = dist(es.pos.x, es.pos.y, wx, wy);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private int nearestPlatformIdx(List<Platform> list, float wx, float wy, float maxDist) {
        int best = -1;
        float bestD = maxDist;
        for (int i = 0; i < list.size(); i++) {
            Platform p = list.get(i);
            float d = dist(p.pos.x, p.pos.y, wx, wy);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private int nearestGroundIdx(float wx, float wy, float maxDist) {
        if (Math.abs(wy) > 1f) return -1; // deve clicar próximo ao chão
        int best = -1;
        float bestD = maxDist;
        for (int i = 0; i < level.ground.size(); i++) {
            GroundSeg s = level.ground.get(i);
            float mid = s.startX + s.length / 2f;
            float d = Math.abs(mid - wx);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private float dist(float ax, float ay, float bx, float by) {
        float dx = ax - bx, dy = ay - by;
        return (float) Math.sqrt(dx * dx + dy * dy);
    }
}
