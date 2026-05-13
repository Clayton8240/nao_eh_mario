package com.bbb.editor.ui;

import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.awt.Font;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.FocusAdapter;
import java.awt.event.FocusEvent;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;
import java.io.File;
import java.io.IOException;

import javax.swing.AbstractAction;
import javax.swing.Box;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JComponent;
import javax.swing.JFileChooser;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JSpinner;
import javax.swing.JTextField;
import javax.swing.KeyStroke;
import javax.swing.SpinnerNumberModel;
import javax.swing.border.EmptyBorder;

import com.bbb.editor.io.LevelSerializer;
import com.bbb.editor.model.EnemyType;
import com.bbb.editor.model.LevelData;
import com.bbb.editor.model.Tool;

/** Janela principal do editor de fases. */
public class EditorWindow extends JFrame {

    private LevelData   currentLevel;
    private File        currentFile;
    private boolean     modified;

    private LevelCanvas canvas;
    private Toolbar     toolbar;
    private JLabel      statusBar;
    private JLabel      coordLabel;

    // widgets de propriedades da fase
    private JTextField  fieldName;
    private JSpinner    spinnerLength;
    private JCheckBox   checkWeapon;
    private JComboBox<EnemyType> comboEnemy;

    public EditorWindow() {
        super("BBB Level Editor");
        setDefaultCloseOperation(JFrame.DO_NOTHING_ON_CLOSE);
        setSize(1280, 800);
        setLocationRelativeTo(null);
        setMinimumSize(new Dimension(900, 600));

        addWindowListener(new WindowAdapter() {
            @Override public void windowClosing(WindowEvent e) { tryClose(); }
        });

        currentLevel = LevelData.createEmpty("Nova Fase", 80);

        canvas  = new LevelCanvas(this);
        canvas.setLevel(currentLevel);

        toolbar = new Toolbar(this);

        setJMenuBar(buildMenuBar());
        setLayout(new BorderLayout());
        add(toolbar, BorderLayout.WEST);
        add(new JScrollPane(canvas), BorderLayout.CENTER);
        add(buildPropertiesPanel(), BorderLayout.SOUTH);

        registerGlobalHotkeys();
        setTool(Tool.GROUND);
        updateTitle();
    }

    // ── barra de menu ─────────────────────────────────────────────────────────
    private JMenuBar buildMenuBar() {
        JMenuBar bar = new JMenuBar();

        JMenu fileMenu = new JMenu("Arquivo");
        fileMenu.setMnemonic(KeyEvent.VK_A);

        JMenuItem itemNew  = item("Nova Fase",   KeyEvent.VK_N, InputEvent.CTRL_DOWN_MASK, e -> newLevel());
        JMenuItem itemOpen = item("Abrir…",      KeyEvent.VK_O, InputEvent.CTRL_DOWN_MASK, e -> openFile());
        JMenuItem itemSave = item("Salvar",      KeyEvent.VK_S, InputEvent.CTRL_DOWN_MASK, e -> save());
        JMenuItem itemSaveAs = item("Salvar Como…", KeyEvent.VK_S,
                InputEvent.CTRL_DOWN_MASK | InputEvent.SHIFT_DOWN_MASK, e -> saveAs());

        fileMenu.add(itemNew);
        fileMenu.add(itemOpen);
        fileMenu.addSeparator();
        fileMenu.add(itemSave);
        fileMenu.add(itemSaveAs);
        fileMenu.addSeparator();
        fileMenu.add(item("Sair", 0, 0, e -> tryClose()));

        bar.add(fileMenu);

        JMenu helpMenu = new JMenu("Ajuda");
        helpMenu.add(item("Sobre", 0, 0, e -> showAbout()));
        bar.add(helpMenu);

        return bar;
    }

    private JMenuItem item(String text, int key, int mask, ActionListener al) {
        JMenuItem mi = new JMenuItem(text);
        if (key != 0) mi.setAccelerator(KeyStroke.getKeyStroke(key, mask));
        mi.addActionListener(al);
        return mi;
    }

