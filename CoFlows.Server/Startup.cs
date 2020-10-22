/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;

using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;

// Lets Encrypt
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using FluffySpoon.AspNet.LetsEncrypt;

using CoFlows.Server.Realtime;

namespace CoFlows.Server
{
    public class Certificate
    {
        public string Key { get; set; }
        public string Data { get; set; }
    }

    public class Challenge
    {
        public string Token { get; set; }
        public string Response { get; set; }
        public string Domains { get; set; }
    }

    public class Startup<T>
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
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

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen();
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "CoFlows Community Edition",
                    Description = "CoFlows CE (Community Edition) is a Containerized Polyglot Runtime that simplifies the development, hosting and deployment of powerful data-centric workflows. CoFlows enables developers to create rich Web-APIs with almost zero boiler plate and scheduled / reactive processes through a range of languages including CoreCLR (C#, F# and VB), JVM (Java and Scala), Python and Javascript. Furthermore, functions written in any of these languages can call each other within the same process with full interop.",
                    TermsOfService = new Uri("https://github.com/CoFlows/CoFlows-CE#license"),
                    Contact = new OpenApiContact
                    {
                        Name = "CoFlows Community",
                        Email = "arturo@coflows.com",
                        Url = new Uri("https://www.coflows.com"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under MIT",
                        Url = new Uri("https://github.com/CoFlows/CoFlows-CE#license"),
                    }
                });

                var filePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "CoFlows.Server.lnx.xml");
                c.IncludeXmlComments(filePath);
            });

            services
                .AddMvc(option => option.EnableEndpointRouting = false)
                .AddNewtonsoftJson(options => {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = 
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            if(Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null)
            {
                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = "coflows-ce",
                            ValidAudience = "coflows-ce",
                            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Program.jwtKey))
                        };
                    })
                    .AddAzureADB2CBearer(options => {
                        options.Instance = Program.config["Server"]["OAuth"]["AzureAdB2C"]["Instance"].ToString();
                        options.ClientId = Program.config["Server"]["OAuth"]["AzureAdB2C"]["ClientId"].ToString();
                        options.Domain = Program.config["Server"]["OAuth"]["AzureAdB2C"]["Domain"].ToString();
                        options.SignUpSignInPolicyId = Program.config["Server"]["OAuth"]["AzureAdB2C"]["SignUpSignInPolicyId"].ToString();
                    });

                services.AddAuthorization(options =>
                {
                    var defaultAuthorizationPolicyBuilder = 
                        new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                            JwtBearerDefaults.AuthenticationScheme,
                            AzureADB2CDefaults.BearerAuthenticationScheme)
                        .RequireAuthenticatedUser();

                    options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
                });
            }
            else
            {
                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = "coflows-ce",
                            ValidAudience = "coflows-ce",
                            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Program.jwtKey))
                        };
                    });
            }


            

            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<RTDSocketManager>();

            if(Program.hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(Program.letsEncryptEmail))
            {
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

                services.AddFluffySpoonLetsEncryptCertificatePersistence(
                    async (key, bytes) => {
                        var mKey = "---LetsEncrypt--" + Program.hostName + "." + Program.letsEncryptEmail + "." + (Program.letsEncryptStaging ? "Staging" : "Production") + ".certificate_" + key;
                        var m = QuantApp.Kernel.M.Base(mKey);

                        var resList = m[x => QuantApp.Kernel.M.V<string>(x, "Key") == key.ToString()];
                        if(resList != null && resList.Count > 0)
                        {
                            var strData = System.Convert.ToBase64String(bytes);
                            m.Exchange(resList[0], new Certificate(){ Key = key.ToString(), Data = strData });

                            Console.WriteLine("LetsEncrypt certificate UPDATED...");
                        }
                        else
                        {
                            var strData = System.Convert.ToBase64String(bytes);
                            m += new Certificate(){ Key = key.ToString(), Data = strData };
                            Console.WriteLine("LetsEncrypt certificate CREATED...");
                        }
                        m.Save();
                    },
                    async (key) => {
                        var mKey = "---LetsEncrypt--" + Program.hostName + "." + Program.letsEncryptEmail + "." + (Program.letsEncryptStaging ? "Staging" : "Production") + ".certificate_" + key;
                        
                        try
                        {
                            var m = QuantApp.Kernel.M.Base(mKey);
                            var resList = m[x => QuantApp.Kernel.M.V<string>(x, "Key") == key.ToString()];
                            if(resList != null && resList.Count > 0)
                            {
                                var data = QuantApp.Kernel.M.V<string>(resList[0], "Data");
                                var bytes = System.Convert.FromBase64String(data);
                                Console.WriteLine("LetsEncrypt found certificate...");
                                return bytes;
                            }

                            Console.WriteLine("LetsEncrypt didn't find a certificate, attempting to create one...");

                            return null;
                        }
                        catch (System.Exception e)
                        {
                            return null;
                        }
                        
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
                app.UseFluffySpoonLetsEncrypt();
                if(!Program.letsEncryptStaging)
                    app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseForwardedHeaders();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoFlows API V1");
            });

            app.UseStatusCodePagesWithReExecute("/");
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                );

            app.UseWebSockets();
            app.UseMiddleware<T>();
            
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
