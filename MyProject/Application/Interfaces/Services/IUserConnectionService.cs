using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IUserConnectionService
    {
        string GetConnectionIdForUser(string userId);
        void AddOrUpdateConnectionId(string userId, string connectionId);
        void RemoveConnectionId(string userId);
        IEnumerable<string> GetAllConnectionIds();
        string GetUserIdForConnection(string connectionId);
    }
}
