using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using IMLD.MixedReality.Audio;
using IMLD.MixedReality.Core;
using IMLD.MixedReality.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestCoreServices
{
    // A Test behaves as an ordinary method
    [Test]
    public void TestServiceLocator()
    {
        Assert.That(ServiceLocator.Instance, Is.Not.Null); // ServiceLocator.Instance should never be null
        Assert.Throws<ArgumentException>(delegate { ServiceLocator.Instance.Get<Config>(); }); // ServiceLocator should not contain this interface

        GameObject ServiceManagerGO = new GameObject();
        var ServiceManager = ServiceManagerGO.AddComponent<ServiceManager>();
        ServiceManager.Config = ServiceManagerGO.AddComponent<Config>();

        ServiceLocator.Instance.Register(typeof(Config), ServiceManager.Config);
        var Config = ServiceLocator.Instance.Get<Config>();

        Assert.That(Config, Is.EqualTo(ServiceManager.Config)); // returned interface should be the one added before
    }

    [Test]
    public void TestConfig()
    {
        // set up config object
        GameObject ConfigGO = new GameObject();
        var Config = ConfigGO.AddComponent<Config>();
        Config.ConfigPath = "delete_me.json";
        Assert.That(File.Exists(Config.ConfigPath), Is.False);

        // try loading non-existing value
        Assert.That(Config.TryLoad<bool>("none", out bool val), Is.False); // should return false

        // try saving & loading a simple value
        Config.Save("int_value", 42);
        Assert.That(File.Exists(Config.ConfigPath));
        Config.TryLoad("int_value", out int int_value);
        Assert.That(int_value, Is.EqualTo(42));

        // try saving & loading a more complex type
        Dictionary<string, object> dict_in = new Dictionary<string, object>
        {
            { "1", 100 },
            { "2", "test" }
        };
        Config.Save("dict_value", dict_in);
        Assert.That(Config.TryLoad("dict_value", out Dictionary<string, object> dict_out), Is.True);
        Assert.That(dict_out["1"], Is.EqualTo(100));
        Assert.That(dict_out["2"], Is.EqualTo("test"));

        // try removing a value
        Assert.That(Config.TryRemove("int_value"), Is.True);
        Assert.That(Config.TryLoad("int_value", out int n), Is.False); // should return false

        // remove temp config file again
        try
        {
            File.Delete(Config.ConfigPath);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
}
