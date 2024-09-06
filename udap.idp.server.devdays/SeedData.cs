/*
 Copyright (c) Joseph Shook. All rights reserved.
 Authors:
    Joseph Shook   Joseph.Shook@Surescripts.com

 See LICENSE in the project root for license information.
*/


using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.EntityFramework.Storage;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Udap.Model;
using Udap.Server.DbContexts;
using Udap.Server.Entities;
using Udap.Server.Models;
using Udap.Server.Storage.Stores;
using Udap.Server.Stores;
using Udap.Util.Extensions;
using ILogger = Serilog.ILogger;

namespace udap.idp.server.devdays;

public static class SeedData
{
    private static Anchor anchor;



    public static async Task InitializeDatabase(IApplicationBuilder app, string certStoreBasePath, Serilog.ILogger logger)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();
        var configDbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        await configDbContext.Database.MigrateAsync();
        await serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();

        await InitializeDatabaseWithUdap(serviceScope, configDbContext, certStoreBasePath, logger);


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


    /// <summary>
    /// Load some test dat
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="certStoreBasePath">Test certs base path</param>
    /// <param name="logger"></param>
    /// <param name="identityProvider">Load different scopes</param>
    public static async Task<int> InitializeDatabaseWithUdap(IServiceScope serviceScope, ConfigurationDbContext configDbContext, string certStoreBasePath, Serilog.ILogger logger)
    {
        var udapContext = serviceScope.ServiceProvider.GetRequiredService<UdapDbContext>();
        await udapContext.Database.MigrateAsync();
        var clientRegistrationStore = serviceScope.ServiceProvider.GetRequiredService<IUdapClientRegistrationStore>();

        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var communities = new List<Tuple<string, X509Certificate2>>();
        var certificateStorePath = "CertificateStore";
        var certificateStoreFullPath = Path.Combine(assemblyPath!, certificateStorePath);


        if (!udapContext.Communities.Any(c => c.Name == "udap://Community1"))
        {
            var community = new Community { Name = "udap://Community1" };
            community.Enabled = true;
            community.Default = false;
            udapContext.Communities.Add(community);
            await udapContext.SaveChangesAsync();
        }

        //
        // Anchor localhost_community for Udap.Identity.Provider1
        //
        var anchorLocalhostCert = new X509Certificate2(
            Path.Combine(assemblyPath!, certStoreBasePath, "Community1/DevDaysCA_1.crt"));

        if ((await clientRegistrationStore.GetAnchors("udap://Community1"))
            .All(a => a.Thumbprint != anchorLocalhostCert.Thumbprint))
        {
            var community = udapContext.Communities.Single(c => c.Name == "udap://Community1");
            var anchor = new Anchor
            {
                BeginDate = anchorLocalhostCert.NotBefore.ToUniversalTime(),
                EndDate = anchorLocalhostCert.NotAfter.ToUniversalTime(),
                Name = anchorLocalhostCert.Subject,
                Community = community,
                X509Certificate = anchorLocalhostCert.ToPemFormat(),
                Thumbprint = anchorLocalhostCert.Thumbprint,
                Enabled = true
            };
            udapContext.Anchors.Add(anchor);

            await udapContext.SaveChangesAsync();

            //
            // Intermediate surefhirlabs_community
            //
            var x509Certificate2Collection = await clientRegistrationStore.GetIntermediateCertificates();

            var intermediateCert = new X509Certificate2(
                Path.Combine(assemblyPath!, certStoreBasePath, "Community1/intermediates/DevDaysSubCA_1.crt"));

            if (x509Certificate2Collection != null && x509Certificate2Collection.ToList()
                    .All(r => r.Thumbprint != intermediateCert.Thumbprint))
            {

                udapContext.IntermediateCertificates.Add(new Intermediate
                {
                    BeginDate = intermediateCert.NotBefore.ToUniversalTime(),
                    EndDate = intermediateCert.NotAfter.ToUniversalTime(),
                    Name = intermediateCert.Subject,
                    X509Certificate = intermediateCert.ToPemFormat(),
                    Thumbprint = intermediateCert.Thumbprint,
                    Enabled = true,
                    Anchor = anchor
                });

                await udapContext.SaveChangesAsync();
            }
        }

        
        /*
         *  "openid",
            "fhirUser",
            "email", ????
            "profile"
         */

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
        // fhirUser
        //
        if (configDbContext.IdentityResources.All(i => i.Name != UdapConstants.StandardScopes.FhirUser))
        {
            var fhirUserIdentity = new UdapIdentityResources.FhirUser();
            configDbContext.IdentityResources.Add(fhirUserIdentity.ToEntity());

            await configDbContext.SaveChangesAsync();
        }

        //
        // udap
        //
        if (configDbContext.IdentityResources.All(i => i.Name != UdapConstants.StandardScopes.Udap))
        {
            var udapIdentity = new UdapApiScopes.Udap();
            configDbContext.ApiScopes.Add(udapIdentity.ToEntity());

            await configDbContext.SaveChangesAsync();
        }

        //
        // profile
        //
        if (configDbContext.IdentityResources.All(i => i.Name != IdentityServerConstants.StandardScopes.Profile))
        {
            var identityResource = new UdapIdentityResources.Profile();
            configDbContext.IdentityResources.Add(identityResource.ToEntity());

            await configDbContext.SaveChangesAsync();
        }

        //
        // email
        //
        if (configDbContext.IdentityResources.All(i => i.Name != IdentityServerConstants.StandardScopes.Email))
        {
            var identityResource = new IdentityResources.Email();
            configDbContext.IdentityResources.Add(identityResource.ToEntity());

            await configDbContext.SaveChangesAsync();
        }

        return 0;
    }
}
