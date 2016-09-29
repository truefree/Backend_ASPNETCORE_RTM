using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Backend_ASPNETCORE_RTM.Models;
using Backend_ASPNETCORE_RTM.Models.SpeakerRecognition;
using Microsoft.EntityFrameworkCore;

namespace Backend_ASPNETCORE_RTM
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                // truefree 20160519
                // custom app setting... ASP.NET Core Options Model임
                // setting file과 동일한 이름의 class를 별도로 선언한다
                .AddJsonFile("WebServiceSettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // truefree 20160519
            // Options Model을 이용하기 위한 처리
            // ASP.NET Core 1.0 RC2 기준, Microsoft.Extension.Options.ConfigurationExtension 1.0.0-rc2-final 패키지 설치 필요
            // Microsoft.EntityFramework.Sqlite와 충돌난다 ㅠㅠ
            // EntityFrameworkCore로 rename 됨... -ㅋ-
            services.AddOptions();
            // Configure method가 IConfiguration 객체에서 class와 동일한 이름의 setting을 mapping 시켜줌
            services.Configure<Models.SpeakerRecognition.WebServiceSettings>(Configuration);

            // Add framework services.
            services.AddMvc();

            // DbContext Injection
            services.AddDbContext<UserDBContext>(options => options.UseSqlite("filename=./WebAPIDB.sqlite"));

            // 서비스 모듈 Dependency Injection........뭐 이리 남발하냐!
            // 아무튼 UserRepository 객체를 Controller에서 사용하기 위한 처리임
            // IUserRepository로 Model-Controller loose coupling
            //services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISpeakerVerificationServiceClient, SpeakerVerificationServiceClient>();
            services.AddSingleton<ITOTPHelper, TOTPHelper>();

            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            app.UseStaticFiles();
            app.UseMvc();
            
        }
    }
}
