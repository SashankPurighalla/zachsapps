﻿using BOS.Auth.Client;
using BOS.StarterCode.Helpers;
using BOS.StarterCode.Models.BOSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace BOS.StarterCode.Controllers
{
    [Authorize(Policy = "IsAuthenticated")]
    public class RolesController : Controller
    {
        private readonly IAuthClient _bosAuthClient;

        public RolesController(IAuthClient authClient)
        {
            _bosAuthClient = authClient;
        }

        public async Task<IActionResult> Index()
        {
            return View(await GetPageData());
        }

        public IActionResult NavigateToModule(Guid id, string code, bool isDefault)
        {
            if (isDefault)
            {
                switch (code)
                {
                    case "MYPFL":
                        return RedirectToAction("Index", "Profile");
                    case "USERS":
                        return RedirectToAction("Index", "Users");
                    case "ROLES":
                        return RedirectToAction("Index", "Roles");
                    case "PRMNS":
                        return RedirectToAction("Index", "Permissions");
                    default:
                        return View("Index", GetPageData());
                }
            }
            return null;
        }

        public IActionResult AddNewRole()
        {
            return View("AddRole");
        }

        public async Task<ActionResult> AddRole(Role role)
        {
            var response = await _bosAuthClient.AddRoleAsync<Role>(role);
            if (response.IsSuccessStatusCode)
            {
                ViewBag.Message = "Role added successfully";
                return View("Index", await GetPageData());
            }
            else {
                ModelState.AddModelError("CustomError", response.BOSErrors[0].Message);
                return View("AddRole", role);
            }
        }

        public async Task<IActionResult> EditRole(string roleId)
        {
            try
            {
                Role role = new Role();
                var roleInfo = await _bosAuthClient.GetRoleByIdAsync<Role>(Guid.Parse(roleId));
                if (roleInfo.IsSuccessStatusCode)
                {
                    role = roleInfo.Role;
                }
                return View("EditRole", role);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<IActionResult> UpdateRole(Role role)
        {
            try
            {
                var response = await _bosAuthClient.UpdateRoleAsync<Role>(role.Id, role);
                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "Role updated successfully";
                    return View("Index", await GetPageData());
                }
                else
                {
                    ModelState.AddModelError("CustomError", response.BOSErrors[0].Message);
                    return View("EditUser", role);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        public async Task<string> UpdateUserRoles([FromBody]List<Role> updatedRoles)
        {
            try
            {
                if (updatedRoles.Count > 0)
                {
                    Guid userId = Guid.Parse(User.FindFirst(c => c.Type == "UserId").Value.ToString());

                    var response = await _bosAuthClient.AssociateUserToMultipleRolesAsync(userId, updatedRoles);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Something went wrong while updating the roles. Please try again later");
                    }
                    else
                    {
                        return "User roles updates successfully";
                    }
                }
                else
                {
                    return "Roles to associate with the user cannot be empty";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [HttpPost]
        public async Task<string> UpdateUserRolesByAdmin([FromBody]JObject data)
        {
            try
            {
                List<Role> updatedRoles = data["UpdatedRoles"].ToObject<List<Role>>();
                var updatedUserId = data["UserId"].ToString();
                StringConversion stringConversion = new StringConversion();
                Guid userId = Guid.Parse(stringConversion.DecryptString(updatedUserId));
                if (updatedRoles.Count > 0)
                {
                    var response = await _bosAuthClient.AssociateUserToMultipleRolesAsync(userId, updatedRoles);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Something went wrong while updating the roles. Please try again later");
                    }
                    else
                    {
                        return "User's roles updates successfully";
                    }
                }
                else
                {
                    return "Roles to associate with the user cannot be empty";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [HttpPost]
        public async Task<string> DeleteRole([FromBody]string roleId)
        {
            var response = await _bosAuthClient.DeleteRoleAsync(Guid.Parse(roleId));
            if (response.IsSuccessStatusCode)
            {
                return "Role deleted successfully";

            }
            else
            {
                throw new Exception(response.BOSErrors[0].Message);
            }
        }

        public ActionResult RoleManagePermissions(string roleId, string roleName)
        {
            return RedirectToAction("FetchPermissions", "Permissions", new { roleId, roleName });
        }

        private async Task<dynamic> GetPageData()
        {
            var modules = HttpContext.Session.GetObject<List<Module>>("Modules");
            dynamic model = new ExpandoObject();
            model.Modules = modules;
            if (User.FindFirst(c => c.Type == "Username") != null || User.FindFirst(c => c.Type == "Role") != null)
            {
                model.Username = User.FindFirst(c => c.Type == "Username").Value.ToString();
                model.Roles = User.FindFirst(c => c.Type == "Role").Value.ToString();
            }

            var response = await _bosAuthClient.GetRolesAsync<Role>();
            if (response.IsSuccessStatusCode)
            {
                model.RoleList = response.Roles;
            }
            return model;
        }

        #region CommentedCode - With AddNewRole, RevokeRole, AssociateUserToMultipleRolesAsync
        //public async Task<string> UpdateUserRoles([FromBody]List<Role> updatedRoles)
        //{
        //    try
        //    {
        //        //Role r = new Role();
        //        //r.Id = Guid.Parse("0cf80c38-8735-4ef3-b971-f37fc4c52218");
        //        //r.Name = "Super Admin";
        //        //updatedRoles.Add(r);

        //        if (updatedRoles.Count > 0)
        //        {
        //            var currentUserRoles = User.FindFirst(c => c.Type == "Role").Value.ToString();
        //            var currentUserRolesArray = currentUserRoles.Split(",");

        //            List<Role> newRoles = new List<Role>();
        //            List<Guid> revokedRoles = new List<Guid>();

        //            Guid userId = Guid.Parse(User.FindFirst(c => c.Type == "UserId").Value.ToString());

        //            foreach (Role newRole in updatedRoles)
        //            {
        //                if (!currentUserRolesArray.Contains(newRole.Name))
        //                {
        //                    newRoles.Add(newRole);
        //                }
        //            }

        //            List<Role> remainingRoles = updatedRoles.Except(newRoles).ToList();

        //            foreach (string revokedRole in currentUserRolesArray)
        //            {
        //                foreach (Role role in remainingRoles)
        //                {
        //                    if (!role.Name.Equals(revokedRole))
        //                    {
        //                        revokedRoles.Add(Guid.Parse(role.Id.ToString()));
        //                    }
        //                }
        //            }

        //            var response = await _bosAuthClient.AssociateUserToMultipleRolesAsync(userId, updatedRoles);
        //            if (!response.IsSuccessStatusCode)
        //            {
        //                throw new Exception("Something went wrong while updating the roles. Please try again later");
        //            }
        //            else
        //            {
        //                if (revokedRoles.Count > 0)
        //                {
        //                    foreach (Guid roleId in revokedRoles)
        //                    {
        //                        var revokeResponse = await _bosAuthClient.RevokeRoleAsync(roleId, userId);
        //                        if (!revokeResponse.IsSuccessStatusCode)
        //                        {
        //                            throw new Exception("Something went wrong while updating the roles. Please try again later");
        //                        }
        //                    }
        //                }
        //                return "User roles updates successfully";
        //            }
        //        }
        //        else
        //        {
        //            return "Roles to associate with the user cannot be empty";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }

        //}
        #endregion
    }
}