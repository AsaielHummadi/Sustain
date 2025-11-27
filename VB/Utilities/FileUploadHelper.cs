using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Sustain.Utilities.Helpers
{
    public static class FileUploadHelper
    {
        public static async Task<string> UploadFileAsync(IFormFile file, string path = "uploads", string slug = "dummy-slug")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            // Slugify the name
            slug = Slugify(slug);

            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{slug}-{currentDate}-{Guid.NewGuid()}{extension}";

            // Create directory if it doesn't exist
            string fullPath = Path.Combine("wwwroot", path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Save file
            string filePath = Path.Combine(fullPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{path}/{fileName}";
        }
       
        public static async Task<string> UploadImageAsBase64Async(string imageBase64)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
                throw new ArgumentException("Base64 string is empty");

            // Extract mime type and extension
            var match = Regex.Match(imageBase64, @"data:image/(?<type>.+?);base64,(?<data>.+)");
            if (!match.Success)
                throw new ArgumentException("Invalid base64 image format");

            string extension = match.Groups["type"].Value;
            string base64Data = match.Groups["data"].Value;

            // Generate unique filename
            string fileName = $"{DateTime.Now.Ticks}{Random.Shared.Next(1, 9999)}.{extension}";
            string path = Path.Combine("wwwroot", "uploads");

            // Create directory if it doesn't exist
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Decode and save
            byte[] imageBytes = Convert.FromBase64String(base64Data);
            string filePath = Path.Combine(path, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/uploads/{fileName}";
        }        
        public static bool RemoveOldImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Remove leading slash if present
            path = path.TrimStart('/');
            string fullPath = Path.Combine("wwwroot", path);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }     
        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "slug";

            text = text.ToLowerInvariant();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            text = Regex.Replace(text, @"-+", "-");
            text = text.Trim('-');

            return string.IsNullOrWhiteSpace(text) ? "slug" : text;
        }
      
        public static bool IsValidImageExtension(string fileName, string[] allowedExtensions = null)
        {
            allowedExtensions ??= new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

            string extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && allowedExtensions.Contains(extension);
        }
       
        public static string GetReadableFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}