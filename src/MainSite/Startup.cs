#region Usings

using System;
using MainSite.Data;
using MainSite.Models;
using MainSite.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#endregion

namespace MainSite
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

            if (env.IsDevelopment())
                builder.AddUserSecrets();

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        private IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession();
            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            //Add Authorization Services
            services.AddAuthorization(
                options => { options.AddPolicy("Admin", policy => policy.RequireClaim("admin", "yes")); });

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration);

            // Configure Identity
            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 10;

                // Cookie settings
                options.Cookies.ApplicationCookie.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.Cookies.ApplicationCookie.LoginPath = "/Account/LogIn";
                options.Cookies.ApplicationCookie.LogoutPath = "/Account/LogOff";

                // User settings
                options.User.RequireUniqueEmail = true;
            });
            services.Configure<MvcOptions>(options => { options.Filters.Add(new RequireHttpsAttribute()); });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.Use(async (context, next) =>
            {
                if (context.Request.IsHttps)
                {
                    await next();
                }
                else
                {
                    var withHttps = "https://" + context.Request.Host + context.Request.Path;
                    context.Response.Redirect(withHttps);
                }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }

            app.UseStaticFiles();

            app.UseIdentity();

            app.UseSession();
            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute("Credentials", "{controller=Machine}/{action=DeleteCredentials}/{cid?}");
                routes.MapRoute("companysearch", "{controller=Companies}/{Action=Index}/{searchstring?}");
            });

        }
    }
}