package com.bbb.editor;
import com.bbb.editor.model.LevelData;
import com.bbb.editor.io.LevelSerializer;

public class TestJson {
    public static void main(String[] args) {
        LevelData ld = LevelData.createEmpty("Test", 100);
        System.out.println(LevelSerializer.toJson(ld));
    }
}
