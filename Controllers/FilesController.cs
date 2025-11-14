using System.Security.Claims;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyAudioApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)] // 50 MB, adjust as needed
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        var userOid = GetUserObjectId();
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"users/{userOid}/{Guid.NewGuid()}-{file.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: false);

        return Ok(new { name = file.FileName });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var userOid = GetUserObjectId();
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

        var prefix = $"users/{userOid}/";
        var files = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            // Strip the "users/{oid}/" prefix from names
            var shortName = blobItem.Name.Substring(prefix.Length);
            files.Add(shortName);
        }

        return Ok(files);
    }
}
