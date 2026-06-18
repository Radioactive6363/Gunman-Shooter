using System.Text;

public static class SimpleEncryption
{
    private const string SecretKey = "CowboyDuel";
    
    public static string ProcessXOR(string data)
    {
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            char encryptedChar =
                (char)(data[i] ^ SecretKey[i % SecretKey.Length]);

            result.Append(encryptedChar);
        }

        return result.ToString();
    }
}