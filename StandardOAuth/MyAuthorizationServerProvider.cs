using Microsoft.Owin.Security.OAuth;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserModel.ResponseModel;
using Repository;
using Utility;
using Microsoft.Owin.Security;

namespace NbfcProject
{
    public class MyAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();

            ///return base.ValidateClientAuthentication(context);
        }
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            try
            {
                var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                CurrentUserDetailModel objCurrentUserDetailModel = null;
                login login = new login();
                if (!string.IsNullOrEmpty(context.UserName) && !string.IsNullOrEmpty(context.Password))
                {
                    Func<login, bool> d = x => x.userid == context.UserName && EncryptDecrypt.passwordDecrypt(x.password) == context.Password && x.Status == 1;
                    using (var dbContext = new AphelionDBEntities())
                    {
                        login = dbContext.logins.FirstOrDefault(d);
                        if (login != null)
                        {
                            string Role = dbContext.MstRoles.FirstOrDefault(x => x.RoleId == login.role).Role;
                            string UserName = string.Empty;
                            if (dbContext.EmployeeRegistrations.Where(x => x.EmployeeId == login.userid).Count() > 0)
                                UserName = dbContext.EmployeeRegistrations.FirstOrDefault(x => x.EmployeeId == login.userid).EmployeeName;
                            else
                                UserName = dbContext.DealerMasters.FirstOrDefault(x => x.DealerCode == login.userid).DealerName;
                            objCurrentUserDetailModel = new CurrentUserDetailModel
                            {
                                UserName = UserName,
                                Status = true,
                                userid = login.userid,
                                roleId = login.role,
                                Role = Role,
                                BranchCode = dbContext.UserBranches.Where(x => x.UserId == login.userid).Select(x => x.BranchCode).ToList(),
                                IsBackDateAllow = login.IsBackDateAllow,
                                AllowDateValue = login.AllowDateValue
                            };
                        }
                    }
                }
                if (objCurrentUserDetailModel != null && objCurrentUserDetailModel.Status)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, objCurrentUserDetailModel.Role));
                    identity.AddClaim(new Claim("UserName", objCurrentUserDetailModel.UserName));
                    identity.AddClaim(new Claim(ClaimTypes.Name, objCurrentUserDetailModel.userid));
                    identity.AddClaim(new Claim("RoleId", Convert.ToString(objCurrentUserDetailModel.roleId)));
                    identity.AddClaim(new Claim("UserRole", objCurrentUserDetailModel.Role));
                    identity.AddClaim(new Claim("IsBackDateAllow", (objCurrentUserDetailModel.IsBackDateAllow ?? false) ? "T" : "F"));
                    identity.AddClaim(new Claim("AllowDateValue", (objCurrentUserDetailModel.AllowDateValue != null ? objCurrentUserDetailModel.AllowDateValue.Value.ToString("dd/MM/yyyy") : "F")));
                    context.Validated(identity);
                }
                else
                {
                    context.SetError("Invalid_Grand", "Provide User and Password is Incorrect");

                }
            }
            catch (Exception ex)
            {

            }
        }
        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            // Change authentication ticket for refresh token requests  
            var newIdentity = new ClaimsIdentity(context.Ticket.Identity);
            newIdentity.AddClaim(new Claim("newClaim", "newValue"));
            var newTicket = new AuthenticationTicket(newIdentity, context.Ticket.Properties);
            context.Validated(newTicket);
            return Task.FromResult<object>(null);
        }
    }
}