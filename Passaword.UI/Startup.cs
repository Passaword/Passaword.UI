using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Passaword.Configuration;
using Passaword.Storage.Sql;

namespace Passaword.UI
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
            if (Configuration.GetSection("Passaword:UseSsl").Get<bool>())
            {
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                });
            }

            services.AddPassaword()
                .AddSqlSecretStore()
                .AddEmailMessaging(options =>
                {
                    options.SendOwnerEmailOnDecrypt = false;
                })
                .AddUserEmailValidation()
                .AddExpiryValidation()
                .AddPassphraseValidation()
                .AddUserIpValidation();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "Google";
                    options.DefaultSignInScheme = "Google";
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                })
                .AddGoogle("Google", options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.ClientId = Configuration["Passaword:Google:ClientId"];
                    options.ClientSecret = Configuration["Passaword:Google:ClientSecret"];
                    options.Scope.Add("email");
                    options.SaveTokens = true;
                });
            
            services.AddMvc();

            InitializeDatabase(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            if (Configuration.GetSection("Passaword:UseSsl").Get<bool>())
            {
                var options = new RewriteOptions()
                    .AddRedirectToHttps();

                app.UseRewriter(options);
            }

            
            app.UseStaticFiles();
            app.UseAuthentication();

            app.Map("/logout", x =>
            {
                x.Run(async (ctx) =>
                {
                    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    ctx.Response.Redirect("/");
                });
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void InitializeDatabase(IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            using (var db = sp.GetService<PassawordDbContext>())
            {
                db.Database.Migrate();
            }
        }
    }
}
