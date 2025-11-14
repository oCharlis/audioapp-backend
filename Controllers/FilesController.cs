using System.Security.Claims;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyAudioApi.Controllers;

[ApiController]
[Route("api/files")]        // 👈 FIXED: lowercase & explicit routing
[Authorize]
public class FilesController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "user-audio";

    public FilesController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    private string GetUserObjectId()
    {
        var oid = User.FindFirstValue("oid");
        if (string.IsNullOrEmpty(oid))
        {
            throw new Exception("User OID claim not found");
        }
        return oid;
    }

    // ------------------------------------------------------------
    // UPLOAD FILE
    // ------------------------------------------------------------
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        var oid = User.FindFirstValue("oid");
        if (oid == null)
            return Unauthorized("OID claim missing");

        var container = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync();

        var fileName = $"{Guid.NewGuid()}-{file.FileName}";
        var blob = container.GetBlobClient($"users/{oid}/{fileName}");

        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream);

        return Ok(new { message = "Uploaded successfully", file = fileName });
    }

    // ------------------------------------------------------------
    // LIST FILES
    // ------------------------------------------------------------
    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var userOid = GetUserObjectId();
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

        var prefix = $"users/{userOid}/";
        var files = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            var shortName = blobItem.Name.Substring(prefix.Length);
            files.Add(shortName);
        }

        return Ok(files);
    }
}
