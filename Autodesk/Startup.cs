using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autodesk.Aps.Models;

namespace Autodesk.Aps
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddNewtonsoftJson(options =>
            {
                //options.SerializerSettings.Converters.Add(new EmptyStringToNullJsonConverter());
                options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                options.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ssZ";
            });

            var clientID = Configuration["APS_CLIENT_ID"];
            var clientSecret = Configuration["APS_CLIENT_SECRET"];
            var callbackURL = Configuration["APS_CALLBACK_URL"];
            if (string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(callbackURL))
            {
                throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, or APS_CALLBACK_URL.");
            }
            services.AddSingleton<APS>(new APS(clientID, clientSecret, callbackURL));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}