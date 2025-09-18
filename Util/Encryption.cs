using System.Security.Cryptography;

namespace Database.Util;

public class Encryption {
    private static ECDiffieHellman _serverEcdh;
    private static byte[] _serverPublicKey;
    private static readonly bool _hasCreatedServerEcdh = false;

    public Encryption() {
        if (_hasCreatedServerEcdh) return;
        _serverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        _serverPublicKey = _serverEcdh.ExportSubjectPublicKeyInfo();
    }

    public byte[] GetPublicKey() => _serverPublicKey;

    public byte[] DecryptPacket(byte[] clientPublicKey, byte[] nonce, byte[] text, byte[] tag) {
        ECDiffieHellman clientKey = ECDiffieHellman.Create();
        clientKey.ImportSubjectPublicKeyInfo(clientPublicKey, out _);

        byte[] sharedSecret = _serverEcdh.DeriveKeyMaterial(clientKey.PublicKey);
        byte[] aesKey = this.HKDF(sharedSecret, 32, "AES-GCM key"u8.ToArray());

        byte[] plain = new byte[text.Length];
        AesGcm gcm = new(aesKey);
        gcm.Decrypt(nonce, text, tag, plain);

        return plain;
    }

    public (byte[] cipherText, byte[] nonce, byte[] tag) EncryptPacket(byte[] clientPublicKey, byte[] data) {
        ECDiffieHellman clientKey = ECDiffieHellman.Create();
        clientKey.ImportSubjectPublicKeyInfo(clientPublicKey, out _);
        byte[] sharedSecret = _serverEcdh.DeriveKeyMaterial(clientKey.PublicKey);
        byte[] aesKey = this.HKDF(sharedSecret, 32, "AES-GCM key"u8.ToArray());

        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipherText = new byte[data.Length];
        byte[] tag = new byte[16];
        AesGcm gcm = new(aesKey);
        gcm.Encrypt(nonce, data, cipherText, tag);

        return (cipherText, nonce, tag);
    }

    private byte[] HKDF(byte[] ikm, int length, byte[] info) {
        HMACSHA256 hmac = new(new byte[32]);
        byte[] prk = hmac.ComputeHash(ikm);

        const int hashLen = 32;
        int n = (int) Math.Ceiling(length / (double) hashLen);
        byte[] okm = new byte[length];
        byte[] t = [];
        int pos = 0;


        for (int i = 1; i <= n; i++) {
            /*
            HMACSHA256 hm = new(prk);
            hm.TransformBlock(t, 0, t.Length, null, 0);

            if (info is { Length: > 0 }) hm.TransformBlock(info, 0, info.Length, null, 0);
            t = hm.Hash!;

            int toCopy = Math.Min(hashLen, length - pos);
            Array.Copy(t, 0, okm, pos, toCopy);
            pos += toCopy;
            */
            using HMACSHA256 hm = new(prk);

            // T(i) = HMAC-PRK(T(i-1) | info | i)
            byte[] input = new byte[t.Length + (info?.Length ?? 0) + 1];
            Buffer.BlockCopy(t, 0, input, 0, t.Length);
            if (info != null) Buffer.BlockCopy(info, 0, input, t.Length, info.Length);
            input[^1] = (byte) i;

            t = hm.ComputeHash(input);

            int toCopy = Math.Min(hashLen, length - pos);
            Array.Copy(t, 0, okm, pos, toCopy);
            pos += toCopy;
        }

        return okm;
    }

    public (byte[] hash, byte[] salt) Hash(string input) {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        Rfc2898DeriveBytes pbkdf2 = new(input, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        return (hash, salt);
    }

    public bool CompareHash(byte[] input, byte[] hash, byte[] salt) {
        Rfc2898DeriveBytes pbkdf2 = new(input, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hashToCheck = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(hash, hashToCheck);
    }

    public (byte[] cipherText, byte[] nonce, byte[] tag) Encrypt(byte[] secret, byte[] data) {
        byte[] aesKey = this.HKDF(secret, 32, "AES-GCM key"u8.ToArray());

        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipherText = new byte[data.Length];
        byte[] tag = new byte[16];
        AesGcm gcm = new(aesKey);
        gcm.Encrypt(nonce, data, cipherText, tag);

        return (cipherText, nonce, tag);
    }

    public byte[] Decrypt(byte[] secret, byte[] nonce, byte[] text, byte[] tag) {
        byte[] aesKey = this.HKDF(secret, 32, "AES-GCM key"u8.ToArray());

        byte[] plain = new byte[text.Length];
        AesGcm gcm = new(aesKey);
        gcm.Decrypt(nonce, text, tag, plain);

        return plain;
    }
}