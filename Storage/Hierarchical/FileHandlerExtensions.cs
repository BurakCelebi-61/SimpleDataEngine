using SimpleDataEngine.Storage.Hierarchical.Models;
using System.Text.Json;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// IFileHandler extension methods - ReadAsync, WriteAsync gibi eksik methodlar için
    /// </summary>
    public static class FileHandlerExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Generic object'i dosyadan oku
        /// </summary>
        public static async Task<T> ReadAsync<T>(this IFileHandler fileHandler, string path) where T : class
        {
            var content = await fileHandler.ReadTextAsync(path);
            if (string.IsNullOrEmpty(content))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Generic object'i dosyaya yaz
        /// </summary>
        public static async Task WriteAsync<T>(this IFileHandler fileHandler, string path, T data) where T : class
        {
            if (data == null)
            {
                await fileHandler.WriteTextAsync(path, string.Empty);
                return;
            }

            var json = JsonSerializer.Serialize(data, DefaultJsonOptions);
            await fileHandler.WriteTextAsync(path, json);
        }

        /// <summary>
        /// IndexEntry listesini dosyadan oku
        /// </summary>
        public static async Task<List<IndexEntry>> ReadIndexEntriesAsync(this IFileHandler fileHandler, string path)
        {
            return await fileHandler.ReadAsync<List<IndexEntry>>(path) ?? new List<IndexEntry>();
        }

        /// <summary>
        /// IndexEntry listesini dosyaya yaz
        /// </summary>
        public static async Task WriteIndexEntriesAsync(this IFileHandler fileHandler, string path, List<IndexEntry> entries)
        {
            await fileHandler.WriteAsync(path, entries ?? new List<IndexEntry>());
        }

        /// <summary>
        /// SegmentData'yı dosyadan oku
        /// </summary>
        public static async Task<SegmentData<T>> ReadSegmentDataAsync<T>(this IFileHandler fileHandler, string path) where T : class
        {
            return await fileHandler.ReadAsync<SegmentData<T>>(path) ?? new SegmentData<T> { Records = new List<T>() };
        }

        /// <summary>
        /// SegmentData'yı dosyaya yaz
        /// </summary>
        public static async Task WriteSegmentDataAsync<T>(this IFileHandler fileHandler, string path, SegmentData<T> segmentData) where T : class
        {
            await fileHandler.WriteAsync(path, segmentData);
        }

        /// <summary>
        /// EntityMetadata'yı dosyadan oku
        /// </summary>
        public static async Task<EntityMetadata> ReadEntityMetadataAsync(this IFileHandler fileHandler, string path)
        {
            return await fileHandler.ReadAsync<EntityMetadata>(path);
        }

        /// <summary>
        /// EntityMetadata'yı dosyaya yaz
        /// </summary>
        public static async Task WriteEntityMetadataAsync(this IFileHandler fileHandler, string path, EntityMetadata metadata)
        {
            await fileHandler.WriteAsync(path, metadata);
        }

        /// <summary>
        /// EntityIndex'i dosyadan oku
        /// </summary>
        public static async Task<EntityIndex> ReadEntityIndexAsync(this IFileHandler fileHandler, string path)
        {
            return await fileHandler.ReadAsync<EntityIndex>(path);
        }

        /// <summary>
        /// EntityIndex'i dosyaya yaz
        /// </summary>
        public static async Task WriteEntityIndexAsync(this IFileHandler fileHandler, string path, EntityIndex index)
        {
            await fileHandler.WriteAsync(path, index);
        }

        /// <summary>
        /// PropertyIndex'i dosyadan oku
        /// </summary>
        public static async Task<PropertyIndex> ReadPropertyIndexAsync(this IFileHandler fileHandler, string path)
        {
            return await fileHandler.ReadAsync<PropertyIndex>(path);
        }

        /// <summary>
        /// PropertyIndex'i dosyaya yaz
        /// </summary>
        public static async Task WritePropertyIndexAsync(this IFileHandler fileHandler, string path, PropertyIndex propertyIndex)
        {
            await fileHandler.WriteAsync(path, propertyIndex);
        }

        /// <summary>
        /// Dosya var mı kontrolü - async
        /// </summary>
        public static async Task<bool> FileExistsAsync(this IFileHandler fileHandler, string path)
        {
            return await fileHandler.ExistsAsync(path);
        }

        /// <summary>
        /// Dosya boyutunu MB olarak getir
        /// </summary>
        public static async Task<double> GetFileSizeMBAsync(this IFileHandler fileHandler, string path)
        {
            var sizeBytes = await fileHandler.GetFileSizeAsync(path);
            return sizeBytes / (1024.0 * 1024.0);
        }

        /// <summary>
        /// JSON formatında dosya doğrulama
        /// </summary>
        public static async Task<bool> ValidateJsonFileAsync<T>(this IFileHandler fileHandler, string path) where T : class
        {
            try
            {
                var content = await fileHandler.ReadTextAsync(path);
                if (string.IsNullOrEmpty(content))
                    return false;

                JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Backup oluştur
        /// </summary>
        public static async Task CreateBackupAsync(this IFileHandler fileHandler, string originalPath, string backupPath)
        {
            if (await fileHandler.ExistsAsync(originalPath))
            {
                await fileHandler.CopyAsync(originalPath, backupPath);
            }
        }

        /// <summary>
        /// Güvenli dosya yazma (önce temp dosyaya yaz, sonra taşı)
        /// </summary>
        public static async Task SafeWriteAsync<T>(this IFileHandler fileHandler, string path, T data) where T : class
        {
            var tempPath = path + ".tmp";
            try
            {
                await fileHandler.WriteAsync(tempPath, data);

                // Backup oluştur
                if (await fileHandler.ExistsAsync(path))
                {
                    var backupPath = path + ".bak";
                    await fileHandler.CopyAsync(path, backupPath);
                }

                // Temp dosyayı asıl dosyaya taşı
                await fileHandler.MoveAsync(tempPath, path);
            }
            catch
            {
                // Temp dosyayı temizle
                if (await fileHandler.ExistsAsync(tempPath))
                {
                    await fileHandler.DeleteAsync(tempPath);
                }
                throw;
            }
        }
    }
}