namespace PracticalAgileCrypto;

using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;


public class AgileCrypto
{
    // salt size is byte count, not bit count
    private const int Saltsize = 128 / 8;

    //This is for testing purposes - 4 versions
    public enum Version
    {
        Version1 = 1,
        Version2 = 2,
        Version3 = 3,
        Version4 = 4,
        VersionLatest = Version4
    };

    // 'Delim' is used to delimit the items in the resulting string
    private const char Delim = '|';

    private Version              _ver;
    private SymmetricAlgorithm?  _symCrypto;
    private HMAC?                _hMac;
    private DeriveBytes?         _keyDerivation;
    private int                  _iterationCount;
    private byte[]              _salt;
    private byte[]?              _keyMaterial;

    public AgileCrypto(Version ver)
    {
        _ver = ver;
        _salt = RandomNumberGenerator.GetBytes(Saltsize);
    }

    public AgileCrypto()
        : this(Version.VersionLatest)
    {
    }

    /// <summary>
    /// Builds the internal crypto classes based on the version#
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private void BuildCryptoObjects(string pwd)
    {
        _keyMaterial = Encoding.ASCII.GetBytes(pwd);

        switch (_ver)
        {
            case Version.Version1:
                _symCrypto = CreateSymmetricAlgorithm("DES");
                _symCrypto.Mode = CipherMode.ECB;
                _symCrypto.Padding = PaddingMode.PKCS7;
                _hMac = new HMACMD5(); 
                _iterationCount = 100;
                _keyDerivation = new Rfc2898DeriveBytes(_keyMaterial, _salt, _iterationCount);
                break;

            case Version.Version2:
                _symCrypto = CreateSymmetricAlgorithm("TripleDes");
                _symCrypto.KeySize = 128;
                _symCrypto.Mode = CipherMode.CBC;
                _symCrypto.Padding = PaddingMode.PKCS7;
                _hMac = new HMACMD5();
                _iterationCount = 1000;
                _keyDerivation = new Rfc2898DeriveBytes(_keyMaterial, _salt, _iterationCount);
                break;

            case Version.Version3:
                _symCrypto = CreateSymmetricAlgorithm("AesManaged");
                _symCrypto.KeySize = 128;
                _symCrypto.Mode = CipherMode.CBC;
                _symCrypto.Padding = PaddingMode.PKCS7;
                _hMac = new HMACSHA1();
                _iterationCount = 4000;
                _keyDerivation = new Rfc2898DeriveBytes(_keyMaterial, _salt, _iterationCount);
                break;

            case Version.Version4:
                _symCrypto = CreateSymmetricAlgorithm("AesManaged");
                _symCrypto.KeySize = 256;
                _symCrypto.Mode = CipherMode.CBC;
                _symCrypto.Padding = PaddingMode.ANSIX923;
                _hMac = new HMACSHA256();
                _iterationCount = 20000;
                _keyDerivation = new Rfc2898DeriveBytes(_keyMaterial, _salt, _iterationCount);
                break;

            default:
                throw new ArgumentException("Invalid crypto version.");
        }
    }

    /// <summary>
    /// Method to encrypt then MAC some plaintext
    /// </summary>
    /// <param name="pwd"></param>
    /// <param name="plaintext"></param>
    /// <returns>Base64-encoded string that includes: version info, IV, PBKDF# etc</returns>
    public string Protect(string pwd, string plaintext)
    {
        if (_keyDerivation is null || _hMac is null || _symCrypto is null)
            throw new ArgumentNullException($"null crypto arguments");

        BuildCryptoObjects(pwd);

        var sb = new StringBuilder();

        byte[] encrypted = EncryptThePlaintext(plaintext);

        sb.Append((int)_ver)
            .Append(Delim)
            .Append(Convert.ToBase64String(_symCrypto.IV))
            .Append(Delim)
            .Append(Convert.ToBase64String(_salt))
            .Append(Delim)
            .Append(Convert.ToBase64String(encrypted))
            .Append(Delim);

        // Now create an HMAC over all the previous data
        // incl the version#, IV, salt, and ciphertext
        // all but the ciphertext are plaintext, we're just protecting
        // them all from tampering

        // Derive a new key for this work
        _hMac.Key = _keyDerivation.GetBytes(_hMac.HashSize);
        _hMac.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        sb.Append(Convert.ToBase64String(_hMac.Hash));

        return sb.ToString();
    }

