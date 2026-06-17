using Microsoft.Extensions.FileProviders;
using udap.certificates.server.devdays;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddDirectoryBrowser();

var app = builder.Build();

app.MapDefaultEndpoints();

// Re-sign any expired/expiring CRLs before we start serving them so that the
// BouncyCastle chain validator in Udap.Client never sees a stale revocation list.
CrlMaintenance.RefreshExpiringCrls(
    Path.Combine(app.Environment.WebRootPath, "crl"),
    Path.Combine(AppContext.BaseDirectory, "SigningKeys"),
    TimeSpan.FromDays(7),
    app.Logger);

// Configure the HTTP request pipeline.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Ensure that HTTPS redirection is not enforced for static files
        ctx.Context.Response.Headers.Remove("Strict-Transport-Security");
    }
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath))
});

app.Run();
