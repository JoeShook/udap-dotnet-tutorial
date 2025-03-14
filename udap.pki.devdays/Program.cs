﻿// See https://aka.ms/new-console-template for more information
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using udap.pki.devdays;
using X509Extension = System.Security.Cryptography.X509Certificates.X509Extension;
using X509Extensions = Org.BouncyCastle.Asn1.X509.X509Extensions;

Console.WriteLine("Generating PKI for UDAP DevDays");



string staticCertPort = "5034";
string certificateStore = $"CertificateStore";
string certificateStoreFullPath = $"{BaseDir()}/{certificateStore}";

var certName = "DevDaysFhirServerRSAClient";
var community = "Community1";


MakeAuthorities($"{certificateStore}/{community}",  //communityStorePath
    "DevDaysCA_1",                                  //anchorName
    "DevDaysSubCA_1"                                //intermediateName
);

var caCert = new X509Certificate2($"{BaseDir()}/{certificateStore}/{community}/DevDaysCA_1.pfx", "udap-test");
var intermediateCert = new X509Certificate2($"{BaseDir()}/{certificateStore}/{community}/intermediates/DevDaysSubCA_1.pfx", "udap-test");



IssueUdapClientCertificate(
    caCert, 
    intermediateCert,
    intermediateCert.GetRSAPrivateKey(),
    $"CN={certName}, OU=DevDays-Community1, O=Fhir Coding, L=Portland, S=Oregon, C=US",   //issuedDistinguishedName
    [
        "https://localhost:7017/fhir/r4",
        "https://host.docker.internal:7017/fhir/r4",
        "http://localhost/fhir/r4"
    ],
    $"{certificateStore}/{community}/issued/{certName}",                                 //client certificate store Path
    new List<string>(){
        $"{BaseDir()}/../udap.fhirserver.devdays/{certificateStore}/{community}/issued/{certName}.pfx"
    },
    new List<string>
    {
        $"http://host.docker.internal:{staticCertPort}/certs/DevDaysSubCA_1.crt",
        $"http://localhost:{staticCertPort}/certs/DevDaysSubCA_1.crt"        
    },
    new List<string> {
        $"http://host.docker.internal:{staticCertPort}/crl/DevDaysSubCA_1.crl",
        $"http://localhost:{staticCertPort}/crl/DevDaysSubCA_1.crl"        
    }
);



certName = "DevDaysIdpClient";
IssueUdapClientCertificate(
    caCert,
    intermediateCert,
    intermediateCert.GetRSAPrivateKey(),
    $"CN={certName}, OU=DevDays-Community1, O=Fhir Coding, L=Portland, S=Oregon, C=US",     //issuedDistinguishedName
    [
        "https://localhost:5202/",
        "https://host.docker.internal:5202/",
        "http://localhost/"
    ],                                                                                      //SubjAltNames (Demonstrate multiple)
    $"{certificateStore}/{community}/issued/{certName}",                                 //client certificate store Path
    new List<string>(){
        $"{BaseDir()}/../udap.idp.server.devdays/{certificateStore}/{community}/issued/{certName}.pfx",
        $"{BaseDir()}/../udap.authserver.devdays/{certificateStore}/{community}/issued/{certName}.pfx"
    },
    new List<string>
    {
        $"http://host.docker.internal:{staticCertPort}/certs/DevDaysSubCA_1.crt",
        $"http://localhost:{staticCertPort}/certs/DevDaysSubCA_1.crt"        
    },
    new List<string> {
        $"http://host.docker.internal:{staticCertPort}/crl/DevDaysSubCA_1.crl",
        $"http://localhost:{staticCertPort}/crl/DevDaysSubCA_1.crl"        
    }
);


certName = "DevDaysECDSAClient";
community = "Community2";


MakeAuthorities($"{certificateStore}/{community}",   //communityStorePath
    "DevDaysCA_2",                                  //anchorName
    "DevDaysSubCA_2"                                //intermediateName
);

caCert = new X509Certificate2($"{BaseDir()}/{certificateStore}/{community}/DevDaysCA_2.pfx", "udap-test");
intermediateCert = new X509Certificate2($"{BaseDir()}/{certificateStore}/{community}/intermediates/DevDaysSubCA_2.pfx", "udap-test");

