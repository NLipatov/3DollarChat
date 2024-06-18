namespace EthachatShared.Encryption;

public enum KeyFormat
{
    Unspecified,
    /// <summary>
    /// Key as a PEM-encoded PKCS #8 object
    /// </summary>
    PemPkcs8,

    /// <summary>
    /// Key as a PEM-encoded SubjectPublicKeyInfo object
    /// </summary>
    PemSpki,

    /// <summary>
    /// Key as byte array
    /// </summary>
    Raw
}
