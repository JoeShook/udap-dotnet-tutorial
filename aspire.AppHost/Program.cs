var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.udap_authserver_devdays>("udap-authserver-devdays");

builder.AddProject<Projects.udap_certificates_server_devdays>("udap-certificates-server-devdays");

builder.AddProject<Projects.udap_fhirserver_devdays>("udap-fhirserver-devdays");

builder.AddProject<Projects.udap_idp_server_devdays>("udap-idp-server-devdays");

var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var emrDirectClientPasscode = builder.Configuration["sampleKeyC"]; // Projects UserSecretsId to point to secrets.json

builder
    .AddDockerfile("UdapEd-Tutorial", "..",
        "Dockerfile.UdapEd") // used to be the image, ghcr.io/joeshook/udaped:v0.4.4.30
    // but need to add a trusted CA.
    .WithContainerName("UdapEd-Tutorial")
    .WithImageTag("latest")
    .WithHttpsEndpoint(port: 7041, targetPort: 8181)
    .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "8181")
    .WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Password", "udap-test")
    .WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Path",
        "/home/app/.aspnet/https/udap-tutorial-dev-tls-cert.pfx")
    .WithEnvironment("sampleKeyC", emrDirectClientPasscode)
    .WithBindMount("../CertificateStore", "/home/app/.aspnet/https", true)
    .WithBindMount("../Packages", "/app/wwwroot/_content/UdapEd.Shared/Packages", true);



builder.Build().Run();
