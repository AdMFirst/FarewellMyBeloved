using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FarewellMyBeloved.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task<string> GetSignedUrlAsync(string key);
    }
}