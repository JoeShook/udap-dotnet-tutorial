using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.Models;
using Duende.IdentityServer;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using Udap.Common.Extensions;
using Udap.Model;
using Udap.Server.DbContexts;
using Udap.Server.Entities;
using Udap.Server.Models;
using Udap.Server.Storage.Stores;
using Udap.Util.Extensions;

namespace udap.authserver.devdays;

public static class SeedData
{

    public static async Task InitializeDatabase(IApplicationBuilder app, Serilog.ILogger logger)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();
        var configDbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        await configDbContext.Database.MigrateAsync();
        await serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();

        await InitializeDatabaseWithUdap(serviceScope, configDbContext, logger);


        //
        // openid
        //
        if (configDbContext.IdentityResources.All(i => i.Name != IdentityServerConstants.StandardScopes.OpenId))
        {
            var identityResource = new IdentityResources.OpenId();
            configDbContext.IdentityResources.Add(identityResource.ToEntity());

            await configDbContext.SaveChangesAsync();
        }


        //
        // profile
        //
        if (configDbContext.IdentityResources.All(i => i.Name != IdentityServerConstants.StandardScopes.Profile))
        {
            var identityResource = new IdentityResources.Profile();
            configDbContext.IdentityResources.Add(identityResource.ToEntity());

            await configDbContext.SaveChangesAsync();
        }

    }


    static async Task InitializeDatabaseWithUdap(IServiceScope serviceScope, ConfigurationDbContext configDbContext, Serilog.ILogger logger)
    {
        var udapContext = serviceScope.ServiceProvider.GetRequiredService<UdapDbContext>();
        await udapContext.Database.MigrateAsync();
        var clientRegistrationStore = serviceScope.ServiceProvider.GetRequiredService<IUdapClientRegistrationStore>();

        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var communities = new List<Tuple<string, X509Certificate2>>();
        var certificateStorePath = "CertificateStore";
        var certificateStoreFullPath = Path.Combine(assemblyPath!, certificateStorePath);

        foreach (var folder in Directory.GetDirectories(certificateStoreFullPath))
        {
            var folderName = new DirectoryInfo(folder).Name;
            var anchorFile = Directory.GetFiles(folder, "*.crt").First();
            var anchorCertificate = new X509Certificate2(anchorFile);
            communities.Add(new Tuple<string, X509Certificate2>(folderName, anchorCertificate));

            logger.Information($"Creating Anchor from: {anchorFile}");
            logger.Information($"Anchor Info: {anchorCertificate.Thumbprint}");
        }

        //
        // Add Communities
        //
        foreach (var communityName in communities.Select(c => c.Item1))
        {
            if (!udapContext.Communities.Any(c => c.Name == communityName))
            {
                var community = new Community { Name = communityName };
                community.Enabled = true;
                community.Default = false;
                udapContext.Communities.Add(community);
                await udapContext.SaveChangesAsync();
            }
        }

        //
        // Load Anchors
        //
        foreach (var communitySeedData in communities)
        {
            var anchorCertificate = communitySeedData.Item2;
            var communityName = communitySeedData.Item1;
            if ((await clientRegistrationStore.GetAnchors(communityName))
                .All(a => a.Thumbprint != anchorCertificate.Thumbprint))
            {

                var community = udapContext.Communities.Single(c => c.Name == communityName);

                var anchor = new Anchor
                {
                    BeginDate = anchorCertificate.NotBefore.ToUniversalTime(),
                    EndDate = anchorCertificate.NotAfter.ToUniversalTime(),
                    Name = anchorCertificate.Subject,
                    Community = community,
                    X509Certificate = anchorCertificate.ToPemFormat(),
                    Thumbprint = anchorCertificate.Thumbprint,
                    Enabled = true
                };

                udapContext.Anchors.Add(anchor);
                await udapContext.SaveChangesAsync();
            }
        }

        Func<string, bool> treatmentSpecification = r => r is "Patient" or "AllergyIntolerance" or "Condition" or "Encounter";
        var scopeProperties = new Dictionary<string, string>();
        scopeProperties.Add("smart_version", "v1");

        await SeedFhirScopes(configDbContext, Hl7ModelInfoExtensions.BuildHl7FhirV1Scopes("patient", treatmentSpecification), scopeProperties);
        await SeedFhirScopes(configDbContext, Hl7ModelInfoExtensions.BuildHl7FhirV1Scopes("user", treatmentSpecification), scopeProperties);
        await SeedFhirScopes(configDbContext, Hl7ModelInfoExtensions.BuildHl7FhirV1Scopes("system", treatmentSpecification), scopeProperties);

        scopeProperties = new Dictionary<string, string>();
        scopeProperties.Add("smart_version", "v2");
        await SeedFhirScopes(configDbContext, Hl7ModelInfoExtensions.BuildHl7FhirV2Scopes("patient", treatmentSpecification), scopeProperties);
        await SeedFhirScopes(configDbContext, Hl7ModelInfoExtensions.BuildHl7FhirV2Scopes("user", treatmentSpecification), scopeProperties);
        await SeedFhirScopes(configDbContext, Hl7ModelInfoExtensions.BuildHl7FhirV2Scopes("system", treatmentSpecification), scopeProperties);

        //
        // fhirUser
        //
        if (configDbContext.IdentityResources.All(i => i.Name != UdapConstants.StandardScopes.FhirUser))
        {
            var fhirUserIdentity = new UdapIdentityResources.FhirUser();
            configDbContext.IdentityResources.Add(fhirUserIdentity.ToEntity());

            configDbContext.SaveChanges();
        }

        //
        // udap
        //
        if (configDbContext.ApiScopes.All(i => i.Name != UdapConstants.StandardScopes.Udap))
        {
            var udapIdentity = new UdapApiScopes.Udap();
            configDbContext.ApiScopes.Add(udapIdentity.ToEntity());

            await configDbContext.SaveChangesAsync();
        }
    }

    private static async Task SeedFhirScopes(
        ConfigurationDbContext configDbContext,
        HashSet<string>? seedScopes,
        Dictionary<string, string> scopeProperties)
    {
        var apiScopes = configDbContext.ApiScopes
            .Include(s => s.Properties)
            .Select(s => s)
            .ToList();

        foreach (var scopeName in seedScopes.Where(s => s.StartsWith("system")))
        {
            if (!apiScopes.Any(s =>
                    s.Name == scopeName && s.Properties.Exists(p => p.Key == "udap_prefix" && p.Value == "system")))
            {
                var apiScope = new ApiScope(scopeName);
                apiScope.ShowInDiscoveryDocument = false;

                if (apiScope.Name.StartsWith("system/*."))
                {
                    apiScope.ShowInDiscoveryDocument = true;
                    apiScope.Enabled = false;
                }

                apiScope.Properties.Add("udap_prefix", "system");

                foreach (var scopeProperty in scopeProperties)
                {
                    apiScope.Properties.Add(scopeProperty.Key, scopeProperty.Value);
                }

                configDbContext.ApiScopes.Add(apiScope.ToEntity());
            }
        }

        foreach (var scopeName in seedScopes.Where(s => s.StartsWith("user")))
        {
            if (!apiScopes.Any(s =>
                    s.Name == scopeName && s.Properties.Exists(p => p.Key == "udap_prefix" && p.Value == "user")))
            {
                var apiScope = new ApiScope(scopeName);
                apiScope.ShowInDiscoveryDocument = false;

                if (apiScope.Name.StartsWith("patient/*."))
                {
                    apiScope.ShowInDiscoveryDocument = true;
                    apiScope.Enabled = false;
                }

                apiScope.Properties.Add("udap_prefix", "user");

                foreach (var scopeProperty in scopeProperties)
                {
                    apiScope.Properties.Add(scopeProperty.Key, scopeProperty.Value);
                }

                configDbContext.ApiScopes.Add(apiScope.ToEntity());
            }
        }

        foreach (var scopeName in seedScopes.Where(s => s.StartsWith("patient")).ToList())
        {
            if (!apiScopes.Any(s => s.Name == scopeName && s.Properties.Exists(p => p.Key == "udap_prefix" && p.Value == "patient")))
            {
                var apiScope = new ApiScope(scopeName);
                apiScope.ShowInDiscoveryDocument = false;

                if (apiScope.Name.StartsWith("patient/*."))
                {
                    apiScope.ShowInDiscoveryDocument = true;
                    apiScope.Enabled = false;
                }

                apiScope.Properties.Add("udap_prefix", "patient");

                foreach (var scopeProperty in scopeProperties)
                {
                    apiScope.Properties.Add(scopeProperty.Key, scopeProperty.Value);
                }

                configDbContext.ApiScopes.Add(apiScope.ToEntity());
            }
        }

        await configDbContext.SaveChangesAsync();

    }
}
