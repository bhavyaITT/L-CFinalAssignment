using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.ExternalService
{
    /// <summary>
    /// BCrypt is the industry standard for password hashing.
    /// Work factor of 12 gives a good balance of security and performance.
    /// This class is in Infrastructure — swapping to Argon2 later means
    /// only this file changes; Application layer is untouched.
    /// </summary>
    public class BcryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        public string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public bool Verify(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
