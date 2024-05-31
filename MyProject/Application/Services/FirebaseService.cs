using Application.Commons;
using Application.Interfaces.Services;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class FirebaseService : IFirebaseService
    {
        private readonly IConfiguration _configuration;

        public FirebaseService(FirebaseConfiguration config, IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<string> SignInAndGetAuthToken()
        {
            //Firebase config
            var config = new FirebaseAuthConfig
            {
                ApiKey = _configuration["FirebaseConfiguration:ApiKey"],
                AuthDomain = _configuration["FirebaseConfiguration:AuthDomain"],
                Providers = new FirebaseAuthProvider[]{
                        new EmailProvider(),
                    }
            };
            var client = new FirebaseAuthClient(config);
            var authResult = await client.SignInWithEmailAndPasswordAsync(_configuration["FirebaseConfiguration:AuthEmail"], _configuration["FirebaseConfiguration:AuthPassword"]);
            return await authResult.User.GetIdTokenAsync();
        }

        public async Task<string?> UploadFileToFirebaseStorage(IFormFile files, string fileName, string folderName)
        {
            if (files.Length > 0)
            {
                var task = new FirebaseStorage(
                    _configuration["FirebaseConfiguration:Bucket"],
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = SignInAndGetAuthToken,
                        ThrowOnCancel = true
                    })
                    .Child(folderName)
                    .Child($"{fileName}.{Path.GetExtension(files.FileName).Substring(1)}")
                    .PutAsync(files.OpenReadStream(), new CancellationTokenSource().Token);
                try
                {
                    string? urlFile = await task;
                    return urlFile;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            return null;

        }

        public async Task DeleteFileInFirebaseStorage(string fileName, string folderName)
        {
            var task = new FirebaseStorage(
               _configuration["FirebaseConfiguration:Bucket"],
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = SignInAndGetAuthToken,
                    ThrowOnCancel = true
                })
                .Child(folderName)
                .Child(fileName)
                .DeleteAsync();
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}