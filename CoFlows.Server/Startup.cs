/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;

using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;

// Lets Encrypt
using Certes;
using FluffySpoon.AspNet.LetsEncrypt;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using System.Security.Cryptography.X509Certificates;


// Identity Server 4
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Services;

using CoFlows.Server.IdentityServer;
using CoFlows.Server.Realtime;

using NLog;

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
            var logger = LogManager.GetCurrentClassLogger();
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
            // else
            // {
            //     services
            //         .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //         .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            //         {
            //             options.TokenValidationParameters = new TokenValidationParameters
            //             {
            //                 ValidateIssuer = true,
            //                 ValidateAudience = true,
            //                 ValidateLifetime = true,
            //                 ValidateIssuerSigningKey = true,
            //                 ValidIssuer = "coflows-ce",
            //                 ValidAudience = "coflows-ce",
            //                 IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Program.jwtKey))
            //             };
            //         });
            // }

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

                            logger.Info("LetsEncrypt certificate UPDATED...");
                        }
                        else
                        {
                            var strData = System.Convert.ToBase64String(bytes);
                            m += new Certificate(){ Key = key.ToString(), Data = strData };
                            logger.Info("LetsEncrypt certificate CREATED...");
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
                                logger.Info("LetsEncrypt found certificate...");
                                return bytes;
                            }

                            logger.Info("LetsEncrypt didn't find a certificate, attempting to create one...");

                            return null;
                        }
                        catch (System.Exception e)
                        {
                            return null;
                        }
                        
                    });
                services.AddFluffySpoonLetsEncryptFileChallengePersistence();
            }

            SetIdentityServer4(services, GetCertificate());
        }

        private ECDsaSecurityKey GetCertificate(int count = 0)
        {
            var logger = LogManager.GetCurrentClassLogger();

            var sslFlag = CoFlows.Server.Program.hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(CoFlows.Server.Program.letsEncryptEmail);
            
            if(!sslFlag && Program.hostName.ToLower() != "localhost")
                return null;

            var throwExction = false;
            try
            {
                var mKey = "---LetsEncrypt--" + Program.hostName + "." + Program.letsEncryptEmail + "." + (Program.letsEncryptStaging ? "Staging" : "Production") + ".certificate_" + CertificateType.Site;
                
                var m = QuantApp.Kernel.M.Base(mKey);

                var resList = m[x => QuantApp.Kernel.M.V<string>(x, "Key") == CertificateType.Site.ToString()];
                if(resList != null && resList.Count > 0)
                {
                    var data = QuantApp.Kernel.M.V<string>(resList[0], "Data");
                    var bytes = System.Convert.FromBase64String(data);
                    X509Certificate2 x509 = new X509Certificate2(bytes, nameof(FluffySpoon));

                    var mess = "\n------------------------------------\n";
                    mess += "Subject: " + x509.Subject + "\n";
                    mess += "Issuer: " + x509.Issuer + "\n";
                    mess += "Version: " + x509.Version + "\n";
                    mess += "Valid Date: " + x509.NotBefore + "\n";
                    mess += "Expiry Date: " + x509.NotAfter + "\n";
                    mess += "Thumbprint: " + x509.Thumbprint + "\n";
                    mess += "Serial Number: " + x509.SerialNumber + "\n";
                    mess += "Friendly Name: " + x509.PublicKey.Oid.FriendlyName + "\n";
                    mess += "Public Key Format: " + x509.PublicKey.EncodedKeyValue.Format(true) + "\n";
                    mess += "Raw Data Length: " + x509.RawData.Length + "\n";
                    mess += "Certificate to string: " + x509.ToString(true) + "\n";
                    mess += "------------------------------------";
                    logger.Debug(mess);
                    
                    var ecdsa = x509.GetECDsaPrivateKey();
                    var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex) };

                    return securityKey;
                }
                else
                {
                    throwExction = true;
                }

                return null;
            }
            catch(Exception e)
            {
                logger.Error(e);
                
                if(throwExction)
                {
                    // Wait 30sec before throwing an exception to restart the server.
                    // This should leave enough time for LetsEncrypt to issue a new certificate and save.
                    System.Threading.Thread.Sleep(1000 * 30);
                    
                    if(count < 2)
                        return GetCertificate(count++);
                    
                }
                return null;
            }
        }

        private void SetIdentityServer4(IServiceCollection services, ECDsaSecurityKey certificate)
        {
            IEnumerable<ApiResource> Apis =
                new List<ApiResource>
                {
                    new ApiResource("resourceapi", "Resource API")
                    {
                        Scopes = {new Scope("api.read")}
                    }
                };

            IEnumerable<Client> Clients =
                new List<Client>
                {
                    new Client {
                        ClientId = "jwt_token",
                        ClientName = "JWT Token",
                        AccessTokenLifetime = 60 * 60 * 24,
                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                        RequireClientSecret = false,
                        AllowAccessTokensViaBrowser = true,
                        AllowedScopes = new List<string> { "resourceapi" }
                    }
                };

            if(certificate == null)
                services.AddIdentityServer()
                    .AddDeveloperSigningCredential()
                    .AddInMemoryApiResources(Apis)
                    .AddInMemoryClients(Clients)
                    .AddCustomUserStore();
            else
            {
                services.AddIdentityServer()
                    .AddSigningCredential(certificate, "ES256")
                    .AddInMemoryApiResources(Apis)
                    .AddInMemoryClients(Clients)
                    .AddCustomUserStore();
            }

            var sslFlag = CoFlows.Server.Program.hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(CoFlows.Server.Program.letsEncryptEmail);
            var hostName = (sslFlag ? "https://" : "http://") + CoFlows.Server.Program.hostName.ToLower();

            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddIdentityServerAuthentication(
                options =>
                {
                    options.Authority = hostName;
                    options.ApiName = "resourceapi";
                    options.RequireHttpsMetadata = false;
                });
            services.AddSingleton<ICorsPolicyService>((container) => {
                var logger = container.GetRequiredService<ILogger<DefaultCorsPolicyService>>();
                return new DefaultCorsPolicyService(logger) {
                    AllowAll = true
                };
            });
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

            // Identity Server 4
            app.UseIdentityServer();
            
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
