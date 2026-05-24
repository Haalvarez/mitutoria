var builder = WebApplication.CreateBuilder(args);

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

builder.WebHost.UseContentRoot(AppContext.BaseDirectory);

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.MapRazorPages();

app.Run();
