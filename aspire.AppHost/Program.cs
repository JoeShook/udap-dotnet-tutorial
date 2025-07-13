var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.udap_authserver_devdays>("udap-authserver-devdays")
    .WithUrlForEndpoint("http",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("https",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "https://host.docker.internal:5102/",
        DisplayText = "Data Holder Auth Server",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
    });

builder.AddProject<Projects.udap_certificates_server_devdays>("udap-certificates-server-devdays")
    .WithUrlForEndpoint("http",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("http", ep => new()
    {
        Url = "http://host.docker.internal:5034/",
        DisplayText = "Static Certificate Server",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
    });

builder.AddProject<Projects.udap_fhirserver_devdays>("udap-fhirserver-devdays")
    .WithUrlForEndpoint("http",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("https",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "https://host.docker.internal:7017/fhir/r4",
        DisplayText = "FHIR Server",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
    })
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "https://host.docker.internal:7017/fhir/r4/.well-known/udap",
        DisplayText = "Well-Known UDAP",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
    })
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "https://host.docker.internal:7017/fhir/r4/.well-known/udap/communities/ashtml",
        DisplayText = "Communities",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
    });
    

builder.AddProject<Projects.udap_idp_server_devdays>("udap-idp-server-devdays")
    .WithUrlForEndpoint("http",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("https",
        url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "https://host.docker.internal:5202/",
        DisplayText = "Identity Server (Tiered OAuth)",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
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
