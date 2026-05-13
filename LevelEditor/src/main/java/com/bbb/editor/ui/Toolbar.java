package com.bbb.editor.ui;

import java.awt.Color;
import java.awt.Component;
import java.awt.Dimension;
import java.awt.Font;
import java.util.HashMap;
import java.util.Map;

import javax.swing.Box;
import javax.swing.BoxLayout;
import javax.swing.ButtonGroup;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JToggleButton;
import javax.swing.border.EmptyBorder;

import com.bbb.editor.model.Tool;

/** Painel vertical à esquerda com botões de ferramenta. */
public class Toolbar extends JPanel {

    private final EditorWindow editor;
    private final Map<Tool, JToggleButton> buttons = new HashMap<>();
    private final ButtonGroup group = new ButtonGroup();

    public Toolbar(EditorWindow editor) {
        this.editor = editor;
        setLayout(new BoxLayout(this, BoxLayout.Y_AXIS));
        setBorder(new EmptyBorder(8, 6, 8, 6));
        setBackground(new Color(45, 45, 48));
        setPreferredSize(new Dimension(130, 0));

        add(sectionLabel("TERRENO"));
        add(toolButton(Tool.GROUND));
        add(toolButton(Tool.PLATFORM));
        add(toolButton(Tool.DISAPPEARING));

        add(Box.createVerticalStrut(10));
        add(sectionLabel("ITENS"));
        add(toolButton(Tool.COIN));
        add(toolButton(Tool.SECRET_COIN));
        add(toolButton(Tool.WEAPON));

        add(Box.createVerticalStrut(10));
        add(sectionLabel("ENTIDADES"));
        add(toolButton(Tool.ENEMY));
        add(toolButton(Tool.CHECKPOINT));
        add(toolButton(Tool.SPAWN));

        add(Box.createVerticalStrut(10));
        add(toolButton(Tool.DELETE));

        add(Box.createVerticalGlue());

        // Seleciona a ferramenta padrão
        selectTool(Tool.GROUND);
    }

    private JLabel sectionLabel(String text) {
        JLabel lbl = new JLabel(text);
        lbl.setForeground(new Color(130, 130, 130));
        lbl.setFont(new Font("SansSerif", Font.PLAIN, 10));
        lbl.setAlignmentX(Component.LEFT_ALIGNMENT);
        lbl.setBorder(new EmptyBorder(4, 0, 2, 0));
        return lbl;
    }

    private JToggleButton toolButton(Tool tool) {
        JToggleButton btn = new JToggleButton(tool.label);
        btn.setAlignmentX(Component.LEFT_ALIGNMENT);
        btn.setMaximumSize(new Dimension(Integer.MAX_VALUE, 30));
        btn.setFont(new Font("SansSerif", Font.PLAIN, 12));
        btn.setForeground(Color.WHITE);
        btn.setBackground(new Color(63, 63, 70));
        btn.setFocusPainted(false);
        btn.setBorderPainted(false);
        btn.addActionListener(e -> editor.setTool(tool));
        group.add(btn);
        buttons.put(tool, btn);
        return btn;
    }

    public void selectTool(Tool tool) {
        JToggleButton btn = buttons.get(tool);
        if (btn != null) btn.setSelected(true);
    }
}
