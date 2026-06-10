var builder = DistributedApplication.CreateBuilder(args);

// Each app binds the port from its own launchSettings.json directly
// (IsProxied = false). UDAP signed metadata embeds these exact host:port URLs
// and each app presents its own host.docker.internal TLS cert, so Aspire's
// default proxy (random app port + Aspire dev cert) cannot be used here.
//
// Binding a bind-all address makes Aspire's auto-generated dashboard URL render
// the machine hostname, so each resource clears its URL list (WithUrls) and
// sets only the curated host.docker.internal links.

builder.AddProject<Projects.udap_authserver_devdays>("udap-authserver-devdays")
    .WithEndpoint("https", e => e.IsProxied = false)
    .WithUrls(context =>
    {
        context.Urls.Clear();
        context.Urls.Add(new()
        {
            Url = "https://host.docker.internal:5102/",
            DisplayText = "Data Holder Auth Server"
        });
    });

builder.AddProject<Projects.udap_certificates_server_devdays>("udap-certificates-server-devdays")
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithUrls(context =>
    {
        context.Urls.Clear();
        context.Urls.Add(new()
        {
            Url = "http://host.docker.internal:5034/",
            DisplayText = "Static Certificate Server"
        });
    });

builder.AddProject<Projects.udap_fhirserver_devdays>("udap-fhirserver-devdays")
    .WithEndpoint("https", e => e.IsProxied = false)
    .WithUrls(context =>
    {
        context.Urls.Clear();
        context.Urls.Add(new()
        {
            Url = "https://host.docker.internal:7017/fhir/r4",
            DisplayText = "FHIR Server"
        });
        context.Urls.Add(new()
        {
            Url = "https://host.docker.internal:7017/fhir/r4/.well-known/udap",
            DisplayText = "Well-Known UDAP"
        });
        context.Urls.Add(new()
        {
            Url = "https://host.docker.internal:7017/fhir/r4/.well-known/udap/communities/ashtml",
            DisplayText = "Communities"
        });
    });


builder.AddProject<Projects.udap_idp_server_devdays>("udap-idp-server-devdays")
    .WithEndpoint("https", e => e.IsProxied = false)
    .WithUrls(context =>
    {
        context.Urls.Clear();
        context.Urls.Add(new()
        {
            Url = "https://host.docker.internal:5202/",
            DisplayText = "Identity Server (Tiered OAuth)"
        });
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
