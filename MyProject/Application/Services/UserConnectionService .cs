using Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserConnectionService : IUserConnectionService
    {
        private readonly Dictionary<string, string> _userConnectionMap = new Dictionary<string, string>();

        public string GetConnectionIdForUser(string userId)
        {
            if (_userConnectionMap.TryGetValue(userId, out string connectionId))
            {
                return connectionId;
            }
            return null;
        }

        public void AddOrUpdateConnectionId(string userId, string connectionId)
        {
            _userConnectionMap[userId] = connectionId;
        }

        public void RemoveConnectionId(string userId)
        {
            _userConnectionMap.Remove(userId);
        }
        public IEnumerable<string> GetAllConnectionIds()
        {
            return _userConnectionMap.Values;
        }

        public string GetUserIdForConnection(string connectionId)
        {
            foreach (var kvp in _userConnectionMap)
            {
                if (kvp.Value == connectionId)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }
}