IssueUdapClientCertificateECDSA(
    caCert,
    intermediateCert,
    intermediateCert.GetRSAPrivateKey(),
    $"CN={certName}, OU=DevDays-Community2, O=Fhir Coding, L=Portland, S=Oregon, C=US", //issuedDistinguishedName
    [
        "https://localhost:7017/fhir/r4",
        "https://host.docker.internal:7017/fhir/r4",
        "http://localhost/fhir/r4"
    ],                                                                                  //SubjAltNames (Demonstrate multiple)
    $"{certificateStore}/{community}/issued/{certName}",                             //client certificate store Path
    $"{BaseDir()}/../udap.fhirserver.devdays/{certificateStore}/{community}/issued/{certName}.pfx",    
    new List<string>
    {
        $"http://host.docker.internal:{staticCertPort}/certs/DevDaysSubCA_2.crt",
        $"http://localhost:{staticCertPort}/certs/DevDaysSubCA_2.crt"        
    },
    new List<string> {
        $"http://host.docker.internal:{staticCertPort}/crl/DevDaysSubCA_2.crl",
        $"http://localhost:{staticCertPort}/crl/DevDaysSubCA_2.crl"        
    }
);



certName = "DevDaysRevokedClient";
community = "Community3";

MakeAuthorities($"{certificateStore}/{community}",   //communityStorePath
    "DevDaysCA_3",                                  //anchorName
    "DevDaysSubCA_3"                                //intermediateName
);

caCert = new X509Certificate2($"{BaseDir()}/{certificateStore}/{community}/DevDaysCA_3.pfx", "udap-test");
intermediateCert = new X509Certificate2($"{BaseDir()}/{certificateStore}/{community}/intermediates/DevDaysSubCA_3.pfx", "udap-test");


// Let's revoke a certificate
//
// Add another Community and certificate to revoke
//


IssueUdapClientCertificate(
    caCert,
    intermediateCert,
    intermediateCert.GetRSAPrivateKey(),
    $"CN={certName}, OU=DevDays-Community3, O=Fhir Coding, L=Portland, S=Oregon, C=US",  //issuedDistinguishedName
    [
        "https://localhost:7017/fhir/r4",
        "https://host.docker.internal:7017/fhir/r4",
        "http://localhost/fhir/r4"
    ],                                                                                  //SubjAltNames (Demonstrate multiple)
    $"{certificateStore}/{community}/issued/{certName}",                             //client certificate store Path
    new List<string>()
    {
        $"{BaseDir()}/../udap.fhirserver.devdays/{certificateStore}/{community}/issued/{certName}.pfx"
    },    
    new List<string>
    {
        $"http://host.docker.internal:{staticCertPort}/certs/DevDaysSubCA_3.crt",
        $"http://localhost:{staticCertPort}/certs/DevDaysSubCA_3.crt"        
    },
    new List<string> {
        $"http://host.docker.internal:{staticCertPort}/crl/DevDaysSubCA_3.crl",
        $"http://localhost:{staticCertPort}/crl/DevDaysSubCA_3.crl"        
    }
);

// Revoke

var subCA = new X509Certificate2($"{certificateStoreFullPath}/Community3/intermediates/DevDaysSubCA_3.pfx", "udap-test", X509KeyStorageFlags.Exportable);
var revokeCertificate = new X509Certificate2($"{certificateStoreFullPath}/Community3/issued/DevDaysRevokedClient.pfx", "udap-test");

RevokeCertificate(subCA, revokeCertificate, $"{certificateStoreFullPath}/Community3/crl/DevDaysSubCA_3.crl");


//
// tls client and use the DevDaysCA_1.crt as the CA
//
GenerateTlsCertificate($"{BaseDir()}/{certificateStore}/tls", $"{BaseDir()}/{certificateStore}/Community1/DevDaysCA_1.pfx");



var baseDir = BaseDir();

File.Copy(
    $"{baseDir}/{certificateStore}/Community1/DevDaysCA_1.crt",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/Community1/DevDaysCA_1.crt",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/Community2/DevDaysCA_2.crt",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/Community2/DevDaysCA_2.crt",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/Community3/DevDaysCA_3.crt",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/Community3/DevDaysCA_3.crt",
    true);


File.Copy(
    $"{baseDir}/{certificateStore}/Community1/issued/DevDaysFhirServerRSAClient.pfx",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/{community}/issued/DevDaysFhirServerRSAClient.pfx",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/Community1/issued/DevDaysIdpClient.pfx",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/{community}/issued/DevDaysIdpClient.pfx",
    true);


