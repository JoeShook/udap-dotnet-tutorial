using Duende.IdentityServer.EntityFramework.Stores;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Udap.Common;
using udap.idp.server.devdays;
using Udap.Server.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .ReadFrom.Configuration(ctx.Configuration));

var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
const string connectionString = @"Data Source=udap.idp.server.devdays.EntityFramework.db";

builder.Services.AddRazorPages();

builder.Services.AddUdapServerAsIdentityProvider(
        options =>
        {
            var udapServerOptions = builder.Configuration.GetOption<ServerSettings>("ServerSettings");
            options.DefaultSystemScopes = udapServerOptions.DefaultSystemScopes;
            options.DefaultUserScopes = udapServerOptions.DefaultUserScopes;
            options.ForceStateParamOnAuthorizationCode = udapServerOptions.ForceStateParamOnAuthorizationCode;
            options.LogoRequired = udapServerOptions.LogoRequired;
            options.AlwaysIncludeUserClaimsInIdToken = udapServerOptions.AlwaysIncludeUserClaimsInIdToken;
        },
        storeOptionAction: options =>
            options.UdapDbContext = b =>
                b.UseSqlite(connectionString,
                    dbOpts =>
                     dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName))
            )
    .AddPrivateFileStore();

    builder.Services.Configure<UdapFileCertStoreManifest>(builder.Configuration.GetSection(Constants.UDAP_FILE_STORE_MANIFEST));

    builder.Services.AddUdapMetadataServer(builder.Configuration);

    builder.Services.AddIdentityServer(options =>
    {
        // https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/api_scopes#authorization-based-on-scopes
        options.EmitStaticAudienceClaim = true;
    })
    .AddServerSideSessions()
    .AddConfigurationStore(options =>
        options.ConfigureDbContext = b => b.UseSqlite(connectionString,
            dbOpts => dbOpts.MigrationsAssembly(migrationsAssembly)))
    .AddOperationalStore(options =>
        options.ConfigureDbContext = b => b.UseSqlite(connectionString,
            dbOpts => dbOpts.MigrationsAssembly(migrationsAssembly)))

    .AddResourceStore<ResourceStore>()
    .AddClientStore<ClientStore>()
    .AddTestUsers(TestUsers.Users);


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHsts();

app.UseSerilogRequestLogging();


await SeedData.InitializeDatabase(app, "../../../../udap.pki.devdays/CertificateStore", Log.Logger);

// uncomment if you want to add a UI
app.UseStaticFiles();
app.UseRouting();

app.UseUdapMetadataServer();
app.UseUdapIdPServer();
app.UseIdentityServer();


// uncomment if you want to add a UI
app.UseAuthorization();
app.MapRazorPages().RequireAuthorization();

app.Run();