    /// <summary>
    /// Method to verify the MAC and then decrypt a protected blob if the HMAC is ok.
    /// </summary>
    /// <param name="pwd"></param>
    /// <param name="protectedBlob"></param>
    /// <returns>The plaintext string</returns>
    /// <exception cref="ArgumentException"></exception>
    public string Unprotect(string pwd, string protectedBlob)
    {
        if (string.IsNullOrWhiteSpace(protectedBlob))
            throw new ArgumentException($"'{nameof(protectedBlob)}' cannot be null or empty.", nameof(protectedBlob));

        if (_keyDerivation is null || _hMac is null || _symCrypto is null)
            throw new ArgumentNullException($"null crypto arguments");

        // Pull out the parts of the protected blob
        // 0: version
        // 1: IV
        // 2: salt
        // 3: ciphertext
        // 4: MAC
        const int version = 0, initvect = 1, salt = 2, ciphertext = 3, mac = 4;
        string[] elements = protectedBlob.Split(new char[] { Delim });

        // Get version
        int.TryParse(elements[version], out int ver);
        _ver = (Version)ver;

        // Get IV/salt/ciphertext
        byte[] iv = Convert.FromBase64String(elements[initvect]);
        _salt = Convert.FromBase64String(elements[salt]);
        byte[] ctext = Convert.FromBase64String(elements[ciphertext]);

        // We have all the data we need to build the crypto algs
        BuildCryptoObjects(pwd);

        _symCrypto.Key = _keyDerivation.GetBytes(_symCrypto.KeySize >> 3);
        _symCrypto.IV = iv;

        // Before we decrypt the ciphertext we need to check the MAC
        if (string.IsNullOrWhiteSpace(elements[mac]))
            throw new ArgumentException($"'{nameof(protectedBlob)}' Missing MAC.", nameof(protectedBlob));

        // Check the HMAC, this works by:
        // 1) stripping the HMAC off the protected blob,
        // 2) creating an HMAC of the resulting string above
        // 3) comparing the HMAC in the protected blob and the generated HMAC
        string blobLessMac = protectedBlob.Substring(0, protectedBlob.LastIndexOf(elements[mac]));
        _hMac.Key = _keyDerivation.GetBytes(_hMac.HashSize);
        byte[] result = _hMac.ComputeHash(Encoding.UTF8.GetBytes(blobLessMac));
        if (string.Compare(elements[mac], Convert.ToBase64String(result), true) != 0)
            throw new ArgumentException($"'{nameof(protectedBlob)}' Incorrect MAC.", nameof(protectedBlob));

        string plaintext = DecryptThePlainText(ctext);

        return plaintext;
    }

    private static SymmetricAlgorithm CreateSymmetricAlgorithm(string name)
    {
        SymmetricAlgorithm? result = SymmetricAlgorithm.Create(name);
        if (result is null)
            throw new InvalidOperationException($"The {name} symmetric algorithm cannot be created.");

        return result;
    }

    private byte[] EncryptThePlaintext(string plaintext)
    {
        if (_symCrypto is null || _keyDerivation is null)
            throw new ArgumentNullException("Symmetric and Key derivations algorithms cannot be NULL.");

        _symCrypto.GenerateIV();
        _symCrypto.Key = _keyDerivation.GetBytes(_symCrypto.KeySize >> 3);

        byte[] encrypted;

        ICryptoTransform encryptor = _symCrypto.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);
        swEncrypt.Write(plaintext);
        swEncrypt.Close();
        encrypted = msEncrypt.ToArray();
      
        return encrypted;
    }

    private string DecryptThePlainText(byte[] ctext)
    {
        if (_symCrypto == null)
            throw new ArgumentNullException("Symmetric algorithm cannot be NULL.");

        string plaintext;
        ICryptoTransform decryptor = _symCrypto.CreateDecryptor();
        using var msDecrypt = new MemoryStream(ctext);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        plaintext = srDecrypt.ReadToEnd();

        return plaintext;
    }
}

static class Program
{
    static void Main()
    {
        // some tests... yes, pwd and plaintext is in the code!
        const string pwd = "SSsshh!!";
        const string plaintext = "Hello, World!";

        string c1 = new AgileCrypto(AgileCrypto.Version.Version1).Protect(pwd, plaintext);
        string p1 = new AgileCrypto().Unprotect(pwd, c1);
        Console.WriteLine($"P1 {p1 == plaintext}");

        string c2 = new AgileCrypto(AgileCrypto.Version.Version2).Protect(pwd, plaintext);
        string p2 = new AgileCrypto().Unprotect(pwd, c2);
        Console.WriteLine($"P2 {p2 == plaintext}");

        string c3 = new AgileCrypto(AgileCrypto.Version.Version3).Protect(pwd, plaintext);
        string p3 = new AgileCrypto().Unprotect(pwd, c3);
        Console.WriteLine($"P3 {p3 == plaintext}");

        string c4 = new AgileCrypto(AgileCrypto.Version.Version4).Protect(pwd, plaintext);
        string p4 = new AgileCrypto().Unprotect(pwd, c4);
        Console.WriteLine($"P4 {p4 == plaintext}");

        string c5 = new AgileCrypto(AgileCrypto.Version.Version4).Protect(pwd, plaintext);
        string p5 = new AgileCrypto().Unprotect(pwd, c5);
        Console.WriteLine($"P5 {p5 == plaintext}");

        // Two plaintexts encrypted with the same
        // algorithm and key should yield two different ciphertexts
        // because the IV and salt are always different
        Console.WriteLine($"C4 != C5 {c4 != c5}");

        // Test a single object used to encrypt and decrypt 
        var ac = new AgileCrypto();
        string c6 = ac.Protect(pwd, plaintext);
        string c7 = ac.Protect(pwd, plaintext);
        string p6 = ac.Unprotect(pwd, c6);
        string p7 = ac.Unprotect(pwd, c7);
        Console.WriteLine($"P6 && P7 {p6 == plaintext && p7 == plaintext}");
    }
}