File.Copy(
    $"{baseDir}/{certificateStore}/Community2/issued/DevDaysECDSAClient.pfx",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/Community2/issued/DevDaysECDSAClient.pfx",
    true);


File.Copy(
    $"{baseDir}/{certificateStore}/Community3/issued/DevDaysRevokedClient.pfx",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/Community3/issued/DevDaysRevokedClient.pfx",
    true);




File.Copy(
    $"{baseDir}/{certificateStore}/Community1/DevDaysCA_1.crt",
    $"{baseDir}/../udap.idp.server.devdays/{certificateStore}/Community1/DevDaysCA_1.crt",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/Community1/intermediates/DevDaysSubCA_1.crt",
    $"{baseDir}/../udap.idp.server.devdays/{certificateStore}/Community1/intermediates/DevDaysSubCA_1.crt",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/Community1/issued/DevDaysIdpClient.pfx",
    $"{baseDir}/../udap.idp.server.devdays/{certificateStore}/Community1/issued/DevDaysIdpClient.pfx",
    true);


//
// Tls distribution
//

File.Copy(
    $"{baseDir}/{certificateStore}/Community1/DevDaysCA_1.crt",
    $"{baseDir}/../CertificateStore/DevDaysCA_1.crt",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    $"{baseDir}/../CertificateStore/udap-tutorial-dev-tls-cert.pfx",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    $"{baseDir}/../CertificateStore/udap-tutorial-dev-tls-cert.cer",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    $"{baseDir}/../udap.fhirserver.devdays/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    $"{baseDir}/../udap.fhirserver.devdays/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    $"{baseDir}/../udap.authserver.devdays/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    $"{baseDir}/../udap.idp.server.devdays/{certificateStore}/tls/udap-tutorial-dev-tls-cert.cer",
    true);

File.Copy(
    $"{baseDir}/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    $"{baseDir}/../udap.idp.server.devdays/{certificateStore}/tls/udap-tutorial-dev-tls-cert.pfx",
    true);


