using System.Runtime.InteropServices;
using UnityEngine;

sealed class TextToSpeech
{
    public void StartSpeech(string text) => ttsrust_say(text);

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
    const string _dll = "__Internal";
#else
    const string _dll = "ttsrust";
#endif

    [DllImport(_dll)]
    static extern void ttsrust_say(string text);
}
