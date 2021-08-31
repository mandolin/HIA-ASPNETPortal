using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IUsersDb
    {
        int AddUser(String fullName, string email, string password);
        void DeleteUser(int userId);
        void UpdateUser(int userId, string email, string password);
        IEnumerable<IRoleItem> GetRolesByUser(String email);
        IEnumerable<string> GetRoleNamesByUser(String email);
        IUserItem GetSingleUser(String email);
        string Login(String email, string password);
    }
}