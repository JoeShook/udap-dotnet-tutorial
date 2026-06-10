var builder = DistributedApplication.CreateBuilder(args);

// Each app binds the port from its own launchSettings.json directly
// (IsProxied = false). UDAP signed metadata embeds these exact host:port URLs
// and each app presents its own host.docker.internal TLS cert, so Aspire's
// default proxy (random app port + Aspire dev cert) cannot be used here.

// Each app binds host.docker.internal directly (IsProxied = false). The auto
// endpoint URL would otherwise display the machine hostname (a bind-all
// address), so rewrite it in place to host.docker.internal. Extra contextual
// links are added with WithUrl.

builder.AddProject<Projects.udap_authserver_devdays>("udap-authserver-devdays")
    .WithEndpoint("https", e => e.IsProxied = false)
    .WithUrlForEndpoint("https", url =>
    {
        url.Url = "https://host.docker.internal:5102/";
        url.DisplayText = "Data Holder Auth Server";
    });

builder.AddProject<Projects.udap_certificates_server_devdays>("udap-certificates-server-devdays")
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithUrlForEndpoint("http", url =>
    {
        url.Url = "http://host.docker.internal:5034/";
        url.DisplayText = "Static Certificate Server";
    });

builder.AddProject<Projects.udap_fhirserver_devdays>("udap-fhirserver-devdays")
    .WithEndpoint("https", e => e.IsProxied = false)
    .WithUrlForEndpoint("https", url =>
    {
        url.Url = "https://host.docker.internal:7017/fhir/r4";
        url.DisplayText = "FHIR Server";
    })
    .WithUrl("https://host.docker.internal:7017/fhir/r4/.well-known/udap", "Well-Known UDAP")
    .WithUrl("https://host.docker.internal:7017/fhir/r4/.well-known/udap/communities/ashtml", "Communities");


builder.AddProject<Projects.udap_idp_server_devdays>("udap-idp-server-devdays")
    .WithEndpoint("https", e => e.IsProxied = false)
    .WithUrlForEndpoint("https", url =>
    {
        url.Url = "https://host.docker.internal:5202/";
        url.DisplayText = "Identity Server (Tiered OAuth)";
    });

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
    .WithBindMount("../Packages", "/app/wwwroot/Packages", true);




builder.Build().Run();