void MakeAuthorities(string communityStorePath,
    string anchorName,
    string intermediateName)
{
    var communityStoreFullPath = $"{BaseDir()}/{communityStorePath}";
    var crlStorePath = $"{communityStorePath}/crl";
    var crlStoreFullPath = $"{BaseDir()}/{crlStorePath}";
    crlStoreFullPath.EnsureDirectoryExists();
    var anchorCrlFile = $"{crlStorePath}/{anchorName}.crl";
    var anchorCrlFullPath = $"{BaseDir()}/{anchorCrlFile}";
    var intermediateCrlFile = $"{crlStorePath}/{intermediateName}.crl";
    var intermediateCrlFullPath = $"{BaseDir()}/{intermediateCrlFile}";

    var intermediateCdp = new List<string> {
        $"http://host.docker.internal:{staticCertPort}/crl/{anchorName}.crl",
        $"http://localhost:{staticCertPort}/crl/{anchorName}.crl"        
    };
    
    var anchorHostedUrl = new List<Uri> {
        new Uri ($"http://host.docker.internal:{staticCertPort}/certs/{anchorName}.crt"),
        new Uri ($"http://localhost:{staticCertPort}/certs/{anchorName}.crt")        
    };
    
    var intermediateStorePath = $"{communityStorePath}/intermediates";
    var intermediateStoreFullPath = $"{BaseDir()}/{intermediateStorePath}";
    
    using (RSA parentRSAKey = RSA.Create(4096))
    using (RSA intermediateRSAKey = RSA.Create(4096))
    {
        var parentReq = new CertificateRequest(
            $"CN={anchorName}, OU=DevDays, O=Fhir Coding, L=Portland, S=Oregon, C=US",
            parentRSAKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        parentReq.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));

        parentReq.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature,
                false));

        parentReq.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(parentReq.PublicKey, false));

        using (var caCert = parentReq.CreateSelfSigned(
                   DateTimeOffset.UtcNow.AddDays(-1),
                   DateTimeOffset.UtcNow.AddYears(10)))
        {

            var parentBytes = caCert.Export(X509ContentType.Pkcs12, "udap-test");
            communityStoreFullPath.EnsureDirectoryExists();
            File.WriteAllBytes($"{communityStoreFullPath}/{anchorName}.pfx", parentBytes);
            var caPem = PemEncoding.Write("CERTIFICATE", caCert.RawData);
            var caFilePath = $"{communityStoreFullPath}/{anchorName}.crt";
            File.WriteAllBytes(caFilePath, caPem.Select(c => (byte)c).ToArray());

            //Distribute
            var caAiaFile =
                $"{BaseDir()}/../udap.certificates.server.devdays/wwwroot/certs/{new FileInfo(caFilePath).Name}";
            caAiaFile.EnsureDirectoryExistFromFilePath();
            File.Copy(caFilePath, caAiaFile, true);

            var caAuthServerFile =
                $"{BaseDir()}/../udap.authserver.devdays/{communityStorePath}/{new FileInfo(caFilePath).Name}";
            caAuthServerFile.EnsureDirectoryExistFromFilePath();
            File.Copy(caFilePath, caAuthServerFile, true);

            CreateCertificateRevocationList(caCert, anchorCrlFullPath);

            var intermediateReq = new CertificateRequest(
                $"CN={intermediateName}, OU=DevDays, O=Fhir Coding, L=Portland, S=Oregon, C=US",
                intermediateRSAKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Referred to as intermediate Cert or Intermediate
            intermediateReq.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            intermediateReq.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign |
                    X509KeyUsageFlags.DigitalSignature,
                    false));

            intermediateReq.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(intermediateReq.PublicKey, false));

            AddAuthorityKeyIdentifier(caCert, intermediateReq);
            intermediateReq.CertificateExtensions.Add(
                MakeCdp(intermediateCdp));

            var authorityInfoAccessBuilder = new AuthorityInformationAccessBuilder();
            authorityInfoAccessBuilder.AddCertificateAuthorityIssuerUris(anchorHostedUrl);
            var aiaExtension = authorityInfoAccessBuilder.Build();
            intermediateReq.CertificateExtensions.Add(aiaExtension);

            var intermediateCert = intermediateReq.Create(
                caCert,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(5),
                new ReadOnlySpan<byte>(RandomNumberGenerator.GetBytes(16)));
            var intermediateCertWithKey = intermediateCert.CopyWithPrivateKey(intermediateRSAKey);
            var intermediateBytes = intermediateCertWithKey.Export(X509ContentType.Pkcs12, "udap-test");
            intermediateStoreFullPath.EnsureDirectoryExists();
            File.WriteAllBytes($"{intermediateStoreFullPath}/{intermediateName}.pfx", intermediateBytes);
            var intermediatePem = PemEncoding.Write("CERTIFICATE", intermediateCert.RawData);
            var subCaFilePath = $"{intermediateStoreFullPath}/{intermediateName}.crt";
            File.WriteAllBytes(subCaFilePath, intermediatePem.Select(c => (byte)c).ToArray());

            //Distribute
            var subCaCopyToFilePath =
                $"{BaseDir()}/../udap.certificates.server.devdays/wwwroot/certs/{new FileInfo(subCaFilePath).Name}";
            subCaCopyToFilePath.EnsureDirectoryExistFromFilePath();
            File.Copy(subCaFilePath, subCaCopyToFilePath, true);

            CreateCertificateRevocationList(intermediateCertWithKey, intermediateCrlFullPath);
        }
    }
}



