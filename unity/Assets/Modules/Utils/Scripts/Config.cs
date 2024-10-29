using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using UnityEngine;

public class Config : MonoBehaviour
{
    public string ConfigPath = "config.json";

    /// <summary>
    /// This method saves data to a config file.
    /// </summary>
    /// <typeparam name="T">The type of the config value, has to be a struct of basic, serializable data types</typeparam>
    /// <param name="key">The unique identifier of the config value</param>
    /// <param name="value">The config value to write, an object of type T that needs to be serializable to JSON.</param>
    public void Save<T>(string key, T value)
    {
        Debug.LogWarning("Config saving: " + key);
        // set config item for storage
        _config[key] = value;

        // write config to file
        WriteConfig();
    }

    /// <summary>
    /// This method loads data from a config file.
    /// </summary>
    /// <typeparam name="T">The type of the config value, has to be a struct of basic, serializable data types</typeparam>
    /// <param name="key">The unique identifier of the config value</param>
    /// <param name="value">Output parameter containing the config value read from file, an object of type T.</param>
    /// <returns>True if reading was successful, false otherwise</returns>
    public bool TryLoad<T>(string key, out T value)
    {
        Debug.LogWarning("Config loading: " + key);
        if (_config.TryGetValue(key, out var configItem))
        {
            try
            {
                value = (T)configItem;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// This method removes a config item.
    /// </summary>
    /// <param name="key">The identifier of the item to remove</param>
    /// <returns>True if the item was removed, false if no such item exists.</returns>
    public bool TryRemove(string key)
    {
        bool output = _config.Remove(key);
        if (output)
        {
            WriteConfig();
        }

        return output;
    }

    void Awake()
    {
        // read config at start of the program
        ReadConfig();
    }

    private void WriteConfig()
    {
        // serialize dictionary to json string
        string configString = JsonConvert.SerializeObject(_config, _jsonSerializerSettings);

        try
        {
            // write json to file
            File.WriteAllText(ConfigPath, configString);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }

    private void ReadConfig()
    {
        _jsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        if (File.Exists(ConfigPath))
        {
            try
            {
                // read json string from the file
                string dataAsJson = File.ReadAllText(ConfigPath);

                // deserialize object dictionary
                _config = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataAsJson, _jsonSerializerSettings);

                Debug.Log("Successfully loaded config file from \"" + ConfigPath + "\"");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }            
        }
        else
        {
            Debug.LogWarning("Config file not found at \"" + ConfigPath + "\"");
        }

        if (_config == null)
        {
            _config = new Dictionary<string, object>();
        }
    }

    private Dictionary<string, object> _config;
    private JsonSerializerSettings _jsonSerializerSettings;

    public class ConfigItem
    {
        public string Key;
        public object Value;

        public ConfigItem()
        {

        }

        public ConfigItem(string key, Type type, object value)
        {
            Key = key;
            Value = value;
        }
    }
}
