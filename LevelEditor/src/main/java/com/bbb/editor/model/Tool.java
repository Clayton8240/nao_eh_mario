package com.bbb.editor.model;

/** Ferramentas de edição disponíveis na toolbar. */
public enum Tool {
    GROUND       ("Chão [G]",          'G'),
    PLATFORM     ("Plataforma [P]",    'P'),
    DISAPPEARING ("Plat. Sumida [U]",  'U'),
    COIN         ("Moeda [M]",         'M'),
    SECRET_COIN  ("Moeda Secreta [X]", 'X'),
    ENEMY        ("Inimigo [I]",       'I'),
    CHECKPOINT   ("Checkpoint [C]",    'C'),
    WEAPON       ("Arma [W]",          'W'),
    SPAWN        ("Spawn [B]",         'B'),
    DELETE       ("Apagar [Del]",      '\0');

    public final String label;
    public final char   hotkey;

    Tool(String label, char hotkey) {
        this.label  = label;
        this.hotkey = hotkey;
    }
}
