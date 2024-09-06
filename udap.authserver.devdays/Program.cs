using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Stores;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Udap.Server.Configuration;
using udap.authserver.devdays;
using Udap.Server.Security.Authentication.TieredOAuth;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Udap.Common;
using Udap.Client.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .ReadFrom.Configuration(ctx.Configuration));

var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
const string connectionString = @"Data Source=udap.authserver.devdays.EntityFramework.db";

//
// Add services to the container.
//

builder.Services.AddRazorPages();

builder.Services.Configure<UdapClientOptions>(builder.Configuration.GetSection("UdapClientOptions"));
builder.Services.Configure<UdapFileCertStoreManifest>(builder.Configuration.GetSection(Constants.UDAP_FILE_STORE_MANIFEST));

builder.Services.AddUdapServer(
    options =>
    {
        var udapServerOptions = builder.Configuration.GetOption<ServerSettings>("ServerSettings");
        options.DefaultSystemScopes = udapServerOptions.DefaultSystemScopes;
        options.DefaultUserScopes = udapServerOptions.DefaultUserScopes;
        options.ForceStateParamOnAuthorizationCode = udapServerOptions.ForceStateParamOnAuthorizationCode;
    },
    storeOptionAction: options => 
        options.UdapDbContext = b =>
            b.UseSqlite(connectionString,
                dbOpts =>
                    dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName)),
    baseUrl: "https://localhost:5102"
    )
    .AddUdapResponseGenerators()
    .AddSmartV2Expander();


builder.Services.AddIdentityServer(options =>
    {
        options.UserInteraction.LoginUrl = "/udapaccount/login";
        options.UserInteraction.LogoutUrl = "/udapaccount/logout";
    })
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = b => b.UseSqlite(connectionString,
            dbOpts => dbOpts.MigrationsAssembly(migrationsAssembly));
    })
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b => b.UseSqlite(connectionString,
            dbOpts => dbOpts.MigrationsAssembly(migrationsAssembly));

    })
    .AddResourceStore<ResourceStore>()
    .AddClientStore<ClientStore>()
    .AddTestUsers(TestUsers.Users);


builder.Services.AddAuthentication()
    .AddTieredOAuth(options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
    });

var app = builder.Build();

//
// Configure the HTTP request pipeline.
//

app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

await SeedData.InitializeDatabase(app, Log.Logger);

app.UseStaticFiles();
app.UseRouting();

app.UseUdapServer();
app.UseIdentityServer();

app.UseAuthorization();
app.MapRazorPages().RequireAuthorization();

app.Run();
