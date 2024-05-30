using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Services
{
    public interface IFirebaseService
    {
        Task<string?> UploadFileToFirebaseStorage(IFormFile files, string fileName, string folderName);
    }
}
