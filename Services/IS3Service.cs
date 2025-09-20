using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FarewellMyBeloved.Services
{
    public enum S3UploadType
    {
        Portrait,
        Background
    }

    public interface IS3Service
    {
        // Actual Function
        Task<string> UploadFileAsync(IFormFile file, S3UploadType type);
        Task<string> GetSignedUrlAsync(string key);
        Task DeleteFileAsync(string key);

        // Helpers
        Task DetectAndDeleteFileAsync(string? url);
        Task<string> DetectAndGetSignedUrlAsync(string? imagePath);

    }
}