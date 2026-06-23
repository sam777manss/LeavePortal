using System;
using System.Collections.Generic;
using System.Text;

namespace LeavePortal.Core.Interfaces
{
    // Uploads a file to blob storage and returns the stored file's URL.
    // Takes a Stream (not IFormFile) on purpose — so Core stays free of ASP.NET types.
    // The controller will open the uploaded file's stream and hand it in.
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
        Task<(Stream Content, string ContentType)> DownloadAsync(string documentUrl, CancellationToken cancellationToken = default);
    }
}
