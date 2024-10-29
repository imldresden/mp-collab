using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractLog : MonoBehaviour, ILog
{
    public string Delimiter { get; set; }

    public abstract void Write(string message, string file = "log");

    public abstract void Write(string file, params object[] args);
}
