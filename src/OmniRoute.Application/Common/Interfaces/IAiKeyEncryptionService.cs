namespace OmniRoute.Application.Common.Interfaces;

public interface IAiKeyEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
