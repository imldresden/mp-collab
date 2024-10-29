using System;
using System.Collections.Generic;

public interface IService
{
    IReadOnlyList<Type> Dependencies { get; }
}

//public class Dependency
//{
//    public Type Type { get; private set; }
//    public bool IsOptional { get; private set; }
//    public Dependency(Type service, bool optional = false)
//    {
//        Type = service;
//        IsOptional = optional;
//    }
//}