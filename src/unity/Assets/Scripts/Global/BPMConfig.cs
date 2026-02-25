using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BPMConfig
{
    public int bpm;
    public List<Note> notes;

    public BPMConfig(int bpm)
    {
        this.bpm = bpm;
        this.notes = new List<Note>();
    }
}

public class BPMConfigurations : MonoBehaviour
{
    public static BPMConfig[] configs = new BPMConfig[]
    {
        CreateBPM120Config(),
        CreateBPM150Config(),
        CreateBPM180Config()
    };

    // 120 BPM - 纵连配置（每拍一个note，持续30秒）
    private static BPMConfig CreateBPM120Config()
    {
        BPMConfig config = new BPMConfig(120);
        float beatInterval = 60f / 120f; // 0.5秒一拍

        for (int i = 0; i < 60; i++) // 60个notes，30秒
        {
            config.notes.Add(new Note(i * beatInterval, 0));
        }

        return config;
    }

    // 150 BPM - 纵连配置
    private static BPMConfig CreateBPM150Config()
    {
        BPMConfig config = new BPMConfig(150);
        float beatInterval = 60f / 150f; // 0.4秒一拍

        for (int i = 0; i < 75; i++) // 75个notes，30秒
        {
            config.notes.Add(new Note(i * beatInterval, 0));
        }

        return config;
    }

    // 180 BPM - 纵连配置
    private static BPMConfig CreateBPM180Config()
    {
        BPMConfig config = new BPMConfig(180);
        float beatInterval = 60f / 180f; // 0.333秒一拍

        for (int i = 0; i < 90; i++) // 90个notes，30秒
        {
            config.notes.Add(new Note(i * beatInterval, 0));
        }

        return config;
    }
}
