package com.bbb.editor.io;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.Reader;
import java.io.Writer;
import java.nio.charset.StandardCharsets;

import com.bbb.editor.model.LevelData;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

/**
 * Salva e carrega LevelData como JSON.
 * O formato gerado é compatível com JsonUtility.FromJson<LevelData> do Unity:
 *   - campos com os mesmos nomes do C#
 *   - Vec2 serializado como {"x": f, "y": f}
 */
public class LevelSerializer {

    private static final Gson GSON = new GsonBuilder()
            .setPrettyPrinting()
            .serializeNulls()
            .create();

    public static void save(LevelData level, File file) throws IOException {
        try (Writer writer = new OutputStreamWriter(
                new FileOutputStream(file), StandardCharsets.UTF_8)) {
            GSON.toJson(level, writer);
        }
    }

    public static LevelData load(File file) throws IOException {
        try (Reader reader = new InputStreamReader(
                new FileInputStream(file), StandardCharsets.UTF_8)) {
            return GSON.fromJson(reader, LevelData.class);
        }
    }

    /** Serializa para string (útil para preview/debug). */
    public static String toJson(LevelData level) {
        return GSON.toJson(level);
    }
}