X509Certificate2 IssueUdapClientCertificate(
            X509Certificate2 caCert,
            X509Certificate2 intermediateCert,
            RSA intermediateKey,
            string distinguishedName,
            List<string> subjectAltNames,
            string clientCertFilePath,
            List<string> deliveryPaths,  // copy the certificate to a project like Fhir Server or Idp Server
            List<string> intermediateHostedUrl,
            List<string>? crl,
            DateTimeOffset notBefore = default,
            DateTimeOffset notAfter = default)
{
    var clientCertFullFilePath = $"{BaseDir()}/{clientCertFilePath}";

    if (notBefore == default)
    {
        notBefore = DateTimeOffset.UtcNow;
    }

    if (notAfter == default)
    {
        notAfter = DateTimeOffset.UtcNow.AddYears(2);
    }


    var intermediateCertWithKey = intermediateCert.HasPrivateKey ?
        intermediateCert :
        intermediateCert.CopyWithPrivateKey(intermediateKey);

    using RSA rsaKey = RSA.Create(2048);

    var clientCertRequest = new CertificateRequest(
        distinguishedName,
        rsaKey,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    clientCertRequest.CertificateExtensions.Add(
        new X509BasicConstraintsExtension(false, false, 0, true));

    clientCertRequest.CertificateExtensions.Add(
        new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature,
            true));

    clientCertRequest.CertificateExtensions.Add(
        new X509SubjectKeyIdentifierExtension(clientCertRequest.PublicKey, false));

    AddAuthorityKeyIdentifier(intermediateCert, clientCertRequest);

    if (crl != null)
    {
        clientCertRequest.CertificateExtensions.Add(MakeCdp(crl));
    }

    var subAltNameBuilder = new SubjectAlternativeNameBuilder();
    foreach (var subjectAltName in subjectAltNames)
    {
        subAltNameBuilder.AddUri(new Uri(subjectAltName)); //Same as iss claim
    }

    var x509Extension = subAltNameBuilder.Build();
    clientCertRequest.CertificateExtensions.Add(x509Extension);

    var authorityInfoAccessBuilder = new AuthorityInformationAccessBuilder();
    authorityInfoAccessBuilder.AddCertificateAuthorityIssuerUris(intermediateHostedUrl.Select(u => new Uri(u)).ToList());
    var aiaExtension = authorityInfoAccessBuilder.Build();
    clientCertRequest.CertificateExtensions.Add(aiaExtension);
    

    var clientCert = clientCertRequest.Create(
        intermediateCertWithKey,
        notBefore,
        notAfter,
        new ReadOnlySpan<byte>(RandomNumberGenerator.GetBytes(16)));
    // Do something with these certs, like export them to PFX,
    // or add them to an X509Store, or whatever.
    var clientCertWithKey = clientCert.CopyWithPrivateKey(rsaKey);


    var certPackage = new X509Certificate2Collection();
    certPackage.Add(clientCertWithKey);
    certPackage.Add(new X509Certificate2(intermediateCert.Export(X509ContentType.Cert)));
    certPackage.Add(new X509Certificate2(caCert.Export(X509ContentType.Cert)));

    clientCertFullFilePath.EnsureDirectoryExistFromFilePath();
    var clientBytes = certPackage.Export(X509ContentType.Pkcs12, "udap-test");
    File.WriteAllBytes($"{clientCertFullFilePath}.pfx", clientBytes!);
    var clientPem = PemEncoding.Write("CERTIFICATE", clientCert.RawData);
    File.WriteAllBytes($"{clientCertFullFilePath}.crt", clientPem.Select(c => (byte)c).ToArray());


    //Distribute
    foreach (var deliveryPath in deliveryPaths)
    {
        deliveryPath.EnsureDirectoryExistFromFilePath();
        File.Copy($"{clientCertFullFilePath}.pfx", deliveryPath, true);
    }

    return clientCert;
}