    // ── painel de propriedades (rodapé) ───────────────────────────────────────
    private JPanel buildPropertiesPanel() {
        JPanel panel = new JPanel(new FlowLayout(FlowLayout.LEFT, 10, 4));
        panel.setBackground(new Color(40, 40, 43));
        panel.setBorder(new EmptyBorder(2, 6, 2, 6));

        panel.add(label("Nome:"));
        fieldName = new JTextField(currentLevel.name, 16);
        fieldName.addActionListener(e -> applyLevelProps());
        fieldName.addFocusListener(new FocusAdapter() {
            @Override public void focusLost(FocusEvent e) { applyLevelProps(); }
        });
        panel.add(fieldName);

        panel.add(label("Comprimento:"));
        spinnerLength = new JSpinner(new SpinnerNumberModel(currentLevel.length, 20, 999, 5));
        spinnerLength.addChangeListener(e -> applyLevelProps());
        spinnerLength.setPreferredSize(new Dimension(70, 24));
        panel.add(spinnerLength);

        checkWeapon = new JCheckBox("Arma?", currentLevel.hasWeaponPickup);
        checkWeapon.setForeground(Color.WHITE);
        checkWeapon.setBackground(new Color(40, 40, 43));
        checkWeapon.addActionListener(e -> {
            currentLevel.hasWeaponPickup = checkWeapon.isSelected();
            setModified(true);
            canvas.repaint();
        });
        panel.add(checkWeapon);

        panel.add(label("Inimigo:"));
        comboEnemy = new JComboBox<>(EnemyType.values());
        comboEnemy.setPreferredSize(new Dimension(120, 24));
        panel.add(comboEnemy);

        panel.add(Box.createHorizontalStrut(20));

        coordLabel = label("x: 0.0  y: 0.0");
        coordLabel.setFont(new Font("Monospaced", Font.PLAIN, 11));
        panel.add(coordLabel);

        statusBar = label("Pronto.");
        panel.add(statusBar);

        return panel;
    }

    private JLabel label(String text) {
        JLabel l = new JLabel(text);
        l.setForeground(new Color(190, 190, 190));
        return l;
    }

    private void applyLevelProps() {
        currentLevel.name   = fieldName.getText().trim();
        currentLevel.length = (int) spinnerLength.getValue();
        canvas.revalidate();
        canvas.repaint();
        setModified(true);
    }

    // ── hotkeys globais (ferramentas) ─────────────────────────────────────────
    private void registerGlobalHotkeys() {
        for (Tool t : Tool.values()) {
            if (t.hotkey == '\0') continue;
            KeyStroke ks = KeyStroke.getKeyStroke(t.hotkey, 0);
            getRootPane().getInputMap(JComponent.WHEN_IN_FOCUSED_WINDOW).put(ks, t);
            getRootPane().getActionMap().put(t, new AbstractAction() {
                @Override public void actionPerformed(ActionEvent e) { setTool(t); }
            });
        }
        // DELETE key
        KeyStroke del = KeyStroke.getKeyStroke(KeyEvent.VK_DELETE, 0);
        getRootPane().getInputMap(JComponent.WHEN_IN_FOCUSED_WINDOW).put(del, Tool.DELETE);
        getRootPane().getActionMap().put(Tool.DELETE, new AbstractAction() {
            @Override public void actionPerformed(ActionEvent e) { setTool(Tool.DELETE); }
        });
    }

    // ── operações de arquivo ──────────────────────────────────────────────────
    private void newLevel() {
        if (!confirmDiscardChanges()) return;
        String name = JOptionPane.showInputDialog(this, "Nome da fase:", "Nova Fase");
        if (name == null) return;
        Object lenObj = JOptionPane.showInputDialog(this, "Comprimento (unidades):", "80");
        if (lenObj == null) return;
        try {
            int len = Integer.parseInt(lenObj.toString().trim());
            currentLevel = LevelData.createEmpty(name, len);
            currentFile  = null;
            modified     = false;
            loadIntoUI();
        } catch (NumberFormatException ex) {
            JOptionPane.showMessageDialog(this, "Comprimento inválido.", "Erro", JOptionPane.ERROR_MESSAGE);
        }
    }

    private void openFile() {
        if (!confirmDiscardChanges()) return;
        JFileChooser fc = levelFileChooser();
        if (fc.showOpenDialog(this) != JFileChooser.APPROVE_OPTION) return;
        try {
            currentLevel = LevelSerializer.load(fc.getSelectedFile());
            currentFile  = fc.getSelectedFile();
            modified     = false;
            loadIntoUI();
            setStatus("Carregado: " + currentFile.getName());
        } catch (IOException ex) {
            JOptionPane.showMessageDialog(this,
                    "Erro ao abrir arquivo:\n" + ex.getMessage(), "Erro", JOptionPane.ERROR_MESSAGE);
        }
    }

