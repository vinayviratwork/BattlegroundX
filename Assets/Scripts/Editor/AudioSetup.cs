using UnityEditor;
using UnityEngine;
using System.IO;

public static class AudioSetup
{
    private const string AudioFolder = "Assets/Audio";

    [MenuItem("Tools/Battleground X/Setup Audio (Procedural)")]
    public static void SetupAudio()
    {
        EnsureAudioFolder();
        GenerateProceduralClips();
        WireIntoAudioManager();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Audio Setup Complete",
            "Procedural audio clips generated and wired into AudioManager.\n\n" +
            "Clips are at: Assets/Audio/\n\n" +
            "To use real audio:\n" +
            "1. Drop your WAV/MP3 files into Assets/Audio/\n" +
            "2. Select AudioManager in the scene\n" +
            "3. Drag clips onto the matching Inspector slots.",
            "OK");
    }

    private static void EnsureAudioFolder()
    {
        if (!AssetDatabase.IsValidFolder(AudioFolder))
            AssetDatabase.CreateFolder("Assets", "Audio");
    }

    private static void GenerateProceduralClips()
    {
        CreateSineClip("Shot",        440f, 0.08f, decay: true);
        CreateSineClip("Reload",      220f, 0.25f, decay: false);
        CreateSineClip("Hit",         880f, 0.06f, decay: true);
        CreateSineClip("Footstep",    110f, 0.07f, decay: true);
        CreateSineClip("ZoneWarning", 330f, 0.5f,  decay: false);
        CreateSineClip("Pickup",      660f, 0.12f, decay: true);
        CreateSineClip("Death",       165f, 0.6f,  decay: true);
    }

    private static void CreateSineClip(string name, float freq, float duration, bool decay)
    {
        string path = $"{AudioFolder}/{name}.wav";
        if (File.Exists(Path.GetFullPath(path))) return;

        int sampleRate = 44100;
        int samples    = Mathf.RoundToInt(sampleRate * duration);
        float[] data   = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t        = (float)i / sampleRate;
            float envelope = decay ? Mathf.Exp(-t * 8f) : 1f;
            data[i]        = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        WriteWav(path, data, sampleRate);
        AssetDatabase.ImportAsset(path);
    }

    private static void WriteWav(string path, float[] data, int sampleRate)
    {
        using var fs  = new FileStream(path, FileMode.Create);
        using var bw  = new BinaryWriter(fs);

        int byteCount = data.Length * 2;

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + byteCount);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(byteCount);

        foreach (float s in data)
            bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
    }

    private static void WireIntoAudioManager()
    {
        var am = Object.FindObjectOfType<AudioManager>();
        if (am == null) return;

        var so = new SerializedObject(am);
        WireClip(so, "shotClip",        "Shot");
        WireClip(so, "reloadClip",      "Reload");
        WireClip(so, "hitClip",         "Hit");
        WireClip(so, "footstepClip",    "Footstep");
        WireClip(so, "zoneWarningClip", "ZoneWarning");
        WireClip(so, "pickupClip",      "Pickup");
        WireClip(so, "deathClip",       "Death");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireClip(SerializedObject so, string field, string clipName)
    {
        string path = $"{AudioFolder}/{clipName}.wav";
        var clip    = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) return;
        so.FindProperty(field).objectReferenceValue = clip;
    }
}
