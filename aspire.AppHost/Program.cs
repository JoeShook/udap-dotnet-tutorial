var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.udap_authserver_devdays>("udap-authserver-devdays");

builder.AddProject<Projects.udap_certificates_server_devdays>("udap-certificates-server-devdays");

builder.AddProject<Projects.udap_fhirserver_devdays>("udap-fhirserver-devdays");

builder.AddProject<Projects.udap_idp_server_devdays>("udap-idp-server-devdays");

builder.Build().Run();
