/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc.NewtonsoftJson;

using Microsoft.Extensions.DependencyInjection;


using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

// Lets Encrypt
using Microsoft.Extensions.DependencyInjection;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using FluffySpoon.AspNet.LetsEncrypt;

using QuantApp.Server.Realtime;

namespace QuantApp.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
                {
                    options.AddPolicy("AllowAllOrigins",
                        builder =>
                        {
                            builder.AllowAnyOrigin();
                        });
                });

            services
                .AddMvc(option => option.EnableEndpointRouting = false)
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver =
                        new Newtonsoft.Json.Serialization.DefaultContractResolver());

            services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "quant.app",
                    ValidAudience = "quant.app",
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("___Secret-QuantApp-Capital!1234"))
                };
            });

            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<RTDSocketManager>();

            if(Program.hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(Program.letsEncryptEmail))
            {
                // services.AddLetsEncrypt(o =>
                //     {
                //         o.DomainNames = new[] { Program.hostName };
                //         o.UseStagingServer = Program.letsEncryptStaging; // <--- use staging

                //         o.AcceptTermsOfService = true;
                        
                //         o.EmailAddress = Program.letsEncryptEmail;
                //     });
                services.AddFluffySpoonLetsEncrypt(new LetsEncryptOptions()
                {
                    Email = Program.letsEncryptEmail,
                    UseStaging = Program.letsEncryptStaging,
                    Domains = new[] { Program.hostName },
                    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
                    CertificateSigningRequest = new CsrInfo()
                    {
                        CountryName = "Multiverse",
                        Locality = "Universe",
                        Organization = "GetStuffDone",
                        OrganizationUnit = "ImportantStuffDone",
                        State = "MilkyWay"
                    }
                });

                // services.AddFluffySpoonLetsEncryptFileCertificatePersistence();
                services.AddFluffySpoonLetsEncryptCertificatePersistence(
                    async (key, bytes) => {
                        File.WriteAllBytes(Program.hostName + "certificate_" + key, bytes)
                    },
                    async (key) => {
                        File.ReadAllBytes(Program.hostName + "certificate_" + key, bytes)
                    });
                services.AddFluffySpoonLetsEncryptFileChallengePersistence();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        public void Configure(IApplicationBuilder app,  IWebHostEnvironment env)
        {
            app.Use(async (httpContext, next) =>
            {
                httpContext.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.CacheControl] = "no-cache";
                await next();
            });

            if(Program.hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(Program.letsEncryptEmail))
            {
                if(!Program.letsEncryptStaging)
                    app.UseHsts();
                app.UseHttpsRedirection();
                app.UseFluffySpoonLetsEncrypt();
            }

            app.UseStatusCodePagesWithReExecute("/");
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                );

            app.UseWebSockets();
            app.UseMiddleware<RTDSocketMiddleware>();


            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
