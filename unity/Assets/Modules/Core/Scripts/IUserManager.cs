using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public interface IUserManager
    {
        List<User> Users { get; }

        User GetUser(Guid id);

    }
}