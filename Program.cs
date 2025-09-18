using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using FarewellMyBeloved.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add S3 Service
var awsConfig = builder.Configuration.GetSection("S3");
var accessKeyId = awsConfig["AccessKey"];
var secretAccessKey = awsConfig["SecretKey"];
var endpoint = awsConfig["Endpoint"]; // e.g., "s3.us-west-002.backblazeb2.com"

// Create a custom endpoint for Backblaze B2
var config = new AmazonS3Config
{
    ServiceURL = endpoint,
    ForcePathStyle = true // Required for Backblaze B2
};

// Initialize the S3 client with Backblaze B2 credentials
var s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);
builder.Services.AddSingleton<IAmazonS3>(s3Client);
builder.Services.AddScoped<IS3Service, S3Service>();

// Add Entity Framework services
builder.Services.AddDbContext<FarewellMyBeloved.Models.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth: cookie for local session, GitHub for challenges
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GitHubAuthenticationDefaults.AuthenticationScheme; // "GitHub"
    })
    .AddCookie()
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        options.SaveTokens = true;

        // Ask GitHub for email too (optional but handy)
        options.Scope.Add("user:email");

        // Ensure email claim is available if GitHub returns it
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", "string");
        // "login" (GitHub username) is mapped by the provider to "urn:github:login"
        // which we'll use for the admin check. :contentReference[oaicite:2]{index=2}
    });

// Only let a specific GitHub username in as "admin"
// Load allowed admin emails once from configuration at startup
var allowedEmails = builder.Configuration
    .GetSection("Admin:Emails")
    .Get<string[]>() ?? Array.Empty<string>();

// Add authorization policy that allows only users with emails in allowedEmails
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminsOnly", policy =>
        policy.RequireAssertion(ctx =>
        {
            var email = ctx.User.FindFirst(ClaimTypes.Email)?.Value;

            return !string.IsNullOrEmpty(email) &&
                allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
        }));
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();

// Middleware to auto-logout authenticated users whose email is not in allowedEmails
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email) || !allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            // Immediately sign them out
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/not-authorized");
            return;
        }
    }

    await next();
});

app.UseAuthorization();

// use app.Environment here
app.UseStatusCodePages(async context =>
{
    var env = app.Environment; // <- get IWebHostEnvironment
    var res = context.HttpContext.Response;
    if (res.StatusCode == 404)
    {
        res.ContentType = "text/html";
        await context.HttpContext.Response.SendFileAsync(Path.Combine(env.WebRootPath, "404.html"));
    }
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.MapGet("/not-authorized", () =>
{
    return Results.Text("You are not authorized to access this page.", statusCode: 403);
});

app.MapControllerRoute(
    name: "slug",
    pattern: "{slug:minlength(1)}",
    defaults: new { controller = "Home", action = "Slug" }
);



app.Run();
