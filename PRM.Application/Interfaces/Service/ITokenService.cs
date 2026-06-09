using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.Interfaces.Service
{    public interface ITokenService
    {
        string GenerateToken(int userId, string username, string role);
    }
}
