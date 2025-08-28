using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using FarewellMyBeloved.Services;

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "slug",
    pattern: "{slug:minlength(1)}",
    defaults: new { controller = "Home", action = "Slug" }
);

app.Run();
