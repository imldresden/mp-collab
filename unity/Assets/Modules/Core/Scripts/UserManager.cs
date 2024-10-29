using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public class UserManager : MonoBehaviour, IUserManager
    {
        public List<User> Users => throw new NotImplementedException();

        public User GetUser(Guid id)
        {
            throw new NotImplementedException();
        }

        private ISessionManager _sessionManager;

        void Start()
        {
            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();

            if(_sessionManager != null )
            {
            }
        }
    }
}