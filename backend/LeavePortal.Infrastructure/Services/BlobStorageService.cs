using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LeavePortal.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace LeavePortal.Infrastructure.Services
{
    // Uploads files to the 'leave-documents' container in Azure Blob Storage.
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["BlobStorage:ConnectionString"]!;
            var containerName = configuration["BlobStorage:ContainerName"]!;

            // BlobContainerClient is thread-safe and meant to be reused (we'll register this as a singleton).
            _container = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            // Prefix with a GUID so two files named "certificate.pdf" never overwrite each other.
            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blob = _container.GetBlobClient(blobName);

            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, cancellationToken);

            // The URL of the uploaded blob — we'll save this on the leave application.
            return blob.Uri.ToString();
        }

        public async Task<(Stream Content, string ContentType)> DownloadAsync(string documentUrl, CancellationToken cancellationToken = default)
        {
            // The blob name is the part of the stored URL after the container — BlobClient parses it for us.
            var blobName = new BlobClient(new Uri(documentUrl)).Name;
            var blob = _container.GetBlobClient(blobName);

            var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return (response.Value.Content, response.Value.Details.ContentType);
        }
    }
}
