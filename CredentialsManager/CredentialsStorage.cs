using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace CredentialsManager;

public class CredentialsStorage
{
    private readonly string _filePath = "credentials.bin";

    public CredentialsStorage(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _filePath = filePath;
        }
    }

    public List<Credential> LoadCredentials()
    {
        if (!File.Exists(_filePath))
            return new List<Credential>();

        try
        {
            var encryptedData = File.ReadAllBytes(_filePath);
#pragma warning disable CA1416 // Validate platform compatibility
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
#pragma warning restore CA1416 // Validate platform compatibility
            var json = Encoding.UTF8.GetString(decryptedData);
            return JsonSerializer.Deserialize<List<Credential>>(json) ?? new List<Credential>();
        }
        catch
        {
            return new List<Credential>();
        }
    }

    public void SaveCredentials(List<Credential> credentials)
    {
        var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
        var data = Encoding.UTF8.GetBytes(json);
#pragma warning disable CA1416 // Validate platform compatibility
        var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
#pragma warning restore CA1416 // Validate platform compatibility
        File.WriteAllBytes(_filePath, encryptedData);
    }
}
