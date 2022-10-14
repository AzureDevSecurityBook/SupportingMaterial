using Azure.Identity;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using Microsoft.Data.Encryption.Cryptography;

var AzureKeyVaultKeyPath = "https://<your key vault>/keys/<key name>/<key version guid>";
var TokenCredential = new DefaultAzureCredential();
var azureKeyProvider = new AzureKeyVaultKeyStoreProvider(TokenCredential);
var keyEncryptionKey = new KeyEncryptionKey("KEK", AzureKeyVaultKeyPath, azureKeyProvider);
var dataEncryptionKey = new ProtectedDataEncryptionKey("DEK", keyEncryptionKey);

var original = DateTime.Now;
Console.WriteLine ("PT: " + original);

var encryptedBytes = original.Encrypt(dataEncryptionKey);
var encryptedHex = encryptedBytes.ToHexString();
Console.WriteLine ("CT:" + encryptedHex);

var bytesToDecrypt = encryptedHex.FromHexString();
var decryptedBytes = bytesToDecrypt.Decrypt<DateTime>(dataEncryptionKey);
Console.WriteLine ("PT: " + decryptedBytes);