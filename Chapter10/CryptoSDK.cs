using Microsoft.Data.Encryption.Cryptography;

string plaintextString = "Hello, World!!";

var encryptionKey = new PlaintextDataEncryptionKey("MyKey");

var ciphertext = plaintextString.Encrypt(encryptionKey);
Console.WriteLine("Ciphertext");
var asHex = String.Join("",ciphertext.Select(c => ((int)c).ToString("x2")));
Console.WriteLine($" B64: {ciphertext.ToBase64String()}");
Console.WriteLine($" Hex: {asHex}");

var decrypted = ciphertext.Decrypt<string>(encryptionKey);
Console.WriteLine($"Plaintext: {decrypted}");