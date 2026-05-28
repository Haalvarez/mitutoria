using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using Resend;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddFolderApplicationModelConvention("/", model =>
        model.Filters.Add(new Microsoft.AspNetCore.Mvc.ServiceFilterAttribute(typeof(miTutoria.Web.Infrastructure.VersionPageFilter))));
});
builder.Services.AddScoped<miTutoria.Web.Infrastructure.VersionPageFilter>();
builder.Services.AddSingleton(sp =>
    Environment.GetEnvironmentVariable("RAILWAY_GIT_COMMIT_SHA") is { Length: >= 7 } hash
        ? hash[..7]
        : "dev");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection no configurada")));
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["RESEND_API_KEY"]
        ?? throw new InvalidOperationException("RESEND_API_KEY no configurada");
});
var resendFrom = builder.Configuration["RESEND_FROM"]
    ?? "noreply@mitutoria.app";
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

app.Run();
