using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyLog : AbstractLog
{
    public override void Write(string message, string file = "log") { }

    public override void Write(string file, params object[] args) { }
}
