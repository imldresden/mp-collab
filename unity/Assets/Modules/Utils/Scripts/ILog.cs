using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILog
{
    public string Delimiter { get; set; }
    public void Write(string message, string file = "log");
    public void Write(string file, params object[] args);
}