X509Certificate2 IssueUdapClientCertificateECDSA(
    X509Certificate2 caCert,
    X509Certificate2 intermediateCert,
    RSA intermediateKey,
    string distinguishedName,
    List<string> subjectAltNames,
    string clientCertFilePath,
    string deliveryPath,  // copy the certificate to a project like Fhir Server or Idp Server
    List<string> intermediateHostedUrl,
    List<string>? crl,
    DateTimeOffset notBefore = default,
    DateTimeOffset notAfter = default)
{
    var clientCertFullFilePath = $"{BaseDir()}/{clientCertFilePath}";

    if (notBefore == default)
    {
        notBefore = DateTimeOffset.UtcNow;
    }

    if (notAfter == default)
    {
        notAfter = DateTimeOffset.UtcNow.AddYears(2);
    }


    var intermediateCertWithKey = intermediateCert.HasPrivateKey ?
        intermediateCert :
        intermediateCert.CopyWithPrivateKey(intermediateKey);

    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);

    var clientCertRequest = new CertificateRequest(
        distinguishedName,
        ecdsa,
        HashAlgorithmName.SHA256);

    clientCertRequest.CertificateExtensions.Add(
        new X509BasicConstraintsExtension(false, false, 0, true));

    clientCertRequest.CertificateExtensions.Add(
        new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature,
            true));

    clientCertRequest.CertificateExtensions.Add(
        new X509SubjectKeyIdentifierExtension(clientCertRequest.PublicKey, false));

    AddAuthorityKeyIdentifier(intermediateCert, clientCertRequest);

    if (crl != null)
    {
        clientCertRequest.CertificateExtensions.Add(MakeCdp(crl));
    }

    var subAltNameBuilder = new SubjectAlternativeNameBuilder();
    foreach (var subjectAltName in subjectAltNames)
    {
        subAltNameBuilder.AddUri(new Uri(subjectAltName)); //Same as iss claim
    }

    var x509Extension = subAltNameBuilder.Build();
    clientCertRequest.CertificateExtensions.Add(x509Extension);

   
        var authorityInfoAccessBuilder = new AuthorityInformationAccessBuilder();
        authorityInfoAccessBuilder.AddCertificateAuthorityIssuerUris(intermediateHostedUrl.Select(u => new Uri(u)).ToList());
        var aiaExtension = authorityInfoAccessBuilder.Build();
        clientCertRequest.CertificateExtensions.Add(aiaExtension);
    

    var clientCert = clientCertRequest.Create(
        intermediateCertWithKey.SubjectName,
        X509SignatureGenerator.CreateForRSA(intermediateKey, RSASignaturePadding.Pkcs1),
        notBefore,
        notAfter,
        new ReadOnlySpan<byte>(RandomNumberGenerator.GetBytes(16)));
    // Do something with these certs, like export them to PFX,
    // or add them to an X509Store, or whatever.
    var clientCertWithKey = clientCert.CopyWithPrivateKey(ecdsa);


    var certPackage = new X509Certificate2Collection();
    certPackage.Add(clientCertWithKey);
    certPackage.Add(new X509Certificate2(intermediateCert.Export(X509ContentType.Cert)));
    certPackage.Add(new X509Certificate2(caCert.Export(X509ContentType.Cert)));

    clientCertFullFilePath.EnsureDirectoryExistFromFilePath();
    var clientBytes = certPackage.Export(X509ContentType.Pkcs12, "udap-test");
    File.WriteAllBytes($"{clientCertFullFilePath}.pfx", clientBytes!);
    var clientPem = PemEncoding.Write("CERTIFICATE", clientCert.RawData);
    File.WriteAllBytes($"{clientCertFullFilePath}.crt", clientPem.Select(c => (byte)c).ToArray());

    //Distribute
    deliveryPath.EnsureDirectoryExistFromFilePath();
    File.Copy($"{clientCertFullFilePath}.pfx", deliveryPath, true);

    return clientCert;
}


void CreateCertificateRevocationList(X509Certificate2 certificate, string crlFilePath){
    // Certificate Revocation
    var bouncyCaCert = DotNetUtilities.FromX509Certificate(certificate);

    var crlGen = new X509V2CrlGenerator();
    var intermediateNow = DateTime.UtcNow;
    crlGen.SetIssuerDN(bouncyCaCert.SubjectDN);
    crlGen.SetThisUpdate(intermediateNow);
    crlGen.SetNextUpdate(intermediateNow.AddYears(1));

    crlGen.AddCrlEntry(BigInteger.One, intermediateNow, CrlReason.PrivilegeWithdrawn);

    crlGen.AddExtension(X509Extensions.AuthorityKeyIdentifier,
        false,
        new AuthorityKeyIdentifierStructure(bouncyCaCert.GetPublicKey()));

    var nextCrlNum = GetNextCrlNumber(crlFilePath);

    crlGen.AddExtension(X509Extensions.CrlNumber, false, nextCrlNum);
    
    var akp = DotNetUtilities.GetKeyPair(certificate.GetRSAPrivateKey()).Private;
    var crl = crlGen.Generate(new Asn1SignatureFactory("SHA256WithRSAEncryption", akp));
    
    File.WriteAllBytes(crlFilePath, crl.GetEncoded());

    //Distribute
    var crlFile = $"{BaseDir()}/../udap.certificates.server.devdays/wwwroot/crl/{new FileInfo(crlFilePath).Name}";
    crlFile.EnsureDirectoryExistFromFilePath();
    File.Copy(crlFilePath, crlFile, true);

}


