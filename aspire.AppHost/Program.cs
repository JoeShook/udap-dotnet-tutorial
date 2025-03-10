var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.udap_authserver_devdays>("udap-authserver-devdays");

builder.AddProject<Projects.udap_certificates_server_devdays>("udap-certificates-server-devdays");

builder.AddProject<Projects.udap_fhirserver_devdays>("udap-fhirserver-devdays");

builder.AddProject<Projects.udap_idp_server_devdays>("udap-idp-server-devdays");

var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var emrDirectClientPasscode = builder.Configuration["sampleKeyC"]; // Projects UserSecretsId to point to secrets.json

builder.AddContainer("UdapEd-Tutorial", "ghcr.io/joeshook/udaped", "v0.4.4.30")
    .WithContainerName("UdapEd-Tutorial")
    .WithHttpsEndpoint(port: 7041, targetPort: 8181)
    .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "8181")
    .WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Password", "password")
    .WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Path", "/home/app/.aspnet/https/aspnetDevCert.pfx")   
    .WithEnvironment("sampleKeyC", emrDirectClientPasscode)
    .WithBindMount($"{userProfile}/.aspnet/https", "/home/app/.aspnet/https", true)
    .WithBindMount("../CertificateStore", "/app/CertificateStore", true);
    

builder.Build().Run();
