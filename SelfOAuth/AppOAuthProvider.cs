using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace WebAppMVC
{
    public class AppOAuthProvider : OAuthAuthorizationServerProvider
    {
        private OAuthDBEntities _db = new OAuthDBEntities();
        
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            string usernameVal = context.UserName;
            string passwordVal = context.Password;
            var user = _db.Logins.Where(x => x.UserName == usernameVal && x.Password == passwordVal).ToList();

            if (user == null || user.Count() <= 0)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }
            var claims = new List<Claim>();
            var userInfo = user.FirstOrDefault();

            claims.Add(new Claim(ClaimTypes.Name, userInfo.UserName));

            ClaimsIdentity oAuthClaimIdentity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesClaimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

            AuthenticationProperties properties = CreateProperties(userInfo.UserName, userInfo.Name);
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthClaimIdentity, properties);

            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesClaimIdentity);
        }
     
        public static AuthenticationProperties CreateProperties(string userName, string name)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
                                               {
                                                   { "UserId", userName },
                                                    {"Name", name}
                                               };
            return new AuthenticationProperties(data);
        }

        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            // Change auth ticket for refresh token requests
            var newIdentity = new ClaimsIdentity(context.Ticket.Identity);
            newIdentity.AddClaim(new Claim("newClaim", "newValue"));
            var newTicket = new AuthenticationTicket(newIdentity, context.Ticket.Properties);
            context.Validated(newTicket);
            return Task.FromResult<object>(null);
        }
    }
}