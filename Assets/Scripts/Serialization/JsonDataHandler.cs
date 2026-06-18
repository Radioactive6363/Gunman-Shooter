using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class JsonDataHandler
{
    public static void SaveJSON(string fileRoute, PlayerProfilePackage datos)
    {
        try
        {
            string jsonText = JsonConvert.SerializeObject(datos, Formatting.Indented);
            
            string jsonEncrypted = SimpleEncryption.ProcessXOR(jsonText);
            
            File.WriteAllText(fileRoute, jsonEncrypted);

            Log.Info($"Profile Saved.\n{fileRoute}");
        }
        catch (System.Exception e)
        {
            Log.Error($"Error Saving JSON: {e.Message}");
        }
    }

    public static PlayerProfilePackage LoadJSON(string fileRoute)
    {
        try
        {
            if (!File.Exists(fileRoute))
            {
                Log.Warning($"File not Found:\n{fileRoute}");

                return null;
            }
            
            string jsonEncrypted = File.ReadAllText(fileRoute);
            
            string jsonText = SimpleEncryption.ProcessXOR(jsonEncrypted);
            
            PlayerProfilePackage data = JsonConvert.DeserializeObject<PlayerProfilePackage>(
                    jsonText);

            Log.Info($"Profile Loaded.\n{fileRoute}");

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading JSON: {e.Message}");

            return null;
        }
    }
}