void RevokeCertificate(X509Certificate2 signingCertificate, X509Certificate2 certificateToRevoke, string crlFilePath)
{
    var bouncyIntermediateCert = DotNetUtilities.FromX509Certificate(signingCertificate);

    var crlGen = new X509V2CrlGenerator();
    var now = DateTime.UtcNow;
    crlGen.SetIssuerDN(bouncyIntermediateCert.SubjectDN);
    crlGen.SetThisUpdate(now);
    crlGen.SetNextUpdate(now.AddMonths(1));
    // crlGen.SetSignatureAlgorithm("SHA256withRSA");

    //
    // revokeCertificate.SerialNumberBytes requires target framework net7.0
    //
    crlGen.AddCrlEntry(new BigInteger(certificateToRevoke.SerialNumberBytes.ToArray()), now,
        CrlReason.PrivilegeWithdrawn);

    crlGen.AddExtension(X509Extensions.AuthorityKeyIdentifier,
        false,
        new AuthorityKeyIdentifierStructure(bouncyIntermediateCert.GetPublicKey()));

    var nextSureFhirClientCrlNum = GetNextCrlNumber(crlFilePath);

    crlGen.AddExtension(X509Extensions.CrlNumber, false, nextSureFhirClientCrlNum);

    var key = signingCertificate.GetRSAPrivateKey();
    using var rsa = RSA.Create(4096);

#if Windows
    //
    // Windows work around.  Otherwise works on Linux
    // Short answer: Windows behaves in such a way when importing the pfx
    // it creates the CNG key so it can only be exported encrypted
    // https://github.com/dotnet/runtime/issues/77590#issuecomment-1325896560
    // https://stackoverflow.com/a/57330499/6115838
    //
        byte[] encryptedPrivKeyBytes = key!.ExportEncryptedPkcs8PrivateKey(
            "ILikePasswords",
            new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                iterationCount: 100_000));

        rsa.ImportEncryptedPkcs8PrivateKey("ILikePasswords".AsSpan(), encryptedPrivKeyBytes.AsSpan(), out int bytesRead);
#else
    rsa.ImportECPrivateKey(key?.ExportECPrivateKey(), out _);
#endif
    
    var akp = DotNetUtilities.GetKeyPair(rsa).Private;
    var crl = crlGen.Generate(new Asn1SignatureFactory("SHA256WithRSAEncryption", akp));

    File.WriteAllBytes(crlFilePath, crl.GetEncoded());

    //Distribute
    var crlFile = $"{BaseDir()}/../udap.certificates.server.devdays/wwwroot/crl/{new FileInfo(crlFilePath).Name}";
    crlFile.EnsureDirectoryExistFromFilePath();
    File.Copy(crlFilePath, crlFile, true);
}



static void AddAuthorityKeyIdentifier(X509Certificate2 caCert, CertificateRequest intermediateReq)
{
    //
    // Found way to generate intermediate below
    //
    // https://github.com/rwatjen/AzureIoTDPSCertificates/blob/711429e1b6dee7857452233a73f15c22c2519a12/src/DPSCertificateTool/CertificateUtil.cs#L69
    // https://blog.rassie.dk/2018/04/creating-an-x-509-certificate-chain-in-c/
    //


    var issuerSubjectKey = caCert.Extensions?["2.5.29.14"]!.RawData;
    var segment = new ArraySegment<byte>(issuerSubjectKey!, 2, issuerSubjectKey!.Length - 2);
    var authorityKeyIdentifier = new byte[segment.Count + 4];
    // these bytes define the "KeyID" part of the AuthorityKeyIdentifier
    authorityKeyIdentifier[0] = 0x30;
    authorityKeyIdentifier[1] = 0x16;
    authorityKeyIdentifier[2] = 0x80;
    authorityKeyIdentifier[3] = 0x14;
    segment.CopyTo(authorityKeyIdentifier, 4);
    intermediateReq.CertificateExtensions.Add(new X509Extension("2.5.29.35", authorityKeyIdentifier, false));
}

static X509Extension MakeCdp(List<string> urls)
{
    if (urls == null || urls.Count == 0)
        throw new ArgumentException("At least one URL must be provided.", nameof(urls));

    // Build DistributionPoint payload for each URL
    var distributionPoints = new Asn1EncodableVector();
    foreach (var url in urls)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URLs cannot be null or empty.", nameof(urls));

        var generalName = new GeneralName(GeneralName.UniformResourceIdentifier, url);
        var generalNames = new GeneralNames(generalName);
        var distributionPointName = new DistributionPointName(DistributionPointName.FullName, generalNames);
        var distributionPoint = new DistributionPoint(distributionPointName, null, null);

        distributionPoints.Add(distributionPoint);
    }

    var sequence = new DerSequence(distributionPoints);
    var extensionValue = sequence.GetDerEncoded();

    return new X509Extension("2.5.29.31", extensionValue, critical: false);
}

