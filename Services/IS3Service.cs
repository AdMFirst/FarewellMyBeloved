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
        Task<string> UploadFileAsync(IFormFile file, S3UploadType type);
        Task<string> GetSignedUrlAsync(string key);
    }
}