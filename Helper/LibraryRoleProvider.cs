using KutuphaneMvc.Models.Entity;
using System;
using System.Linq;
using System.Web.Security;

namespace KutuphaneMvc.Helper
{
    public class LibraryRoleProvider : RoleProvider
    {
        public override string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var db = new LibraryDBEntities1())
            {
                var user = db.UYE.FirstOrDefault(u => u.EMAIL == username);
                if (user == null) return new string[0];

                var roleName = db.ROLE.Where(r => r.ROLE_ID == user.ROLE_ID)
                                      .Select(r => r.ROLE_AD)
                                      .FirstOrDefault();
                return roleName != null ? new[] { roleName } : new string[0];
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}