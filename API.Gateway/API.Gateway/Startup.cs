using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.RequestId.Middleware;
using Ocelot.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Ocelot.Provider.Polly;
using Ocelot.Cache.CacheManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Gateway.Services.Services;
using IdGen;
using System.Threading;

namespace API.Gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var key = "ThisIsTheVaApiGatewayKeyToGenerateBearerTokens";
            services.AddSingleton<IJwtAuthenticationManager>(new JwtAuthenticationManager(key));

            // Swagger Service
            services.AddSwaggerGen();

            // Authentication Service
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            // Ocelot Service
            services.AddOcelot()
                    .AddPolly()
                    .AddCacheManager(settings => settings.WithDictionaryHandle());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Use Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Use Authentication
            app.UseAuthentication();

            // Use Authorization
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var configuration = new OcelotPipelineConfiguration
            {
                PreErrorResponderMiddleware = async (ctx, next) =>
                {
                    Console.WriteLine("\n\nPreQueryStringBuilderMiddleware\n\n");
                    var downstreamRoute = ctx.Items.DownstreamRoute();

                    //var header = ctx.Request.Headers;
                    //foreach (var element in header)
                    //{
                    //    if (element.Key == "RequestId")
                    //    {
                    //        var generator = new IdGenerator(0);
                    //        string key = "RequestId";
                    //        string id = generator.CreateId().ToString();
                    //        header.Remove(element.Key);
                    //        header.Add(key, id);
                    //        break;
                    //    }
                    //    Console.WriteLine("{0} and {1}", element.Key, element.Value);
                    //}
                    var generator = new IdGenerator(0);
                    string id = generator.CreateId().ToString();

                    ctx.Request.Headers.Remove("requestId");
                    ctx.Request.Headers.Add("requestId", id);

                    foreach (var element in ctx.Request.Headers)
                    {
                        Console.WriteLine("{0} : {1}", element.Key, element.Value);
                    }
                    //var body = ctx.Items;
                    //foreach (var element in body)
                    //{
                    //    Console.WriteLine("{0} and {1}", element.Key, element.Value);
                    //}

                    //for (int i = 0; i >= keys.Count; i++)
                    //{
                    //    Console.WriteLine(keys);
                    //}
                    Console.Write("\n\n");
                    await next.Invoke();
                }
            };

            // Use Ocelot
            app.UseOcelot(configuration).Wait();
        }
    }
}