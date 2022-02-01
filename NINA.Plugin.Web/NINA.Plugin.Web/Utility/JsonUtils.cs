using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Web.NINAPlugin.Utility {

    public class JsonUtils {

        /// <summary>
        /// Serialize the provided object to JSON and write to the file.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fileName"></param>
        /// <param name="overWrite"></param>
        /// <exception cref="IOException"></exception>
        public static void WriteJson(object obj, string fileName, bool overWrite = false) {

            if (obj == null) {
                throw new IOException("WriteJson: null object");
            }

            if (string.IsNullOrEmpty(fileName)) {
                throw new IOException($"WriteJson: bad file name: '{fileName}'");
            }

            if (!overWrite && File.Exists(fileName)) {
                throw new IOException($"WriteJson: will not overwrite existing file: '{fileName}'");
            }

            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(fileName))
            using (JsonWriter writer = new JsonTextWriter(sw)) {
                serializer.Serialize(writer, obj);
            }
        }

        /// <summary>
        /// Read the JSON file and convert to the provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public static T ReadJson<T>(string fileName) {

            if (!File.Exists(fileName)) {
                throw new IOException($"ReadJson: file does not exist or is not readable: '{fileName}'");
            }

            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Return the MD5 hash of the argument object JSON-serialized as a string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>MD5 hash of the object serialized to a string</returns>
        public static string GetHashCode(object obj) {
            if (obj == null) {
                return "";
            }

            string json = JsonConvert.SerializeObject(obj);

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = Encoding.ASCII.GetBytes(json);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

    }
}
