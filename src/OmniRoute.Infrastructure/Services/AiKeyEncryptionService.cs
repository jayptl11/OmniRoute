using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Infrastructure.Settings;

namespace OmniRoute.Infrastructure.Services;

internal sealed class AiKeyEncryptionService : IAiKeyEncryptionService
{
    private readonly byte[] _key;

    public AiKeyEncryptionService(IOptions<AiSettings> settings)
    {
        var base64Key = settings.Value.EncryptionKey;
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new InvalidOperationException("AiSettings:EncryptionKey is not configured.");

        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new InvalidOperationException("AiSettings:EncryptionKey must be a Base64-encoded 32-byte value (AES-256).");
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV so it can be extracted at decrypt time
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var fullBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullBytes.Length - iv.Length];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullBytes, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
