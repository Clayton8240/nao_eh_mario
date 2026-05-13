package com.bbb.editor;

import javax.swing.SwingUtilities;
import javax.swing.UIManager;

import com.bbb.editor.ui.EditorWindow;

public class Main {
    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            try {
                UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
            } catch (Exception ignored) {
                // usa o look-and-feel padrão se o sistema não estiver disponível
            }
            new EditorWindow().setVisible(true);
        });
    }
}