    private void save() {
        if (currentFile == null) { saveAs(); return; }
        doSave(currentFile);
    }

    private void saveAs() {
        JFileChooser fc = levelFileChooser();
        if (currentFile != null) fc.setSelectedFile(currentFile);
        else fc.setSelectedFile(new File(safeName(currentLevel.name) + ".json"));
        if (fc.showSaveDialog(this) != JFileChooser.APPROVE_OPTION) return;
        File f = fc.getSelectedFile();
        if (!f.getName().endsWith(".json")) f = new File(f.getPath() + ".json");
        doSave(f);
    }

    private void doSave(File f) {
        applyLevelProps();
        try {
            LevelSerializer.save(currentLevel, f);
            currentFile = f;
            setModified(false);
            setStatus("Salvo: " + f.getName());
        } catch (IOException ex) {
            JOptionPane.showMessageDialog(this,
                    "Erro ao salvar:\n" + ex.getMessage(), "Erro", JOptionPane.ERROR_MESSAGE);
        }
    }

    private JFileChooser levelFileChooser() {
        JFileChooser fc = new JFileChooser();
        fc.setFileFilter(new javax.swing.filechooser.FileNameExtensionFilter("JSON de fase (*.json)", "json"));
        fc.setCurrentDirectory(suggestDirectory());
        return fc;
    }

    private File suggestDirectory() {
        // tenta abrir direto na pasta Resources/levels do projeto Unity
        String[] tries = {
            System.getProperty("user.dir") + "/Assets/Resources/levels",
            System.getProperty("user.dir") + "/../Assets/Resources/levels",
            System.getProperty("user.home")
        };
        for (String p : tries) {
            File f = new File(p);
            if (f.isDirectory()) return f;
        }
        return new File(System.getProperty("user.home"));
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private void loadIntoUI() {
        canvas.setLevel(currentLevel);
        fieldName.setText(currentLevel.name);
        spinnerLength.setValue(currentLevel.length);
        checkWeapon.setSelected(currentLevel.hasWeaponPickup);
        canvas.revalidate();
        canvas.repaint();
        updateTitle();
    }

    public void setTool(Tool t) {
        currentLevel = canvas.getLevel() != null ? canvas.getLevel() : currentLevel;
        canvas.setTool(t);
        toolbar.selectTool(t);
        setStatus("Ferramenta: " + t.label);
    }

    public void setModified(boolean m) {
        this.modified = m;
        updateTitle();
    }

    public void onWeaponPickupChanged(boolean has) {
        checkWeapon.setSelected(has);
    }

    public EnemyType getSelectedEnemyType() {
        return (EnemyType) comboEnemy.getSelectedItem();
    }

    public void updateStatusBar(float wx, float wy) {
        coordLabel.setText(String.format("x: %.1f  y: %.1f", wx, wy));
    }

    public void setStatus(String msg) {
        if (statusBar != null) statusBar.setText(msg);
    }

    private void updateTitle() {
        String name = (currentLevel != null) ? currentLevel.name : "—";
        String file = (currentFile  != null) ? " [" + currentFile.getName() + "]" : " [sem arquivo]";
        setTitle("BBB Level Editor — " + name + file + (modified ? " *" : ""));
    }

    private boolean confirmDiscardChanges() {
        if (!modified) return true;
        int r = JOptionPane.showConfirmDialog(this,
                "Há mudanças não salvas. Descartar?", "Confirmar",
                JOptionPane.YES_NO_OPTION);
        return r == JOptionPane.YES_OPTION;
    }

    private void tryClose() {
        if (confirmDiscardChanges()) dispose();
    }

    private String safeName(String name) {
        return name.replaceAll("[^a-zA-Z0-9_\\-]", "_").toLowerCase();
    }

    private void showAbout() {
        JOptionPane.showMessageDialog(this,
                "BBB Level Editor\nEditor visual de fases para Blue Bunny Blaster.\n\n" +
                "Atalhos: G=Chão  P=Plataforma  M=Moeda  I=Inimigo\n" +
                "         C=Checkpoint  W=Arma  B=Spawn  Del=Apagar\n\n" +
                "Clique direito também apaga elementos.",
                "Sobre", JOptionPane.INFORMATION_MESSAGE);
    }
}
