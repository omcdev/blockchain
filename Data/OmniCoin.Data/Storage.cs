

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace OmniCoin.Data
{
    public class Storage
    {
        private string baseDirectory;
        public static Storage Instance;

        static Storage()
        {
            Instance = new Storage();
        }

        public Storage()
        {
            this.baseDirectory = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), DbDomains.StorageFile);
        }

        public void Put(string container, string key, string value)
        {
            var dir = Path.Combine(baseDirectory, container);

            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, key);
            File.WriteAllText(file, value);
        }

        public void PutData(string container, string key, object data)
        {
            var dir = Path.Combine(baseDirectory, container);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, key);
            using (var stream = File.Open(file, FileMode.Create))
            {
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(stream, data);

                stream.Flush();
            }
        }

        public bool Delete(string container, string key)
        {
            var dir = Path.Combine(baseDirectory, container);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, key);

            if(File.Exists(file))
            {
                File.Delete(file);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string Get(string container, string key)
        {
            var dir = Path.Combine(baseDirectory, container);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, key);

            if (File.Exists(file))
            {
                return File.ReadAllText(file);
            }
            else
            {
                return null;
            }
        }

        public T GetData<T>(string container, string key)
        {
            var dir = Path.Combine(baseDirectory, container);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, key);
            var result = default(T);
            if (File.Exists(file))
            {
                bool isError = false;
                using (var stream = File.Open(file, FileMode.Open))
                {
                    try
                    {
                        BinaryFormatter b = new BinaryFormatter();
                        var obj = b.Deserialize(stream);

                        if (obj != null)
                        {
                            return (T)obj;
                        }
                    }
                    catch(Exception ex)
                    {
                        isError = true;
                        LogHelper.Error("Tx File Deserialize failed:" + file, ex);
                    }

                    result = default(T);
                }
                if (isError)
                    Delete(container, key);
            }
            return result;
        }

        public List<string> GetAllKeys(string container)
        {
            var dir = Path.Combine(baseDirectory, container);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var files = Directory.GetFiles(dir);
            var keys = new List<string>();

            foreach(var file in files)
            {
                keys.Add(Path.GetFileName(file));
            }

            return keys;
        }

        public List<string> GetAllConfigData(string dat)
        {
            var result = new List<string>();
            var file = Path.Combine(baseDirectory, dat + ".dat");

            if(File.Exists(file))
            {
                var lines = File.ReadAllLines(file);

                if(lines != null && lines.Length > 0)
                {
                    result.AddRange(lines);
                }
            }

            return result;
        }

        public void UpdateAllConfigData(string dat, string[] items)
        {
            try
            {
                var result = new List<string>();
                var file = Path.Combine(baseDirectory, dat + ".dat");
                if (!Directory.Exists(baseDirectory))
                {
                    Directory.CreateDirectory(baseDirectory);
                }

                File.WriteAllLines(file, items);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }
        }
    }
}
