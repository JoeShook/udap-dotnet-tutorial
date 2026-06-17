using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using X509Extensions = Org.BouncyCastle.Asn1.X509.X509Extensions;

namespace udap.certificates.server.devdays;

/// <summary>
/// Keeps the served Certificate Revocation Lists fresh.
///
/// The BouncyCastle <c>TrustChainValidator</c> in Udap.Client rejects a CRL whose
/// <c>NextUpdate</c> has passed (reported as <c>RevocationStatusUnknown</c>). The PKI generator
/// signs the Community3 SubCA CRL with only a one-month window, so it goes stale quickly. At
/// startup we re-sign any CRL that is expired (or about to expire) using the matching CA/SubCA
/// signing key shipped in the <c>SigningKeys</c> folder, preserving the existing revoked entries
/// and bumping the CRL number.
/// </summary>
public static class CrlMaintenance
{
    private const string SigningKeyPassword = "udap-test";

    /// <summary>
    /// Validity window applied to a freshly re-signed CRL (one week into the future).
    /// </summary>
    private static readonly TimeSpan NewValidity = TimeSpan.FromDays(7);

    /// <summary>
    /// Inspects every <c>*.crl</c> in <paramref name="crlDirectory"/> and re-signs any that are
    /// expired or within <paramref name="renewWithin"/> of expiring. A CRL named
    /// <c>DevDaysSubCA_3.crl</c> is signed with <c>SigningKeys/DevDaysSubCA_3.pfx</c> (the signer
    /// PFX shares the CRL's base file name).
    /// </summary>
    public static void RefreshExpiringCrls(
        string crlDirectory,
        string signingKeysDirectory,
        TimeSpan renewWithin,
        ILogger logger)
    {
        if (!Directory.Exists(crlDirectory))
        {
            logger.LogWarning("CRL directory not found, skipping CRL refresh: {CrlDirectory}", crlDirectory);
            return;
        }

        var now = DateTime.UtcNow;
        var renewBefore = now.Add(renewWithin);

        foreach (var crlPath in Directory.GetFiles(crlDirectory, "*.crl"))
        {
            try
            {
                var existing = new X509CrlParser().ReadCrl(File.ReadAllBytes(crlPath));

                var nextUpdate = existing.NextUpdate?.Value.ToUniversalTime();
                if (nextUpdate.HasValue && nextUpdate.Value > renewBefore)
                {
                    logger.LogDebug(
                        "CRL {Crl} still valid until {NextUpdate:u}, leaving as is.",
                        Path.GetFileName(crlPath), nextUpdate.Value);
                    continue;
                }

                var baseName = Path.GetFileNameWithoutExtension(crlPath);
                var signingKeyPath = Path.Combine(signingKeysDirectory, baseName + ".pfx");
                if (!File.Exists(signingKeyPath))
                {
                    logger.LogWarning(
                        "No signing key {SigningKey} for expiring CRL {Crl}; cannot regenerate.",
                        Path.GetFileName(signingKeyPath), Path.GetFileName(crlPath));
                    continue;
                }

                Regenerate(crlPath, signingKeyPath, existing, now, logger);
            }
            catch (Exception ex)
            {
                // A single bad/locked CRL must not stop the server from starting and serving the rest.
                logger.LogWarning(ex, "Failed to refresh CRL {Crl}", Path.GetFileName(crlPath));
            }
        }
    }

    private static void Regenerate(
        string crlPath,
        string signingKeyPath,
        X509Crl existing,
        DateTime now,
        ILogger logger)
    {
        using var signer = X509CertificateLoader.LoadPkcs12FromFile(
            signingKeyPath, SigningKeyPassword, X509KeyStorageFlags.Exportable);

        var bcSigner = DotNetUtilities.FromX509Certificate(signer);

        var crlGen = new X509V2CrlGenerator();
        crlGen.SetIssuerDN(bcSigner.SubjectDN);
        crlGen.SetThisUpdate(now);
        crlGen.SetNextUpdate(now.Add(NewValidity));

        // Preserve every previously revoked entry (e.g. the Community3 revoked client).
        var preserved = 0;
        var revoked = existing.GetRevokedCertificates();
        if (revoked != null)
        {
            foreach (X509CrlEntry entry in revoked)
            {
                crlGen.AddCrlEntry(entry.SerialNumber, entry.RevocationDate, ReadReason(entry));
                preserved++;
            }
        }

        crlGen.AddExtension(
            X509Extensions.AuthorityKeyIdentifier,
            false,
            new AuthorityKeyIdentifierStructure(bcSigner.GetPublicKey()));

        crlGen.AddExtension(X509Extensions.CrlNumber, false, NextCrlNumber(existing));

        using var rsa = RSA.Create();
        rsa.ImportParameters(signer.GetRSAPrivateKey()!.ExportParameters(true));
        var signingKey = DotNetUtilities.GetKeyPair(rsa).Private;

        var crl = crlGen.Generate(new Asn1SignatureFactory("SHA256WithRSAEncryption", signingKey));
        File.WriteAllBytes(crlPath, crl.GetEncoded());

        logger.LogInformation(
            "Regenerated CRL {Crl}: preserved {Count} revoked entr{Plural}, new NextUpdate {NextUpdate:u}.",
            Path.GetFileName(crlPath), preserved, preserved == 1 ? "y" : "ies", now.Add(NewValidity));
    }

    private static int ReadReason(X509CrlEntry entry)
    {
        // Default matches the reason the PKI generator uses; preserve the original when present.
        try
        {
            var reasonExt = entry.GetExtensionValue(X509Extensions.ReasonCode);
            if (reasonExt != null)
            {
                var asn1 = X509ExtensionUtilities.FromExtensionValue(reasonExt);
                return DerEnumerated.GetInstance(asn1).Value.IntValue;
            }
        }
        catch
        {
            // fall through to the default reason
        }

        return CrlReason.PrivilegeWithdrawn;
    }

    private static CrlNumber NextCrlNumber(X509Crl existing)
    {
        try
        {
            var crlNumExt = existing.GetExtensionValue(X509Extensions.CrlNumber);
            if (crlNumExt != null)
            {
                var asn1 = X509ExtensionUtilities.FromExtensionValue(crlNumExt);
                var prev = DerInteger.GetInstance(asn1).PositiveValue;
                return new CrlNumber(prev.Add(BigInteger.One));
            }
        }
        catch
        {
            // fall through to the default
        }

        return new CrlNumber(BigInteger.One);
    }
}
