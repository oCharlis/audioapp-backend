using System.Security.Claims;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Auth: Entra ID (AAD)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// BlobServiceClient using managed identity (in Azure) or dev credentials locally
var storageUrl = new Uri(builder.Configuration["BlobStorage:Url"]!);
builder.Services.AddSingleton(sp =>
{
    var credential = new DefaultAzureCredential();
    return new BlobServiceClient(storageUrl, credential);
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
