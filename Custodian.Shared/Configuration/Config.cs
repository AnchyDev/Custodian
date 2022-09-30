using System.IO;
using System.Threading.Tasks;

namespace Custodian.Shared.Configuration
{
    public class Config
    {
        /// <summary>
        /// Fetches the deserialized json config from file path.
        /// </summary>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        /// <param name="file">The path to the config.json</param>
        /// <param name="createIfNotExist">If the config does not exist, create it.</param>
        /// <returns>The deserialized config.</returns>
        public static async Task<T?> GetAsync<T>(string file, bool createIfNotExist = true) where T : new()
        {
            var path = Path.GetDirectoryName(file);
            var fileName = Path.GetFileName(file);
            var fullPath = Path.Combine(path, fileName);

            if (!Directory.Exists(path))
            {
                if (createIfNotExist)
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    throw new DirectoryNotFoundException();
                }
            }

            if(!File.Exists(fullPath))
            {
                if (createIfNotExist)
                {
                    using var cfs = File.Create(fullPath);
                    await System.Text.Json.JsonSerializer.SerializeAsync<T>(cfs, new T());
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }

            using var rfs = File.Open(fullPath, FileMode.Open, FileAccess.Read);

            return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(rfs);
        }
    }
}
