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

        /// <summary>
        /// Extracts the object key from a full S3 URL if it matches the configured endpoint and bucket.
        /// </summary>
        /// <param name="url">The full S3 URL. Can be <c>null</c> or empty.</param>
        /// <returns>
        /// - The extracted key if URL belongs to configured S3 bucket  
        /// - <c>null</c> if <paramref name="url"/> is null, empty, does not start with http/https, or is not an S3 URL  
        /// </returns>
        public string? ExtractS3Key(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Validate that the URL has http or https scheme
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            // Construct the expected S3 URL prefix from configured endpoint and bucket
            var endpoint = _configuration.GetSection("S3")["Endpoint"];
            var bucketName = _configuration.GetSection("S3")["Bucket"];
            var prefix = $"{endpoint}/{bucketName}/";

            // If URL starts with the expected prefix, extract and return the S3 object key
            if (url.StartsWith(prefix))
                return url.Substring(prefix.Length);

            // Not an S3 URL
            return null;
        }

        /// <summary>
        /// Validates if the input path or URL is a valid image path or URL by checking common image extensions or valid URI.
        /// </summary>
        /// <param name="url">The image path or URL to validate.</param>
        /// <returns><c>true</c> if valid image path or URL, otherwise <c>false</c>.</returns>
        public bool IsValidImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var lower = url.ToLowerInvariant().Trim();

            // Valid if well-formed absolute URI or ends with common image extensions
            return Uri.IsWellFormedUriString(url, UriKind.Absolute)
                || lower.EndsWith(".jpg")
                || lower.EndsWith(".jpeg")
                || lower.EndsWith(".png")
                || lower.EndsWith(".gif")
                || lower.EndsWith(".bmp")
                || lower.EndsWith(".tiff")
                || lower.EndsWith(".tif")
                || lower.EndsWith(".webp")
                || lower.EndsWith(".svg")
                || lower.EndsWith(".ico")
                || lower.EndsWith(".heic")
                || lower.EndsWith(".heif");
        }

        /// <summary>
        /// Generates a signed preview URL if the input is a valid S3 image URL.
        /// Returns configured default or deleted image URLs for special or invalid cases.
        /// Returns the original URL for external valid image URLs.
        /// Returns empty string if input is null or empty.
        /// </summary>
        /// <param name="imagePath">The image URL or path (nullable).</param>
        /// <returns>
        /// - Signed S3 URL if input points to S3 bucket  
        /// - Default or deleted image URL for invalid or special inputs  
        /// - Original valid external image URL otherwise  
        /// - Empty string if null or empty input  
        /// </returns>
        public async Task<string> DetectAndGetSignedUrlAsync(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return string.Empty;

            // Handle special sentinel value indicating image deleted by admin
            if (imagePath.Equals("DELETED BY ADMIN"))
                return _configuration.GetSection("Image")["DeletedUrl"] ?? "https://s6.imgcdn.dev/YQkM7y.jpg";

            // Validate if imagePath is a valid image URL or path
            if (!IsValidImageUrl(imagePath))
                return _configuration.GetSection("Image")["DefaultUrl"] ?? "https://s6.imgcdn.dev/YQO8MN.webp";

            // Extract S3 object key if applicable
            var key = ExtractS3Key(imagePath);
            if (key != null)
            {
                // Return signed URL for S3 object
                return await GetSignedUrlAsync(key);
            }

            // Return original URL if it's an external valid image URL
            return imagePath;
        }

        /// <summary>
        /// Detects if the input URL is an S3 object. If yes, deletes the corresponding file asynchronously.
        /// Does nothing for null, empty, or non-S3 URLs.
        /// </summary>
        /// <param name="url">The input URL (nullable).</param>
        public async Task DetectAndDeleteFileAsync(string? url)
        {
            var key = ExtractS3Key(url);
            if (key != null)
                await DeleteFileAsync(key);
        }

    }
}