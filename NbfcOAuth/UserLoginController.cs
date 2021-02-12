using AutoMapper;
using BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using UserModel.CommonModel;
using UserModel.ResponseModel;
using Utility;

namespace NbfcProject.Controllers
{
    [Authorize]
    public class UserLoginController : ApiController
    {
        public readonly IExecuteExceptionBusiness ExecuteExceptionBusiness;

        public UserLoginController(IExecuteExceptionBusiness _ExecuteExceptionBusiness)
        {
            ExecuteExceptionBusiness = _ExecuteExceptionBusiness;
        }

        public AddUpdateDeleteResponse<dynamic> GetLoginUser()
        {
            try
            {
                var claimList = (Request.GetRequestContext().Principal as ClaimsPrincipal).Claims.Select(z => new { z.Type, z.Value }).ToList();
                var RoleId = claimList.Where(c => c.Type == "RoleId").Single().Value;
                var UserName = claimList.Where(c => c.Type == "UserName").Single().Value;
                var Role = claimList.Where(c => c.Type == "UserRole").Single().Value;

                var IsBackDateAllow = claimList.Where(c => c.Type == "IsBackDateAllow").Single().Value == "T";
                var AllowDateValue = claimList.Where(c => c.Type == "AllowDateValue").Single().Value;

                ///var BranchCode = claimList.Where(c => c.Type == "BranchCode").Single().Value;

                return new AddUpdateDeleteResponse<dynamic>() { Data = new LoginUserDetail { AllowDateValue = AllowDateValue, IsBackDateAllow = IsBackDateAllow, Role = Role, UserName = UserName, UserId = User.Identity.Name }, Message = "User detail found successfully!!!", Status = true };
            }
            catch (Exception ex)
            {
                return ExecuteExceptionBusiness.GetDynamicResponse(ex);
            }
        }
    }
}
