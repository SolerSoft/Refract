using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Refract
{
    /// <summary>
    /// A simple helper class for loading and saving objects as JSON data locally.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On HoloLens these files can be viewed, downloaded or deleted by going to:
    /// Device Portal -> System -> File explorer -> LocalAppData -> YourAppName -> LocalState
    /// For example:
    /// Device Portal -> System -> File explorer -> LocalAppData -> TaskGuidance -> LocalState
    /// </para>
    /// <para>
    /// On PC these files can be viewed, downloaded or deleted by going to:
    /// %USERPROFILE%/AppData/LocalLow/YourCompanyName/YourAppName
    /// For example:
    /// %USERPROFILE%/AppData/LocalLow/DefaultCompany/DefaultAppName
    /// </para>
    /// </remarks>
    static public class DataStore
    {

        /// <summary>
        /// Loads an object of the specified type from the specified file.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to load. This type must be marked with <see cref="SerializableAttribute"/>.
        /// </typeparam>
        /// <param name="filename">
        /// The name of the file to load the object from.
        /// </param>
        /// <returns>
        /// The object if loaded; otherwise <see langword = "null" /> if the file couldn't be loaded.
        /// </returns>
        static public T LoadObject<T>(string filename) where T : class
        {
            // Validate
            if (string.IsNullOrEmpty(filename)) { throw new ArgumentException(nameof(filename)); }

            // Build full path to the file
            string path = $"{Application.persistentDataPath}/{filename}.json";

            // If the file doesn't exist, skip
            if (!File.Exists(path)) { return null; }

            // Read all bytes into memory
            byte[] data = File.ReadAllBytes(path);

            // Read string from bytes
            string json = Encoding.ASCII.GetString(data);

            // Convert json string to the object
            T obj = JsonUtility.FromJson(json, typeof(T)) as T;

            // Return loaded object
            return obj;
        }

        /// <summary>
        /// Saves the object to the specified file.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to save. This type must be marked with <see cref="SerializableAttribute"/>.
        /// </typeparam>
        /// <param name="obj"></param>
        /// <param name="filename"></param>
        static public void SaveObject<T>(T obj, string filename) where T : class
        {
            // Validate
            if (obj == null) { throw new ArgumentNullException(nameof(obj)); }
            if (string.IsNullOrEmpty(filename)) { throw new ArgumentException(nameof(filename)); }

            // Build full path to the file
            string path = $"{Application.persistentDataPath}/{filename}.json";

            // Convert the object to a json string
            string json = JsonUtility.ToJson(obj);

            // Get bytes of the string
            byte[] data = Encoding.ASCII.GetBytes(json);

            // Write the bytes to the file
            File.WriteAllBytes(path, data);
        }
    }
}