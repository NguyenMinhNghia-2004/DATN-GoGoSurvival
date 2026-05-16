namespace Luzart
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using System;
    using System.Threading.Tasks;
    using System.Security.Cryptography;
    using System.Text;
#if NETFX_CORE
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage.Streams;
#else
#if NEWTONSOFT
    using Newtonsoft.Json;
#endif
#endif
    public enum TypeSave
    {
        None = 0,
        Encryption = 1,
    }
    public static class SaveLoadUtil
    {
        private const string SALT = "luzart";
        private const string PASSWORD = "luzart";
        public static void SaveDataPrefs<T>(T data, string key)
        {
            string stringData = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, stringData);
            PlayerPrefs.Save();
        }
        public static T LoadDataPrefs<T>(string key)
        {
            T t = default;
            string stringData = PlayerPrefs.GetString(key,"");
            if(!string.IsNullOrEmpty(stringData))
            {
                t = JsonUtility.FromJson<T>(stringData);
            }
            return t;
        }
    #if UNITY_EDITOR
        public static void SaveDataToResources<T>(T data, string relativePath)
        {
            string json = JsonUtility.ToJson(data);
            string directoryPath = Path.Combine(Application.dataPath, "Resources", Path.GetDirectoryName(relativePath));
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string fullPath = Path.Combine(Application.dataPath, "Resources", relativePath).Replace("\\", "/");
            File.WriteAllText(fullPath, json);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Data saved to:{fullPath} and file {json}" );
        }
    #endif
        /// <summary>
        /// Read a file at specified path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="isAbsolutePath">Is this path an absolute one?</param>
        /// <returns>Data of the file, in byte[] format</returns>
        public static byte[] LoadFile(string filePath, bool isAbsolutePath = false)
        {
            if (filePath == null || filePath.Length == 0)
            {
                return null;
            }
            string absolutePath = filePath;
            if (!isAbsolutePath) { absolutePath = GetWritablePath(filePath); }
            if (System.IO.File.Exists(absolutePath))
            {
                return System.IO.File.ReadAllBytes(absolutePath);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Save a byte array to storage at specified path and return the absolute path of the saved file
        /// </summary>
        /// <param name="bytes">Data to write</param>
        /// <param name="filePath">Where to save file</param>
        /// <param name="isAbsolutePath">Is this path an absolute one or relative</param>
        /// <returns>Absolute path of the file</returns>
        public static string SaveFile(byte[] bytes, string filePath, bool isAbsolutePath = false)
        {
            if (filePath == null || filePath.Length == 0)
            {
                return null;
            }
            //path to the file, in absolute format
            string path = filePath;
            if (!isAbsolutePath)
            {
                path = GetWritablePath(filePath);
            }
            string folderName = Path.GetDirectoryName(path);
            //Debug.Log("Folder name: " + folderName);
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            File.WriteAllBytes(path, bytes);
            return path;
        }
        /// <summary>
        /// Return a path to a writable folder on a supported platform
        /// </summary>
        /// <param name="relativeFilePath">A relative path to the file, from the out most writable folder</param>
        /// <returns></returns>
        public static string GetWritablePath(string relativeFilePath)
        {
            string result = "";
            //folder += (folder.Trim().Equals("")) ? "" : "/";
            //extension = (fileName.Trim().Equals("")) ? "" : "." + extension;
    #if UNITY_EDITOR
            result = Application.persistentDataPath + Path.DirectorySeparatorChar + relativeFilePath;
    #elif UNITY_ANDROID
    		result = Application.persistentDataPath + Path.DirectorySeparatorChar + relativeFilePath;
    #elif UNITY_IPHONE
                result = Application.persistentDataPath + Path.DirectorySeparatorChar + relativeFilePath;
    #elif UNITY_WP8 || NETFX_CORE || UNITY_WSA
    		result = Application.persistentDataPath + "/" + relativeFilePath;
    #endif
            return result;
        }
        public static void DeleteFile(string fileName)
        {
            if (fileName == null || fileName.Length == 0)
                return;
            string file = GetWritablePath(fileName);
            if (dicStream != null && dicStream.ContainsKey(fileName))
            {
                dicStream[fileName].Close();
                dicStream.Remove(fileName);
            }
            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }
        }
        /// <summary>
        /// Serialize an object into JSON string and write it into file
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Object to serialize</param>
        /// <param name="fileName">Filename to write to</param>
        public static void SerializeObjectToFile<T>(T data, string fileName, string password = null, bool isCacheStream = true)
        {
            if (data != null)
            {
    #if NEWTONSOFT
                string json = JsonConvert.SerializeObject(data);
    #else
                string json = JsonUtility.ToJson(data);
    #endif
                byte[] bytes;
                if (!string.IsNullOrEmpty(password))
                {
                    bytes = EncryptionHelper.Encrypt(json, password, SALT);
                }
                else
                {
                    bytes = System.Text.Encoding.UTF8.GetBytes(json);
                }
                // test aaa
                //SaveFile(bytes, fileName, isAbsoluteFilePath);
                SaveDataFS(bytes, fileName, isCacheStream);
            }
        }
        /// <summary>
        /// Deserialize an object from JSON file
        /// </summary>
        /// <typeparam name="T">Type of result object</typeparam>
        /// <param name="fileName">Json file content the serialized object</param>
        /// <returns>Object serialized in json file, if the file is not existed or invalid, the result will be default(T)</returns>
        public static T DeserializeObjectFromFile<T>(string fileName, string password = null, bool isCacheStream = true)
        {
            T data = default(T);
            // byte[] localSaved = LoadFile(fileName, isAbsolutePath);
            byte[] localSaved = LoadDataFS(fileName, isCacheStream);
            if (localSaved == null)
            {
                Debug.Log(fileName + " not exist, returning null");
            }
            else
            {
                if (!string.IsNullOrEmpty(password))
                {
                    string decrypt = EncryptionHelper.Decrypt(localSaved, password, SALT);
                    if (string.IsNullOrEmpty(decrypt))
                    {
                        Debug.LogWarning("Can't decrypt file " + fileName);
                        return data;
                    }
                    else
                    {
                        data = JsonUtility.FromJson<T>(decrypt);
                    }
                }
                else
                {
                    var str = System.Text.Encoding.UTF8.GetString(localSaved);
                    data = JsonUtility.FromJson<T>(str);
                }
            }
            return data;
        }
        public static async Task<T> DeserializeObjectFromFileAsync<T>(string fileName, string password = null, bool isCacheStream = true)
        {
            T data = default(T);
            byte[] localSaved = await LoadDataFSAsync(fileName, isCacheStream);
            if (localSaved == null)
            {
                Debug.Log(fileName + " not exist, returning null");
            }
            else
            {
                if (!string.IsNullOrEmpty(password))
                {
                    string decrypt = await EncryptionHelper.DecryptAsync(localSaved, password, SALT);
                    if (string.IsNullOrEmpty(decrypt))
                    {
                        Debug.LogWarning("Can't decrypt file " + fileName);
                        return data;
                    }
                    else
                    {
                        data = JsonUtility.FromJson<T>(decrypt);
                    }
                }
                else
                {
                    var str = System.Text.Encoding.UTF8.GetString(localSaved);
                    data = JsonUtility.FromJson<T>(str);
                }
            }
            return data;
        }
        public static void ByteToFile(byte[] byteData, string fileName, string password = null, bool isCacheStream = true)
        {
            if (byteData != null)
            {
                byte[] bytes;
                if (!string.IsNullOrEmpty(password))
                {
                    bytes = EncryptionHelper.Encrypt(byteData, password, SALT);
                }
                else
                {
                    bytes = byteData;
                }
                SaveDataFS(bytes, fileName, isCacheStream);
            }
        }
        public static byte[] ByteFromFile(string fileName, string password = null, bool isCacheStream = true)
        {
            // byte[] localSaved = LoadFile(fileName, isAbsolutePath);
            byte[] localSaved = LoadDataFS(fileName, isCacheStream);
            if (localSaved == null)
            {
                Debug.Log(fileName + " not exist, returning null");
            }
            else
            {
                if (!string.IsNullOrEmpty(password))
                {
                    return EncryptionHelper.DecryptToByte(localSaved, password, SALT);
                }
                else
                {
                    return localSaved;
                }
            }
            return null;
        }
        #region FileStream
        public static Dictionary<string, FileStream> dicStream = new Dictionary<string, FileStream>();
        public const int BUFFER_SIZE = 4096;
        public static byte[] Buffer = new byte[BUFFER_SIZE];
        public static void SaveDataFS(byte[] data, string fileName, bool cacheStream = false, FileAccess fileAccess = FileAccess.ReadWrite)
        {
            var path = GetWritablePath(fileName);
            string folderName = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            if (cacheStream)
            {
                if (!dicStream.ContainsKey(fileName))
                {
                    var str = new FileStream(path, FileMode.OpenOrCreate, fileAccess);
                    dicStream.Add(fileName, str);
                }
                dicStream[fileName].Seek(0, SeekOrigin.Begin);
                dicStream[fileName].Write(data, 0, data.Length);
                dicStream[fileName].SetLength(data.Length);
                dicStream[fileName].Flush();
            }
            else
            {
                using (var str = new FileStream(path, FileMode.OpenOrCreate, fileAccess))
                {
                    str.Seek(0, SeekOrigin.Begin);
                    str.Write(data, 0, data.Length);
                    str.SetLength(data.Length);
                    str.Flush();
                    str.Close();
                }
            }
        }
        public static byte[] LoadDataFS(string fileName, bool cacheStream = false, FileAccess fileAccess = FileAccess.ReadWrite)
        {
            var path = GetWritablePath(fileName);
            string folderName = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            if (cacheStream)
            {
                if (!dicStream.ContainsKey(fileName))
                {
                    var str = new FileStream(path, FileMode.OpenOrCreate, fileAccess);
                    dicStream.Add(fileName, str);
                }
                dicStream[fileName].Seek(0, SeekOrigin.Begin);
                var ls = new List<byte>();
                int c;
                while ((c = dicStream[fileName].Read(Buffer, 0, Buffer.Length)) > 0)
                {
                    ls.AddRange(new ArraySegment<byte>(Buffer, 0, c));
                }
                return ls.Count == 0 ? null : ls.ToArray();
            }
            else
            {
                using (var str = new FileStream(path, FileMode.OpenOrCreate, fileAccess))
                {
                    str.Seek(0, SeekOrigin.Begin);
                    var ls = new List<byte>();
                    int c;
                    while ((c = str.Read(Buffer, 0, Buffer.Length)) > 0)
                    {
                        ls.AddRange(new ArraySegment<byte>(Buffer, 0, c));
                    }
                    str.Close();
                    return ls.Count == 0 ? null : ls.ToArray();
                }
            }
        }
        public static async Task<byte[]> LoadDataFSAsync(string fileName, bool cacheStream = false, FileAccess fileAccess = FileAccess.ReadWrite)
        {
            var path = GetWritablePath(fileName);
            string folderName = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            if (cacheStream)
            {
                if (!dicStream.ContainsKey(fileName))
                {
                    var str = new FileStream(path, FileMode.OpenOrCreate, fileAccess);
                    dicStream.Add(fileName, str);
                }
                dicStream[fileName].Seek(0, SeekOrigin.Begin);
                var ls = new List<byte>();
                int c;
                var Buffer = new byte[BUFFER_SIZE];
                while ((c = await dicStream[fileName].ReadAsync(Buffer, 0, Buffer.Length)) > 0)
                {
                    ls.AddRange(new ArraySegment<byte>(Buffer, 0, c));
                }
                return ls.Count == 0 ? null : ls.ToArray();
            }
            else
            {
                using (var str = new FileStream(path, FileMode.OpenOrCreate, fileAccess))
                {
                    str.Seek(0, SeekOrigin.Begin);
                    var ls = new List<byte>();
                    int c;
                    var Buffer = new byte[BUFFER_SIZE];
                    while ((c = await str.ReadAsync(Buffer, 0, Buffer.Length)) > 0)
                    {
                        ls.AddRange(new ArraySegment<byte>(Buffer, 0, c));
                    }
                    str.Close();
                    return ls.Count == 0 ? null : ls.ToArray();
                }
            }
        }
        #endregion
        public static void SaveData<T>(T data, string fileName, bool isCacheStream = true, TypeSave typeSave = TypeSave.Encryption)
        {
            if (typeSave == TypeSave.None)
            {
                SerializeObjectToFile(data, fileName, null, isCacheStream);
            }
            else if (typeSave == TypeSave.Encryption)
            {
                SerializeObjectToFile(data, fileName, PASSWORD, isCacheStream);
            }
        }
        public static T LoadData<T>(string fileName, bool isCacheStream = true, TypeSave typeSave = TypeSave.Encryption)
        {
            T t = default;
            if (typeSave == TypeSave.None)
            {
                t = DeserializeObjectFromFile<T>(fileName, null, isCacheStream);
            }
            else if (typeSave == TypeSave.Encryption)
            {
                t = DeserializeObjectFromFile<T>(fileName, PASSWORD, isCacheStream);
            }
            return t;
        }
        public static void SaveDataBytes(byte[] data, string fileName, bool isCacheStream = true, TypeSave typeSave = TypeSave.Encryption)
        {
            ByteToFile(data, fileName, PASSWORD, isCacheStream);
        }
        public static byte[] LoadDataBytes(string fileName, bool isCacheStream = true, TypeSave typeSave = TypeSave.Encryption)
        {
            return ByteFromFile(fileName, PASSWORD, isCacheStream);
        }
        public static void SaveDataAsync<T>(T data, string fileName, bool isCacheStream = true, TypeSave typeSave = TypeSave.Encryption)
        {
            if (typeSave == TypeSave.None)
            {
                SerializeObjectToFile(data, fileName, null, isCacheStream);
            }
            else if (typeSave == TypeSave.Encryption)
            {
                SerializeObjectToFile(data, fileName, PASSWORD, isCacheStream);
            }
        }
        public static async Task<T> LoadDataAsync<T>(string fileName, bool isCacheStream = true, TypeSave typeSave = TypeSave.Encryption)
        {
            Debug.Log($"Start Load {fileName}");
            T t = default;
            if (typeSave == TypeSave.None)
            {
                t = await DeserializeObjectFromFileAsync<T>(fileName, null, isCacheStream);
            }
            else if (typeSave == TypeSave.Encryption)
            {
                t = await DeserializeObjectFromFileAsync<T>(fileName, PASSWORD, isCacheStream);
            }
            Debug.Log($"End Load {fileName}");
            return t;
        }
    }
    public static class EncryptionHelper
    {
#if NETFX_CORE
            public static byte[] Encrypt(string plainText, string pw, string salt = "")
            {
                IBuffer pwBuffer = CryptographicBuffer.ConvertStringToBinary(pw, BinaryStringEncoding.Utf8);
                IBuffer saltBuffer = CryptographicBuffer.ConvertStringToBinary(salt, BinaryStringEncoding.Utf16LE);
                IBuffer plainBuffer = CryptographicBuffer.ConvertStringToBinary(plainText, BinaryStringEncoding.Utf16LE);
                // Derive key material for password size 32 bytes for AES256 algorithm
                KeyDerivationAlgorithmProvider keyDerivationProvider = Windows.Security.Cryptography.Core.KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
                // using salt and 1000 iterations
                KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, 1000);
                // create a key based on original key and derivation parmaters
                CryptographicKey keyOriginal = keyDerivationProvider.CreateKey(pwBuffer);
                IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Parms, 32);
                CryptographicKey derivedPwKey = keyDerivationProvider.CreateKey(pwBuffer);
                // derive buffer to be used for encryption salt from derived password key
                IBuffer saltMaterial = CryptographicEngine.DeriveKeyMaterial(derivedPwKey, pbkdf2Parms, 16);
                // display the buffers � because KeyDerivationProvider always gets cleared after each use, they are very similar unforunately
                string keyMaterialString = CryptographicBuffer.EncodeToBase64String(keyMaterial);
                string saltMaterialString = CryptographicBuffer.EncodeToBase64String(saltMaterial);
                SymmetricKeyAlgorithmProvider symProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
                // create symmetric key from derived password key
                CryptographicKey symmKey = symProvider.CreateSymmetricKey(keyMaterial);
                // encrypt data buffer using symmetric key and derived salt material
                IBuffer resultBuffer = CryptographicEngine.Encrypt(symmKey, plainBuffer, saltMaterial);
                byte[] result;
                CryptographicBuffer.CopyToByteArray(resultBuffer, out result);
                return result;
            }
            public static string Decrypt(byte[] encryptedData, string pw, string salt = "")
            {
                IBuffer pwBuffer = CryptographicBuffer.ConvertStringToBinary(pw, BinaryStringEncoding.Utf8);
                IBuffer saltBuffer = CryptographicBuffer.ConvertStringToBinary(salt, BinaryStringEncoding.Utf16LE);
                IBuffer cipherBuffer = CryptographicBuffer.CreateFromByteArray(encryptedData);
                // Derive key material for password size 32 bytes for AES256 algorithm
                KeyDerivationAlgorithmProvider keyDerivationProvider = Windows.Security.Cryptography.Core.KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
                // using salt and 1000 iterations
                KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, 1000);
                // create a key based on original key and derivation parmaters
                CryptographicKey keyOriginal = keyDerivationProvider.CreateKey(pwBuffer);
                IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Parms, 32);
                CryptographicKey derivedPwKey = keyDerivationProvider.CreateKey(pwBuffer);
                // derive buffer to be used for encryption salt from derived password key
                IBuffer saltMaterial = CryptographicEngine.DeriveKeyMaterial(derivedPwKey, pbkdf2Parms, 16);
                // display the keys � because KeyDerivationProvider always gets cleared after each use, they are very similar unforunately
                string keyMaterialString = CryptographicBuffer.EncodeToBase64String(keyMaterial);
                string saltMaterialString = CryptographicBuffer.EncodeToBase64String(saltMaterial);
                SymmetricKeyAlgorithmProvider symProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
                // create symmetric key from derived password material
                CryptographicKey symmKey = symProvider.CreateSymmetricKey(keyMaterial);
                // encrypt data buffer using symmetric key and derived salt material
                IBuffer resultBuffer = CryptographicEngine.Decrypt(symmKey, cipherBuffer, saltMaterial);
                string result = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf16LE, resultBuffer);
                return result;
            }
