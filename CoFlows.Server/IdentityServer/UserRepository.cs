/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace CoFlows.Server.IdentityServer
{
    public class UserRepository : IUserRepository
    {
        public bool ValidateCredentials(string username, string password)
        {
            var user = FindByUsername(username);

            if (user != null)
            {
                var quser = QuantApp.Kernel.User.FindUser(user.SubjectId);

                if(quser != null && username.ToLower() == user.Email.ToLower())
                    return quser.VerifyPassword(password);
                else if(quser != null)
                    return true;
            }

            return false;
        }

        public Task<CustomUser> FindBySubjectId(string subjectId)
        {
            var quser = QuantApp.Kernel.User.FindUser(subjectId);
            if(quser == null)
                quser = QuantApp.Kernel.User.FindUserBySecret(subjectId);
            return Task.FromResult(new CustomUser{ SubjectId = quser.ID, Email = quser.Email, FirstName = quser.FirstName, LastName = quser.LastName });
        }

        public CustomUser FindByUsername(string username)
        {
            var id = "QuantAppSecure_" + username.ToLower().Replace('@', '.').Replace(':', '.');
            var quser = QuantApp.Kernel.User.FindUser(id);
            if(quser == null)
                quser = QuantApp.Kernel.User.FindUserBySecret(username);
            if(quser == null)
                return null;

            return new CustomUser{ SubjectId = quser.ID, Email = quser.Email, FirstName = quser.FirstName, LastName = quser.LastName };
        }
    }
}