static CrlNumber GetNextCrlNumber(string fileName)
{
    CrlNumber nextCrlNum = new CrlNumber(BigInteger.One);

    if (File.Exists(fileName))
    {
        byte[] buf = File.ReadAllBytes(fileName);
        var crlParser = new X509CrlParser();
        var prevCrl = crlParser.ReadCrl(buf);
        var prevCrlNum = prevCrl.GetExtensionValue(X509Extensions.CrlNumber);
        var asn1Object = X509ExtensionUtilities.FromExtensionValue(prevCrlNum);
        var prevCrlNumVal = DerInteger.GetInstance(asn1Object).PositiveValue;
        nextCrlNum = new CrlNumber(prevCrlNumVal.Add(BigInteger.One));
    }

    return nextCrlNum;
}

static string BaseDir()
{
    var assembly = Assembly.GetExecutingAssembly();
    var resourcePath = String.Format(
        $"{Regex.Replace(assembly.ManifestModule.Name, @"\.(exe|dll)$", string.Empty, RegexOptions.IgnoreCase)}" +
        $".Resources.ProjectDirectory.txt");

    var rm = new ResourceManager("Resources", assembly);
    using var stream = assembly.GetManifestResourceStream(resourcePath);
    using var streamReader = new StreamReader(stream!);

    return streamReader.ReadToEnd().Trim();
}

static void GenerateTlsCertificate(string tlsStoreFullPath, string caCertificate)
{
    using var caCert = new X509Certificate2(caCertificate, "udap-test", X509KeyStorageFlags.Exportable);

    using RSA rsaHostDockerInternal = RSA.Create(2048);

    var hostDockerInternal = new CertificateRequest(
        "CN=host.docker.internal, OU=udap tutorial, O=Fhir Coding, L=Portland, S=Oregon, C=US",
        rsaHostDockerInternal,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    hostDockerInternal.CertificateExtensions.Add(
        new X509BasicConstraintsExtension(false, false, 0, true));

    hostDockerInternal.CertificateExtensions.Add(
        new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature,
            true));

    hostDockerInternal.CertificateExtensions.Add(
        new X509SubjectKeyIdentifierExtension(hostDockerInternal.PublicKey, false));

    AddAuthorityKeyIdentifier(caCert, hostDockerInternal);
    // hostDockerInternal.CertificateExtensions.Add(MakeCdp(SureFhirLabsRootCrl)); 

    var subAltNameBuilder = new SubjectAlternativeNameBuilder();
    subAltNameBuilder.AddDnsName("host.docker.internal");
    subAltNameBuilder.AddDnsName("localhost");
    var x509Extension = subAltNameBuilder.Build();
    hostDockerInternal.CertificateExtensions.Add(x509Extension);

    hostDockerInternal.CertificateExtensions.Add(
        new X509EnhancedKeyUsageExtension(
            new OidCollection {
                            new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
                            new Oid("1.3.6.1.5.5.7.3.1"), // TLS Server auth
            },
            true));

    using (var clientCert = hostDockerInternal.Create(
                caCert,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(2),
                new ReadOnlySpan<byte>(RandomNumberGenerator.GetBytes(16))))
    {
        // Do something with these certs, like export them to PFX,
        // or add them to an X509Store, or whatever.
        var sslCert = clientCert.CopyWithPrivateKey(rsaHostDockerInternal);

        tlsStoreFullPath.EnsureDirectoryExists();
        var clientBytes = sslCert.Export(X509ContentType.Pkcs12, "udap-test");
        File.WriteAllBytes($"{tlsStoreFullPath}/udap-tutorial-dev-tls-cert.pfx", clientBytes);
        char[] certificatePem = PemEncoding.Write("CERTIFICATE", clientCert.RawData);
        File.WriteAllBytes($"{tlsStoreFullPath}/udap-tutorial-dev-tls-cert.cer", certificatePem.Select(c => (byte)c).ToArray());
    }
}



