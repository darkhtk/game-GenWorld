using System.Threading.Tasks;
using UnityEngine;

public class OllamaClient
{
    readonly string _url, _fastModel, _largeModel;

    public OllamaClient(string url = "http://localhost:11434", string fastModel = "gemma3:4b", string largeModel = "gemma3:12b") { _url = url; _fastModel = fastModel; _largeModel = largeModel; }
    public async Task<bool> CheckAvailability() { await Task.Delay(1); return false; }
    public async Task<string> GenerateDialogue(string prompt) { await Task.Delay(1); return null; }
}