#else
        public static byte[] Encrypt(string dataToEncrypt, string password, string salt)
        {
            AesManaged aes = null;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            try
            {
                //Generate a Key based on a Password, Salt and HMACSHA1 pseudo-random number generator
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));
                //Create AES algorithm with 256 bit key and 128-bit block size
                aes = new AesManaged();
                aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
                rfc2898.Reset(); //needed for WinRT compatibility
                aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
                //Create Memory and Crypto Streams
                memoryStream = new MemoryStream();
                cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                //Encrypt Data
                byte[] data = Encoding.Unicode.GetBytes(dataToEncrypt);
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                //Return encrypted data
                return memoryStream.ToArray();
            }
            catch (Exception eEncrypt)
            {
                Debug.LogWarning(eEncrypt.ToString());
                return null;
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (memoryStream != null)
                    memoryStream.Close();
                if (aes != null)
                    aes.Clear();
            }
        }
        public static byte[] Encrypt(byte[] data, string password, string salt)
        {
            AesManaged aes = null;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            try
            {
                //Generate a Key based on a Password, Salt and HMACSHA1 pseudo-random number generator
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));
                //Create AES algorithm with 256 bit key and 128-bit block size
                aes = new AesManaged();
                aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
                rfc2898.Reset(); //needed for WinRT compatibility
                aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
                //Create Memory and Crypto Streams
                memoryStream = new MemoryStream();
                cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                //Encrypt Data
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                //Return encrypted data
                return memoryStream.ToArray();
            }
            catch (Exception eEncrypt)
            {
                Debug.LogWarning(eEncrypt.ToString());
                return null;
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (memoryStream != null)
                    memoryStream.Close();
                if (aes != null)
                    aes.Clear();
            }
        }
        public static string Decrypt(byte[] dataToDecrypt, string password, string salt)
        {
            AesManaged aes = null;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            string decryptedText = "";
            try
            {
                //Generate a Key based on a Password, Salt and HMACSHA1 pseudo-random number generator
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));
                //Create AES algorithm with 256 bit key and 128-bit block size
                aes = new AesManaged();
                aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
                rfc2898.Reset(); //neede to be WinRT compatible
                aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
                //Create Memory and Crypto Streams
                memoryStream = new MemoryStream();
                cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                //Decrypt Data
                cryptoStream.Write(dataToDecrypt, 0, dataToDecrypt.Length);
                cryptoStream.FlushFinalBlock();
                //Return Decrypted String
                byte[] decryptBytes = memoryStream.ToArray();
                decryptedText = Encoding.Unicode.GetString(decryptBytes, 0, decryptBytes.Length);
            }
            catch (Exception eDecrypt)
            {
                Debug.LogWarning(eDecrypt.ToString());
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (memoryStream != null)
                    memoryStream.Close();
                if (aes != null)
                    aes.Clear();
            }
            return decryptedText;
        }
        public static async Task<string> DecryptAsync(byte[] dataToDecrypt, string password, string salt)
        {
            AesManaged aes = null;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            string decryptedText = "";
            try
            {
                //Generate a Key based on a Password, Salt and HMACSHA1 pseudo-random number generator
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));
                //Create AES algorithm with 256 bit key and 128-bit block size
                aes = new AesManaged();
                aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
                rfc2898.Reset(); //neede to be WinRT compatible
                aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
                //Create Memory and Crypto Streams
                memoryStream = new MemoryStream();
                cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                //Decrypt Data
                await cryptoStream.WriteAsync(dataToDecrypt, 0, dataToDecrypt.Length);
                cryptoStream.FlushFinalBlock();
                //Return Decrypted String
                byte[] decryptBytes = memoryStream.ToArray();
                decryptedText = Encoding.Unicode.GetString(decryptBytes, 0, decryptBytes.Length);
            }
            catch (Exception eDecrypt)
            {
                Debug.LogWarning(eDecrypt.ToString());
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (memoryStream != null)
                    memoryStream.Close();
                if (aes != null)
                    aes.Clear();
            }
            return decryptedText;
        }
        public static byte[] DecryptToByte(byte[] dataToDecrypt, string password, string salt)
        {
            AesManaged aes = null;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            byte[] decryptBytes = null;
            try
            {
                //Generate a Key based on a Password, Salt and HMACSHA1 pseudo-random number generator
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));
                //Create AES algorithm with 256 bit key and 128-bit block size
                aes = new AesManaged();
                aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
                rfc2898.Reset(); //neede to be WinRT compatible
                aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
                //Create Memory and Crypto Streams
                memoryStream = new MemoryStream();
                cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                //Decrypt Data
                cryptoStream.Write(dataToDecrypt, 0, dataToDecrypt.Length);
                cryptoStream.FlushFinalBlock();
                //Return Decrypted String
                decryptBytes = memoryStream.ToArray();
            }
            catch (Exception eDecrypt)
            {
                Debug.LogWarning(eDecrypt.ToString());
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (memoryStream != null)
                    memoryStream.Close();
                if (aes != null)
                    aes.Clear();
            }
            return decryptBytes;
        }
#endif
    }
}
