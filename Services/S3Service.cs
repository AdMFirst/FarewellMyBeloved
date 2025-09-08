using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FarewellMyBeloved.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(IFormFile file, S3UploadType type)
        {
            // Validate input
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded or file is empty.");
            }

            // Retrieve configuration
            var bucketName = _configuration.GetSection("S3")["Bucket"];
            var endpoint = _configuration.GetSection("S3")["Endpoint"];
            
            if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("S3 bucket or endpoint configuration is missing.");
            }

            // Generate unique key for the file
            var typePrefix = type.ToString().ToLower(); // "portrait" or "background"
            var key = $"{typePrefix}/{Guid.NewGuid()}-{Path.GetFileName(file.FileName)}";

            // Buffer file into MemoryStream for validation
            using (var stream = new MemoryStream())
            {
                // Copy file to MemoryStream
                await file.CopyToAsync(stream);

                // Validate stream length
                if (stream.Length != file.Length)
                {
                    throw new InvalidOperationException("File was not copied correctly to stream.");
                }

                // Reset stream position
                stream.Position = 0;

                // Prepare S3 upload request
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    CannedACL = S3CannedACL.PublicRead,
                    DisablePayloadSigning = true, // Disable chunked encoding signatures
                    DisableDefaultChecksumValidation = true // Explicitly disable checksums
                };

                // Explicitly disable checksum algorithm
                request.ChecksumAlgorithm = null;

                // Upload to S3
                await _s3Client.PutObjectAsync(request);

                // Construct and return the file URL
                var url = $"{endpoint.TrimEnd('/')}/{bucketName}/{key}";
                return url;
            }
        }

        public async Task<string> GetSignedUrlAsync(string key)
        {
            var bucketName = _configuration.GetSection("S3")["Bucket"];
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(1) // 60 minutes expiration
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }

        public async Task DeleteFileAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var bucketName = _configuration.GetSection("S3")["Bucket"];
            
            if (string.IsNullOrEmpty(bucketName))
                throw new InvalidOperationException("S3 bucket configuration is missing.");

            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                // Log the error but don't throw - we don't want to fail the entire operation
                // if S3 deletion fails for some reason
                Console.WriteLine($"Failed to delete S3 object {key}: {ex.Message}");
            }
        }
    }
}