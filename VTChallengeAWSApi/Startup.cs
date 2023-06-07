using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NugetVTChallenge.Interfaces;
using Swashbuckle.AspNetCore.SwaggerUI;
using VTChallengeAWSApi.Data;
using VTChallengeAWSApi.Helpers;
using VTChallengeAWSApi.Repositories;
using VTChallengeAWSApi.Services;

namespace VTChallengeAWSApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HelperOAuthToken>();
        HelperOAuthToken helper = new HelperOAuthToken(Configuration);
        services.AddAuthentication(helper.GetAuthenticationOptions()).AddJwtBearer(helper.GetJwtOptions());


        // Add services to the container.
        string connectionString = Configuration.GetConnectionString("MySqlAWS");

        services.AddSingleton<HelperCryptography>();
        services.AddTransient<HelperUserToken>();
        services.AddHttpContextAccessor();
        services.AddTransient<HttpClient>();
        services.AddTransient<IVtChallenge, RepositoryVtChallenge>();
        services.AddTransient<IServiceValorant, ServiceValorant>();
        services.AddDbContext<VTChallengeContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        services.AddCors(options =>
        {
            options.AddPolicy("AllowOrigin", x => x.AllowAnyOrigin());
        });
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "1.0",
                Title = "API VTChallenge",
                Description = "API VTChallenge is an API designed for the VTChallenge."
                // Add any additional configuration options or customizations here
            });

            // Configure the security scheme for JWT
            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = ParameterLocation.Header,
                Description = "Copy and paste the JWT token in the 'Value' field as follows: Bearer {Token JWT}."
            });

            // Add the security requirement for JWT
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "JWT"
                        }
                    },
                    new string[] {}
                }
            });
        });
        services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseCors(options => options.AllowAnyOrigin());
        app.UseSwagger();
        app.UseSwaggerUI(options => {
            options.SwaggerEndpoint(url: "swagger/v1/swagger.json", name: "API VTCHALLENGE V1");
            options.RoutePrefix = "";
            options.DocExpansion(DocExpansion.None);
        });


        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}